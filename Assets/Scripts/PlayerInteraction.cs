using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float currentLife = 100f;

    PlayerController _controller;
    PlayerInputs _input;
    WeaponWheel _wheel;

    private bool isInteracting = false;
    public bool IsInteracting { get { return isInteracting; } }

    private void Start()
    {
        _controller = GetComponent<PlayerController>();
        _input = GetComponent<PlayerInputs>();
        _wheel = GetComponentInChildren<WeaponWheel>();
    }

    private void Update()
    {

        HandleFireAndReload();
        HandleLoot();
        HandleInteractable();
    }

    void HandleFireAndReload()
    {
        if (_input.shoot)
        {
            if(!_wheel.IsSwitchingWeapon)
                _wheel.CurrentWeapon.Fire();

            if (!_wheel.CurrentWeapon.isAutomatic)
                _input.shoot = false;
        }

        if (_input.reload)
        {
            Weapon currentWp = _wheel.CurrentWeapon;
            _input.reload = false;

            if(!_wheel.IsSwitchingWeapon)
                currentWp.Reload();
        }
    }

    void HandleLoot()
    {
        //TODO: to implement -> LOOTABLE interface
    }

    void HandleInteractable()
    {
        //TODO: to implement -> INTERACTABLE interface
    }

}
