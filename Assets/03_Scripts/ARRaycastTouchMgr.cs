using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARRaycastTouchMgr : MonoBehaviour
{
    [SerializeField] private GameObject[] objs;
    [SerializeField] private float curRotSpeed = 0.3f;
    
    private GameObject _createdObject;
    private readonly List<ARRaycastHit> _rayCastHitList = new();
    private ARRaycastManager _raycastMgr;
    private Pose _curPose;
    
    private enum TouchState { None, Tap, Swipe }
    private TouchState _currentState = TouchState.None;

    private Vector2 _touchStartPos;
    private const float SwipeThreshold = 50f;

    private Vector2 _previousTouchPos;

    private void Start()
    {
        _raycastMgr = GetComponent<ARRaycastManager>();
        _raycastMgr.raycastPrefab = objs[0];
    }

    private void Update()
    {
        if (Input.touchCount == 0)
        {
            _currentState = TouchState.None;
            return;
        }

        var touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _touchStartPos = touch.position;
                _previousTouchPos = touch.position;
                _currentState = TouchState.None;
                break;
            case TouchPhase.Moved:
                float distance = Vector2.Distance(touch.position, _touchStartPos);
                if (distance > SwipeThreshold)
                {
                    if (_currentState != TouchState.Swipe)
                        _currentState = TouchState.Swipe;
                    else
                    {
                        Vector2 currentTouchPos = touch.position;
                        Vector2 deltaPosition = currentTouchPos - _previousTouchPos;
                        _previousTouchPos = currentTouchPos;

                        RotateObject(deltaPosition);
                    }
                }
                break;
            case TouchPhase.Ended:
                if (_currentState == TouchState.None)
                {
                    _currentState = TouchState.Tap;
                    OnTap(touch.position);
                }
                break;
            case TouchPhase.Canceled:
                _currentState = TouchState.None;
                break;
        }
    }

    // 탭 시 객체 이동 처리
    private void OnTap(Vector2 touchPosition)
    {
        if (_raycastMgr.Raycast(touchPosition, _rayCastHitList, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            _curPose = _rayCastHitList[0].pose;
            if (_createdObject == null)
            {
                _createdObject = Instantiate(_raycastMgr.raycastPrefab, _curPose.position, _curPose.rotation);
            }
            else
            {
                _createdObject.transform.position = _curPose.position;
            }
        }
    }

    private void RotateObject(Vector2 deltaPosition)
    {
        if (_createdObject != null)
        {
            _createdObject.transform.Rotate(_createdObject.transform.up, deltaPosition.x * -1.0f * curRotSpeed, Space.World);
        }
    }
}
