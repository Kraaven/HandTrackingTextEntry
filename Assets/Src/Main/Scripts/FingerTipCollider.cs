using UnityEngine;
using UnityEngine.Events;

public class FingerTipCollider : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<Collision> OnFingerTipCollide;
    public UnityEvent<Collision> OnFingerTipExit;
    [Header("Collider Type")]
    public HandType HandType;
    public FingerType FingerType;

}

public enum HandType { Left, Right}
public enum FingerType { Thumb, Index, Middle, Ring, Little};   
