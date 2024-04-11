using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Taken from https://github.com/Acacia-Developer/Unity-FPS-Controller/blob/master/Assets/Script/PlayerController.cs
    [SerializeField] Transform playerCamera = null;
    [SerializeField] float mouseSensitivity = 7.5f;
    [SerializeField] float walkSpeed = 3.0f;
    [SerializeField] float gravity = -13.0f;
    [SerializeField] [Range(0.0f, 0.5f)] float moveSmoothTime = 0.3f;
    [SerializeField] [Range(0.0f, 0.5f)] float mouseSmoothTime = 0.03f;

    [SerializeField] bool lockCursor = true;

    float cameraPitch = 0.0f;
    float velocityY = 0.0f;
    CharacterController controller = null;

    Vector2 currentDir = Vector2.zero;
    Vector2 currentDirVelocity = Vector2.zero;

    Vector2 currentMouseDelta = Vector2.zero;
    Vector2 currentMouseDeltaVelocity = Vector2.zero;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMouseLook();
        UpdateMovement();
    }

    void UpdateMouseLook()
    {
        float rotateInputX = 0.0f;
        if (Input.GetKey(KeyCode.Q))
        {
            rotateInputX = -1.0f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            rotateInputX = 1.0f;
        }

        float rotateInputY = 0.0f;
        if (Input.GetKey(KeyCode.Z))
        {
            rotateInputY = -1.0f;
        }
        else if (Input.GetKey(KeyCode.C))
        {
            rotateInputY = 1.0f;
        }

        Vector3 rotation = transform.eulerAngles;
        rotation.x += rotateInputX * 20f * Time.deltaTime;
        rotation.y += rotateInputY * 30f * Time.deltaTime;
        transform.eulerAngles = rotation;
    }

    void UpdateMovement()
    {
        Vector2 targetDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        targetDir.Normalize();

        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentDirVelocity, moveSmoothTime);

        if (controller.isGrounded)
            velocityY = 0.0f;

        velocityY += gravity * Time.deltaTime;

        Vector3 velocity = (transform.forward * currentDir.y + transform.right * currentDir.x) * walkSpeed + Vector3.up * velocityY;

        controller.Move(velocity * Time.deltaTime);

    }
}