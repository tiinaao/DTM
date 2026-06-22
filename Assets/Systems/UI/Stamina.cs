using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stamina : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float maxStamina = 3f;
    [SerializeField] private float drainRate = 1f;
    [SerializeField] private float regenRate = 1f;
    [SerializeField] private float minStaminaToStartSprint = 0.1f;
    [SerializeField] private float regenDelay = 2.0f;
    [SerializeField] private AnimReach animReach;

    private bool _isSprinting = false;
    private bool _isExhausted = false;
    private Coroutine _staminaRoutine;
    private float _lastStaminaDrainTime = 0f;

    private float CurrentValue => slider != null ? slider.value : maxStamina;

    public bool CanSprint => !_isExhausted && CurrentValue > minStaminaToStartSprint;
    public bool IsSprinting => _isSprinting;
    public bool IsExhausted => _isExhausted;
    public float CurrentStamina => CurrentValue;
    public bool HasStaminaForJump => CurrentValue > 0.1f;

    private void Awake()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxStamina;
            slider.value = maxStamina;
        }
        _lastStaminaDrainTime = Time.time;

        if (animReach == null)
            animReach = FindAnyObjectByType<AnimReach>();
    }

    private void OnEnable()
    {
        if (_staminaRoutine == null)
            _staminaRoutine = StartCoroutine(StaminaRoutine());
    }

    private void OnDisable()
    {
        if (_staminaRoutine != null)
        {
            StopCoroutine(_staminaRoutine);
            _staminaRoutine = null;
        }
    }

    public bool SetSprinting(bool sprint)
    {
        if (sprint)
        {
            if (!CanSprint)
                return false;
            _isSprinting = true;
            return true;
        }

        _isSprinting = false;
        return true;
    }

    public void UseStamina(float amount)
    {
        if (slider == null) return;
        slider.value = Mathf.Max(0f, slider.value - amount);
        _lastStaminaDrainTime = Time.time;
        if (slider.value <= 0f && _isSprinting)
        {
            _isSprinting = false;
            _isExhausted = true;
        }
    }

    private IEnumerator StaminaRoutine()
    {
        bool wasSlowedLastFrame = false;

        while (true)
        {
            if (slider == null)
            {
                yield return null;
                continue;
            }

            bool isSlowed = animReach != null && animReach.IsInSlowZone;

            if (isSlowed)
            {
                slider.value -= drainRate * Time.deltaTime;
                if (slider.value < 0f)
                    slider.value = 0f;
                if (slider.value <= 0f)
                    _isExhausted = true;
                wasSlowedLastFrame = true;
            }
            else
            {
                if (wasSlowedLastFrame)
                {
                    _lastStaminaDrainTime = Time.time;
                    wasSlowedLastFrame = false;
                }

                if (_isSprinting && slider.value > 0f)
                {
                    slider.value -= drainRate * Time.deltaTime;
                    if (slider.value < 0f)
                        slider.value = 0f;
                    _lastStaminaDrainTime = Time.time;
                    if (slider.value <= 0f)
                    {
                        _isSprinting = false;
                        _isExhausted = true;
                    }
                }
                else if (!_isSprinting && slider.value < maxStamina)
                {
                    float timeSinceDrain = Time.time - _lastStaminaDrainTime;
                    if (timeSinceDrain >= regenDelay)
                    {
                        slider.value += regenRate * Time.deltaTime;
                        if (slider.value > maxStamina)
                            slider.value = maxStamina;

                        if (_isExhausted && slider.value >= minStaminaToStartSprint)
                            _isExhausted = false;
                    }
                }
            }

            yield return null;
        }
    }
}