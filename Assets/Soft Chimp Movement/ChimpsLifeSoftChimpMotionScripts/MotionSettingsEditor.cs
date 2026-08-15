#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SoftChimpMotion
{
    [CustomEditor(typeof(MotionSettings))]
    public class MotionSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MotionSettings motionSettings = (MotionSettings)target;

            // Draw logo
            string logoPath = "Assets/Soft Chimp Movement/Logos/Recharge Logo 1.png";
            Texture2D logoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);

            if (logoTexture != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    logoTexture, 
                    GUILayout.Width(EditorGUIUtility.currentViewWidth - 25), 
                    GUILayout.Height(logoTexture.height * ((EditorGUIUtility.currentViewWidth - 25) / (float)logoTexture.width))
                );
                GUILayout.EndVertical();

                GUILayout.Space(10f);
            }
            else
            {
                EditorGUILayout.HelpBox($"Logo texture not found at path: {logoPath}", MessageType.Warning);
            }

            // Draw inspector fields
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Apply Movement Profiles", EditorStyles.boldLabel);

            if (motionSettings.movementProfiles != null && motionSettings.movementProfiles.Count > 0)
            {
                foreach (var profileEntry in motionSettings.movementProfiles)
                {
                    if (GUILayout.Button($"Apply '{profileEntry.name}'"))
                    {
                        Undo.RecordObject(motionSettings, $"Apply Movement Profile: {profileEntry.name}");
                        motionSettings.SetMovementMode(profileEntry.name);
                        motionSettings.ApplyMovementSettings(true);
                        EditorUtility.SetDirty(motionSettings);
                        SceneView.RepaintAll();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No movement profiles defined.", MessageType.Info);
            }
        }
    }
}
#endif
