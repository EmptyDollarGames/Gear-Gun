using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponWheel : MonoBehaviour
{
    [Header("Player Weapons")]
    public List<GameObject> weapons;

    [Header("Player Inputs")]
    public PlayerInputs _playerInput;

    [Header("Player Interactions")]
    public PlayerInteraction _playerInt;

    [Header("Parent of weapons")]
    [SerializeField] private Transform handle;

    private Weapon currentWeapon;

    public Weapon CurrentWeapon { get { return currentWeapon; } }

    private int selectedWeaponIndex = 0;

    private bool isSwitchingWeapon = false;

    public bool IsSwitchingWeapon { get { return isSwitchingWeapon; } }

    private void Start()
    {
        //Init starting available weapons
        foreach (GameObject wp in weapons)
        {
            GameObject weapon = Instantiate(wp, handle);
            weapon.SetActive(false);
        }

        //we equip the first weapon
        SwitchWeapon();

        if (_playerInput == null)
            _playerInput = transform.parent.GetComponent<PlayerInputs>();
    }

    private void Update()
    {
        HandleSwitchWeapon();
    }
    
    private void HandleSwitchWeapon()
    {
        if (isSwitchingWeapon)
            return;

        int lastSelectedWeapon = selectedWeaponIndex;

        if (_playerInput.switchWeapon >= 0f)
        {
            if (selectedWeaponIndex >= weapons.Count - 1)
                selectedWeaponIndex = 0;
            else
                selectedWeaponIndex++;
        }
        if (_playerInput.switchWeapon <= 0f)
        {
            if (selectedWeaponIndex <= 0)
                selectedWeaponIndex = transform.childCount - 1;
            else
                selectedWeaponIndex--;
        }

        if (lastSelectedWeapon != selectedWeaponIndex)
        {
            isSwitchingWeapon = true;
            StartCoroutine(WaitWeaponSwitch(currentWeapon, false));
        }
    }

    private void SwitchWeapon()
    {
        int i = 0;
        foreach(Transform t in transform)
        {
            if (i == selectedWeaponIndex)
            {
                Debug.Log(t.name);
                t.gameObject.SetActive(true);
                currentWeapon = t.GetComponent<Weapon>();
                Debug.Log(currentWeapon.weaponName);
                StartCoroutine(WaitWeaponSwitch(currentWeapon, true));
            }
            else
            {
                t.gameObject.SetActive(false);
            }
            i++;
        }

        isSwitchingWeapon = false;
    }

    IEnumerator WaitWeaponSwitch(Weapon wp, bool type)
    {
        Debug.Log("Wait Weapon Switch.. ->" + wp.weaponName);
        Animator anim;

        if (!type)
        {
            anim = wp.WeaponHide();

            while (!anim.GetCurrentAnimatorStateInfo(0).IsName("Hide"))
            {
                yield return null;
            }

            float time = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
            while (time < 1)
            {
                time = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
                Debug.Log("Time: " + time);
                yield return null;
            } 

            SwitchWeapon();

        }
        else
            wp.WeaponTake();

        Debug.Log("Switch Completed");
    }

    private void OnNewWeaponAdded(GameObject weapon)
    {
        
        GameObject wp = Instantiate(weapon, handle);
        //Maybe we can equip it right away when we have no weapons
        wp.SetActive(false);

        //The list "weapons" will be probably eliminated in the future, cause we don't have any weapon at the beginnin of the game (or do we?)
        weapons.Add(weapon);
    }

}
