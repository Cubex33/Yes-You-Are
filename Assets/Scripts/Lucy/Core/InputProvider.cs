using System.Security.AccessControl;
// Core layer
// Objective Singleton
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lucy.Core
{
    public class InputProvider : MonoBehaviour
    {
        public GlobalController Controls { get; private set; }
        public static InputProvider instance;
        public static InputProvider Global
        {
            get
            {
                if (instance == null && Application.isPlaying && !Application.isBatchMode)
                {
                    if (true) Debug.Log("Creating InputProvider via Lazy: " + System.Environment.StackTrace);
                    var go = new GameObject("InputProvider");
                    instance = go.AddComponent<InputProvider>();
                    instance.Controls = new GlobalController();
                    instance.Controls.Player.Enable();
                    instance.Controls.UI.Enable();
                    instance.Controls.Cutscene.Enable();        
                            
                }
                return instance;
            }
        }
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }
        void OnDestroy()
        {
            Controls?.Dispose();
            if (instance == this) instance = null;
        }
    }
}
