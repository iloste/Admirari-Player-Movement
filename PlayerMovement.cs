using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance = null;

    Rigidbody rb;

    // walking/running
    float shieldSpeed = 1.5f;
    float walkSpeed = 4;
    float runSpeed = 4;
    float jumpSpeed = 4;
    float currentSpeed;
    Vector3 startPosition;
    Vector3 knockedBack;

    // jumping
    int jumpAcceleration = 9;
    int fallAcceleration = 20;
    bool jumpApex;
    Vector3 jumpVelocity;
    int groundLayer = 1 << 8;
    private bool jumpVelocitySet;
    float fallDistance;
    float jumpHeight;
    float landHeight;
    float coyoteTimer;
    float coyoteTime = 0.25f;

    // crouched
    [Tooltip("contains the standing collider and the crouched collider")]
    [SerializeField] CapsuleCollider[] colliders;
    GameObject cameraGO;
    private bool hitCeiling;


    bool takenLavaDamage;
    

    int lavaLayer = 1 << 13;
    public float GroundedY { get; private set; }
    public bool Crouched { get; private set; }
    public bool Jumped { get; private set; }
    public bool Grounded { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
       
    }


    void Start()
    {
        PlayerMovement.instance.transform.parent = null;
        cameraGO = transform.GetChild(0).gameObject;
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;
        startPosition = this.transform.position;
        knockedBack = Vector3.zero;
        SetPosition(0); // calling method for when "respawning" to put him in the correct position
    }


    void Update()
    {
        DetectGround();
        JumpInput();
        SpeedInput();
        //CrouchInput();

        if (Grounded)
        {
            GroundedY = transform.position.y;
        }
    }


    private void FixedUpdate()
    {
        MovementInput();
        rb.velocity = Vector3.zero;
    }


    private void DetectGround()
    {
        // check for ground
        if (Physics.CheckSphere(transform.position - new Vector3(0, 0.5f, 0), 0.1f, groundLayer))
        {
            Grounded = true;
            takenLavaDamage = false;
        }
        // check for lava
        else if (Physics.CheckSphere(transform.position - new Vector3(0, 0.5f, 0), 0.1f, lavaLayer))
        {
            Jumped = true;
            Grounded = true;
            coyoteTimer = 0;

            if (!takenLavaDamage)
            {
                takenLavaDamage = true;
                GetComponent<hsPlayer>().TakeDamageNoKnockback(1, 0, true); 
            }
        }
        // is in the air
        else
        {
            Grounded = false;
            takenLavaDamage = false;
        }
    }


    private void JumpInput()
    {
        // This works by setting the initial y velocity to jumpAcceleration, then decreases it by fallAcceleration*time.delta time, thus slowing the y velocity to 0, 
        // then going into the negatives until you hit the ground.

        if (!Crouched)
        {
            if (Grounded)
            // if (coyoteTimer > 0)
            {
                if (Input .GetKeyDown(KeyCode.Space))
                {
                    Jumped = true;
                    coyoteTimer = 0;
                }

                // if player has jumped, fallen, and landed, reset variables. 
                if (Jumped && jumpApex)
                {
                    FindObjectOfType<AudioManager>().Play("Landing");
                    jumpVelocity = Vector3.zero;
                    Jumped = false;
                    jumpVelocitySet = false;
                    jumpApex = false;
                    hitCeiling = false;
                    landHeight = transform.position.y;
                    fallDistance = jumpHeight - landHeight;
                    coyoteTimer = coyoteTime;

                    if (fallDistance > 3f)
                    {
                        if (fallDistance < 4f)
                        {
                            CameraShake.instance.IncreaseTrauma(1f);
                        }
                        else if (fallDistance < 5f)
                        {
                            CameraShake.instance.IncreaseTrauma(1.2f);
                        }
                        else
                        {
                            CameraShake.instance.IncreaseTrauma(1.5f);
                        }
                    }

                    jumpHeight = 0;
                    landHeight = 0;
                    fallDistance = 0;
                    coyoteTimer = coyoteTime;
                    

                }
            }

            if (!Grounded)
            {
                coyoteTimer -= Time.deltaTime;
            }

            if (Jumped)
            {
                
                // set jump velocity (moves player upwards)
                if (!jumpVelocitySet)
                {
                    
                    jumpVelocity = new Vector3(0, jumpAcceleration, 0);
                    jumpVelocitySet = true;
                    

                }
                else
                {
                    // decrease jump velocity until it has reach its terminal velocity (this happens until the player is grounded)
                    if (jumpVelocity.y > -fallAcceleration)
                    {
                        // if you hit the under side of a platform whilst jumping, make player fall
                        if (!hitCeiling && Physics.CheckSphere(transform.position + new Vector3(0, 0.5f, 0), 0.1f, groundLayer))
                        {
                            jumpVelocity = Vector3.zero;
                            hitCeiling = true;
                        }

                        jumpVelocity -= new Vector3(0, fallAcceleration * Time.deltaTime, 0);

                        
                    }

                    // player is now falling
                    if (jumpVelocity.y <= 0)
                    {
                        if (jumpHeight == 0)
                        {
                            jumpHeight = transform.position.y;
                        }
                        fallDistance += Time.deltaTime;
                        jumpApex = true;
                    }
                }
            }

            if (!Grounded && coyoteTimer > 0)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    
                    Jumped = true;
                    jumpVelocitySet = false;
                    coyoteTimer = 0;
                    

                }
            }

            // if player fell off ledge
            if (!Grounded && !Jumped)
            {
                Jumped = true;
                jumpVelocitySet = true;
            }
        }
    }


    private void SpeedInput()
    {
        if (Grounded)
        {
            if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Mouse1))
            {
                currentSpeed = runSpeed;
            }
            else if (Input.GetKey(KeyCode.Mouse1))
            {
                currentSpeed = shieldSpeed;
            }
            else
            {
                currentSpeed = walkSpeed;
            }
        }
        else
        {
            if (currentSpeed > walkSpeed)
            {
                currentSpeed = jumpSpeed;
            }
        }
    }



    private void MovementInput()
    {
        if (!Crouched)
        {
            Vector3 newPos = Vector3.zero;

            if (knockedBack.x != 0)
            {
                if (Physics.Raycast(transform.position, -knockedBack, 2f))
                {
                    knockedBack = Vector3.zero;
                }

                if (knockedBack.x < -0.2f)
                {
                    knockedBack.x += Time.deltaTime;
                }
                else if (knockedBack.x > 0.2f)
                {
                    knockedBack.x -= Time.deltaTime;
                }
                else
                {
                    knockedBack = Vector3.zero;
                }

                newPos = transform.position - knockedBack;

            }
            else
            {
                // get direction player is moving in
                Vector3 dirVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * 5;
                dirVector = transform.TransformDirection(dirVector).normalized;

                // get desired location for next frame
                newPos = transform.position + (dirVector * currentSpeed * Time.deltaTime);
               // newPos = new Vector3(newPos.x, newPos.y, Mathf.Clamp(newPos.z, startPosition.z - 7f, startPosition.z + 7f));
                newPos += jumpVelocity * Time.deltaTime;
            }

            // move to desired location
            rb.MovePosition(newPos);
            

        }
    }


    public void SetPosition(int conntrollerINT) // controller int made for the script to know if moving to next/prev level or restarting the level (death)in a checkpoint or at the begining
    {
       

        Vector3 newPos;
        if (conntrollerINT == 0) // spawn at the begining of a level
        {
            newPos = SceneTransition.instance.beginPos.position;
            transform.position = newPos + new Vector3(2, 1, 0);
            transform.eulerAngles = new Vector3(0, 90, 0);
        }
        else if (conntrollerINT == 1) // spawn at the end of a level
        {
            newPos = SceneTransition.instance.endPos.position;
            transform.position = newPos - new Vector3(2, 1, 0);
            transform.eulerAngles = new Vector3(0, -90, 0);
        }
        else if (conntrollerINT == 2) // respawning in the same level After death / in the begining or at a checkpoint 
        {

            if ( SceneTransition.instance.savePoint != null  ) //  only called if a checkpoint has been reached in the level
            {
                newPos = SceneTransition.instance.savePoint.position;
                transform.position = newPos; // + new Vector3(2, 0, 0);
                transform.eulerAngles = new Vector3(0, 90, 0); 
            }
            else // need to add an if to this so it will respawn on the endPos of a level if player is going back
            {
                newPos = SceneTransition.instance.beginPos.position;
                transform.position = newPos + new Vector3(2, 0, 0);
                transform.eulerAngles = new Vector3(0, 90, 0);
            }
        }
    }

    public void KnockBack()
    {
        knockedBack = transform.forward * 0.4f;
    }
    public void GoBackToNotDestroyOnLoad()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void KillMe()
    {
        Destroy(this.gameObject);
    }

}
