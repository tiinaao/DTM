using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float damageRate = 1f;
    [SerializeField] private AnimReach animReach;

    [SerializeField] private CharacterController controller;
    [SerializeField] private float minFallDistance = 3f;
    [SerializeField] private float fallDamagePerMeter = 1f;

    private float _fallStartY;
    private bool _wasGrounded;

    private Coroutine _healthRoutine;

    private float CurrentValue => slider != null ? slider.value : maxHealth;

    public float CurrentHealth => CurrentValue;
    public bool IsDead => CurrentValue <= 0f;
    public static Health Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxHealth;
            slider.value = maxHealth;
        }

        if (animReach == null)
            animReach = FindAnyObjectByType<AnimReach>();

        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    public bool IsFull() => slider != null && slider.value >= slider.maxValue;

    public void Heal(float amount)
    {
        if (slider == null) return;
        slider.value = Mathf.Min(slider.value + amount, slider.maxValue);
    }

    private void OnEnable()
    {
        if (_healthRoutine == null)
            _healthRoutine = StartCoroutine(HealthRoutine());
    }

    private void OnDisable()
    {
        if (_healthRoutine != null)
        {
            StopCoroutine(_healthRoutine);
            _healthRoutine = null;
        }
    }

    public void UseHealth(float amount)
    {
        if (slider == null) return;
        slider.value = Mathf.Max(0f, slider.value - amount);
    }

    public void CheckFallDamage(bool isGrounded, float currentY)
    {
        if (_wasGrounded && !isGrounded)
        {
            _fallStartY = currentY;
        }
        else if (!_wasGrounded && isGrounded)
        {
            float fallDistance = _fallStartY - currentY;
            if (fallDistance > minFallDistance)
            {
                float damage = (fallDistance - minFallDistance) * fallDamagePerMeter;
                UseHealth(damage);
            }
        }

        _wasGrounded = isGrounded;
    }

    private IEnumerator HealthRoutine()
    {
        while (true)
        {
            if (slider != null)
            {
                bool inDanger = animReach != null && animReach.IsInSlowZone;

                if (inDanger && slider.value > 0f)
                {
                    slider.value -= damageRate * Time.deltaTime;
                    if (slider.value < 0f)
                        slider.value = 0f;
                }
            }

            yield return null;
        }
    }
}