using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Boss : MonoBehaviour
{
    public abstract void OnReceiveDamage(float dmg,BodyDamageHandler bodypart);
}
