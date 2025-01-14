using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecoilController : MonoBehaviour
{

    private float recoil = 0.0f;
    private float maxRecoil_x = -50f;
    private float maxRecoil_y = 40f;
    private float recoilSpeed = 2f;
    private float recoilReset = 0.5f;
    private float rnd;

    // Update is called once per frame
    void Update()

    {
        Recoiling();
    }

    public void StartRecoil(float recoilParam, float maxRecoil_xParam, float maxRecoil_YParam, float recoilSpeedParam, float recoilReset)

    {
        // in seconds
        recoil = recoilParam;
        maxRecoil_x = maxRecoil_xParam;
        recoilSpeed = recoilSpeedParam;
        maxRecoil_y = Random.Range(-maxRecoil_YParam, maxRecoil_YParam);
        this.recoilReset = recoilReset;
    }


    void Recoiling()

    {
        if (recoil > 0f)
        {
            Quaternion maxRecoil = Quaternion.Euler(-maxRecoil_x, maxRecoil_y, 0f);
            //Quaternion maxRecoil2 = Quaternion.Euler (0, maxRecoil_y, 0f);
            // Dampen towards the target rotation
            transform.localRotation = Quaternion.Slerp(transform.localRotation, maxRecoil, Time.deltaTime * recoilSpeed / 6);
            //.transform.localRotation = Quaternion.Slerp (transform.localRotation, maxRecoil2, Time.deltaTime * recoilSpeed);
            recoil -=  recoilReset * Time.deltaTime;

        }
        else
        {
            recoil = 0f;
            // Dampen towards the target rotation
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * recoilSpeed / 6);
            //vc.transform.localRotation = Quaternion.Slerp (transform.localRotation, Quaternion.identity, Time.deltaTime * recoilSpeed / 2);

        }

    }
}
