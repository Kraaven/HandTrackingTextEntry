using UnityEngine;
using UnityEngine.Events;

public class FingerTipCollider : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<FingerTipCollider, FingerTipCollider> OnFingerTipCollide;
    public UnityEvent<FingerTipCollider, FingerTipCollider> OnFingerTipExit;
    [Header("Collider Type")]
    public HandType HandType;
    public FingerType FingerType;


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FingerTipCollider otherFinger))
        {
            OnFingerTipCollide.Invoke(this, otherFinger);

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FingerTipCollider otherFinger))
        {
            OnFingerTipExit.Invoke(this, otherFinger);

        }
    }
}


public enum HandType { Left, Right}
public enum FingerType { Thumb, Index, Middle, Ring, Little};   
