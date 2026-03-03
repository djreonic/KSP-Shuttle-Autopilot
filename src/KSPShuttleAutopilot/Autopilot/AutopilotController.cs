using UnityEngine;

public class AutopilotController : MonoBehaviour
{
    private Settings _settings;
    private Plans _plans;

    public void Initialize()
    {
        _settings = new Settings();
        _plans = new Plans();
        try
        {
            _settings.Load();
            _plans.Load();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error loading settings and plans: " + e.Message);
        }
    }

    public void Dispose()
    {
        try
        {
            Save();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error during disposal: " + e.Message);
        }
    }

    private void OnUpdate()
    {
        // Ensure no file IO occurs here.
    }

    private void Save()
    {
        // Save logic here
    }
}