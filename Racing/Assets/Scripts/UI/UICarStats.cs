using UnityEngine;
using UnityEngine.UI;

public class UICarStats : MonoBehaviour
{
    [SerializeField] private Slider maxSpeedSlider;
    [SerializeField] private Slider accelerationSlider;
    [SerializeField] private Slider handlingSlider;
    [SerializeField] private Slider difficultySlider;
    
    private float _maxSpeed = 1f;
    private float _acceleration = 1f;
    private float _handling = 1f;
    private float _difficulty = 1f;

    private void Update()
    {
        const float lerpSpeed = 5f;

        maxSpeedSlider.value = Mathf.Lerp(maxSpeedSlider.value, _maxSpeed, Time.deltaTime * lerpSpeed);
        accelerationSlider.value = Mathf.Lerp(accelerationSlider.value, _acceleration, Time.deltaTime * lerpSpeed);
        handlingSlider.value = Mathf.Lerp(handlingSlider.value, _handling, Time.deltaTime * lerpSpeed);
        difficultySlider.value = Mathf.Lerp(difficultySlider.value, _difficulty, Time.deltaTime * lerpSpeed);
    }

    public void SetCar(Car car)
    {
        _maxSpeed = car.maxSpeed;
        _acceleration = car.acceleration;
        _handling = car.handling;
        _difficulty = car.difficulty;
    }
}
