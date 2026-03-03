using System;
using UnityEngine;

namespace KSPShuttleAutopilot
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class ShuttleAutopilotAddon : MonoBehaviour
    {
        private Autopilot.AutopilotController _controller;

        public void Start()
        {
            try
            {
                _controller = new Autopilot.AutopilotController();
                _controller.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[KSPShuttleAutopilot] Start failed: {ex}");
            }
        }

        public void Update() => _controller?.OnUpdate();

        public void OnGUI() => _controller?.OnGUI();

        public void OnDestroy()
        {
            _controller?.Dispose();
            _controller = null;
        }
    }
}
