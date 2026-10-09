using System;
using Unity.Cinemachine;
using UnityEngine;

public class SpineRotator : MonoBehaviour
{
    [SerializeField] private CinemachinePanTilt cinemachinePan;
    [SerializeField] private Animator animator;
    private static readonly int Tilt = Animator.StringToHash("Tilt");
    private void Update()
    {
        // print(cinemachinePan.TiltAxis.Value); // lol, print?
        animator.SetFloat(Tilt, cinemachinePan.TiltAxis.Value);
    }
}
