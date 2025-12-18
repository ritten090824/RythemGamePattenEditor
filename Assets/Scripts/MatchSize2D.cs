using UnityEngine;

public class MatchUISizeAndPosition : MonoBehaviour
{
    public RectTransform uiImage;       // UI 이미지
    public SpriteRenderer spriteObj;    // 2D 오브젝트
    public Canvas canvas;               // UI 캔버스
    public Camera uiCamera;             // UI 카메라 (Orthographic)

    void Update()
    {
        if (uiImage == null || spriteObj == null || canvas == null || uiCamera == null)
            return;

        // 1. UI 이미지 크기 (참조 해상도 기준)
        Vector2 uiSize = uiImage.sizeDelta;

        // 2. 캔버스 스케일 팩터
        float scaleFactor = canvas.scaleFactor;

        // 3. 픽셀 → 월드 단위 변환
        float pixelsToWorld = (2f * uiCamera.orthographicSize) / uiCamera.pixelHeight;
        Vector2 worldSize = uiSize * scaleFactor * pixelsToWorld;

        // 4. 스프라이트 원본 크기
        Vector2 spriteSize = spriteObj.sprite.bounds.size;

        // 5. 스케일 계산
        Vector3 newScale = new Vector3(
            worldSize.x / spriteSize.x,
            worldSize.y / spriteSize.y,
            1f
        );
        spriteObj.transform.localScale = newScale;

        // 6. UI 이미지 위치 → 월드 좌표
        Vector3 worldPos = uiCamera.ScreenToWorldPoint(uiImage.position);
        worldPos.z = 0f; // UI 카메라 앞쪽으로 설정

        // 7. 오브젝트 위치 설정
        spriteObj.transform.position = worldPos;
    }
}
