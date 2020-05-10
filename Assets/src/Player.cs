using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    GameObject world;
    World worldScript;

    GameObject cam;
    Vector3 cameraVelocity = Vector3.zero;
    public float camSmoothTime = 0.15f;
    Resolution resolution;
    public float cameraYOffset = 3;

    public float playerMoveSpeed = 10f;
    public bool isGrounded = false;
    public Vector2 velocity;
    public float moveSmoothTime = 0.1f;
    public float horizontalVelocity, horizontalVelocityRef;
    public float gravity = 9.81f;
    public float maxSlopValue = 70f;
    public float jumpHeight = 100;
    Vector2 prevPos;


    public float dashSpeed, dashDuration = 0.1f, dashDurationCount = 10f;
    public bool dashTrigger = false;
    public AnimationCurve dashCurve;

    public Transform feetTransform;
    public float stepDistance;
    public float stickToGroundDistance;

    public Animator anim;
    public GameObject spriteHolder;
    Rigidbody2D rb;
    public Vector2 prevTrackingTransform;
    public Transform trackingTransform;

    public LayerMask ignorePlayerMask;

    public GameObject followingLight;

    void Start()
    {
        cam = Camera.main.gameObject;
        resolution = Screen.currentResolution;
        world = GameObject.Find("World");
        worldScript = world.GetComponent<World>();
        worldScript.player = this.gameObject;
        rb = this.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        CheckForJump();
        CheckForDash();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float horizontalInput = GetHorizontalInput();
        PlayerMovement(horizontalInput);
        CameraControl();
        LandingCheck();
        FollowingLight();
    }

    float GetHorizontalInput()
    {
        float rawHorizontalInput = Input.GetAxis("Horizontal");
        return rawHorizontalInput;
    }

    void CheckForJump()
    {
        if (Input.GetButton("Jump"))
        {
            if (isGrounded)
            {
                anim.SetTrigger("Jump");
                anim.SetBool("Grounded", false);
                velocity.y = jumpHeight;
                isGrounded = false;
            }
        }
    }

    void CheckForDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            dashTrigger = true;
        }
    }

    void LandingCheck()
    {
        if (velocity.y < 0)
        {
            Debug.DrawRay(feetTransform.position, Vector2.down * Time.fixedDeltaTime * -velocity.y * 500, Color.white);
            RaycastHit2D landingRayCheck = Physics2D.Raycast(feetTransform.position, Vector2.down, Time.fixedDeltaTime * -velocity.y * 500);
            if (landingRayCheck) //checks from inside the player to the ground handles steps
            {
                anim.SetBool("Landing", true);
                Debug.Log("Landing");
            }
        }
    }

    void PlayerMovement(float _horizontalInput)
    {

        float dashSpeedAtCount = 0;
        if (dashTrigger)
        {
            anim.SetBool("Dash", true);
            dashTrigger = false;
            dashDurationCount = 0;
        }
        bool isInDash = false;
        if (dashDurationCount < dashDuration)
        {
            isInDash = true;
            dashDurationCount += Time.fixedDeltaTime;
            dashSpeedAtCount = dashCurve.Evaluate(dashDurationCount / dashDuration);
            velocity.y = 0;
        }
        else
        {
            anim.SetBool("Dash", false);
        }


        //horizontalVelocity = Mathf.SmoothDamp(horizontalVelocity, _horizontalInput, ref horizontalVelocityRef, moveSmoothTime);
        if (Mathf.Abs(horizontalVelocity) > 0)
        {
            horizontalVelocity = (isInDash) ? (horizontalVelocity > 0) ? dashSpeedAtCount : -dashSpeedAtCount : Mathf.Lerp(horizontalVelocity, _horizontalInput, Time.fixedDeltaTime * 200);
        }
        else
        {
            horizontalVelocity = (isInDash) ? (horizontalVelocity > 0) ? dashSpeedAtCount : -dashSpeedAtCount : Mathf.Lerp(horizontalVelocity, _horizontalInput, Time.fixedDeltaTime * 1000); ;
        }


        Vector2 targetMoveAmount = horizontalVelocity * Vector2.right * Time.fixedDeltaTime * playerMoveSpeed;

        if (trackingTransform != null)
        {
            targetMoveAmount += new Vector2(trackingTransform.position.x, trackingTransform.position.y) - prevTrackingTransform;
        }


        targetMoveAmount += velocity;
        velocity.y -= gravity * Time.fixedDeltaTime;

        bool couldBeGrounded = false;
        bool couldHitWall = false;
        Vector2 groundedAt = Vector2.zero;


        Vector2 feetPos = new Vector2(feetTransform.position.x, feetTransform.position.y);
        /*RaycastHit2D wallCheckRay = Physics2D.BoxCast((feetPos + Vector2.up * stepDistance) + Vector2.up * 0.5f, new Vector2(0.001f, 1), 0, targetMoveAmount.normalized, targetMoveAmount.magnitude * 5);
        if (wallCheckRay)
        {
            targetMoveAmount = new Vector2(0, targetMoveAmount.y);
        }*/

        Vector2 targetFeetPos = new Vector2(feetTransform.position.x, feetTransform.position.y) + targetMoveAmount;
        Debug.DrawRay(targetFeetPos + Vector2.up * stepDistance, Vector2.down * stepDistance, Color.green);
        Debug.DrawRay(targetFeetPos, Vector2.down * stickToGroundDistance, Color.red);
        RaycastHit2D stepCheckRay = Physics2D.Raycast(targetFeetPos + Vector2.up * stepDistance, Vector2.down, stepDistance, ignorePlayerMask);
        if (stepCheckRay) //checks from inside the player to the ground handles steps
        {
            /*float hitAngle = (1 - Vector2.Dot(stepCheckRay.normal, Vector2.up)) * 90;
            Debug.Log(hitAngle);
            if (hitAngle < maxSlopValue)
            {
                couldBeGrounded = true;
                groundedAt = stepCheckRay.point;
            }*/
            couldBeGrounded = true;
            groundedAt = stepCheckRay.point;
            trackingTransform = stepCheckRay.transform;
        }
        else
        {
            stepCheckRay = Physics2D.Raycast(targetFeetPos, Vector2.down, stickToGroundDistance, ignorePlayerMask);
            if (stepCheckRay)
            {
                /*float hitAngle = (1 - Vector2.Dot(stepCheckRay.normal, Vector2.up)) * 90;
                if (hitAngle < maxSlopValue)
                {
                    anim.SetBool("Grounded", false);
                    couldBeGrounded = true;
                    groundedAt = stepCheckRay.point;
                }*/
                couldBeGrounded = true;
                groundedAt = stepCheckRay.point;
                trackingTransform = stepCheckRay.transform;

            }
            else
            {
                couldBeGrounded = false;
                trackingTransform = null;
            }
        }

        Vector3 moveToPos;
        if (couldBeGrounded)
        {
            anim.SetBool("Landing", false);
            velocity.y = 0;
            //rb.MovePosition(groundedAt);
            moveToPos = groundedAt;
            anim.SetBool("Grounded", true);
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            anim.SetBool("Grounded", false);
            //rb.MovePosition(transform.position + new Vector3((couldHitWall) ? 0 : targetMoveAmount.x, targetMoveAmount.y, 0));
            moveToPos = transform.position + new Vector3((couldHitWall) ? 0 : targetMoveAmount.x, targetMoveAmount.y, 0);
        }
        rb.MovePosition(moveToPos);
        Vector2 movement = new Vector2(transform.position.x, transform.position.y) - prevPos;
        if (trackingTransform != null)
        {
            movement -= new Vector2(trackingTransform.position.x, trackingTransform.position.y) - prevTrackingTransform;
        }
        anim.SetFloat("HorizontalVelocity", Mathf.Abs(movement.x * Time.deltaTime * 250));
        anim.SetFloat("VerticalVelocity", velocity.y);
        if (Mathf.Abs(movement.x * Time.deltaTime * 250) > 0f)
        {

            transform.localScale = new Vector3((_horizontalInput == 0) ? transform.localScale.x : (_horizontalInput > 0) ? 1 : -1, transform.localScale.y, transform.localScale.z);
        }

        spriteHolder.transform.eulerAngles = new Vector3(spriteHolder.transform.rotation.eulerAngles.x, spriteHolder.transform.rotation.eulerAngles.y, movement.x * Time.deltaTime * 250 * -9f);
        prevPos = transform.position;
        if (trackingTransform != null)
        {
            prevTrackingTransform = trackingTransform.position;
        }

    }

    void CameraControl()
    {
        if (resolution.width != Screen.width || resolution.height != Screen.height)
        {
            worldScript.CalculateLevelConstaints();
            resolution = Screen.currentResolution;
        }
        WorldConstrints currentConstaints = worldScript.worldConstraints;
        Vector3 targetPos = new Vector3(this.transform.position.x, this.transform.position.y + cameraYOffset, -10);
        targetPos.x = Mathf.Clamp(targetPos.x, currentConstaints.minX, currentConstaints.maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, currentConstaints.minY, currentConstaints.maxY);
        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, targetPos, ref cameraVelocity, camSmoothTime);

    }

    void FollowingLight()
    {
        followingLight.transform.position = Vector3.Lerp(followingLight.transform.position, transform.position - Vector3.forward * 3 + Vector3.up * 2 + Vector3.right * 1.5f, 3f * Time.deltaTime);
    }


}
