using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerNetwork : MonoBehaviourPun
{
    CinemachineFreeLook cinemachineFreeLook;

    public bool isControlled = true;

    // joystick
    Joystick joystick;
    JoyButton joyButton;

    public float turnSmoothTime = 0.1f;
    public float movementSpeed = 4f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float groundDistance = 0.25f;
    public float maxFallZone = -100f;

    public LayerMask groundMask;

    public GameObject groundCheck;

    CharacterController characterController;

    Animator animator;

    GameObject cam;

    Vector3 move;
    Vector3 velocity;

    float turnSmoothVelocity;
    float canJump = 0f;
    float horizontal;
    float vertical;

    bool isGrounded;
    bool isRunning;

    [HideInInspector]
    public float currentTransformY;

    void Awake()
    {
        if (photonView.IsMine == false && isControlled == true)
        {
            isControlled = false;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (isControlled)
        {
            cinemachineFreeLook = FindObjectOfType<CinemachineFreeLook>();
            cinemachineFreeLook.Follow = gameObject.transform;
            cinemachineFreeLook.LookAt = gameObject.transform;

            cam = GameObject.FindGameObjectWithTag("MainCamera");

            joystick = FindObjectOfType<Joystick>();
            joyButton = FindObjectOfType<JoyButton>();
        }

        characterController = GetComponent<CharacterController>();

        animator = GetComponentInChildren<Animator>();

        characterController.enabled = false;
        characterController.transform.position = Vector3.up;
        characterController.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.x != 0 || transform.position.y != 0 || transform.position.z != 0)
        {
            isRunning = true;
        }

        if (isControlled)
        {
            // if fall
            if (characterController.transform.position.y < maxFallZone)
            {
                characterController.enabled = false;
                characterController.transform.position = SpawnPoint.FindObjectOfType<SpawnPoint>().transform.position;
                characterController.enabled = true;
            }

            currentTransformY = GetComponent<Transform>().transform.eulerAngles.y;
            horizontal = Input.GetAxisRaw("Horizontal") + joystick.Horizontal;
            vertical = Input.GetAxisRaw("Vertical") + joystick.Vertical;

            isGrounded = Physics.CheckSphere(groundCheck.transform.position, groundDistance, groundMask);

            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            move = new Vector3(horizontal, 0f, vertical).normalized;

            // movement
            if (move.magnitude >= 0.1f)
            {
                float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;

                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                characterController.Move(moveDirection.normalized * movementSpeed * Time.deltaTime);
            }

            // bool run animator
            bool hasHorizontalInput = !Mathf.Approximately(horizontal, 0f);
            bool hasVerticalInput = !Mathf.Approximately(vertical, 0f);
            isRunning = hasHorizontalInput || hasVerticalInput;

            // animator
            animator.SetBool("IsRunning", isRunning);
            animator.SetBool("IsGrounded", isGrounded);

            // jump 
            if ((Input.GetKey(KeyCode.Space) || joyButton.pressed) && isGrounded && Time.time > canJump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                canJump = Time.time + 1f;
            }

            velocity.y += gravity * Time.deltaTime;

            characterController.Move(velocity * Time.deltaTime);
        }
    }

    public static void RefreshInstance(ref PlayerControllerNetwork playerControllerNetwork, PlayerControllerNetwork prefab)
    {
        var position = Vector3.zero;
        var rotation = Quaternion.identity;

        if (playerControllerNetwork != null)
        {
            position = playerControllerNetwork.transform.position;
            rotation = playerControllerNetwork.transform.rotation;

            PhotonNetwork.Destroy(playerControllerNetwork.gameObject);
        }

        playerControllerNetwork = PhotonNetwork.Instantiate(prefab.gameObject.name, position, rotation).GetComponent<PlayerControllerNetwork>();
    }
}
