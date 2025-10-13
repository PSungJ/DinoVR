using System.Collections;
using UnityEngine;

public class EnviManager : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Material proceduralSkybox; // Procedural Skybox 1°³¸¸ »ç¿ë
    [Range(0f, 1f)] public float currentTime = 0f; // 0 = ³·, 1 = ¹ã
    private bool reverse = false; // ÁÖ±â ¹æÇâ (³·¡æ¹ã¡æ³·)

    [Header("Lighting Settings")]
    public Light directionalLight;
    public float cycleDuration = 600f; // ³·¡ê¹ã ÀüÃ¼ ÁÖ±â (ÃÊ)
    public float lightIntensityDay = 1.2f;
    public float lightIntensityNight = 0.05f;
    public Color lightColorDay = new Color(1f, 0.95f, 0.8f);
    public Color lightColorNight = new Color(0.3f, 0.35f, 0.6f);

    [Header("Fog Settings")]
    public Color dayFogColor = new Color(0.7f, 0.8f, 0.8f, 1f);
    public Color nightFogColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    public float dayFogDensity = 0.002f;
    public float nightFogDensity = 0.005f;

    [Header("Rain Settings")]
    public ParticleSystem rainParticle;
    private bool isRaining = false;
    private float rainTimer = 0f;

    [Range(0f, 1f)] public float rainStartChance = 0.1f; // ºñ ½ÃÀÛ È®·ü
    [Range(0f, 1f)] public float rainStopChance = 0.1f; // ºñ ¸ØÃâ È®·ü

    private void Start()
    {
        RenderSettings.skybox = proceduralSkybox;
        RenderSettings.fog = true;
        UpdateEnvironment(0f);

        if (directionalLight != null)
            directionalLight.enabled = true;

        StartCoroutine(DayNightCycle());
        StartCoroutine(RainRoutine());
    }

    private IEnumerator DayNightCycle()
    {
        float halfCycle = cycleDuration / 2f;
        float speed = 1f / halfCycle;

        while (true)
        {
            // ³· ¡æ ¹ã ¡æ ³· ¹Ýº¹
            currentTime += (reverse ? -1 : 1) * Time.deltaTime * speed;
            currentTime = Mathf.Clamp01(currentTime);

            if (currentTime >= 1f) reverse = true;
            else if (currentTime <= 0f) reverse = false;

            UpdateEnvironment(currentTime);
            yield return null;
        }
    }

    private void UpdateEnvironment(float t)
    {
        if (proceduralSkybox == null) return;

        // Skybox ÆÄ¶ó¹ÌÅÍ º¸°£
        float atmosphere = Mathf.Lerp(0.6f, 1.0f, t);
        Color skyTint = Color.Lerp(new Color(0.6f, 0.8f, 1f), new Color(0.05f, 0.05f, 0.2f), t);
        Color groundColor = Color.Lerp(new Color(0.4f, 0.3f, 0.2f), new Color(0.05f, 0.05f, 0.05f), t);

        proceduralSkybox.SetFloat("_AtmosphereThickness", atmosphere);
        proceduralSkybox.SetColor("_SkyTint", skyTint);
        proceduralSkybox.SetColor("_GroundColor", groundColor);

        // Directional Light (ÅÂ¾ç ±ËÀû + »ö»ó + ¼¼±â)
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(50f, -30f, t), 0f, 0f);
            directionalLight.intensity = Mathf.Lerp(lightIntensityDay, lightIntensityNight, t);
            directionalLight.color = Color.Lerp(lightColorDay, lightColorNight, t);
        }

        // Fog º¸°£
        RenderSettings.fogColor = Color.Lerp(dayFogColor, nightFogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(dayFogDensity, nightFogDensity, t);

        DynamicGI.UpdateEnvironment();
    }

    private IEnumerator RainRoutine()
    {
        while (true)
        {
            rainTimer += Time.deltaTime;

            // 10ºÐ¸¶´Ù ³¯¾¾ º¯È­ ½Ãµµ
            if (rainTimer >= 600f)
            {
                rainTimer = 0f;
                HandleRainChance();
            }

            yield return null;
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
        Debug.Log("ºñ°¡ ³»¸®±â ½ÃÀÛÇÕ´Ï´Ù.");
    }

    private void StopRain()
    {
        isRaining = false;
        if (rainParticle != null)
            rainParticle.Stop();
        Debug.Log("ºñ°¡ ±×ÃÆ½À´Ï´Ù.");
    }
}