﻿using UnityEngine;
using System.Collections;
using System;

public class MoveController : MonoBehaviour
{
    public JoyStick joystick;
    bool isRun;
    float h, v;
    Vector3 moveVec;
    Animator am;

    //换装
    public SkinnedMeshRenderer skinBody;
    public SkinnedMeshRenderer eye;
    public SkinnedMeshRenderer face;
    public SkinnedMeshRenderer hair;
    public SkinnedMeshRenderer hand;
    public SkinnedMeshRenderer pouch;
    public Material eyeball1;
    public Material skin1;
    public Material ware1;
    public Material eyeball2;
    public Material skin2;
    public Material ware2;
    public Material eyeball3;
    public Material skin3;
    public Material ware3;

    //最大速度
    [SerializeField, Range(0, 100f)] float maxSpeed = 10f;

    //最大加速度、空气中加速度
    [SerializeField, Range(0f, 100f)] float maxAcceleration = 10f, maxAirAcceleration = 1f;

    //跳跃高度
    [SerializeField, Range(0f, 10f)] float jumpHeight = 2f;

    //空中跳跃
    [SerializeField, Range(0, 5)] int maxAirJumps = 0;

    //最大地面角度
    [SerializeField, Range(0f, 90f)] float maxGroundAngle = 25f;


    //速度
    private Vector3 velocity;
    int groundContactCount;
    private int skinIndex = 1;

    bool OnGround => groundContactCount > 0;

    Rigidbody body;

    //跳跃阶段
    int jumpPhase;

    //最小地面点积
    float minGroundDotProduct;

    //期望速度
    private Vector3 desiredVelocity;

    //跳跃
    bool desiredJump;
    Vector3 contactNormal;
    // 相机航向角
    private float _orbitAngles = 0;

    void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    void Start()
    {
        //开启多点触控
        Input.multiTouchEnabled = true;
        
        am = transform.GetComponent<Animator>();

        joystick.onJoystickDownEvent += OnJoystickDownEvent;
        joystick.onJoystickUpEvent += OnJoystickUpEvent;
        joystick.onJoystickDragEvent += OnJoystickDragEvent;
        joystick.onJoystickDragEndEvent += OnJoystickDragEndEvent;
    }

    void Update()
    {

        // if (Input.touchCount == 2)
        // {
        //     //双指跳跃
        //     if (Input.touches[1].phase == TouchPhase.Began)
        //     {
        //         desiredJump = true;
        //     }
        //     
        // }
        // if (Input.touchCount == 3)
        // {
        //     //三指换装
        //     if (Input.touches[2].phase == TouchPhase.Began)
        //     {
        //         ChangeSkinToIndex(skinIndex++%3+1);
        //     }
        // }

        if (skinBody != null)
        {
            ChangeSkin();
        }
        am.SetBool("run", isRun);

        desiredJump |= Input.GetButtonDown("Jump");

        if (isRun && (h != 0 || v != 0))
        { //运动角度(相对于世界坐标) - 减去相机相对世界坐标的偏移角
            var atan2 = Mathf.Atan2(v, h) * Mathf.Rad2Deg;
            float angle = atan2 -_orbitAngles;
            Vector2 playerInput;
            angle *= Mathf.Deg2Rad;
            playerInput.y = Mathf.Sin(angle);
            playerInput.x = Mathf.Cos(angle);
            // playerInput.x = h;
            // playerInput.y = v;
            
            // playerInput.x = Input.GetAxis("Horizontal");
            // playerInput.y = Input.GetAxis("Vertical");
            // ClampMagnitude：返回单位向量
            playerInput = Vector2.ClampMagnitude(playerInput, 1f);

            //定义期望速度
            desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
            moveVec = new Vector3(playerInput.x, 0f, playerInput.y).normalized;
            
            RotatePlayer();
        }
        else
        {
            //定义期望速度
            desiredVelocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        //更新速度、跳跃状态
        UpdateState();
        //调整速度
        AdjustVelocity();
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.velocity = velocity;
        ClearState();
    }

    void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    void UpdateState()
    {
        velocity = body.velocity;
        if (OnGround)
        {
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize();
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            float alignedSpeed = Vector3.Dot(velocity, contactNormal);
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }

            velocity += contactNormal * jumpSpeed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }
    
    public void OnSkinChangeBtnClick()
    {
        ChangeSkinToIndex(skinIndex++%3+1);
    }
    
    public void OnJumpBtnClick()
    {
        desiredJump = true;
    }

    void AdjustVelocity()
    {
        //normalized：返回大小为1的词此向量
        //方向向量？
        Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;
        //x方向的速度
        float currentX = Vector3.Dot(velocity, xAxis);
        float currentZ = Vector3.Dot(velocity, zAxis);
        //最大加速度
        float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        //最大速度
        float maxSpeedChange = acceleration * Time.deltaTime;

        //MoveTowards：将a变为b，最大不可超过c
        float newX =
            Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        float newZ =
            Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);
        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    private void RotatePlayer()
    {
        //向量v围绕y轴旋转cameraAngle.y度
        //向量旋转到正前方
        Vector3 vec = Quaternion.Euler(0, 0, 0) * moveVec;
        // Vector3 vec = moveVec;
        if (vec == Vector3.zero)
            return;
        //人物看向那个方向
        Quaternion look = Quaternion.LookRotation(vec);
        transform.rotation = Quaternion.Lerp(transform.rotation, look, Time.deltaTime * 100);
    }

    private void ChangeSkin()
    {
        // 换肤
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeSkinToIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeSkinToIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeSkinToIndex(3);
        }
    }

    private void ChangeSkinToIndex(int index)
    {
        switch (index)
        {
            case 1:
                skinBody.material = ware1;
                eye.material = eyeball1;
                face.material = skin1;
                hair.material = skin1;
                hand.material = ware1;
                pouch.material = ware1;
                break;
            case 2:
                skinBody.material = ware2;
                eye.material = eyeball2;
                face.material = skin2;
                hair.material = skin2;
                hand.material = ware2;
                pouch.material = ware2;
                break;
            case 3:
                skinBody.material = ware3;
                eye.material = eyeball3;
                face.material = skin3;
                hair.material = skin3;
                hand.material = ware3;
                pouch.material = ware3;
                break;
            
        }
    
    }

    void OnDestroy()
    {
        joystick.onJoystickDownEvent -= OnJoystickDownEvent;
        joystick.onJoystickUpEvent -= OnJoystickUpEvent;
        joystick.onJoystickDragEvent -= OnJoystickDragEvent;
        joystick.onJoystickDragEndEvent -= OnJoystickDragEndEvent;
    }

    private void OnJoystickUpEvent()
    {
        //停止移动
        isRun = false;
        h = 0;
        v = 0;

        // moveVec = new Vector3(h, 0, v).normalized;
    }

    /// <summary>
    /// 按下
    /// </summary>
    /// <param name="obj"></param>
    private void OnJoystickDownEvent(Vector2 obj)
    {
        //停止移动
        isRun = false;
        h = 0;
        v = 0;

        // moveVec = new Vector3(h, 0, v).normalized;
        if (Camera.main != null)
        {
            _orbitAngles = Camera.main.transform.eulerAngles.y;
            if (_orbitAngles > 180)
            {
                _orbitAngles = -(360 - _orbitAngles);
            }
        }
    }

    /// <summary>
    /// 传入一个方向 向量
    /// </summary>
    /// <param name="obj"></param>
    private void OnJoystickDragEvent(Vector2 obj)
    {
        //开始移动
        isRun = true;
        h = obj.x;
        v = obj.y;

        // moveVec = new Vector3(h, 0, v).normalized;
    }

    /// <summary>
    /// 拖动结束
    /// </summary>
    /// <param name="obj"></param>
    private void OnJoystickDragEndEvent(Vector2 obj)
    {
    }
}