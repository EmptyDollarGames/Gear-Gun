using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;
	public bool jump;
	public bool sprint;
	public bool crouch_slide;
	public bool shoot;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = false;
	public bool cursorInputForLook = false;

	[Header("Weapon Inputs")]
	public float switchWeapon;
	public bool reload;

	public void OnMove(InputValue value)
	{
		MoveInput(value.Get<Vector2>());
	}

	public void OnLook(InputValue value)
	{
		if (cursorInputForLook)
		{
			LookInput(value.Get<Vector2>());
		}
	}

	public void OnJump(InputValue value)
	{
		JumpInput(value.isPressed);
	}

	public void OnSprint(InputValue value)
	{
		SprintInput(value.isPressed);
	}

	public void OnCrouch_Slide(InputValue value)
    {
		CrouchSlideInput(value.isPressed);
    }
	public void OnShoot(InputValue value)
	{
		ShootInput(value.isPressed);
	}

	public void OnReload(InputValue value)
	{
		ReloadInput(value.isPressed);
	}

	public void OnSwitchWeapon(InputValue value)
	{
		SwitchWeaponInput(value.Get<float>());
	}
	public void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	}

	public void LookInput(Vector2 newLookDirection)
	{
		look = newLookDirection;
	}

	public void JumpInput(bool newJumpState)
	{
		jump = newJumpState;
	}

	public void SprintInput(bool newSprintState)
	{
		sprint = newSprintState;
	}

	public void CrouchSlideInput(bool newCrouchState)
	{
		crouch_slide = newCrouchState;
	}

	public void ShootInput(bool newShootState)
	{
		shoot = newShootState;
	}

	public void SwitchWeaponInput(float newSwitchWeaponValue)
	{
		switchWeapon = newSwitchWeaponValue;
	}

	public void ReloadInput(bool newReloadState)
	{
		reload = newReloadState;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		SetCursorState(cursorLocked);
	}

	private void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}

}
