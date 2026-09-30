using System;
using UnityEngine;

/// <summary>シーンに一つだけ必要なコンポーネントを、初期化時に検証して取得する。</summary>
public static class SceneReferenceResolver
{
    public static T RequireUnique<T>(Component requester) where T : Component
    {
        var matches = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"{requester.name}: {typeof(T).Name} はシーンに1件必要です。現在 {matches.Length} 件です。");
        }
        return matches[0];
    }
}
