using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTouchMgr : MonoBehaviour
{
    public float curRotSpeed = 0.3f;
    private Touch curTouch;
    private Pose curPose;
    private RaycastHit curRaycastHit;
    private Vector3 curDeltaPosition;

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            curTouch = Input.GetTouch(0);
            if (curTouch.phase == TouchPhase.Moved)
            {
                OnTouchMoved();
            }
        }
    }
    
    // 드래그 해서 움직이게 할 경우의 처리
    private void OnTouchMoved()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out curRaycastHit, Mathf.Infinity, 1 << 7))
        {
            curDeltaPosition = curTouch.deltaPosition;
            transform.Rotate(transform.up, curDeltaPosition.x * -1.0f * curRotSpeed);
        }
    }
}