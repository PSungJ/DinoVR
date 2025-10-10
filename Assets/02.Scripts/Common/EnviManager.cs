using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnviManager : MonoBehaviour
{
    [Header("Skybox Settings")]
    public Material daySkybox;
    public Material nightSkybox;
    private bool isDay = true;

    [Header("Lighting Settings")]
    public Light directionalLight; // Directional Light 참조
    public bool enableFogAtDay = true; // 낮에만 안개 표시
    public bool disableFogAtNight = true; // 밤에 안개 끔

    [Header("Rain Settings")]
    public ParticleSystem rainParticle;
    private bool isRaining = false;

    [Header("Cycle Settings")]
    public float cycleDuration = 600f; // 10분 (600초)

    private void Start()
    {
        RenderSettings.skybox = daySkybox;
        RenderSettings.fog = enableFogAtDay;
        if (directionalLight != null)
            directionalLight.enabled = true;

        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(cycleDuration);
            SwitchDayNight();
            HandleRainChance();
        }
    }

    private void SwitchDayNight()
    {
        isDay = !isDay;
        // true : daySkybox, false : nightSkybox
        RenderSettings.skybox = isDay ? daySkybox : nightSkybox;

        // 낮/밤에 따라 라이트 On/Off, 안개 조정
        if (directionalLight != null)
            directionalLight.enabled = isDay;

        // disableFogAtNight은 끄는 조건이기에 "켜는 설정값(RenderSettings.fog)"에 넣을 때는 논리 반전(!) 이 필요
        RenderSettings.fog = isDay ? enableFogAtDay : !disableFogAtNight;

        // 즉시 환경 반영
        /* DynamicGI(Dynamic Global Illumination) : 실시간 조명 및 반사 환경을 갱신하거나 제어하기 위한 유니티의 핵심 유틸리티 클래스
           주로 라이트맵이 아닌, 실시간으로 변화하는 조명/환경을 다룰 때 사용 */
        DynamicGI.UpdateEnvironment();

        Debug.Log(isDay ? "낮으로 전환되었습니다." : "밤으로 전환되었습니다.");
    }

    private void HandleRainChance()
    {
        if (!isRaining)
        {
            // 비가 안 오는 중 → 10% 확률로 시작
            if (Random.value <= 0.1f) // float 0 ~ 1 값 사이 랜덤 값 추출 
                StartRain();
        }
        else
        {
            // 비 오는 중 → 10% 확률로 종료
            if (Random.value <= 0.1f)
                StopRain();
        }
    }

    private void StartRain()
    {
        isRaining = true;
        if (rainParticle != null)
            rainParticle.Play();

        Debug.Log("비가 내리기 시작합니다.");
    }

    private void StopRain()
    {
        isRaining = false;
        if (rainParticle != null)
            rainParticle.Stop();

        Debug.Log("비가 그쳤습니다.");
    }
}
