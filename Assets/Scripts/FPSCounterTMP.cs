using UnityEngine;
using TMPro;

public class FPSCounterTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float timePassed;
    private int frameCount;

    private void Update()
    {
        frameCount++;
        timePassed += Time.unscaledDeltaTime;

        if (timePassed >= updateInterval)
        {
            float fps = frameCount / timePassed;

            if (fpsText != null)
                fpsText.text = $"FPS : {Mathf.RoundToInt(fps)}";

            frameCount = 0;
            timePassed = 0f;
        }
    }
}