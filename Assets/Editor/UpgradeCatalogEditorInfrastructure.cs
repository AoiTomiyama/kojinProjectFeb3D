using UnityEditor;
using UnityEngine;

/// <summary>一覧と各項目を同じInspectorで編集し、表示・効果・条件の設定先を統一する。</summary>
[CustomEditor(typeof(UpgradeCatalogConfiguration))]
public class UpgradeCatalogEditorInfrastructure : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        UpgradeDefinitionConfiguration[] definitions;
        try { definitions = ((UpgradeCatalogConfiguration)target).CreateSnapshot(); }
        catch (System.InvalidOperationException error)
        {
            EditorGUILayout.HelpBox(error.Message, MessageType.Error);
            return;
        }
        foreach (var definition in definitions)
        {
            EditorGUILayout.LabelField($"{definition.Id}: {definition.Title}", EditorStyles.boldLabel);
            var item = new SerializedObject(definition);
            item.Update();
            var property = item.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false)) EditorGUILayout.PropertyField(property, true);
            item.ApplyModifiedProperties();
        }
    }
}
