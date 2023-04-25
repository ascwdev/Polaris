using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

/**
 * Player.cs
 * Author: Alex Williams
 * Date: 15/03/23
 *
 * This is a player controller script for influencing the behavior of the
 * player character in Polaris. It contains methods for movement, jumping,
 * dashing, as well as the relevant associated variables for the player object.
 */

public class Player : MonoBehaviour
{
    #region Variables

    [Header("Components")]
    private Rigidbody2D rb;
    private Animator an;
    private Camera cam;
    private SpriteRenderer sr;
    private Animation am;
    private BoxCollider2D bc;
    private GameObject cp;

    [Header("Layer Masks")] 
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Collisions")] 
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isWalled;
    [SerializeField] private bool groundTouch;
    [SerializeField] private bool onRightWall;
    [SerializeField] private bool onLeftWall;
    [SerializeField] private float collisionRadius;
    [SerializeField] private Vector2 bottomOffset, rightOffset, leftOffset;
    [SerializeField] private int wallSide;
    [SerializeField] private int side = 1;
    private float bottomOffsetX = -0.19f;
    private float colliderX = -0.06276393f;

    [Header("Animation")]

    [Header("Controls")]
    private Vector3 mousePos;
    private Vector3 playerPos;
    public Vector3 playerToMouse;
    private float horizontalDirection;
    private float verticalDirection;
    private float horizontalMouse;
    private float verticalMouse;

    [Header("Run")]
    [SerializeField] private float acceleration;
    [SerializeField] private float topSpeed;
    [SerializeField] private float deceleration;
    [SerializeField] private bool canMove = true;
    private bool changingDirection => (rb.velocity.x > 0f && horizontalDirection < 0f) ||
                                      (rb.velocity.x < 0f && horizontalDirection > 0f);
    
    [Header("Jump")] 
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallForce = 8f;
    [SerializeField] private float lowJumpFallForce = 5f;
    [SerializeField] private float airResistance = 2.5f;
    [SerializeField] private float airResistanceDiag = 10f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float coyoteTimeCounter;
    [SerializeField] private float jumpTime = .2f;
    private float jumpTimer;
    private bool canJump => coyoteTimeCounter > 0 && Input.GetButtonDown("Jump") && !hasDashed;
    private bool queueJump; 

    [Header("Dash")] 
    [SerializeField] private bool isDashing;
    [SerializeField] private bool hasDashed;
    [SerializeField] private float dashForce = 20f;
    private Vector2 dashDir;
    private bool queueDash;

    [Header("Wall Slide")] 
    [SerializeField] private float slideFriction = 2f;
    [SerializeField] private bool wallSlide;

    [Header("Wall Climb")] 
    [SerializeField] private bool wallGrab;
    [SerializeField] private float climbSpeed = 5f;
    private static float climbMaxStamina = 110;
    [SerializeField] private float stamina = climbMaxStamina;
    [SerializeField] private float staminaCost = 5f;
    [SerializeField] private float staminaCostClimbing = 10f;
    [SerializeField] private float climbTiredThreshold = 50;
    private bool isTired => stamina < climbTiredThreshold;

    [Header("Wall Jump")]
    [SerializeField] private bool wallJump;
    [SerializeField] private float wallJumpLerp = 10f;
    private Vector2 wallDir;
    private bool queueWallJump;
    private static readonly int IsClimbing = Animator.StringToHash("IsClimbing");

    #endregion

    #region Debug

    // OnDrawGizmos() draws debug information when Gizmos are toggled in the editor.
    // Used mostly for collision debugging.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        var position = transform.position;
        Gizmos.DrawWireSphere((Vector2)position  + bottomOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)position + rightOffset, collisionRadius);
        Gizmos.DrawWireSphere((Vector2)position + leftOffset, collisionRadius);
        Gizmos.DrawRay((Vector2)position, playerToMouse);
    }

    #endregion

    #region Setup

    // Awake() is called once, and is the first event called in the order of execution.
    // This method is used to setup all of the component references.
    private void Awake()
    {
        cp = GameObject.FindGameObjectWithTag("Respawn");
        rb = GetComponent<Rigidbody2D>();
        an = GetComponent<Animator>();
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        bc = GetComponent<BoxCollider2D>();
    }

    #endregion

    #region Updates

    // Update() runs every frame. Most input-dependant functionality is present here.
    private void Update()
    {

        // Inputs
        horizontalDirection = GetInput().x;
        verticalDirection = GetInput().y;

        horizontalMouse = GetMouseInput().x;
        verticalMouse = GetMouseInput().y;

        // Running Anim
        an.SetFloat("Speed", Mathf.Abs(horizontalDirection));

        // Variable Jump Time
        if (jumpTimer > 0)
        {
            jumpTimer -= Time.deltaTime;
        }
        
        // Wall Climb Setup
        if (isWalled && Input.GetButtonDown("Climb") && canMove)
        {
            if (side != wallSide)
            {
                Flip(side*=-1);
            }
            wallGrab = true;
            wallSlide = false;
            an.SetBool(IsClimbing, true);
        }
        
        if (Input.GetButtonUp("Climb") || !isWalled || !canMove)
        {
            wallGrab = false;
            wallSlide = false;
            an.SetBool("IsClimbing", false);
        }
        
        if (isGrounded && !isDashing)
        {
            wallJump = false;
        }
        
        CoyoteTime();
        
        if (canJump)
        {
            queueJump = true;
        }
        
        if (!canJump && Input.GetButtonDown("Jump") && isWalled && !isGrounded)
        {
            queueWallJump = true;
        }
        
        if (wallGrab & !isDashing)
        {
            WallGrab();
        }
        
        // Wall Sliding
        if (isWalled && !isGrounded)
        {
            if (horizontalDirection != 0 && !wallGrab)
            {
                wallSlide = true;
                WallSlide();
            }
        }

        // Dashing
        if (Input.GetButtonDown("Dash") && !hasDashed)
        {
            queueDash = true;
        }

        // Set Falling Anim
        if (isGrounded)
        {
            an.SetBool("IsFalling", false);
        }
        
        if (isGrounded && !groundTouch)
        {
            GroundTouch();
            groundTouch = true;
        }
        
        if (!isGrounded && groundTouch)
        {
            groundTouch = false;
        }
        
        // Return if Player is doing one of these Actions
        if (wallGrab || wallSlide || !canMove)
            return;

        // Flip Player
        if (horizontalDirection > 0)
        {
            side = 1;
            Flip(side);
        }

        if (horizontalDirection < 0)
        {
            side = -1;
            Flip(side);
        }
    }

    // FixedUpdate() works on a fixed time step, and therefore is appropriate
    // for handling physics related method calls.
    private void FixedUpdate()
    {
        CheckCollisions();
        MoveCharacter();

        if (queueJump)
        {
            Jump(Vector2.up);
            queueJump = false;
        }

        if (queueWallJump)
        {
            WallJump();
            queueWallJump = false;
        }

        if (queueDash)
        {
            Dash(horizontalMouse, verticalMouse);
            queueDash = false;
        }
        
        if (isGrounded)
        {
            ApplyDeceleration();
        }
        else
        {
            ApplyAirResistance();
            FallMultiplier();
        }
    }

    #endregion

    #region Input

    private static Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
    
    private Vector2 GetMouseInput()
    {
        // Find mouse position relative to player object
        playerPos = cam.WorldToScreenPoint(transform.position);
        mousePos = Input.mousePosition;
        playerToMouse = (mousePos - playerPos);

        return new Vector2(playerToMouse.x, playerToMouse.y);
    }

    #endregion

    #region Dashing

    private void Dash(float x, float y)
    {
        hasDashed = true;

        rb.velocity = Vector2.zero; // Break momentum
        dashDir = new Vector2(x, y);

        rb.velocity = dashDir.normalized * dashForce;

        StartCoroutine(DashWait(.15f));
    }

    private IEnumerator DashWait(float time)
    {
        rb.gravityScale = 0;
        isDashing = true;

        yield return new WaitForSeconds(time);
        
        isDashing = false;

        if (isGrounded)
        {
            hasDashed = false;
        }
    }
    
    #endregion

    #region Climbing
    
    private void WallGrab()
    {
        if (!canMove)
        {
            return;
        }
        
        rb.gravityScale = 0f;

        // Unfinished climbing jump
        if (horizontalDirection > .2f || horizontalDirection < -.2f)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
        }
        
        rb.velocity = new Vector2(rb.velocity.x, verticalDirection * climbSpeed);

        // Decrease player's stamina during climbing
        if (rb.velocity.y == 0)
        {
            stamina -= staminaCost * Time.deltaTime;
        }
        else
        {
            stamina -= staminaCostClimbing * Time.deltaTime;
        }

        // Animation warning low stamina
        if (isTired)
        {
            
        }
        
        if (stamina <= 0)
        {
            wallGrab = false;
        }
    }

    #endregion

    #region Running
    
    private void MoveCharacter()
    {
        if (!canMove)
        {
            return;
        }

        if (wallGrab)
        {
            return;
        }

        if (!wallJump)
        {
            if (Mathf.Abs(rb.velocity.x) > topSpeed)
            {
                rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * topSpeed, rb.velocity.y);
            }
            
            rb.AddForce(new Vector2(horizontalDirection * acceleration, 0), ForceMode2D.Force);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, (new Vector2(horizontalDirection * topSpeed, rb.velocity.y)),
                wallJumpLerp * Time.deltaTime);
        }
        
        
    }

    private void ApplyDeceleration()
    {
        if (isDashing)
        {
            rb.drag = 0f;
        }
        else
        {
            if (Mathf.Abs(horizontalDirection) < 0.4f || changingDirection)
            {
                rb.drag = deceleration;
            }
            else
            {
                rb.drag = 0f;
            }
        }
       
    }
    
    #endregion

    #region Jumping
    
    private void Jump(Vector2 dir)
    {
        // Measures variable jump time
        jumpTimer = jumpTime;

        if (!Input.GetButton("Jump"))
        {
            an.SetBool("IsJumpingFast", true);
        }
        else
        {
            an.SetBool("IsJumpingSlow", true);
        }

        // Initiate jump
        rb.AddForce(dir * jumpForce, ForceMode2D.Impulse);
    }
    
    private void FallMultiplier()
    {
        if (wallGrab)
        {
            rb.gravityScale = 0;
        }
        else
        {
            if (rb.velocity.y < 0)
            {
                an.SetBool("IsFalling", true);
            }
            
            // Variable gravity depending on short or long jump
            if (jumpTimer < 0.01f)
            {
                rb.gravityScale = fallForce;
            } 
            else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
            {
                rb.gravityScale = lowJumpFallForce;
            }
            else
            {
                rb.gravityScale = 1f;
            }
        }
        
    }
    
    private void ApplyAirResistance()
    {
        if (horizontalDirection is > 0 or < 0 && rb.velocity.y > 0 || isDashing)
        {
            rb.drag = airResistanceDiag;
        }
        else
        {
            rb.drag = airResistance;
        }

    }
    
    private void CoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        if (Input.GetButtonUp("Jump"))
        {
            coyoteTimeCounter = 0;
        }
    }
    
    private void WallJump()
    {
        StopCoroutine(DisableMovement(0));
        
        wallDir = onRightWall ? Vector2.left : Vector2.right;

        StartCoroutine(DisableMovement(.25f));
        
        Jump(Vector2.up / 1.5f + wallDir / 1.5f);

        wallJump = true;
    }
    
    private void WallSlide()
    {

        if (!canMove)
        {
            return;
        }

        var pushingWall = (rb.velocity.x > 0 && onRightWall) || (rb.velocity.x < 0 && onLeftWall);
        var push = pushingWall ? 0 : rb.velocity.x;
        
        rb.velocity = new Vector2(push, Mathf.Clamp(rb.velocity.y, -slideFriction, float.MaxValue));
    }
    
    private IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }
    
    #endregion
    
    #region Respawning
    
    private void Die()
    {
        // Freeze player
        rb.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;
        
        // Disable all animations
        an.SetBool("IsJumpingFast", false);
        an.SetBool("IsJumpingSlow", false);
        an.SetBool("IsFalling", false);
        an.SetBool("IsClimbing", false);
        StartCoroutine(DisableMovement(1f));
        
        // Fire Death animation
        an.SetTrigger("Death");
    }
    
    // (Animation Event) Called at last frame of Death animation
    private void Respawn()
    {
        // Reset player
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        sr.enabled = true;
        
        // Move player position to respawn point
        transform.position = cp.transform.position;
    }
    
    #endregion
    
    #region Collisions

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Obstacle")
        {
            // Check for Respawn Point
            while (!cp.activeSelf)
            {
                cp = GameObject.FindWithTag("Respawn");
            }

            Die();
        }
    }

    private void CheckCollisions()
    {
        var position = transform.position;
        
        // Use overlapping circle to check for ground, walls etc.
        isGrounded = Physics2D.OverlapCircle((Vector2)position + bottomOffset, collisionRadius, groundLayer);
        isWalled = onRightWall || onLeftWall;
        onRightWall = Physics2D.OverlapCircle((Vector2)position + rightOffset, collisionRadius, wallLayer);
        onLeftWall = Physics2D.OverlapCircle((Vector2)position + leftOffset, collisionRadius, wallLayer);

        wallSide = onRightWall ? 1 : -1;
    }

    // Reset abilities when touching ground
    private void GroundTouch()
    {
        an.SetBool("IsFalling", false);
        an.SetBool("IsJumpingFast", false);
        an.SetBool("IsJumpingSlow", false);
        
        hasDashed = false;
        isDashing = false;
        stamina = climbMaxStamina;

        side = sr.flipX ? -1 : 1;
    }
    
    #endregion

    #region Animation

    private void Flip(int side)
    {
        if (wallGrab || wallSlide)
        {
            switch (side)
            {
                case -1 when sr.flipX:
                case 1 when !sr.flipX:
                    return;
            }
        }

        var state = (side != 1);
        sr.flipX = state;

        // Flip collisions!
        if (side == 1)
        {
            bc.offset = new Vector2(colliderX, bc.offset.y);
            bottomOffset.x = bottomOffsetX;
        }
        else if (side == -1)
        {
            bc.offset = new Vector2(-colliderX, bc.offset.y);
            bottomOffset.x = -bottomOffsetX;
        }
        
    }

    #endregion
}
