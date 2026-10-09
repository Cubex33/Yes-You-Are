using Lucy.Core;
using UnityEngine;

public class DoubleJump : MonoBehaviour
{
    [Header("Setting")]
    public bool doubleJumpEnabled= true;
    [Header("Runtime")]
public Movement movement;
    public bool isDoubleJumping = false;
    public void Jump()
    {
        if (doubleJumpEnabled && !isDoubleJumping && movement.Controller.isGrounded)
        {
            movement.Jump();
            isDoubleJumping = true;
        }
    }
}   