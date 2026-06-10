using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWave : MonoBehaviour
{
    public AudioSource audioSource;      // Reference to the AudioSource

    public LineRenderer lowLine;         // LineRenderer for low frequencies
    public LineRenderer midLine;         // LineRenderer for mid frequencies
    public LineRenderer highLine;

    public LineRenderer allLine;
    // LineRenderer for high frequencies
    public Transform lowTransform;       // Transform for the low frequencies
    public Transform midTransform;       // Transform for the mid frequencies
    public Transform highTransform;

    public Transform allTransform;
    // Transform for the high frequencies
    public int resolution = 128;         // Number of points per line
    public float lineLength = 10f;       // Total length of each line in world units
    public float heightMultiplier = 1f;  // Scale for the waveform's height
    public bool centeredFrequencyRange = true; // Toggle for frequency range mapping

    public bool combined;

    private float[] spectrum;            // Array to store spectrum data

    void Start()
    {
        // Initialize spectrum array and set LineRenderer point counts
        spectrum = new float[512]; // Higher resolution for better accuracy

        lowLine.positionCount = resolution;
        midLine.positionCount = resolution;
        highLine.positionCount = resolution;

        allLine.positionCount = resolution;

        if (!combined)
        {
            allLine.gameObject.SetActive(false);
        }
        else
        {
            lowLine.gameObject.SetActive(false);
            midLine.gameObject.SetActive(false);
            highLine.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Get the spectrum data
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        if (Input.GetKeyDown(KeyCode.P))
        {
            audioSource.Play();
        }

        // Update each line, positioned relative to its transform
        if (!combined)
        {
            allLine.gameObject.SetActive(false);

            lowLine.gameObject.SetActive(true);
            midLine.gameObject.SetActive(true);
            highLine.gameObject.SetActive(true);

            UpdateLine(lowLine, lowTransform, 0, 50);    // Low frequencies: 0-50
            UpdateLine(midLine, midTransform, 51, 150); // Mid frequencies: 51-150
            UpdateLine(highLine, highTransform, 151, 511); // High frequencies: 151-511
        }
        else
        {
            allLine.gameObject.SetActive(true);

            lowLine.gameObject.SetActive(false);
            midLine.gameObject.SetActive(false);
            highLine.gameObject.SetActive(false);

            UpdateLine(allLine, allTransform, 0, 511); // All frequencies: 0-511
        }
    }

    void UpdateLine(LineRenderer line, Transform lineTransform, int startFreq, int endFreq)
    {
        // Base position on the Transform's position
        Vector3 centerPosition = lineTransform.position;

        // Calculate points for the line
        for (int i = 0; i < resolution; i++)
        {
            // Map the current index to a frequency range
            int freqIndex = centeredFrequencyRange
                ? Mathf.FloorToInt(Mathf.Lerp(startFreq, endFreq, Mathf.Abs((float)i / (resolution - 1) - 0.5f) * 2f)) // Centered frequency mapping
                : Mathf.FloorToInt(Mathf.Lerp(startFreq, endFreq, (float)i / (resolution - 1))); // Linear frequency mapping

            // Use spectrum data to set y-position
            float y = spectrum[freqIndex] * heightMultiplier;

            // Calculate x-position (this part remains the same)
            float x = ((float)i / (resolution - 1) - 0.5f) * lineLength;

            // Update the line's position relative to the center
            line.SetPosition(i, centerPosition + new Vector3(x, y, 0));
        }
    }
}