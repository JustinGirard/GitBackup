using UnityEngine;

public class WSADControl : MonoBehaviour
{
    public float movementSpeed = 10f;  // Default movement speed
    public float fastSpeedMultiplier = 2f;  // Speed multiplier when holding Shift
    public float mouseSensitivity = 2f;  // Mouse sensitivity for rotation

    private float rotationX = 0f;
    private float rotationY = 0f;
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            LockCursor();
        }
    }

    void OnMouseDown()
    {
        LockCursor();
    }    

    void Start()
    {
        // Lock and hide cursor for immersive control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize rotation
        rotationX = transform.localEulerAngles.x;
        rotationY = transform.localEulerAngles.y;
    }

    void Update()
    {
        // Mouse Look
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);  // Prevent over-rotation

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        // Movement
        float moveSpeed = movementSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastSpeedMultiplier : 1f);
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += transform.forward;
        if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
        if (Input.GetKey(KeyCode.A)) direction -= transform.right;
        if (Input.GetKey(KeyCode.D)) direction += transform.right;
        if (Input.GetKey(KeyCode.Q)) direction -= transform.up;
        if (Input.GetKey(KeyCode.E)) direction += transform.up;

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;

        // Exit Play Mode with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Debug.Log("Debug Camera Control Disabled");
        }
    }
}
