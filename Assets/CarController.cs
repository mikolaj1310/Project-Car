using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

enum AccelerationType
{
    AT_AllWheelDrive,
    AT_FrontWheelDrive,
    AT_BackWheelDrive
}

struct Wheel
{
    public GameObject m_WheelObject;
    public GameObject m_WheelModel;
    public bool m_GivePower;
}

public class CarController : NetworkBehaviour
{
    private Rigidbody m_CarRigidbody;
    private List<Wheel> m_Wheels = new List<Wheel>();
    private Wheel m_WheelFR;
    private Wheel m_WheelFL;
    private Wheel m_WheelBR;
    private Wheel m_WheelBL;
    private float m_AccelInput = 0f;
    public Transform m_StartTransform;
    
    [Header("Suspension")]
    [SerializeField] private float m_SuspensionRestDistance;
    [SerializeField] private float m_SpringStrength; 
    [SerializeField] private float m_DampingStrength;
    
    [Header("Steering")]
    [SerializeField] [Range(0, 1)] private float m_TireGripFactor;
    [SerializeField] private float m_TireMass;
    [SerializeField] private float m_TurnRadious;

    [Header("Acceleration")] 
    [SerializeField] private AccelerationType accelerationType;
    [SerializeField] private float m_CarTopSpeed;
    [SerializeField] private float m_Acceleration;
    [SerializeField] private AnimationCurve m_PowerCurve;
    [SerializeField] [Range(0, 1)] private float m_BreakingFactor;

    [Header("Gearbox")] [SerializeField] private List<AnimationCurve> m_Gears;
    private int currentGear;
    private TMP_Text m_CurrentGearText;
    
    //NETWORKING

    
    // Start is called before the first frame update
    void Start()
    {
        
        m_CarRigidbody = GetComponent<Rigidbody>();
        m_WheelFR.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelFR").gameObject;
        m_WheelFR.m_WheelModel = m_WheelFR.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelFL.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelFL").gameObject;
        m_WheelFL.m_WheelModel = m_WheelFL.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelBR.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelBR").gameObject;
        m_WheelBR.m_WheelModel = m_WheelBR.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelBL.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelBL").gameObject;
        m_WheelBL.m_WheelModel = m_WheelBL.m_WheelObject.transform.Find("Model").gameObject;
        
        if (!isOwned) return;
        GameObject.Find("Main Camera").GetComponent<CameraController>().m_Player = transform;
        m_CurrentGearText = GameObject.Find("GearNumber").GetComponent<TMP_Text>();
        currentGear = 0;
        
        switch (accelerationType)
        {
            case AccelerationType.AT_AllWheelDrive:
                m_WheelFR.m_GivePower = true;
                m_WheelFL.m_GivePower = true;
                m_WheelBR.m_GivePower = true;
                m_WheelBL.m_GivePower = true;
                break;
            case AccelerationType.AT_BackWheelDrive:
                m_WheelFR.m_GivePower = false;
                m_WheelFL.m_GivePower = false;
                m_WheelBR.m_GivePower = true;
                m_WheelBL.m_GivePower = true;
                break;
            case AccelerationType.AT_FrontWheelDrive:
                m_WheelFR.m_GivePower = true;
                m_WheelFL.m_GivePower = true;
                m_WheelBR.m_GivePower = false;
                m_WheelBL.m_GivePower = false;
                break;
        }
        
        m_Wheels.Add(m_WheelFR);
        m_Wheels.Add(m_WheelFL);
        m_Wheels.Add(m_WheelBR);
        m_Wheels.Add(m_WheelBL);

        m_StartTransform = GameObject.Find("SpawnPoints").transform;
    }

    private void Update()
    {
        if (!isOwned) return;
        if (Input.GetButtonDown("RightBumper"))
        {
            if (currentGear < m_Gears.Count - 1)
            {
                currentGear++;
            }
        }
        if (Input.GetButtonDown("LeftBumper"))
        {
            if (currentGear > 0)
            {
                currentGear--;
            }
        }
        
        m_CurrentGearText.SetText((currentGear + 1).ToString());
        
        m_Wheels[0].m_WheelObject.transform.rotation = transform.rotation;
        m_Wheels[1].m_WheelObject.transform.rotation = transform.rotation;
       
        {
            m_Wheels[0].m_WheelObject.transform.Rotate(new Vector3(0, Input.GetAxisRaw("Horizontal") * m_TurnRadious, 0));
            m_Wheels[1].m_WheelObject.transform.Rotate(new Vector3(0, Input.GetAxisRaw("Horizontal") * m_TurnRadious, 0));
        }

        
        m_AccelInput = Input.GetAxisRaw("RightTrigger");

        //if (Input.GetKey(KeyCode.W))
        //{
        //    m_AccelInput = 1f;
        //}
        //else if (Input.GetKey(KeyCode.S))
        //{
        //    m_AccelInput = -1f;
        //}
        //else
        //{
        //    m_AccelInput = 0f;
        //}
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (isClient)
        {
            UpdateCar();
        }
        //if(isServer)
        //    CmdUpdateCars();
    }

    [Server]
    private void CmdUpdateCars()
    {
        RpcUpdateCars();
    }

    [ClientRpc]
    private void RpcUpdateCars()
    {
        foreach (var car in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (car == this.gameObject)
            {
                foreach (var wheel in car.GetComponent<CarController>().m_Wheels)
                {
                    RaycastHit hit;
                    var tireTransform = wheel.m_WheelObject.transform;
                    wheel.m_WheelModel.transform.localPosition = new Vector3(0, m_SuspensionRestDistance + (wheel.m_WheelModel.GetComponent<MeshRenderer>().bounds.size.y / 2), 0);
                    //if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance))
                    //{
                    //    if(hit.distance < m_SuspensionRestDistance)
                    //        wheel.m_WheelModel.transform.localPosition = new Vector3(0, -hit.distance + (wheel.m_WheelModel.GetComponent<MeshRenderer>().bounds.size.y / 2), 0);
                    //    else
                    //    {
                    //        wheel.m_WheelModel.transform.localPosition = new Vector3(0, m_SuspensionRestDistance + (wheel.m_WheelModel.GetComponent<MeshRenderer>().bounds.size.y / 2), 0);
                    //    }
                    //}
                    Debug.Log("Happened");
                }
                
            }
        }
    }

    [ClientCallback]
    private void UpdateCar()
    {
        bool carGrounded = false;
        foreach (var wheel in m_Wheels)
        {
            RaycastHit hit;
            var tireTransform = wheel.m_WheelObject.transform;
            //Suspension
            if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance))
            {
                carGrounded = true;
                Vector3 springDir = tireTransform.up;
                Vector3 tireWorldVel = m_CarRigidbody.GetPointVelocity(tireTransform.position);
                
                float offset = m_SuspensionRestDistance - hit.distance;
                if(hit.distance < m_SuspensionRestDistance)
                    wheel.m_WheelModel.transform.localPosition = new Vector3(0, -hit.distance + (wheel.m_WheelModel.GetComponent<MeshRenderer>().bounds.size.y / 2), 0);
                else
                {
                    wheel.m_WheelModel.transform.localPosition = new Vector3(0, m_SuspensionRestDistance + (wheel.m_WheelModel.GetComponent<MeshRenderer>().bounds.size.y / 2), 0);
                }
                float vel = Vector3.Dot(springDir, tireWorldVel);
                float force = (offset * m_SpringStrength) - (vel * m_DampingStrength);
                
                m_CarRigidbody.AddForceAtPosition(springDir * force, tireTransform.position);
                Debug.DrawLine(tireTransform.position, tireTransform.position + (springDir * force) / 100f, Color.green);
            }
            
            //Turning
            if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance))
            {
                if (!isOwned) break;
                Vector3 steeringDir = tireTransform.right;
                Vector3 tireWorldVel = m_CarRigidbody.GetPointVelocity(tireTransform.position);

                float steeringVel = Vector3.Dot(steeringDir, tireWorldVel);
                float desiredVelChange = -steeringVel * m_TireGripFactor;
                float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                m_CarRigidbody.AddForceAtPosition(steeringDir * m_TireMass * desiredAccel, tireTransform.position);
                Debug.DrawLine(tireTransform.position, tireTransform.position + tireTransform.right , Color.red);
            }
            
            //Acceleration
            if (wheel.m_GivePower)
            {
                if (!isOwned) break;
                if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance))
                {
                    Vector3 accelDir = tireTransform.forward;

                    float carSpeed = Vector3.Dot(transform.forward, m_CarRigidbody.velocity);

                    float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / m_CarTopSpeed);
                    
                    if (m_AccelInput != 0.0f)
                    {

                        float availableTorque = m_Gears[currentGear].Evaluate(normalizedSpeed) * m_AccelInput;

                        m_CarRigidbody.AddForceAtPosition((accelDir * availableTorque) * m_Acceleration,
                            tireTransform.position);
                        
                        
                    }

                    if (m_Gears.Count > currentGear && currentGear < m_Gears.Count - 1)
                    {
                        if (m_Gears[currentGear].Evaluate(normalizedSpeed)<
                            m_Gears[currentGear + 1].Evaluate(normalizedSpeed))
                        {
                            currentGear++;
                        }
                    }

                    if (currentGear != 0)
                    {
                        if (m_Gears[currentGear - 1].Evaluate(normalizedSpeed)>
                            m_Gears[currentGear].Evaluate(normalizedSpeed))
                        {
                            currentGear--;
                        }
                    }
                    



                    if (m_AccelInput == 0)
                    {
                        Vector3 tireWorldVel = m_CarRigidbody.GetPointVelocity(tireTransform.position);
                        float desiredVelChange = Vector3.Dot(accelDir, tireWorldVel) * -m_BreakingFactor;
                        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
                        
                        m_CarRigidbody.AddForceAtPosition((accelDir * desiredVelChange) * m_Acceleration,
                            tireTransform.position);
                    }
                }
            }
        }

        if (!isOwned) return;
        if (!carGrounded)
        {
            Vector3 airRotation = new Vector3(-Input.GetAxisRaw("Vertical") * 10, 0, -Input.GetAxisRaw("Horizontal"));
            m_CarRigidbody.AddRelativeTorque(airRotation * 3, ForceMode.Force);
        }
        
        
        
        if (Input.GetButton("Reset"))
        {
            m_CarRigidbody.velocity = Vector3.zero;
            transform.position = m_StartTransform.position;
            transform.rotation = m_StartTransform.rotation;
            currentGear = 0;
        }
    }
}
