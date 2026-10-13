using Lucy.Core;
using UnityEngine;

public class ReturnController : MonoBehaviour
{
    public GameObject CameraCut;
    public GameObject CameraMove;
    public Movement movement;

    public void ReturnMovementController()
    {
        CameraCut.SetActive(false);
        CameraMove.SetActive(true);
        movement.enabled = true;
    }
}
