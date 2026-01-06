using UnityEngine;

public class XRKey : MonoBehaviour
{
    public char letter;

    [Header("Input Tuning")]
    [SerializeField] private float debounceDelay = 0.15f;
    
    private float _nextValidPressTime;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FingerTipCollider fingerTip) && fingerTip.FingerType == FingerType.Index) {
            if (Time.time < _nextValidPressTime) return;

            _nextValidPressTime = Time.time + debounceDelay;
            if (letter == '\\')GameManager.Instance.DeleteCharacter();
            else GameManager.Instance.InsertCharacter(letter);
        }
    }
}
