using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour {

    public Camera cam;
    public UnityEngine.EventSystems.EventSystem events;

    public float camMoveSpeed = 1f; //how many screen-heights per second?
    public float zoomScrollSpeed = 1f; //how fast to increment/decrement zoom when scrolling?
    float baseCamSize; //Will be calculated. At min zoom (0), screen is mapHeight*1.1f units tall.
    public AnimationCurve camSizeCurve; //Curve for camera size percentage between zoom level 0 and 1.
    Vector2 camPosExtents;

    public float currZoom = 0;

    private void Awake()
    {
        WorldManager.OnWorldLoaded.AddListener(OnWorldLoaded);
        cam = GetComponent<Camera>();
    }
    private void OnWorldLoaded()
    {
        CalcCameraValues();
    }

    private void Update()
    {
        //No Shift, No Ctrl
        if (!Input.GetKey(KeyCode.LeftShift) && !(Input.GetKey(KeyCode.LeftControl) || Application.isEditor && Input.GetKey(KeyCode.BackQuote)))
        {
            if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.1f && !Input.GetKey(KeyCode.Tab) && !events.IsPointerOverGameObject())
            {
                SetZoom(currZoom + Input.mouseScrollDelta.y * zoomScrollSpeed);
            }
            if (Input.GetKey(KeyCode.W))
            {
                Vector3 pos = transform.position;
                pos.y = Mathf.Clamp(pos.y + camMoveSpeed * cam.orthographicSize*2f * Time.deltaTime, -camPosExtents.y, camPosExtents.y);
                transform.position = pos;
            }
            if (Input.GetKey(KeyCode.S))
            {
                Vector3 pos = transform.position;
                pos.y = Mathf.Clamp(pos.y - camMoveSpeed * cam.orthographicSize*2f * Time.deltaTime, -camPosExtents.y, camPosExtents.y);
                transform.position = pos;
            }
            if (Input.GetKey(KeyCode.A))
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x - camMoveSpeed * cam.orthographicSize*2f * Time.deltaTime, -camPosExtents.x, camPosExtents.x);
                transform.position = pos;
            }
            if (Input.GetKey(KeyCode.D))
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.Clamp(pos.x + camMoveSpeed * cam.orthographicSize*2f * Time.deltaTime, -camPosExtents.x, camPosExtents.x);
                transform.position = pos;
            }
        }
    }

    public void CalcCameraValues()
    {
        Vector2Int mapSize = WorldManager.instance.worldData.mapData.mapSize;
        baseCamSize = (mapSize.y / 2f)*1.1f;
        camPosExtents = new Vector2(mapSize.x/2f, mapSize.y/2f);
    }
    public void SetZoom(float zoom)
    {
        currZoom = Mathf.Clamp01(zoom);
        cam.orthographicSize = Mathf.Max(baseCamSize * camSizeCurve.Evaluate(currZoom), 2);
    }
}
