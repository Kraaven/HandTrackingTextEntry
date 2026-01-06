using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneKey : MonoBehaviour
{

    public string SceneToMoveTo;
    public EntryType EntryType;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Button Pressed");
        if (other.TryGetComponent(out FingerTipCollider FingerTip)) {
            GameManager.Instance.entryType = EntryType;
            SceneManager.LoadScene(SceneToMoveTo);
        }
    }
}
