using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearGun : Weapon
{
    public enum FireMode { Normal, Fire, Ice, Demon};

    public FireMode currentFireMode = FireMode.Normal;

    public void HandleFireModeChange()
    {
        //Wait for the player to switch mode
    }
}
