using UnityEngine;
using UnityEngine.InputSystem;

public class Resolution : MonoBehaviour
{
    bool isFullScreen = false;

    // 저장용 창 모드 해상도
    int windowWidth = 1280;
    int windowHeight = 720;

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            if (isFullScreen)
            {
                // 창 모드로 돌아갈 때 기존 창 크기 유지
                Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.Windowed);
                Screen.fullScreenMode = FullScreenMode.Windowed;
                isFullScreen = false;
            }
            else
            {
                // 현재 창 크기 저장
                windowWidth = Screen.width;
                windowHeight = Screen.height;

                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                isFullScreen = true;
            }
        }
    }
}
