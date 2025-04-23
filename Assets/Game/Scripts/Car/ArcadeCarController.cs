using UnityEngine;
using TMPro;

[System.Serializable]
public class CarConfig
{
    [Header("Engine")]
    public float maxSpeed = 230f;
    public float maxMotorTorque = 60000f;
    public float motorResponse = 15f; // Реакция на газ
    public AnimationCurve torqueCurve;
    public float engineBrake = 10f;
    public float idleRPM = 800f;
    
    [Header("Steering")]
    public float maxSteeringAngle = 25f;
    public float steeringSpeed = 25f; // Быстрая реакция
    public float minSteeringAtSpeed = 5f; // Минимальный угол на скорости
    public float steeringSpeedFactor = 0.5f; // Влияние скорости на руление
    
    [Header("Brakes")]
    public float brakeForce = 20000f;
    public float handBrakeForce = 25000f;
    public float brakeResponse = 20f; // Реакция тормозов
    
    [Header("Transmission")]
    public float[] gearRatios = { 3.5f, 2.5f, 1.8f, 1.4f, 1.1f, 0.9f };
    public float finalDriveRatio = 3.2f;
    public float shiftUpRPM = 6500f;
    public float shiftDownRPM = 3000f;
    
    [Header("Drivetrain")]
    [Range(0,1)] public float frontTorque = 0.7f;
    [Range(0,1)] public float frontBrake = 0.6f;
}

public class ArcadeCarController : MonoBehaviour
{
    public CarConfig config;
    public Transform centerOfMass;
    public Camera carCamera;
    public float cameraShake = 0.1f;
    
    [Header("UI")]
    public TMP_Text speedText;
    public TMP_Text rpmText;
    public TMP_Text gearText;
    
    private Rigidbody rb;
    private WheelCollider[] wheels;
    private float currentRPM = 0f;
    private int currentGear = 1;
    private float shiftTimer = 0f;
    private float steeringInput;
    private float throttleInput;
    private bool isBraking;
    private bool isHandBraking;
    private Vector3 originalCameraPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        wheels = GetComponentsInChildren<WheelCollider>();
        originalCameraPos = carCamera.transform.localPosition;
        
        if (centerOfMass)
            rb.centerOfMass = centerOfMass.localPosition;
        
        // Настройка кривой крутящего момента по умолчанию
        if (config.torqueCurve.length == 0)
        {
            config.torqueCurve = new AnimationCurve(
                new Keyframe(0, 0.2f),
                new Keyframe(0.4f, 0.8f),
                new Keyframe(0.8f, 1f),
                new Keyframe(1f, 0.7f)
            );
        }
    }

    void Update()
    {
        GetInput();
        HandleUI();
        HandleCamera();
        HandleGearShift();
    }

    void FixedUpdate()
    {
        HandleEngine();
        HandleSteering();
        HandleBrakes();
    }

    private void GetInput()
    {
        // Резкий отклик на ввод
        throttleInput = Mathf.MoveTowards(throttleInput, 
            Input.GetAxis("Vertical"), 
            Time.deltaTime * config.motorResponse);
        
        steeringInput = Mathf.MoveTowards(steeringInput,
            Input.GetAxis("Horizontal"),
            Time.deltaTime * config.steeringSpeed);
        
        isBraking = Input.GetKey(KeyCode.Space);
        isHandBraking = Input.GetKey(KeyCode.LeftShift);
    }

    private void HandleEngine()
    {
        // Рассчет RPM
        float speedKPH = rb.velocity.magnitude * 3.6f;
        currentRPM = Mathf.Lerp(currentRPM, 
            config.idleRPM + (config.shiftUpRPM - config.idleRPM) * 
            Mathf.Clamp01(speedKPH / config.maxSpeed) * 
            config.gearRatios[currentGear] * 0.8f, 
            Time.fixedDeltaTime * 10f);
        
        // Мгновенная остановка двигателя при отпускании газа
        if (Mathf.Abs(throttleInput) < 0.1f && !isBraking)
        {
            foreach (var wheel in wheels)
            {
                wheel.motorTorque = 0f;
            }
            // Тормоз двигателем
            rb.AddForce(-rb.velocity.normalized * config.engineBrake * rb.mass);
            return;
        }
        
        // Ограничение скорости
        if (speedKPH >= config.maxSpeed)
        {
            foreach (var wheel in wheels)
            {
                wheel.motorTorque = 0f;
            }
            return;
        }
        
        // Применение крутящего момента
        float torque = throttleInput * config.maxMotorTorque * 
                      config.torqueCurve.Evaluate(currentRPM / config.shiftUpRPM) * 
                      config.gearRatios[currentGear] * config.finalDriveRatio;
        
        for (int i = 0; i < wheels.Length; i++)
        {
            float torqueFactor = (i < 2) ? config.frontTorque : (1 - config.frontTorque);
            wheels[i].motorTorque = torque * torqueFactor / wheels.Length;
        }
    }

    private void HandleSteering()
    {
        // Учет скорости в угле поворота
        float speedFactor = Mathf.Clamp01(1 - rb.velocity.magnitude * config.steeringSpeedFactor / 50f);
        float steeringAngle = steeringInput * 
                            Mathf.Lerp(config.minSteeringAtSpeed, config.maxSteeringAngle, speedFactor);
        
        // Мгновенный поворот колес
        for (int i = 0; i < 2; i++) // Только передние колеса
        {
            wheels[i].steerAngle = steeringAngle;
        }
    }

    private void HandleBrakes()
    {
        float brakeTorque = isHandBraking ? config.handBrakeForce : config.brakeForce;
        
        // Резкое торможение
        if (isBraking)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                float brakeFactor = (i < 2) ? config.frontBrake : (1 - config.frontBrake);
                wheels[i].brakeTorque = brakeTorque * brakeFactor;
                wheels[i].motorTorque = 0f;
            }
            
            // Дополнительное торможение
            rb.AddForce(-rb.velocity.normalized * config.brakeResponse * rb.mass);
        }
        else
        {
            foreach (var wheel in wheels)
            {
                wheel.brakeTorque = 0f;
            }
        }
    }

    private void HandleGearShift()
    {
        if (Input.GetKeyDown(KeyCode.Q)) // Задняя передача
        {
            currentGear = 0;
            shiftTimer = 0.5f;
        }
        
        if (Input.GetKeyDown(KeyCode.E)) // Нейтраль
        {
            currentGear = 1;
            shiftTimer = 0.5f;
        }
        
        // Автоматическое переключение при движении вперед
        if (currentGear >= 1 && shiftTimer <= 0)
        {
            if (currentRPM > config.shiftUpRPM && currentGear < config.gearRatios.Length - 1)
            {
                currentGear++;
                shiftTimer = 0.3f;
            }
            else if (currentRPM < config.shiftDownRPM && currentGear > 1)
            {
                currentGear--;
                shiftTimer = 0.3f;
            }
        }
    }

    private void HandleCamera()
    {
        // Эффекты камеры
        float shake = Mathf.Clamp01(rb.velocity.magnitude / 50f) * cameraShake;
        
        // Толчок при старте
        if (throttleInput > 0.5f && rb.velocity.magnitude < 5f)
        {
            shake += 0.3f * cameraShake;
        }
        
        // Тряска при торможении
        if (isBraking && rb.velocity.magnitude > 10f)
        {
            shake += 0.2f * cameraShake;
        }
        
        carCamera.transform.localPosition = originalCameraPos + 
            new Vector3(
                Random.Range(-shake, shake),
                Random.Range(-shake, shake),
                0f
            );
    }

    private void HandleUI()
    {
        float speedKPH = rb.velocity.magnitude * 3.6f;
        
        if (speedText) speedText.text = $"{speedKPH:0}";
        if (rpmText) rpmText.text = $"{currentRPM:0}";
        
        if (gearText)
        {
            gearText.text = currentGear switch
            {
                0 => "R",
                1 => "N",
                _ => (currentGear - 1).ToString()
            };
        }
    }
}