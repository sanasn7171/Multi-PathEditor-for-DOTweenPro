using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using DG.Tweening;
using DG.DOTweenEditor;
public class DOTweenPathEditor : EditorWindow
{
    #region EditorWindow Initialization
    private Editor editor;
    private ScriptableObject target;
    private SerializedObject so;

    [MenuItem("Window/DOTweenPathEditor")]
    /// <summary>
    /// Excuted when window opend.
    /// </summary>
    public static void Init()
    {
        var window = EditorWindow.GetWindow(typeof(DOTweenPathEditor), true, "Path", true);
        window.Show();
        window.autoRepaintOnSceneChange = true;
    }
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    #endregion

    [System.Serializable]
    public class pathContent
    {
        public DOTweenPath path;
        public Color color = new Color(1, 1, 1, 1);
        public bool isActive;
        public bool isEditable;
        public bool isShowingPoints;

        public pathContent()
        {
        }
        public pathContent(DOTweenPath path)
        {
            this.path = path;
        }
    }

    private GameObject prePathNode; // Check if path node changed.
    public GameObject pathNode; // Multi-Path parent node

    public DOTweenPath path;
    private int prePathAmount = 0;
    public int pathAmount;
    public List<pathContent> selectedPaths = new List<pathContent>();

    Vector2 scrollPosition = Vector2.zero;

    #region Base Function
    private void OnGUI()
    {
        #region Variables Initialization
        target = this;
        so = new SerializedObject(target);
        so.Update();

        EditorGUIUtility.labelWidth = 64;

        SerializedProperty _pathNode = so.FindProperty("pathNode");
        #endregion

        #region Multipath Editing
        EditorGUILayout.ObjectField(_pathNode, label: new GUIContent("Parent Object"));
        pathAmount = EditorGUILayout.IntField(label: "Path Amount", pathAmount, GUILayout.Width(128f));

        // Check if parent node changed.
        if (prePathNode != pathNode)
        {
            selectedPaths.Clear();

            if(pathNode == null)
            {
                pathAmount = 0;
                prePathAmount = 0;
                prePathNode = null;
                return;
            }

            for (int i = 0; i < pathNode.transform.childCount; i++)
            {
                var node = pathNode.transform.GetChild(i).GetComponent<DOTweenPath>();
                if (node != null)
                {
                    selectedPaths.Add(new pathContent(node));
                }
            }
            pathAmount = selectedPaths.Count;
            prePathNode = pathNode;

            so.ApplyModifiedProperties();
            return;
        }

        #endregion

        #region Path editing interface
        if (prePathAmount != pathAmount)
        {
            // Check if path list already exist.
            if (selectedPaths != null)
            {
                for (int i = 0; i < pathAmount; i++)
                {
                    if (i >= selectedPaths.Count)
                    {
                        selectedPaths.Add(new pathContent());
                    }
                }
            }
            else
            {
                selectedPaths = new List<pathContent>(pathAmount);
            }

            prePathAmount = pathAmount;
            so.ApplyModifiedProperties();
            return;
        }

        SerializedProperty _pathList = so.FindProperty("selectedPaths");
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        // Draw path interface
        for (int i = 0; i < pathAmount; i++)
        {
            var element = _pathList.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.PrefixLabel("Path" + (i + 1));
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.ObjectField(element.FindPropertyRelative("path"), label: new GUIContent("Path Object"), GUILayout.Width(256f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            element.FindPropertyRelative("color").colorValue = EditorGUILayout.ColorField(label: "Path Color", element.FindPropertyRelative("color").colorValue, GUILayout.Width(128f));
            element.FindPropertyRelative("isActive").boolValue = EditorGUILayout.Toggle(label: "Show Path", element.FindPropertyRelative("isActive").boolValue);
            if (element.FindPropertyRelative("isActive").boolValue)
            {
                element.FindPropertyRelative("isEditable").boolValue = EditorGUILayout.Toggle(label: "Show Points", element.FindPropertyRelative("isEditable").boolValue);
                element.FindPropertyRelative("isShowingPoints").boolValue = EditorGUILayout.Toggle(label: "Path Details", element.FindPropertyRelative("isShowingPoints").boolValue);
            }
            else
            {
                element.FindPropertyRelative("isEditable").boolValue = false;
                element.FindPropertyRelative("isShowingPoints").boolValue = false;
            }
            EditorGUILayout.EndHorizontal();

            if(element.FindPropertyRelative("path").objectReferenceValue == null)
            {
                continue;
            }

            var _wps = (element.FindPropertyRelative("path").objectReferenceValue as DOTweenPath).wps;
            // Showing waypoints on editing Window
            if (element.FindPropertyRelative("isShowingPoints").boolValue)
            {
                for (int i_wps = 0; i_wps < _wps.Count; i_wps++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Point" + (i_wps + 1));
                    string labelWP = "";
                    Color c = Color.red;
                    var guistyle = new GUIStyle(GUI.skin.label);

                    guistyle.normal.textColor = c;
                    EditorGUILayout.LabelField(labelWP, guistyle);
                    EditorGUILayout.EndHorizontal();
                    _wps[i_wps] = EditorGUILayout.Vector3Field("Position", _wps[i_wps]);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add"))
                {
                    _wps.Add(new Vector3());
                }
                if (GUILayout.Button("Remove"))
                {
                    _wps.RemoveAt(_wps.Count - 1);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        GUILayout.EndScrollView();

        prePathAmount = pathAmount;

        so.ApplyModifiedProperties();
        #endregion
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (selectedPaths != null)
        {
            if (selectedPaths.Count >= 1)
            {
                foreach (var element in selectedPaths)
                {
                    if (element.path != null)
                    {
                        if (!element.isActive)
                        {
                            continue;
                        }
                        DrawPath(element);
                    }
                }
            }
        }
    }
    #endregion

    #region Scene View Drawing
    private void DrawPath(pathContent path)
    {
        var doTweenPath = path.path;
        doTweenPath.pathColor = path.color;
        Handles.color = path.color;

        for (int i = 0; i < doTweenPath.wps.Count; i++)
        {
            // Draw straight Line on Scene VIew
            if (doTweenPath.pathType == PathType.Linear) 
            {
                if (i == 0)
                {
                    Handles.DrawPolyLine(doTweenPath.transform.position, doTweenPath.wps[0]);
                }
                if (i < doTweenPath.wps.Count - 1)
                {
                    Handles.DrawPolyLine(doTweenPath.wps[i], doTweenPath.wps[i + 1]);
                }
            }

            // Draw Handle Object
            if (path.isEditable)
            {
                doTweenPath.wps[i] = Handles.FreeMoveHandle // Create Handle Object
                    (
                        GUIUtility.GetControlID(FocusType.Passive), // Handles ID
                        doTweenPath.wps[i], // Handle's Position
                        Quaternion.identity, // Handle's Quaternion
                        HandleUtility.GetHandleSize(doTweenPath.wps[i]) * .15f,// Handle's Size
                        Vector3.one * .5f, // Cap's Direction
                        new Handles.CapFunction(Handles.SphereHandleCap)
                    );
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(doTweenPath);
            }
        }

        // Draw catmull-rom Line
        if (doTweenPath.pathType == PathType.CatmullRom) 
        {
            DrawCatmullRomLine(doTweenPath);
        }
    }

    /// <summary>
    /// Draw Catmull-Rom Line on Scene VIew
    /// </summary>
    private void DrawCatmullRomLine(DOTweenPath path)
    {
        if (path.wps.Count < 2)
        {
            return;
        }
        Vector3[] wps = NonLinearPath(path);
        Handles.color = path.pathColor;
        for (int i = 0; i < wps.Length - 2; i++)
        {
            Handles.DrawPolyLine(wps[i], wps[i + 1]);
        }

        Handles.DrawPolyLine(wps[wps.Length - 2], path.wps[path.wps.Count - 1]);
    }

    /// <summary>
    /// Return Caculated Catmull-Rom Points in Path.
    /// </summary>
    private Vector3[] NonLinearPath(DOTweenPath p)
    {
        List<Vector3> path = new List<Vector3>(p.wps);
        path.Insert(0, p.transform.position);

        int pointAmount = (p.wps.Count + 1) * 10;
        Vector3[] result = new Vector3[pointAmount + 1];

        for (int i = 0; i < pointAmount; i++)
        {
            float perc = ((float)i / (float)pointAmount);

            result[i] = GetNonLinearPoint(perc, path, p);
        }
        return result;
    }
    /// <summary>
    /// Return Single Point in Catmull-Rom Line.
    /// </summary>
    private Vector3 GetNonLinearPoint(float perc, List<Vector3> wps, DOTweenPath p)
    {
        int num = wps.Count - 1;
        int num2 = (int)Mathf.Floor((perc * num));
        int index = num - 1;
        if (index > num2)
        {
            index = num2;
        }
        float num4 = (perc * num) - index;

        Vector3 vector = (index == 0) ? p.transform.position : wps[index - 1];
        Vector3 vector2 = wps[index];
        Vector3 vector3 = wps[index + 1];
        Vector3 vector4 = ((index + 2) > (wps.Count - 1)) ? wps[wps.Count - 1] : wps[index + 2];

        return (Vector3)(0.5f * (((((((-vector + (3f * vector2)) - (3f * vector3)) + vector4) * ((num4 * num4) * num4)) + (((((2f * vector) - (5f * vector2)) + (4f * vector3)) - vector4) * (num4 * num4))) + ((-vector + vector3) * num4)) + (2f * vector2)));
    }
    #endregion

}