using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public static class StreamingAssetsLoader
{
    public static IEnumerator LoadTextFile(
        string relativePath,
        System.Action<string> onLoaded,
        System.Action<string> onError = null)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error);
            }
            else
            {
                onLoaded?.Invoke(request.downloadHandler.text);
            }
        }
#else
        // Windows, Editor, macOS, etc.
        if (!File.Exists(fullPath))
        {
            onError?.Invoke("File not found: " + fullPath);
            yield break;
        }

        string text = File.ReadAllText(fullPath);
        onLoaded?.Invoke(text);
        yield return null;
#endif
    }
}
