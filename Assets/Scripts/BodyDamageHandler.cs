using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyDamageHandler : MonoBehaviour
{
    public enum BodyPart { Head, Body, Limbs}

    public BodyPart bodyPart;
    public Boss controller;
    public ParticleSystem hitParticles;

    public void OnTakeDamage(float damage)
    {
        controller.OnReceiveDamage(damage, this);
    }

    public void ParticleHitEffect(Vector3 pos)
    {
        hitParticles.transform.position = pos;
        hitParticles.transform.forward = - Camera.main.transform.forward;
        hitParticles.Play();
    }
}