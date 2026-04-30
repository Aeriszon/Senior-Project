using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CiDy
{
    [CustomEditor(typeof(CiDyRoad))]
    //[CanEditMultipleObjects]
    public class CiDyRoadEditor : Editor
    {
        //Road being Edited
        CiDyRoad road;
        Transform roadTrans;
        //Road Level
        SerializedProperty roadLevel;
        //Lane Type
        SerializedProperty laneType;
        //Light Type
        SerializedProperty lightType;
        //Editor UI Textures for Buttons
        // Define a texture and GUIContent for the Different Editor States
        private Texture regenBtn;
        //GUI content to Nest Textures In.
        private GUIContent regenBtn_con;

        void OnEnable()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            {
                road = target as CiDyRoad;
                if (road)
                {
                    roadTrans = road.transform;
                    //Pre-Defined Spline Meshes
                    //roadLevel = serializedObject.FindProperty("roadLevel");
                    laneType = serializedObject.FindProperty("laneType");
                    //Stop Light Type
                    lightType = serializedObject.FindProperty("lightType");
                    //Grab Texture for Regenerate
                    GrabEditorUITextures();
                }
                //Hide Default Transform
                Hidden = true;
            }
        }

        void GrabEditorUITextures()
        {
            //Load a Texture (Assets/Resources/Textures/texture01.png)
            regenBtn = Resources.Load<Texture2D>("EditorUITextures/RegenerateRoadEditorTexture");
            if (regenBtn == null)
            {
                Debug.LogError("No RegenerateRoadEditorTexture.png");
            }
            //Define a GUIContent which uses the texture
            regenBtn_con = new GUIContent(regenBtn, "Regenerate Road: Allows you to Re-Generate the Current Road");
        }

        private void OnDisable() // *** UNCOMMENTED/ADDED OnDisable to restore default gizmo visibility ***
        {
            Hidden = false;
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }
            serializedObject.Update();
            EditorGUILayout.Space();
            GUILayout.Label("---Road Generation---", EditorStyles.boldLabel);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(regenBtn_con, GUILayout.Width(100), GUILayout.Height(100)))
            {
                if (road)
                {
                    //Call Function
                    road.Regenerate();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            EditorGUILayout.Space();
            GUILayout.Label("---Lane Type Settings---", EditorStyles.boldLabel);
            //Left/Right Hand Traffic
            road.flipStopSign = EditorGUILayout.Toggle("Flip Traffic Sign Side: ", road.flipStopSign);
            //Traffic/Stop Sign Enum
            //Pre-Defined Procedural Spline Mesh
            EditorGUILayout.PropertyField(lightType);
            //Pre-Defined Procedural Spline Mesh
            EditorGUILayout.PropertyField(laneType);
            //EditorGUILayout.PropertyField(roadLevel);
            //Spacing for Road Generation. LaneWidth, Left,Divider Lane, Right Shoulder
            road.laneWidth = EditorGUILayout.FloatField("Lane Width: ", road.laneWidth);
            road.leftShoulderWidth = EditorGUILayout.FloatField("Left Shoulder Width: ", road.leftShoulderWidth);
            road.centerSpacing = EditorGUILayout.FloatField("Divider Lane: ", road.centerSpacing);
            road.rightShoulderWidth = EditorGUILayout.FloatField("Right Shoulder Width: ", road.rightShoulderWidth);
            //Cross Walks At Intersection
            road.crossWalksAtIntersections = EditorGUILayout.Toggle("Cross Walks At Intersections: ", road.crossWalksAtIntersections);
            //One Way
            road.oneWay = EditorGUILayout.Toggle("One-Way: ", road.oneWay);
            EditorGUILayout.Space();
            GUILayout.Label("---Road Mesh Details---", EditorStyles.boldLabel);
            //Segment Length
            road.segmentLength = EditorGUILayout.IntField("Segment Length: ", road.segmentLength);
            road.flattenAmount = EditorGUILayout.IntField("Flatten Amount: ", road.flattenAmount);
            //Uvs Road Set
            road.uvsRoadSet = EditorGUILayout.Toggle("Uvs for Road: ", road.uvsRoadSet);
            EditorGUILayout.Space();
            GUILayout.Label("---Road Materials & Markings---", EditorStyles.boldLabel);
            road.createMarkings = EditorGUILayout.Toggle("Create Road Markings: ", road.createMarkings);
            //Materials that can be set for the Detailed Road Mesh
            EditorGUILayout.Space();
            GUILayout.Label("Road Material", EditorStyles.boldLabel);
            road.roadMaterial = (Material)EditorGUILayout.ObjectField(road.roadMaterial, typeof(Material), false, GUILayout.Width(150));
            EditorGUILayout.Space();
            GUILayout.Label("Divider Lane Material", EditorStyles.boldLabel);
            road.dividerLaneMaterial = (Material)EditorGUILayout.ObjectField(road.dividerLaneMaterial, typeof(Material), false, GUILayout.Width(150));
            EditorGUILayout.Space();
            GUILayout.Label("Shoulder Material", EditorStyles.boldLabel);
            road.shoulderMaterial = (Material)EditorGUILayout.ObjectField(road.shoulderMaterial, typeof(Material), false, GUILayout.Width(150));
            road.stopSign = (GameObject)EditorGUILayout.ObjectField("StopSign/Light", road.stopSign, typeof(GameObject), true);
            //TODO Add Road Level for Ground Support Logic
            /*///Ground support Variables
            GUILayout.Label("---Ground Support Settings---", EditorStyles.boldLabel);
            //TODO Add Road Level for Ground Support Logic
            switch (roadLevel.enumValueIndex)
            {
                case 0:
                    //Path
                    break;
                case 1:
                    //Road
                    //Show Snap to Ground
                    road.snapToGround = EditorGUILayout.Toggle("Contour To Terrain: ", road.snapToGround);
                    break;
                case 2:
                    //Highway
                    break;
            }
            road.supportSideWidth = EditorGUILayout.FloatField("Support Side Width: ", road.supportSideWidth);
            road.supportSideHeight = EditorGUILayout.FloatField("Support Side Height: ", road.supportSideHeight);
            road.beamBaseWidth = EditorGUILayout.FloatField("Beam-Base Width: ", road.beamBaseWidth);
            road.beamBaseHeight = EditorGUILayout.FloatField("Beam-Base Height: ", road.beamBaseHeight);*/
            /*GUILayout.Label("---Default Transform---", EditorStyles.boldLabel);
            if (GUILayout.Button("Show/Hide Default Transform"))
            {
                //Hide Default Transform
                Hidden = !Hidden;
            }*/
            //Add CiDy Spawner
            GUILayout.Label("----------Spawner Add Function----------", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Spawner Spline"))
            {
                road.AddSpawnSpline();
            }
            //Show Active Spawners on this Road
            if (road.spawnerSplines != null && road.spawnerSplines.Count > 0)
            {
                GUILayout.Label("----------Sub Spawner List----------", EditorStyles.boldLabel);
                for (int i = 0; i < road.spawnerSplines.Count; i++)
                {
                    GUILayout.BeginVertical();
                    GUILayout.Label("----------Spawner Remove Function----------", EditorStyles.boldLabel);
                    if (GUILayout.Button("Remove Spawner Spline"))
                    {
                        if (EditorUtility.DisplayDialog("Remove This Spawner?", "Are you sure you want to Remove this spawner from Road?", "Yes", "No"))
                        {
                            road.RemoveSpawnerSpline(i);
                        }
                    }
                    else
                    {
                        //spawnerEditors[i].OnInspectorGUI();
                        CiDySpawner spawner = road.spawnerSplines[i];
                        if (spawner != null)
                        {
                            //Draw Inspector for CiDySpawner
                            CiDySpawnerEditor spawnerEditor = (CiDySpawnerEditor)Editor.CreateEditor(spawner);
                            //spawnerEditor.DrawHeader();
                            spawnerEditor.OnInspectorGUI();
                            DestroyImmediate(spawnerEditor);
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            //Apply Modified Properties
            serializedObject.ApplyModifiedProperties();
            /*GUILayout.Label("---Draw DEFAULT INSPECTOR<REMOVE THIS---", EditorStyles.boldLabel);
            DrawDefaultInspector();*/
        }

        //In Scene Editing
        protected virtual void OnSceneGUI()
        {
            CiDyRoad roadGraph = (CiDyRoad)target;

            if (roadGraph.cpPoints != null && roadGraph.cpPoints.Length > 0)
            {
                Vector3 ourPos = roadGraph.transform.position;

                for (int i = 0; i < roadGraph.cpPoints.Length; i++)
                {
                    // --- Draw Connecting Line ---
                    if (i < roadGraph.cpPoints.Length - 1)
                    {
                        Handles.color = Color.yellow;
                        Handles.DrawLine(roadGraph.cpPoints[i], roadGraph.cpPoints[i + 1]);
                    }

                    // Node Controls first and last position not user.
                    if (i == 0 || i == roadGraph.cpPoints.Length - 1)
                    {
                        continue;
                    }

                    // --- Setup for Handle ---
                    Vector3 curPos = roadGraph.cpPoints[i];
                    Vector3 nxtPos = roadGraph.cpPoints[i + 1];
                    Vector3 fwd = (nxtPos - curPos).normalized;
                    fwd.y = 0;

                    // The actual world position of the road point
                    Vector3 handlePosition = roadTrans.TransformVector(roadGraph.cpPoints[i]) + ourPos;
                    Quaternion handleRotation = Quaternion.LookRotation(fwd, Vector3.up);

                    // Calculate custom handle size
                    float handleSize = HandleUtility.GetHandleSize(handlePosition) * 0.18f;

                    EditorGUI.BeginChangeCheck();

                    // 1. Draw the standard PositionHandle. This gives us the 3-axis movement (X, Y, Z).
                    // We set the color to cyan to differentiate the *axes* from the default yellow.
                    Handles.color = Color.cyan;
                    Vector3 newTargetPosition = Handles.PositionHandle(handlePosition, handleRotation);

                    // 2. Draw a custom DotHandleCap at the exact same (new) position.
                    // This is drawn OVER the small yellow cube cap of the PositionHandle to change its appearance.
                    Handles.color = Color.red; // Use a distinct color for the point visual

                    // We use the position returned by PositionHandle so the visual snaps during the drag operation.
                    Handles.DotHandleCap(
                        GUIUtility.GetControlID(FocusType.Passive),
                        newTargetPosition,
                        Quaternion.identity,
                        handleSize,
                        EventType.Repaint
                    );

                    // 3. Redraw the connected lines to the *new* position to ensure visual continuity during movement
                    Handles.color = Color.yellow;
                    if (i > 0)
                    {
                        Handles.DrawLine(roadGraph.cpPoints[i - 1], newTargetPosition);
                    }
                    if (i < roadGraph.cpPoints.Length - 1)
                    {
                        Handles.DrawLine(newTargetPosition, roadGraph.cpPoints[i + 1]);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(roadGraph, "Change Road Control Point Position");
                        // Assign the world position returned by the handle back to the road point array.
                        roadGraph.cpPoints[i] = newTargetPosition;
                    }
                }
            }
            //Now that the Control Points have been Drawn. Lets Draw its Projected New Position with two lines.
            //Calculate a Single Center Line.
            Vector3[] projectedCenterLine = CiDyUtils.CreateBezier(roadGraph.cpPoints, roadGraph.segmentLength);
            //Controue this 
            roadGraph.graph.ContourPathToTerrain(ref projectedCenterLine, roadGraph.blendingTerrains, !roadGraph.snapToGroundLocal);
            int projectedLength = projectedCenterLine.Length;
            //Now that we Have this Line. Offset it left by half width
            float halfWidth = roadGraph.width / 2;
            Vector3 srtDir = (projectedCenterLine[1] - projectedCenterLine[0]).normalized;
            Vector3 endDir = (projectedCenterLine[projectedLength - 1] - projectedCenterLine[projectedLength - 2]).normalized;

            Vector3[] leftLine = CiDyUtils.OffsetPath(projectedCenterLine, -halfWidth, ref srtDir, ref endDir);
            Vector3[] rightLine = CiDyUtils.OffsetPath(projectedCenterLine, halfWidth, ref srtDir, ref endDir);
            //Show This Line
            for (int i = 0; i < projectedLength; i++)
            {
                if (i < projectedLength - 1)
                {
                    Handles.color = Color.green;
                    //Draw Connecting Line
                    Handles.DrawLine(leftLine[i], leftLine[i + 1]);
                    Handles.DrawLine(rightLine[i], rightLine[i + 1]);
                }
            }
        }

        //Show/Hide Default Transform for Object
        public static bool Hidden
        {
            get
            {
                Type type = typeof(Tools);
                FieldInfo field = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
                return ((bool)field.GetValue(null));
            }
            set
            {
                Type type = typeof(Tools);
                FieldInfo field = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, value);
            }
        }
    }
}