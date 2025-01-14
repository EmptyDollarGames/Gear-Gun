using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public string weaponName = "weapon";
    [Tooltip("Determines the fire rate of the weapon")]
    public float fireRate = 0.25f;
    [Tooltip("Magazine capacity")]
    public int magSize = 8;
    [Tooltip("Max bullets ownable")]
    public int maxBullets = 64;
    [Tooltip("Damage for a body shot (x2 for the Head, /2 for the limbs")]
    public float damage = 50f;
    [Tooltip("How far can shoot the weapon")]
    public float range = 20f;
    [Tooltip("Determines if I have to hold or tap to fire")]
    public bool isAutomatic = false;
    [Tooltip("Es. sniper with single bullet or shotgun")]
    public bool singleBulletReload = false;
    [Tooltip("For single bullets reload weapons")]
    public float startingReloadingDelay = 0.1f;
    [Tooltip("For single bullets reload weapons")]
    public float singleBulletReloadTime = 0.5f;

    [Header("Recoil Params")]
    [Tooltip("Recoil of the weapon")]
    public float recoil = 0.5f;
    [Tooltip("Recoil speed")]
    public float recoilSpeed = 0.5f;
    [Tooltip("Horizontal maxRecoil")]
    public float maxRecoilX = 0.5f;
    [Tooltip("Vertical maxRecoil")]
    public float maxRecoilY = 0.5f;
    [Tooltip("Determines how fast the weapon reset after recoil. If it's high, the weapon reset faster.")]
    public float recoilReset = 0.5f;

    [SerializeField] public LayerMask enemy;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] public Animator _animator;
    public Sprite crosshairIcon;

    private int currentBulletsInMag;
    private int currentBulletsStored;
    public float currentGunHeat;

    private bool isReloading = false;
    private bool canShoot = true;
    private Coroutine activeReloadCoroutine;

    public bool CanShoot { get => canShoot; set => canShoot = value; }
 
    public int CurrentBulletsInMag { get { return currentBulletsInMag; } }
    public int CurrentBulletsStored { get { return currentBulletsStored; } }
    private void Awake()
    {

        if (_animator == null)
            _animator = GetComponent<Animator>();
    }
    private void Start()
    {
        currentBulletsInMag = magSize;
        currentBulletsStored = maxBullets;
    }

    private void Update()
    {
        if (currentGunHeat > 0f)
        {
            currentGunHeat -= Time.deltaTime;
        }

        if (currentBulletsInMag <= 0 && currentBulletsStored>0)
        {
            Reload();
            //return;
        }
    }
    private void OnEnable()
    {
        SetReloadBool(false);
    }

    public virtual void SetReloadBool(bool reloading)
    {
        _animator.SetBool("Reload", reloading);
        isReloading = reloading;
    }

    public virtual void Fire()
    {
        if (!canShoot)
            return;

        if (currentBulletsInMag <= 0)
        {
            Debug.Log("RELOAD WEAPON!");
            return;
        }
        if (currentGunHeat <= 0f)
        {
            //We reset the coroutines in case the player is reloading
            
           if(activeReloadCoroutine!=null)
              StopCoroutine(activeReloadCoroutine);

            _animator.SetBool("Reload", false);
            isReloading = false;

            RecoilController recoilController = GetComponent<RecoilController>();
            recoilController.StartRecoil(recoil, maxRecoilY, maxRecoilX , recoilSpeed, recoilReset);

            if(muzzleFlash!=null)
                muzzleFlash.Play();

            Debug.Log("FIRE!");

            HandleFireRaycast();
            
            currentBulletsInMag--;
            currentGunHeat = 1 - fireRate;

            _animator.SetTrigger("Shoot");
        }
    }

    public virtual void HandleFireRaycast()
    {
        RaycastHit hit;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, range, ~enemy))
        {
            Debug.Log("HIT!");
            BodyDamageHandler body = hit.collider.GetComponent<BodyDamageHandler>();

            if (body != null)
            {
                if (body.bodyPart == BodyDamageHandler.BodyPart.Head)
                    body.OnTakeDamage(damage * 2f);
                else if (body.bodyPart == BodyDamageHandler.BodyPart.Body)
                    body.OnTakeDamage(damage);
                else if (body.bodyPart == BodyDamageHandler.BodyPart.Limbs)
                    body.OnTakeDamage(damage / 2);

                //Blood particle instantiate
                body.ParticleHitEffect(hit.point);

            }

        }
    }

    public bool IsAutomatic()
    {
        return isAutomatic;
    }

    public virtual void Reload()
    {
        Debug.Log(isReloading);
        if (currentBulletsInMag < magSize && currentBulletsStored > 0 && !isReloading)
        {
            isReloading = true;
            _animator.SetBool("Reload", isReloading);

            if(singleBulletReload)
                activeReloadCoroutine = StartCoroutine(SingleBulletReloadCoroutine());
            else
                activeReloadCoroutine = StartCoroutine(ReloadCoroutine());
        }
    }

    
    IEnumerator SingleBulletReloadCoroutine()
    {
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
        {
            yield return null;
        }

        float elapsedTime = 0f;
        float duration = _animator.GetCurrentAnimatorStateInfo(0).length;

        int i = 1;
        while (currentBulletsInMag<magSize)
        {
            if (elapsedTime > duration * i)
            {
                currentBulletsStored -= 1;
                currentBulletsInMag += 1;
                i++;
            }

            elapsedTime += Time.deltaTime;
            Debug.Log("Reloading...");

            //Termination condition if player isSprinting


            yield return null;
        }

        Debug.Log("RELOADING OF" + name + "DONE!");

        isReloading = false;
        _animator.SetBool("Reload", isReloading);
        activeReloadCoroutine = null;
    }

    IEnumerator ReloadCoroutine()
    {
        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Reload"))
        {
            yield return null;
        }

        float elapsedTime = 0f;
        float duration = _animator.GetCurrentAnimatorStateInfo(0).length;

        //yield return new WaitForSeconds(duration);
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            Debug.Log("Reloading...");
            yield return null;
        }

        Debug.Log("DONE!");
        currentBulletsStored -= magSize;

        if (currentBulletsStored >= 0)
        {
            currentBulletsInMag = magSize;
        }
        else
        {
            currentBulletsInMag = magSize + currentBulletsStored;
            currentBulletsStored = 0;
        }

        isReloading = false;
        _animator.SetBool("Reload", isReloading);
        activeReloadCoroutine = null;
    }

    public virtual Animator WeaponTake()
    {
        if(UIManager.instance!=null)
            UIManager.instance.ChangeCrosshair(crosshairIcon, 20f, 20f);

        _animator.SetBool("Take", true);

        return _animator;
    }
    public virtual Animator WeaponHide()
    {
        if (activeReloadCoroutine != null)
            StopCoroutine(activeReloadCoroutine);

        isReloading = false;

        _animator.SetBool("Take", false);

        return _animator;
    }


    public void AddBullets(int bulletsNum)
    {
        if (currentBulletsStored + bulletsNum >= maxBullets)
            currentBulletsStored = maxBullets;
        else
            currentBulletsStored += bulletsNum;
    }
}
