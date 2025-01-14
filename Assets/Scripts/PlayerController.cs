using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 4.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 6.0f;
	[Tooltip("Crouch speed of the character in m/s")]
	public float CrouchSpeed = 2.0f;
	[Tooltip("Slide speed of the character in m/s")]
	public float SlideSpeed = 6.0f;
	[Tooltip("InAir speed of the character in m/s")]
	public float InAirSpeed = 6.0f;
	[Tooltip("Rotation speed of the character")]
	public float RotationSpeed = 1.0f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;
	[Tooltip("Acceleration and deceleration")]
	public float InAirChangeRate = 0.4f;
	[Tooltip("Acceleration and deceleration while sliding")]
	public float SlideSpeedChangeRate = 10.0f;

	[Space(10)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;
	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15.0f;

	[Space(10)]
	[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
	public float JumpTimeout = 0.1f;
	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;
	[Tooltip("Slide Time in s")]
	public float SlideTimeout = 1f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	public bool Grounded = true;
	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = -0.14f;
	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.5f;
	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Cinemachine")]
	[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
	public GameObject CinemachineCameraTarget;
	[Tooltip("How far in degrees can you move the camera up")]
	public float TopClamp = 90.0f;
	[Tooltip("How far in degrees can you move the camera down")]
	public float BottomClamp = -90.0f;

	public Cinemachine.CinemachineVirtualCamera followPlayerCamera;
	public Animator _animator;

	public enum PlayerState { Grounded, InAir }

	public PlayerState currentState;

	[SerializeField] private bool airControl = false;

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private float _speed;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private float _terminalVelocity = 53.0f;
	private Vector3 _movement;

	//playerActions
	public bool isJumping = false;
	public bool isCrouching = false;
	public bool isSprinting = false;
	public bool isSliding = false;

	private bool isInteracting;

	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;
	private float _slideTimeoutDelta;

	//Animation
	private float velocityX = 0f;
	private float velocityY = 0f;

	private CharacterController _controller;
	private PlayerInputs _playerInput;
	private GameObject _mainCamera;

	private Vector3 currentMovementDirection;
	private Vector3 lastMovementDirection;

	private const float _threshold = 0.01f;

	public bool IsJumping { get => isJumping; set => isJumping = value; }
	public bool IsCrouching { get => isCrouching; set => isCrouching = value; }
	public bool IsSprinting { get => isSprinting; set => isSprinting = value; }
	public bool IsSliding { get => isSliding; set => isSliding = value; }

	private void Awake()
	{

		// get a reference to our main camera
		if (_mainCamera == null)
		{
			_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}

	}

	private void Start()
	{
		// reset our timeouts on start
		_jumpTimeoutDelta = JumpTimeout;
		_fallTimeoutDelta = FallTimeout;
		_slideTimeoutDelta = SlideTimeout;

		_controller = GetComponent<CharacterController>();
		followPlayerCamera = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
		followPlayerCamera.Follow = CinemachineCameraTarget.transform;
		_playerInput = GetComponent<PlayerInputs>();

		if (_animator == null)
			_animator = GetComponent<Animator>();

	}

	private void Update()
	{
		switch (currentState)
		{
			case PlayerState.Grounded:

				JumpAndGravity();

				//Check for ground
				GroundedCheck();

				if (!Grounded)
				{
					currentState = PlayerState.InAir;
					isSliding = false;
					break;
				}

				Move();
				break;

			case PlayerState.InAir:

				JumpAndGravity();

				GroundedCheck();

				if (Grounded)
				{
					currentState = PlayerState.Grounded;
					break;
				}

				if (airControl)
				{
					//MoveInAir();
				}
				else
				{
					//We just keep the momentum for now (no air control)
					_controller.Move(currentMovementDirection * _speed * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
				}

				break;
		}

		#region Movement Animation

		if (_animator == null)
			return;

		float targetVelocity = isSprinting ? 1f : 0.5f;
		if (_playerInput.move.magnitude != 0)
		{
			if (_playerInput.move.x > 0f)
			{
				velocityX = targetVelocity;
			}
			else if (_playerInput.move.x < 0f)
			{
				velocityX = -targetVelocity;
			}
			else
			{
				velocityX = 0f;
			}

			if (_playerInput.move.y > 0f)
			{
				velocityY = targetVelocity;
			}
			else if (_playerInput.move.y < 0f)
			{
				velocityY = -targetVelocity;
			}
			else
			{
				velocityY = 0f;
			}
		}
		else
		{
			velocityY = 0f;
			velocityX = 0f;
		}


		_animator.SetFloat("Horizontal", velocityX, 0.1f, Time.deltaTime);
		_animator.SetFloat("Vertical", velocityY, 0.1f, Time.deltaTime);
		_animator.SetBool("IsJumping", isJumping);
		_animator.SetBool("IsSliding", isSliding);

		#endregion
	}

	private void LateUpdate()
	{

		CameraRotation();

		switch (currentState)
		{
			case PlayerState.Grounded:

				if (Mathf.Abs(_controller.velocity.magnitude) > MoveSpeed)
					CameraWobble(0.05f, 0.5f);
				else if (Mathf.Abs(_controller.velocity.magnitude) < MoveSpeed && Mathf.Abs(_controller.velocity.magnitude) > 0f)
					CameraWobble(0.05f, 0.2f);
				else
					CameraWobble(0f, 0f);

				break;
		}

	}

	private void GroundedCheck()
	{
		// set sphere position, with offset
		Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
		Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
	}

	private void CameraRotation()
	{
		// if there is an input
		if (_playerInput.look.sqrMagnitude >= _threshold)
		{
			//Don't multiply mouse input by Time.deltaTime
			float deltaTimeMultiplier = 1.0f;

			_cinemachineTargetPitch += _playerInput.look.y * RotationSpeed * deltaTimeMultiplier;
			_rotationVelocity = _playerInput.look.x * RotationSpeed * deltaTimeMultiplier;

			// clamp our pitch rotation
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Update Cinemachine camera target pitch
			CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

			// rotate the player left and right
			transform.Rotate(Vector3.up * _rotationVelocity);
		}
	}

	private void CameraWobble(float amplitudeGain, float frequencyGain)
	{
		CinemachineBasicMultiChannelPerlin noise = followPlayerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

		noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, amplitudeGain, 4.5f * Time.deltaTime);
		noise.m_FrequencyGain = Mathf.Lerp(noise.m_FrequencyGain, frequencyGain, 4.5f * Time.deltaTime);

	}
	private void Move()
	{
		// set target speed based on move speed, sprint speed and if sprint is pressed
		// We can sprint if we are running straight forward (or almost)


		if (_playerInput.sprint && _playerInput.move.y > 0.8f && !isCrouching)
			isSprinting = true;
		else
			isSprinting = false;

		//if we are grounded and sprinting, we can slide (lock the movement for a certain time)
		if (isSprinting && Grounded && _playerInput.crouch_slide && !isSliding)
		{
			isSliding = true;
		}
		else if (Grounded && _playerInput.crouch_slide)
		{
			isCrouching = true;
		}
		else
		{
			isCrouching = false;
		}

		float targetSpeed = isSprinting ? SprintSpeed : isCrouching ? CrouchSpeed : MoveSpeed;

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

		// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is no input, set the target speed to 0
		if (_playerInput.move == Vector2.zero) targetSpeed = 0.0f;

		//camera adjustment
		Vector3 cameraTargetPos = new Vector3(0f, 1.64f, 0f);
		Vector3 currentPos = CinemachineCameraTarget.transform.localPosition;


		if (isSliding)
		{
			if (_slideTimeoutDelta > 0.1f)
			{
				targetSpeed = SlideSpeed;

				//reduce character controller height, size and center if sliding
				_controller.center = new Vector3(_controller.center.x, 0.42f, _controller.center.z);
				_controller.height = 0.72f;


				cameraTargetPos = new Vector3(0f, 0.9f, 0f);
				currentPos = CinemachineCameraTarget.transform.localPosition;

				_slideTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				isSliding = false;

				//set the normal size and center
				_controller.center = new Vector3(_controller.center.x, 0.92f, _controller.center.z);
				_controller.height = 1.82f;

				_slideTimeoutDelta = SlideTimeout;

			}
		}
		else if (isCrouching)
		{
			//reduce character controller height, size and center if sliding
			_controller.center = new Vector3(_controller.center.x, 0.42f, _controller.center.z);
			_controller.height = 0.72f;


			cameraTargetPos = new Vector3(0f, 0.9f, 0f);
			currentPos = CinemachineCameraTarget.transform.localPosition;
		}
		else
		{
			//set the normal size and center
			_controller.center = new Vector3(_controller.center.x, 0.92f, _controller.center.z);
			_controller.height = 1.82f;

			_slideTimeoutDelta = SlideTimeout;
		}

		if (CinemachineCameraTarget.transform.localPosition != cameraTargetPos)
		{
			CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(currentPos, cameraTargetPos, Time.deltaTime * 6f);
		}

		// a reference to the players current horizontal velocity
		Vector3 currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z);

		float speedOffset = 0.1f;
		float inputMagnitude = _playerInput.analogMovement ? _playerInput.move.magnitude : 1f;

		// normalise input direction
		Vector3 inputDirection = new Vector3(_playerInput.move.x, 0.0f, _playerInput.move.y).normalized;

		// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is a move input rotate player when the player is moving
		if (_playerInput.move != Vector2.zero)
		{
			// move
			inputDirection = transform.right * _playerInput.move.x + transform.forward * _playerInput.move.y;
		}

		currentMovementDirection = inputDirection.normalized;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed.magnitude < targetSpeed - speedOffset || currentHorizontalSpeed.magnitude > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed.magnitude, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
			_movement = Vector3.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude * inputDirection.normalized, Time.deltaTime * SpeedChangeRate);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
			_movement = targetSpeed * inputDirection;
		}

		// move the player
		if (isSliding)
		{
			_speed = targetSpeed;
			_movement = targetSpeed * lastMovementDirection;
		}
		else
		{
			lastMovementDirection = currentMovementDirection;
		}

		_controller.Move(_movement * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

	}

	private void JumpAndGravity()
	{
		if (Grounded)
		{
			if (isJumping)
				isJumping = false;

			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			// stop our velocity dropping infinitely when grounded
			if (_verticalVelocity < 0.0f)
			{
				_verticalVelocity = -2f;
			}

			// Jump
			if (_playerInput.jump && _jumpTimeoutDelta <= 0.0f)
			{
				// the square root of H * -2 * G = how much velocity needed to reach desired height
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				isJumping = true;
			}

			// jump timeout
			if (_jumpTimeoutDelta >= 0.0f)
			{
				_jumpTimeoutDelta -= Time.deltaTime;
			}
		}
		else
		{
			// reset the jump timeout timer
			_jumpTimeoutDelta = JumpTimeout;

			// fall timeout
			if (_fallTimeoutDelta >= 0.0f)
			{
				_fallTimeoutDelta -= Time.deltaTime;
			}

			// if we are not grounded, do not jump
			_playerInput.jump = false;
		}

		// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
		if (_verticalVelocity < _terminalVelocity)
		{
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}

	void Slide()
	{
		//animation based
		if (!_animator.GetBool("IsSliding"))
		{
			isSliding = false;
			return;
		}

		float targetSpeed = SlideSpeed;

		float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

		float speedOffset = 0.1f;

		if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * SlideSpeedChangeRate);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
		}

		//reduce character controller height, size and center
		_controller.center = new Vector3(_controller.center.x, 0.42f, _controller.center.z);
		_controller.height = 0.72f;
		_controller.radius = 0.22f;

		//camera adjustment

		Vector3 cameraTargetPos = new Vector3(0f, 0.5f, 0f);
		Vector3 currentPos = CinemachineCameraTarget.transform.localPosition;

		if (CinemachineCameraTarget.transform.localPosition != cameraTargetPos)
		{
			CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(currentPos, cameraTargetPos, Time.deltaTime * 6f);
		}

		//move the player till slide Speed
		_controller.Move(currentMovementDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
	}

	//TODO: to implement later
	private void MoveInAir()
	{

	}


	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

	private void OnDrawGizmosSelected()
	{
		Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
		Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

		if (Grounded) Gizmos.color = transparentGreen;
		else Gizmos.color = transparentRed;

		// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
	}
}
