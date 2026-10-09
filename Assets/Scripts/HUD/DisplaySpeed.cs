using Lucy.Core;
using TMPro;
using UnityEngine;

public class DisplaySpeed : MonoBehaviour
{
    [Header("Reference")]
    public Movement movement;
    public TMP_Text label;

    void Update() => label.text = movement.currentHorizontalVelocity.magnitude.ToString();
}