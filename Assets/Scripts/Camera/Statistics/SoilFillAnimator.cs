using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 화분 안 흙 표면의 위치/크기를 서서히 변화시켜 "흙이 채워지는" 애니메이션을 재생한다.
/// (원본 PotSoilFiller를 이름만 바꿔 그대로 복제한 버전. 기능 변경 없음, 구근 감지와는 무관.)
/// </summary>
public class SoilFillAnimator : MonoBehaviour
{
    [Header("Soil Surface")]
    [SerializeField] private Transform soilSurface;
    [SerializeField] private float emptyLocalY = -0.15f;
    [SerializeField] private float fullLocalY = 0.15f;
    [SerializeField] private Vector2 emptyScaleXZ = new Vector2(0.15f, 0.15f);
    [SerializeField] private Vector2 fullScaleXZ = new Vector2(0.8f, 0.8f);

    [Header("Fill")]
    [Min(0.01f)]
    [SerializeField] private float fillDuration = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float fillAmount;
    [SerializeField] private bool playOnStart;
    [SerializeField] private bool hideWhenEmpty = true;

    [Header("Optional Effect")]
    [SerializeField] private ParticleSystem soilParticles;

    [Header("Events")]
    public UnityEvent onFull;

    private bool isFilling;
    private bool fullEventInvoked;

    public float FillAmount => fillAmount;
    public bool IsFilling => isFilling;
    public bool IsFull => fillAmount >= 1f;

    private void Awake()
    {
        ApplyVisual(true);
    }

    private void Start()
    {
        if (playOnStart)
            BeginFill();
    }

    private void Update()
    {
        if (!isFilling)
            return;

        SetFillAmount(fillAmount + Time.deltaTime / Mathf.Max(0.01f, fillDuration));

        if (fillAmount < 1f)
            return;

        isFilling = false;
        StopParticles();

        if (fullEventInvoked)
            return;

        fullEventInvoked = true;
        onFull?.Invoke();
    }

    private void OnValidate()
    {
        fillDuration = Mathf.Max(0.01f, fillDuration);
        fillAmount = Mathf.Clamp01(fillAmount);
        ApplyVisual(false);
    }

    [ContextMenu("Begin Fill")]
    public void BeginFill()
    {
        if (soilSurface == null)
        {
            Debug.LogWarning("[SoilFillAnimator] Soil Surface is not assigned.", this);
            return;
        }

        if (fillAmount >= 1f)
            return;

        isFilling = true;
        fullEventInvoked = false;
        soilSurface.gameObject.SetActive(true);

        if (soilParticles != null && !soilParticles.isPlaying)
            soilParticles.Play();
    }

    public void StopFill()
    {
        isFilling = false;
        StopParticles();
    }

    [ContextMenu("Empty Soil")]
    public void Empty()
    {
        isFilling = false;
        fullEventInvoked = false;
        StopParticles();
        SetFillAmount(0f);
    }

    public void SetFillAmount(float value)
    {
        fillAmount = Mathf.Clamp01(value);
        ApplyVisual(true);

        if (fillAmount < 1f)
            fullEventInvoked = false;
    }

    private void ApplyVisual(bool updateVisibility)
    {
        if (soilSurface == null)
            return;

        Vector3 localPosition = soilSurface.localPosition;
        localPosition.y = Mathf.Lerp(emptyLocalY, fullLocalY, fillAmount);
        soilSurface.localPosition = localPosition;

        Vector3 localScale = soilSurface.localScale;
        localScale.x = Mathf.Lerp(emptyScaleXZ.x, fullScaleXZ.x, fillAmount);
        localScale.z = Mathf.Lerp(emptyScaleXZ.y, fullScaleXZ.y, fillAmount);
        soilSurface.localScale = localScale;

        if (updateVisibility)
            soilSurface.gameObject.SetActive(!hideWhenEmpty || fillAmount > 0.0001f);
    }

    private void StopParticles()
    {
        if (soilParticles != null)
            soilParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}