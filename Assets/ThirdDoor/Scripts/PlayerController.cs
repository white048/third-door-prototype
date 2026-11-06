using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float mouseSensitivity = 2.2f;
    [SerializeField] private float gravity = -18f;

    private float cameraPitch;
    private float verticalVelocity;
    private bool controlsEnabled = true;
    private Vector3 planarVelocity;
    public float Sensitivity
    {
        get => mouseSensitivity;
        set { mouseSensitivity = Mathf.Clamp(value, .5f, 5f); if(!EndingArchive.IsValidationRun){PlayerPrefs.SetFloat("ThirdDoor_Sensitivity", mouseSensitivity);PlayerPrefs.Save();} }
    }

    public Camera PlayerCamera => playerCamera;
    public bool ControlsEnabled => controlsEnabled;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    private void Awake()
    {
        mouseSensitivity = EndingArchive.IsValidationRun?2.2f:Mathf.Clamp(PlayerPrefs.GetFloat("ThirdDoor_Sensitivity", 2.2f),.5f,5f);
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Start()
    {
        // Only lock the cursor if controls are currently enabled. The intro / language picker
        // disables controls via SetControlsEnabled(false) before Start fires; unconditional lock
        // here would hide the cursor right when the picker needs the player to click.
        if (controlsEnabled)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        if (!controlsEnabled || ExhibitionHUD.Paused || !Application.isFocused)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }

        Look();
        Move();
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled) planarVelocity = Vector3.zero;

        if (enabled)
        {
            LockCursor();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -78f, 78f);
        playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = (transform.right * horizontal + transform.forward * vertical).normalized;
        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 1.55f : moveSpeed;
        planarVelocity = Vector3.MoveTowards(planarVelocity, input * speed, Time.deltaTime * 18f);
        Vector3 velocity = planarVelocity;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else if (controlsEnabled && !ExhibitionHUD.Paused) LockCursor();
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
