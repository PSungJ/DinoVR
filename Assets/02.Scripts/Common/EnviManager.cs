using System.Collections;
using UnityEngine;

public class EnviManager : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Material proceduralSkybox; // Procedural Skybox 1개만 사용
    [Range(0f, 1f)] public float currentTime = 0f; // 0 = 낮, 1 = 밤
    private bool reverse = false; // 주기 방향 (낮→밤→낮)

    [Header("Lighting Settings")]
    public Light directionalLight;
    public float cycleDuration = 600f; // 낮↔밤 전체 주기 (초)
    public float lightIntensityDay = 1.2f;
    public float lightIntensityNight = 0.05f;
    public Color lightColorDay = new Color(1f, 1f, 1f);
    public Color lightColorNight = new Color(0.3f, 0.35f, 0.6f);

    [Header("Fog Settings")]
    public Color dayFogColor = new Color(0.7f, 0.8f, 0.8f, 1f);
    public Color nightFogColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    public float dayFogDensity = 0.002f;
    public float nightFogDensity = 0.005f;

    [Header("Rain Settings")]
    public ParticleSystem rainParticle;
    private bool isRaining = false;

    [Range(0f, 1f)] public float rainStartChance = 0.1f; // 비 시작 확률
    [Range(0f, 1f)] public float rainStopChance = 0.1f; // 비 멈출 확률

    // 추가된 변수: 환경 업데이트 주기 조절용
    private float envUpdateTimer = 0f;

    private void Start()
    {
        RenderSettings.skybox = proceduralSkybox;
        RenderSettings.fog = true;
        UpdateEnvironment(0f, true); // 초기 한 번만 즉시 반영

        if (directionalLight != null)
            directionalLight.enabled = true;

        StartCoroutine(DayNightCycle());
        StartCoroutine(RainRoutine());
        StartCoroutine(PeriodicGIUpdate()); // GI는 별도 코루틴에서 주기적으로 업데이트
    }

    private IEnumerator DayNightCycle()
    {
        float halfCycle = cycleDuration / 2f;
        float speed = 1f / halfCycle;

        while (true)
        {
            // 낮 → 밤 → 낮 반복
            currentTime += (reverse ? -1 : 1) * Time.deltaTime * speed;
            currentTime = Mathf.Clamp01(currentTime);

            if (currentTime >= 1f) reverse = true;
            else if (currentTime <= 0f) reverse = false;

            // Skybox, Fog, Light 업데이트 빈도를 줄여 프레임 부하 완화 (0.2초마다)
            envUpdateTimer += Time.deltaTime;
            if (envUpdateTimer >= 0.2f)
            {
                UpdateEnvironment(currentTime);
                envUpdateTimer = 0f;
            }

            yield return null;
        }
    }

    private void UpdateEnvironment(float t, bool immediate = false)
    {
        if (proceduralSkybox == null) return;

        // Skybox 파라미터 보간
        float atmosphere = Mathf.Lerp(0.6f, 1.0f, t);
        Color skyTint = Color.Lerp(new Color(0.6f, 0.8f, 1f), new Color(0.05f, 0.05f, 0.2f), t);
        Color groundColor = Color.Lerp(new Color(0.4f, 0.3f, 0.2f), new Color(0.05f, 0.05f, 0.05f), t);

        // Skybox 머티리얼 업데이트
        proceduralSkybox.SetFloat("_AtmosphereThickness", atmosphere);
        proceduralSkybox.SetColor("_SkyTint", skyTint);
        proceduralSkybox.SetColor("_GroundColor", groundColor);

        // Directional Light (태양 궤적 + 색상 + 세기)
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(50f, -30f, t), 0f, 0f);
            directionalLight.intensity = Mathf.Lerp(lightIntensityDay, lightIntensityNight, t);
            directionalLight.color = Color.Lerp(lightColorDay, lightColorNight, t);
        }

        // Fog 보간
        RenderSettings.fogColor = Color.Lerp(dayFogColor, nightFogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, t);

        // DynamicGI.UpdateEnvironment()는 매우 무겁기 때문에 매 프레임 호출 금지
        //    → 별도의 코루틴(PeriodicGIUpdate)에서 일정 간격으로 호출되도록 변경
        if (immediate)
            DynamicGI.UpdateEnvironment(); // 초기 설정 시 한 번만 즉시 반영
    }

    // GI 업데이트를 일정 간격(30초)마다 수행하여 프레임 안정화
    private IEnumerator PeriodicGIUpdate()
    {
        while (true)
        {
            DynamicGI.UpdateEnvironment();
            yield return new WaitForSeconds(30f);
        }
    }

    private IEnumerator RainRoutine()
    {
        while (true)
        {
            // 불필요한 매 프레임 연산 제거 → 10분마다 날씨 변화 시도
            yield return new WaitForSeconds(600f);
            HandleRainChance();
        }
    }

    private void HandleRainChance()
    {
        if (!isRaining)
        {
            if (Random.value <= rainStartChance)
                StartRain();
        }
        else
        {
            if (Random.value <= rainStopChance)
                StopRain();
        }
    }

    private void StartRain()
    {
        isRaining = true;
        if (rainParticle != null)
            rainParticle.Play();
#if UNITY_EDITOR
        Debug.Log("비가 내리기 시작합니다.");
#endif
    }

    private void StopRain()
    {
        isRaining = false;
        if (rainParticle != null)
            rainParticle.Stop();
#if UNITY_EDITOR
        Debug.Log("비가 그쳤습니다.");
#endif
    }
}