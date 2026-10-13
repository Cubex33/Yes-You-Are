using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace IDosGames.WebBridge
{
    /// <summary>Add to a uniquely named GameObject, for example IDosBridge.</summary>
    public sealed class IDosWebsiteBridge : MonoBehaviour
    {
        [Serializable]
        public sealed class JsonReceivedEvent : UnityEvent<string> { }

        [SerializeField] private JsonReceivedEvent onDataReceived = new JsonReceivedEvent();
        public event Action<string> DataReceived;
        public string LastReceivedJson { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void SendDataToSite(string json);
        [DllImport("__Internal")] private static extern void YesUR_RequestInventory(string receiver);
        [DllImport("__Internal")] private static extern void YesUR_ReportAchievement(string receiver);
#endif

        /// <summary>Send any valid JSON to the website's unity:data event listener.</summary>
        public void SendJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON must not be empty.", nameof(json));
#if UNITY_WEBGL && !UNITY_EDITOR
            SendDataToSite(json);
#else
            Debug.Log("[Website bridge] SendJson (WebGL only): " + json, this);
#endif
        }

        /// <summary>Read server-owned items; response arrives in ReceiveDataFromSite.</summary>
        public void RequestInventory()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YesUR_RequestInventory(gameObject.name);
#else
            EditorOnlyResponse("inventory");
#endif
        }

        /// <summary>Report completion; this is not proof of victory or a token payout.</summary>
        public void ReportGameCompleted()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YesUR_ReportAchievement(gameObject.name);
#else
            EditorOnlyResponse("achievement");
#endif
        }

        // Public string receiver for Unity's SendMessage from website JavaScript.
        public void ReceiveDataFromSite(string json)
        {
            LastReceivedJson = json;
            DataReceived?.Invoke(json);
            onDataReceived.Invoke(json);
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private void EditorOnlyResponse(string type)
        {
            ReceiveDataFromSite("{\"type\":\"" + type +
                "\",\"ok\":false,\"error\":\"Website bridge requires a WebGL build running on the website.\"}");
        }
#endif
    }
}
