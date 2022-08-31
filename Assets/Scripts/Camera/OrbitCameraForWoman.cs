using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCameraForWoman : MonoBehaviour
{
    //被跟踪物体
    [SerializeField] Transform focus = default;

    //追踪半径
    [SerializeField, Min(0f)] float focusRadius = 1f;

    //相机离焦点距离
    [SerializeField, Range(1f, 20f)] float distance = 5f;

    //焦点居中
    [SerializeField, Range(0f, 1f)] float focusCentering = 0.5f;

    //旋转速度
    [SerializeField, Range(1f, 360f)] float rotationSpeed = 90f;

    //视觉角度
    [SerializeField, Range(-89f, 89f)] float minVerticalAngle = 10f, maxVerticalAngle = 89f;

    //相机自动对其延迟时间
    [SerializeField, Min(0f)] float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)] float alignSmoothRange = 45f;


    //相机轨道角
    Vector2 orbitAngles = new Vector2(45f, 0f);

    //滑动冲突
    public JoyStick joystick;
    private bool isTouching = false;

    Vector3 focusPoint, previousFocusPoint;

    //上次旋转相机时间
    float lastManualRotationTime;

    void Awake()
    {
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    private void Start()
    {
        if (joystick != null)
        {
            joystick.onJoystickDownEvent += OnJoystickDownEvent;
            joystick.onJoystickUpEvent += OnJoystickUpEvent;
            joystick.onJoystickDragEvent += OnJoystickDragEvent;
            joystick.onJoystickDragEndEvent += OnJoystickDragEndEvent;
        }
    }

    void LateUpdate()
    {
        // 根据聚焦半径更新目标位置 focusPoint
        UpdateFocusPoint();
        Quaternion lookRotation; // 四元数转化，代表三维空间每个角转化的角度 Quaternion.Euler(0,90,90)
        //旋转相机角度
        if (ManualRotation() || AutomaticRotation())
        {
            //限制角度
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }
        else
        {
            // 原始向量
            lookRotation = transform.localRotation;
        }

        //todo 方向的计算规则不清楚？？？
        Vector3 lookDirection = lookRotation * Vector3.forward;
        // 跳到目标位置后面
        Vector3 lookPosition = focusPoint - lookDirection * distance;
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    void ConstrainAngles()
    {
        orbitAngles.x =
            Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        }
        else if (orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

    bool ManualRotation()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Vertical Camera"),
            Input.GetAxis("Horizontal Camera")
        );
        const float e = 0.001f;
        if (input.x < e || input.x > e || input.y < e || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }

        return false;
    }

    /**
     * 是否自动改变了轨道
     */
    bool AutomaticRotation()
    {
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }

        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z
        );
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.0001f)
        {
            return false;
        }

        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        float rotationChange =
            rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }

        orbitAngles.y =
            Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    /**
     * 将2d方向转化为角度
     */
    static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle;
    }

    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        //目标位置
        Vector3 targetPoint = focus.position;
        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            // 焦点中心大小
            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }

            // 距离大于聚焦半径
            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }

            // a到b中间的一个位置，聚焦半径边缘
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }

    void OnDestroy()
    {
        if (joystick != null)
        {
            joystick.onJoystickDownEvent -= OnJoystickDownEvent;
            joystick.onJoystickUpEvent -= OnJoystickUpEvent;
            joystick.onJoystickDragEvent -= OnJoystickDragEvent;
            joystick.onJoystickDragEndEvent -= OnJoystickDragEndEvent;
        }
    }

    private void OnJoystickDownEvent(Vector2 obj)
    {
        isTouching = true;
    }

    private void OnJoystickUpEvent()
    {
        isTouching = false;
    }

    private void OnJoystickDragEvent(Vector2 obj)
    {
        isTouching = true;
    }


    private void OnJoystickDragEndEvent(Vector2 obj)
    {
        isTouching = false;
    }
}