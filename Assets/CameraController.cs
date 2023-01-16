using System;
using System.Collections;
using System.Collections.Generic;
using Mirror.Examples.CCU;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform m_Player;
    private Vector3 m_DesiredPosition;
    private Vector3 velocity = Vector3.zero;
    public float m_SmoothTime;

    // Update is called once per frame
    private void Start()
    {
    }

    void LateUpdate () {
        if (m_Player)
        {
            m_DesiredPosition = m_Player.Find("CameraPoint").transform.position;
            transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition, ref velocity, m_SmoothTime);
            //transform.LookAt(Vector3.Lerp(transform.rotation.eulerAngles, m_Player.transform.position + m_Player.transform.forward, m_SmoothTime), m_Player.up);
            
            float rotationSpeed = 30 * Time.deltaTime;
            var targetRot = Quaternion.LookRotation(m_Player.position + m_Player.forward - transform.position, m_Player.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed);
        }
    }
}
