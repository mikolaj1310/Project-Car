using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking.Types;
using UnityEngine.UI;

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
    public ParticleSystem m_BurnoutParticleSystem;
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
    [SerializeField] [Range(0, 1)] private float m_IdleBreakingFactor;
    private float m_BreakInput;

    [Header("Gearbox")] [SerializeField] private List<AnimationCurve> m_Gears;
    private int currentGear;
    private TMP_Text m_CurrentGearText;

    [Header("Physics")] 
    [SerializeField] private float m_GravityForce;
    public Vector3 m_LastGravityDirection { get; private set; }
    [SerializeField] private LayerMask m_RoadLayer;

    [Header("Particles")] 
    [SerializeField] private float m_BurnoutAngle;
    
    //NETWORKING


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        m_CarRigidbody = GetComponent<Rigidbody>();
        m_WheelFR.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelFR").gameObject;
        m_WheelFR.m_WheelModel = m_WheelFR.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelFR.m_BurnoutParticleSystem = null;

        m_WheelFL.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelFL").gameObject;
        m_WheelFL.m_WheelModel = m_WheelFL.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelFL.m_BurnoutParticleSystem = null;
        
        m_WheelBR.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelBR").gameObject;
        m_WheelBR.m_WheelModel = m_WheelBR.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelBR.m_BurnoutParticleSystem = m_WheelBR.m_WheelObject.transform.GetComponentInChildren<ParticleSystem>();
        m_WheelBR.m_BurnoutParticleSystem.Stop();
        
        m_WheelBL.m_WheelObject = transform.Find("Chasis").Find("Wheels").Find("WheelBL").gameObject;
        m_WheelBL.m_WheelModel = m_WheelBL.m_WheelObject.transform.Find("Model").gameObject;
        m_WheelBL.m_BurnoutParticleSystem = m_WheelBL.m_WheelObject.transform.GetComponentInChildren<ParticleSystem>();
        m_WheelBL.m_BurnoutParticleSystem.Stop();

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

        Physics.gravity = Vector3.zero;
    }

    private void Update()
    {
        if (!isOwned) return;

        if (GameObject.Find("Main Camera"))
        {
            GameObject.Find("Main Camera").GetComponent<CameraController>().m_Player = transform;
        }
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

        if (Input.GetButton("Break"))
        {
            m_BreakInput = 1f;
        }
        else
        {
            m_BreakInput = 0f;
        }
        
        float rotationSpeed = 15 * Time.deltaTime;

        var inputDirection = Input.GetAxisRaw("Horizontal");
        
        float carSpeed = Vector3.Dot(transform.forward, m_CarRigidbody.velocity);
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / m_CarTopSpeed);
        float desiredTurnRadious = m_TurnRadious * (1f - (normalizedSpeed / 2f));
        
        var desiredRot = Quaternion.Euler(
            transform.localRotation.x, 
            inputDirection * desiredTurnRadious, 
            transform.localRotation.z);


        if (m_Wheels.Count > 0)
        {
            m_Wheels[0].m_WheelObject.transform.localRotation =
                Quaternion.Lerp(m_Wheels[0].m_WheelObject.transform.localRotation, desiredRot, rotationSpeed);
            m_Wheels[1].m_WheelObject.transform.localRotation =
                Quaternion.Lerp(m_Wheels[1].m_WheelObject.transform.localRotation, desiredRot, rotationSpeed);
        }

        m_AccelInput = Input.GetAxisRaw("RightTrigger");
    }
    
    void FixedUpdate()
    {
        if(!m_CurrentGearText && GameObject.Find("GearNumber"))
            m_CurrentGearText = GameObject.Find("GearNumber").GetComponent<TMP_Text>();
        if (isClient)
        {
            UpdateCar();
            UpdateGravity();
        }

    }

    private void UpdateGravity()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, 1, m_RoadLayer))
        {
            m_CarRigidbody.AddForce(-hit.normal * m_GravityForce);
            m_LastGravityDirection = -hit.normal;
        }
        else
        {
            //m_CarRigidbody.AddForce(-Vector3.up * m_GravityForce);
            m_CarRigidbody.AddForce(m_LastGravityDirection * m_GravityForce);
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
            if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance, m_RoadLayer))
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
            if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance, m_RoadLayer))
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
                if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance, m_RoadLayer))
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

                    
                    if (m_AccelInput == 0)
                    {
                        Vector3 tireWorldVel = m_CarRigidbody.GetPointVelocity(tireTransform.position);
                        float desiredVelChange = Vector3.Dot(accelDir, tireWorldVel) * -m_IdleBreakingFactor;
                        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
                        
                        m_CarRigidbody.AddForceAtPosition((accelDir * desiredVelChange) * m_Acceleration,
                            tireTransform.position);
                    }
                }
            }

            if (m_BreakInput != 0)
            {
                
                if (m_AccelInput == 0)
                {
                    if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance,
                        m_RoadLayer))
                    {
                        Vector3 accelDir = tireTransform.forward;
                        Vector3 tireWorldVel = m_CarRigidbody.GetPointVelocity(tireTransform.position);
                        float desiredVelChange = Vector3.Dot(accelDir, tireWorldVel) * -m_BreakingFactor;
                        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

                        m_CarRigidbody.AddForceAtPosition((accelDir * desiredVelChange) * m_Acceleration,
                            tireTransform.position);
                    }
                }
            }

            if (wheel.m_BurnoutParticleSystem)
            {
                if (Physics.Raycast(tireTransform.position, -tireTransform.up, out hit, m_SuspensionRestDistance + 0.1f,
                    m_RoadLayer))
                {
                    //calculation for drift particle
                    var n = m_LastGravityDirection;
                    var v = m_CarRigidbody.velocity;

                    float d = Vector3.Dot(v, n);
                    if (d > 0f) v -= n * d;

                    float angle = Vector3.Angle(v, transform.forward);

                    if (angle > m_BurnoutAngle)
                        wheel.m_BurnoutParticleSystem.Play();
                    else
                        wheel.m_BurnoutParticleSystem.Stop();
                }
                else
                    wheel.m_BurnoutParticleSystem.Stop();
            }

        }
        

        float normalizedCarSpeed = Mathf.Clamp01(Mathf.Abs(Vector3.Dot(transform.forward, m_CarRigidbody.velocity)) / m_CarTopSpeed);
        
        if (m_Gears.Count > currentGear && currentGear < m_Gears.Count - 1)
        {
            if (m_Gears[currentGear].Evaluate(normalizedCarSpeed)<
                m_Gears[currentGear + 1].Evaluate(normalizedCarSpeed))
            {
                currentGear++;
            }
        }

        if (currentGear != 0)
        {
            if (m_Gears[currentGear - 1].Evaluate(normalizedCarSpeed)>
                m_Gears[currentGear].Evaluate(normalizedCarSpeed))
            {
                currentGear--;
            }
        }

        

        if(m_CurrentGearText)
            m_CurrentGearText.SetText((currentGear + 1).ToString());


        if (!isOwned) return;
        if (!carGrounded)
        {
            Vector3 airRotation = new Vector3(-Input.GetAxisRaw("Vertical") * 10000, 0, -Input.GetAxisRaw("Horizontal") * 1000);
            m_CarRigidbody.AddRelativeTorque(airRotation * Time.fixedDeltaTime, ForceMode.Force);
        }
        
        
        
        if (Input.GetButton("Reset"))
        {
            if (m_StartTransform)
            {
                m_CarRigidbody.velocity = Vector3.zero;
                transform.position = m_StartTransform.position;
                m_CarRigidbody.angularVelocity = Vector3.zero;
                transform.rotation = m_StartTransform.rotation;
                currentGear = 0;
                m_LastGravityDirection = -m_StartTransform.up;
                foreach (var wheel in m_Wheels)
                {
                    if (wheel.m_BurnoutParticleSystem)
                    {
                        wheel.m_BurnoutParticleSystem.Stop();
                    }
                }
            }
            else
            {
                m_StartTransform = GameObject.Find("SpawnPoints").transform;
            }
        }
    }
}
