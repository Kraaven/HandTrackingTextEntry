using UnityEngine;

public class StartSessionKey : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FingerTipCollider FingerTip)) GameManager.Instance.StartSession();
    }
}
        