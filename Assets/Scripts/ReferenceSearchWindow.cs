using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class ReferenceSearchWindow : EditorWindow
{
    [MenuItem("Window/Reference Search %#r")]
    static void showWindow()
    {
        var window = GetWindow<ReferenceSearchWindow>();
    }

    /// <summary>
    /// このオブジェクトを参照しているオブジェクトを検索する
    /// </summary>
    Object searchObject;

    /// <summary>
    /// searchObject への参照を置き換える参照
    /// </summary>
    Object replaceReference;
    Vector2 scrollPosition;

	class SearchHitInfo
	{
		public GameObject hitGameObject;
		public Object referencedObject;
		public string hitProperty;
	}

    /// <summary>
    ///  検索にヒットしたゲームオブジェクトのリスト
    /// </summary>
    List<SearchHitInfo> hitInfoList = new List<SearchHitInfo>();

    /// <summary>
    /// エディタの状態追跡用
    /// </summary>
    ActiveEditorTracker tracker = new ActiveEditorTracker();

    void OnEnable()
    {
        // ウインドウタイトル変更
        titleContent = new GUIContent("Reference Search");
		updateSearch();
    }

    void OnInspectorUpdate()
    {
        if (!!tracker.isDirty) {
            Repaint();
        }
    }

    void OnGUI()
    {
        switch (Event.current.type) {
        case EventType.Repaint:
            tracker.ClearDirty();
            break;
        }

		using (var changeScope = new EditorGUI.ChangeCheckScope()) {
			searchObject = EditorGUILayout.ObjectField("Search Object", searchObject, typeof(Object), true);
			replaceReference = EditorGUILayout.ObjectField("Replace Reference", replaceReference, typeof(Object), true);

			if (!!changeScope.changed) {
				updateSearch();
			}
		}

		if (GUILayout.Button("Replace")) {
            var gameObjects = FindObjectsOfType<GameObject>();
            foreach (var gameObject in gameObjects) {
                foreach (var component in gameObject.GetComponents<MonoBehaviour>()) {
                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();
                    iterator.Next(true);
                    while (iterator.Next(true)) {
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference) {
                            if (iterator.objectReferenceValue == searchObject &&
                                iterator.objectReferenceValue != gameObject) {
                                iterator.objectReferenceValue = replaceReference;
                            }
                        }
                    }
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        drawSeparator();

        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition)) {
            if (!!searchObject) {

                foreach (var hitInfo in hitInfoList) {
                    var content = EditorGUIUtility.ObjectContent(hitInfo.referencedObject, typeof(GameObject));

					using (var horizontal = new EditorGUILayout.HorizontalScope()) {
						if (GUILayout.Button(content, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(Screen.width / 2.0f - 6.0f))) {
							EditorGUIUtility.PingObject(hitInfo.hitGameObject);
							Selection.activeGameObject = hitInfo.hitGameObject;
						}

						EditorGUILayout.TextArea(hitInfo.hitProperty, GUILayout.Width(Screen.width / 2.0f - 6.0f));
					}
				}
            }

            scrollPosition = scrollViewScope.scrollPosition;
        }

		if (GUILayout.Button("Refresh")) {
			updateSearch();
		}
	}

    void updateSearch()
    {
        var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        hitInfoList.Clear();
        foreach (var gameObject in gameObjects) {
            foreach (var component in gameObject.GetComponents<MonoBehaviour>()) {
                var serializedObject = new SerializedObject(component);
                var iterator = serializedObject.GetIterator();
                iterator.Next(true);
                while (iterator.Next(true)) {
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference) {
                        if (gameObject.scene.isLoaded &&
							(iterator.objectReferenceValue == searchObject || ((searchObject is GameObject) && ((GameObject)searchObject).GetComponents<MonoBehaviour>().Any(item => item == iterator.objectReferenceValue))) && // GameObjectの場合は、アタッチされているコンポーネントも検索対象に含める
							searchObject != gameObject) {
							var hitInfo = new SearchHitInfo();
							hitInfo.hitGameObject = gameObject;
							hitInfo.referencedObject = iterator.objectReferenceValue;
							hitInfo.hitProperty = iterator.name;
							hitInfoList.Add(hitInfo);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 区切り線の描画
    /// </summary>
    void drawSeparator()
    {
        var lineStyle = new GUIStyle("box");
        lineStyle.border.top = lineStyle.border.bottom = 1;
        lineStyle.margin.top = lineStyle.margin.bottom = 1;
        lineStyle.padding.top = lineStyle.padding.bottom = 1;
        lineStyle.margin.left = lineStyle.margin.right = 0;
        lineStyle.padding.left = lineStyle.padding.right = 0;
        GUILayout.Box(GUIContent.none, lineStyle, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
    }
}
