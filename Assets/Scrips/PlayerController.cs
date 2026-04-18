using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Iniciable variables")]
    [SerializeField] private Transform mPitchController;
    [SerializeField] private Camera playerCamera;

    [Header("Direction variables")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private bool InvertPitch;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float jumpSpeed = 5.0f;

    [Header("Agacharse")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchSpeed = 8f;
    [SerializeField] private float standingCameraY = 0.8f;
    [SerializeField] private float crouchingCameraY = 0.4f;
    [SerializeField] private LayerMask ceilingMask = ~0;

    private float mYaw;
    private float mPitch;

    private Vector2 mDirection;
    private Vector2 mLookDirection;
    private float mVerticalSpeed;
    private bool isSprinting;
    private bool isCrouching;
    private CharacterController mController;

    void Start()
    {
        mController = GetComponent<CharacterController>();

        mYaw = transform.eulerAngles.y;
        mPitch = mPitchController.localEulerAngles.x;
        if (mPitch > 180f) mPitch -= 360f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ApplyControllerHeight(standingHeight);

        standingCameraY = standingHeight * 0.75f;
        crouchingCameraY = crouchingHeight * 0.75f;

        Vector3 camPos = mPitchController.localPosition;
        camPos.y = standingCameraY;
        mPitchController.localPosition = camPos;
    }

    void Update()
    {
        mYaw += mLookDirection.x * rotationSpeed * Time.deltaTime;
        mPitch -= mLookDirection.y * rotationSpeed * Time.deltaTime;

        mPitch = Mathf.Clamp(mPitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0.0f, mYaw, 0.0f);
        mPitchController.localRotation = Quaternion.Euler(mPitch * (InvertPitch ? -1 : 1), 0.0f, 0.0f);

        HandleCrouch();

        float speed = maxSpeed;
        if (isSprinting && !isCrouching)
            speed *= sprintMultiplier;

        Vector3 finalDirection = (transform.forward * mDirection.y + transform.right * mDirection.x) * speed * Time.deltaTime;

        mController.Move(finalDirection);
    }

    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;

        if (!isCrouching && !CanStandUp())
        {
            targetHeight = crouchingHeight;
        }

        float currentHeight = Mathf.Lerp(mController.height, targetHeight, crouchSpeed * Time.deltaTime);
        ApplyControllerHeight(currentHeight);

        float targetCameraY = isCrouching ? crouchingCameraY : standingCameraY;
        if (!isCrouching && !CanStandUp())
            targetCameraY = crouchingCameraY;

        Vector3 camPos = mPitchController.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchSpeed * Time.deltaTime);
        mPitchController.localPosition = camPos;
    }

    private bool CanStandUp()
    {
        float radius = mController.radius * 0.95f;

        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (standingHeight - radius);

        return !Physics.CheckCapsule(bottom, top, radius, ceilingMask, QueryTriggerInteraction.Ignore);
    }

    private void ApplyControllerHeight(float newHeight)
    {
        mController.height = newHeight;

        Vector3 center = mController.center;
        center.y = newHeight * 0.5f;
        mController.center = center;
    }

    public void OnMove(InputAction.CallbackContext c)
    {
        if (c.performed || c.canceled)
            mDirection = c.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext c)
    {
        if (c.performed || c.canceled)
            mLookDirection = c.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext c)
    {
        if (c.started) isSprinting = true;
        if (c.canceled) isSprinting = false;
    }

    public void OnCrouch(InputAction.CallbackContext c)
    {
        Debug.Log("OnCrouch llamado: " + c.phase);

        if (c.started) isCrouching = true;
        if (c.canceled) isCrouching = false;
    }
}