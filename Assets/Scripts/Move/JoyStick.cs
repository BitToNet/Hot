using UnityEngine;
using System.Collections;
using System;//新增命名空间 
using UnityEngine.EventSystems;//新增

public class JoyStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
{
    public float outerCircleRadius = 50;//可以拖动的最大距离

    Transform thumb;//触摸球

    Vector2 thumb_start;

    Vector2 direction;//滑动方向

    public Action<Vector2> onJoystickDownEvent;     // 按下事件
    public Action onJoystickUpEvent;               // 抬起事件
    public Action<Vector2> onJoystickDragEvent;     // 滑动事件
    public Action<Vector2> onJoystickDragEndEvent;  // 滑动结束事件

    void Start()
    {
        thumb = transform.Find("Thumb");
        thumb_start = transform.position;
        //初始化起始位置为陀螺仪的位置  下边利用减法计算相对位置
    }

    /// <summary>
    /// 按下
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("按下");

        //thumb.gameObject.SetActive(true);
        thumb.transform.position = eventData.position;

        if (onJoystickDownEvent != null)
            onJoystickDownEvent(eventData.position);

    }

    /// <summary>
    /// 抬起
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        thumb.transform.localPosition = Vector3.zero;
        //鼠标松开后 内圆 回到原点

        //thumb.gameObject.SetActive(false);

        if (onJoystickUpEvent != null)
        {
            Debug.Log("抬起");
            onJoystickUpEvent();
        }
    }

    /// <summary>
    /// 拖动事件
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPos = eventData.position - thumb_start;
        direction = touchPos;
        if (Vector3.Distance(touchPos, Vector2.zero) < outerCircleRadius)
            thumb.transform.localPosition = touchPos;
        else
        {
            thumb.transform.localPosition = touchPos.normalized * outerCircleRadius;
        }

        if (onJoystickDragEvent != null)
            onJoystickDragEvent(direction);

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        direction= eventData.position - thumb_start;
        if (onJoystickDragEndEvent != null)
            onJoystickDragEndEvent(direction);
        Debug.Log("结束拖动");
    }
}