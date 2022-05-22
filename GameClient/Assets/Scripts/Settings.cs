using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Settings : MonoBehaviour
{
#pragma warning disable 0649
    //[SerializeField]
    //private PlayerCamera cam;
    [SerializeField]
    private TMP_Dropdown resolutionDropdown;
#pragma warning restore 0649
    Resolution[] resolutions;

    // Start is called before the first frame update
    void Start()
    {
        SetUpResolutions();
    }

    private void SetUpResolutions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetQuality(int settingsIndex)
    {
        QualitySettings.SetQualityLevel(settingsIndex);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
    
    public void SetFullscreen(bool _isFullscreen)
    {
        Screen.fullScreen = _isFullscreen;
    }

    public void SetVSync(int _mode)
    {
        QualitySettings.vSyncCount = _mode;
    }

    public void SetFPS(int _option)
    {
        switch (_option)
        {
            case 0:
                Application.targetFrameRate = 25;
                break;
            case 1:
                Application.targetFrameRate = 30;
                break;
            case 2:
                Application.targetFrameRate = 60;
                break;
            case 3:
                Application.targetFrameRate = 80;
                break;
            case 4:
                Application.targetFrameRate = 120;
                break;
            case 5:
                Application.targetFrameRate = 144;
                break;
            case 6:
                Application.targetFrameRate = 200;
                break;
            case 7:
                Application.targetFrameRate = 240;
                break;
            case 8:
                Application.targetFrameRate = -1; // Uncapped
                break;
        }
    }
}
