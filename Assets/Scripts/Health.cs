using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float damageRate = 1f;
    [SerializeField] private AnimReach animReach;

    private Coroutine _healthRoutine;

    private float CurrentValue => slider != null ? slider.value : maxHealth;

    public float CurrentHealth => CurrentValue;
    public bool IsDead => CurrentValue <= 0f;

    private void Awake()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxHealth;
            slider.value = maxHealth;
        }

        if (animReach == null)
            animReach = FindAnyObjectByType<AnimReach>();
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