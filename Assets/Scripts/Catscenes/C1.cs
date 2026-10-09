using Lucy.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class C1 : MonoBehaviour
{
    public CinemachineCamera introCamera;
    public CinemachineCamera playerCamera;
    public PlayerController playerController;
    public CinemachineInputAxisController inputAxisController;
    public CinemachinePanTilt cinemachinePan;
    public PlayableDirector timeline;
    public string text;
    public Canvas HUD;
    public Canvas[] sequence; 
    bool started = false;
    private int currentStep = 0;
    public UnityEvent OnEnd;

    public void StartCutscene()
    {
        introCamera.Priority = 1;
        playerCamera.Priority = -1;
        cinemachinePan.enabled = playerController.enabled = inputAxisController.enabled = false;
        HUD.gameObject.SetActive(false);
        currentStep = 0;
        if (sequence != null && sequence.Length > 0)
        {
            for (int i = 0; i < sequence.Length; i++) sequence[i].gameObject.SetActive(i == 0);
        }
        InputProvider.Global.Controls.Cutscene.Any.performed += NextStep;
    }

    public void EndCutscene()
    {
        var e = introCamera.transform.rotation.eulerAngles;
        introCamera.Priority = -1;
        playerCamera.Priority = 1;
        cinemachinePan.PanAxis.Value = e.y;
        cinemachinePan.TiltAxis.Value = e.x;
        cinemachinePan.enabled = inputAxisController.enabled = playerController.enabled = true;
        started = true;
        timeline.Stop();
        HUD.gameObject.SetActive(true);
        OnEnd.Invoke();
        if (InputProvider.instance != null) InputProvider.Global.Controls.Cutscene.Any.performed -= NextStep;
    }

    void NextStep(InputAction.CallbackContext context)
    {
        NextStep();
    }

    void OnDestroy()
    {
        if (InputProvider.instance != null) InputProvider.Global.Controls.Cutscene.Any.performed -= NextStep;
    }

    public void NextStep()
    {
        if (sequence == null || sequence.Length == 0) return;

        // Hide current step
        if (currentStep >= 0 && currentStep < sequence.Length)
        {
            sequence[currentStep].gameObject.SetActive(false);
        }

        // Move to next step
        currentStep++;

        // If there are more steps, show the next one
        if (currentStep < sequence.Length)
        {
            sequence[currentStep].gameObject.SetActive(true);
        }
        else
        {
            // End of sequence - end the cutscene
            EndCutscene();
        }
    }

}