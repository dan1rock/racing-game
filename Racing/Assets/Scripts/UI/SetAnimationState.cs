using UnityEngine;

public class SetAnimationState : MonoBehaviour
{
    [SerializeField] private string stateName;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _animator.Play(stateName);
    }
}
