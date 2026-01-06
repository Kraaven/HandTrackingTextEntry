using UnityEngine;

public class ExitSession : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FingerTipCollider FingerTip)) GameManager.Instance.ExitSession();
    }
}
