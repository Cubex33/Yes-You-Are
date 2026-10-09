using TMPro;
using UnityEngine;

namespace HUD
{
    public class MissionDisplay : MonoBehaviour
    {
        public TMP_Text missionTitle;
        public TMP_Text missionDescription;
        public BaseGameManager gameManager;

        [Tooltip("How often (seconds) to check for a new leaf mission inside the current root mission.")]
        public float pollInterval = 0.2f;

        // root mission (the one provided by GameManager). We keep only one subscription to GameManager.
        private BaseMission rootMission;
        private BaseMission lastLeafShown;
        private float pollTimer;

        private void Start()
        {
            if (missionTitle == null) Debug.LogWarning("MissionDisplay: missionTitle not assigned.");
            if (missionDescription == null) Debug.LogWarning("MissionDisplay: missionDescription not assigned.");
            if (gameManager == null)
            {
                Debug.LogWarning("MissionDisplay: gameManager not assigned.");
                return;
            }

            // Single subscription only:
            gameManager.OnMissionChanged += OnGameManagerMissionChanged;

            // initialize from current mission if any
            rootMission = gameManager.CurrentMission;
            UpdateDisplayImmediate();
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnMissionChanged -= OnGameManagerMissionChanged;
        }

        private void OnGameManagerMissionChanged(BaseMission newMission)
        {
            // only update the root mission reference; the polling loop will find leaf and update UI.
            rootMission = newMission;
            // Immediately update UI for responsiveness
            UpdateDisplayImmediate();
        }

        private void Update()
        {
            // polling with interval to avoid per-frame tree walking
            if (rootMission == null) return;

            pollTimer -= Time.deltaTime;
            if (pollTimer > 0f) return;
            pollTimer = pollInterval;

            var leaf = GetLeafMission(rootMission);
            if (leaf != lastLeafShown)
            {
                UpdateMissionDisplay(leaf);
            }
        }

        private void UpdateDisplayImmediate()
        {
            var leaf = rootMission != null ? GetLeafMission(rootMission) : null;
            UpdateMissionDisplay(leaf);
            // reset timer so that Update() won't immediately re-evaluate
            pollTimer = pollInterval;
        }

        private void UpdateMissionDisplay(BaseMission mission)
        {
            lastLeafShown = mission;

            if (mission == null)
            {
                if (missionTitle != null) missionTitle.text = "";
                if (missionDescription != null) missionDescription.text = "";
                return;
            }

            if (missionTitle != null) missionTitle.text = mission.missionName;
            if (missionDescription != null) missionDescription.text = mission.missionDescription;
        }

        /// <summary>
        /// Walks down MissionGroup children until non-MissionGroup leaf is found (or null).
        /// Requires MissionGroup to expose a method/property to get current child (see note).
        /// </summary>
        private BaseMission GetLeafMission(BaseMission start)
        {
            if (start == null) return null;

            BaseMission current = start;

            // Keep going while current is a group and it has a child
            while (true)
            {
                if (current is MissionGroup group)
                {
                    // Prefer a public API on MissionGroup:
                    //   public BaseMission CurrentChild { return CurrentChild; }
                    // If your MissionGroup exposes CurrentChild publicly, use that instead.
                    BaseMission child = null;
                    try
                    {
                        child = group.CurrentChild;
                    }
                    catch
                    {
                        // fallback if GetCurrentMission doesn't exist or throws:
                        // try to use reflection to find a property named 'CurrentChild' (best-effort)
                        var prop = group.GetType().GetProperty("CurrentChild", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (prop != null)
                            child = prop.GetValue(group) as BaseMission;
                    }

                    if (child == null) return group; // no active child — show group itself
                    current = child;
                    continue;
                }

                // not a group → leaf mission
                return current;
            }
        }
    }
}