using System;
using UnityEngine;

/// <summary>要求元と同じシーンの必須参照を、件数を検証して取得する。</summary>
public static class SceneReferenceResolverInfrastructure
{
    public static T RequireUnique<T>(Component requester) where T : Component
    {
        var matches = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        T result = null;
        int count = 0;
        // 加算ロードした別シーンのサービスを、このシーンの依存先として使わない。
        foreach (var match in matches)
        {
            if (match.gameObject.scene != requester.gameObject.scene) continue;
            result = match;
            count++;
        }
        if (count != 1)
        {
            throw new InvalidOperationException(
                $"{Describe(requester)}: 同じシーンに {typeof(T).Name} が1件必要です。現在 {count} 件です。");
        }
        return result;
    }

    private static string Describe(Component requester) =>
        $"{requester.gameObject.scene.name}/{requester.name} ({requester.GetType().Name})";
}
