using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    // Create RigidBody2D Object
    private Rigidbody2D rb;
    
    // Movement variables
    private float movement;
    public float speed;

    // Jump Variables
    private bool isGrounded;
    public LayerMask whatIsGround;
    public Transform feetPos;
    public float checkRadius;
    
    public float jumpForce;
    private float jumpTimeCounter;
    public float jumpTime;
    public bool isJumping;

    private Vector3 targetPos;

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, checkRadius, whatIsGround);
        
        // Check if player is grounded and pressing jump key
        if(isGrounded == true && Input.GetKeyDown(KeyCode.Space)) {
            isJumping = true;
            jumpTimeCounter = jumpTime;
            rb.velocity = Vector2.up * jumpForce;
        }
        
        // Check if the player is jumping and pressing jump key
        if(Input.GetKey(KeyCode.Space) && isJumping == true) {
            
            if(jumpTimeCounter > 0) {
                rb.velocity = Vector2.up * jumpForce;
                jumpTimeCounter -= Time.deltaTime;
            }
            else {
                isJumping = false;
            }
        }
        
        // If the player releases the space bar, stop jumping
        if(Input.GetKeyUp(KeyCode.Space)) {
            isJumping = false;
        }
    }

    void FixedUpdate() {
        movement = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(movement * speed, rb.velocity.y);
    }
}
