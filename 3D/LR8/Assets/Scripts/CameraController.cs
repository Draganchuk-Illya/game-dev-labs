using UnityEngine;


public class CameraController : MonoBehaviour
{
    public float moveSpeedXZ = 20f;
    public float moveSpeedY = 10f;
    public float rotationSpeed = 5f;

    private float yaw = 0f;
    private float pitch = 0f;
    
    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        Vector3 moveXZ = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        moveXZ = transform.TransformDirection(moveXZ);
        transform.position += moveXZ * moveSpeedXZ * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            transform.position += Vector3.up * scroll * moveSpeedY;
        }
    }
}

