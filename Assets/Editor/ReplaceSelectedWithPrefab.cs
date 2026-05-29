using UnityEngine;
using UnityEditor;

public class ReplaceSelectedWithPrefab : EditorWindow
{
    private GameObject prefab;

    [MenuItem("Tools/Replace Selected With Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceSelectedWithPrefab>("Replace With Prefab");
    }

    private void OnGUI()
    {
        GUILayout.Label("선택한 오브젝트들을 프리팹으로 교체", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField(
            "New Prefab",
            prefab,
            typeof(GameObject),
            false
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Replace Selected"))
        {
            ReplaceSelected();
        }
    }

    private void ReplaceSelected()
    {
        if (prefab == null)
        {
            Debug.LogWarning("교체할 Prefab을 넣어주세요.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("교체할 오브젝트를 선택해주세요.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Selected With Prefab");

        foreach (GameObject oldObject in selectedObjects)
        {
            Transform oldTransform = oldObject.transform;
            Transform parent = oldTransform.parent;
            int siblingIndex = oldTransform.GetSiblingIndex();

            Vector3 localPosition = oldTransform.localPosition;
            Quaternion localRotation = oldTransform.localRotation;
            Vector3 localScale = oldTransform.localScale;
            string oldName = oldObject.name;

            SpriteRenderer oldRenderer = oldObject.GetComponent<SpriteRenderer>();

            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(newObject, "Create Prefab Replacement");

            newObject.name = oldName;
            newObject.transform.localPosition = localPosition;
            newObject.transform.localRotation = localRotation;
            newObject.transform.localScale = localScale;
            newObject.transform.SetSiblingIndex(siblingIndex);

            SpriteRenderer newRenderer = newObject.GetComponent<SpriteRenderer>();

            if (oldRenderer != null && newRenderer != null)
            {
                newRenderer.sortingLayerID = oldRenderer.sortingLayerID;
                newRenderer.sortingOrder = oldRenderer.sortingOrder;
                newRenderer.flipX = oldRenderer.flipX;
                newRenderer.flipY = oldRenderer.flipY;
                newRenderer.color = oldRenderer.color;
            }

            Undo.DestroyObjectImmediate(oldObject);
        }

        Debug.Log("선택한 오브젝트들을 Prefab으로 교체했습니다.");
    }
}