using System;
using System.Collections;
using System.Collections.Generic;
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
        m_DesiredPosition = m_Player.Find("CameraPoint").transform.position;
        transform.position = Vector3.SmoothDamp(transform.position, m_DesiredPosition,  ref velocity, m_SmoothTime);
        transform.LookAt(m_Player.transform.position + m_Player.transform.forward);
    }
}
