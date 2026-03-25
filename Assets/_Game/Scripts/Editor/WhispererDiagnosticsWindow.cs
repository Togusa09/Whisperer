using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Whisperer.Editor
{
    public class WhispererDiagnosticsWindow : EditorWindow
    {
        const int TextAreaHeight = 140;

        Vector2 scroll;
        bool autoRefresh = true;
        double nextRefreshTime;

        [MenuItem("Window/Whisperer/Diagnostics")]
        static void Open()
        {
            WhispererDiagnosticsWindow window = GetWindow<WhispererDiagnosticsWindow>("Whisperer Diagnostics");
            window.minSize = new Vector2(560, 460);
            window.Show();
        }

        void OnEnable()
        {
            nextRefreshTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += HandleEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= HandleEditorUpdate;
        }

        void HandleEditorUpdate()
        {
            if (!autoRefresh) return;
            if (EditorApplication.timeSinceStartup < nextRefreshTime) return;

            nextRefreshTime = EditorApplication.timeSinceStartup + 1.0;
            Repaint();
        }

        void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect runtime diagnostics.", MessageType.Info);
                return;
            }

            LetterUiController controller = FindAnyObjectByType<LetterUiController>();
            if (controller == null)
            {
                EditorGUILayout.HelpBox("No LetterUiController found in the current scene.", MessageType.Warning);
                return;
            }

            DrawControls(controller);
            DrawSummary(controller);
            DrawPrompting(controller);
            DrawConsistency(controller);
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                Repaint();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void DrawControls(LetterUiController controller)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("LLM Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            string pauseLabel = controller.DiagnosticsIsPaused ? "Resume LLM" : "Pause LLM";
            if (GUILayout.Button(pauseLabel, GUILayout.Height(24)))
            {
                controller.SetLlmPausedForDiagnostics(!controller.DiagnosticsIsPaused);
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Stop Generation", GUILayout.Height(24)))
            {
                controller.StopGenerationForDiagnostics();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Clear Context", GUILayout.Height(24)))
            {
                controller.ClearContextForDiagnostics();
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawSummary(LetterUiController controller)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Runtime Summary", EditorStyles.boldLabel);

            string model = controller.llmAgent != null && controller.llmAgent.llm != null
                ? controller.llmAgent.llm.model
                : "not configured";
            int chatMessages = controller.llmAgent != null && controller.llmAgent.chat != null ? controller.llmAgent.chat.Count : 0;

            long managedBytes = GC.GetTotalMemory(false);
            long monoBytes = Profiler.GetMonoUsedSizeLong();
            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();

            EditorGUILayout.LabelField("Model", model);
            EditorGUILayout.LabelField("Chat Messages", chatMessages.ToString());
            EditorGUILayout.LabelField("Paused", controller.DiagnosticsIsPaused.ToString());
            EditorGUILayout.LabelField("In Flight", controller.DiagnosticsIsRequestInFlight.ToString());
            EditorGUILayout.LabelField("Fallback Used", controller.DiagnosticsLastFallbackUsed.ToString());
            EditorGUILayout.LabelField("Last Generation (ms)", controller.DiagnosticsLastGenerationMs.ToString());
            EditorGUILayout.LabelField("Last Prompt Chars", controller.DiagnosticsLastPromptChars.ToString());
            EditorGUILayout.LabelField("Last Response Chars", controller.DiagnosticsLastResponseChars.ToString());
            EditorGUILayout.LabelField("Managed Memory", FormatMegabytes(managedBytes) + " MB");
            EditorGUILayout.LabelField("Mono Memory", FormatMegabytes(monoBytes) + " MB");
            EditorGUILayout.LabelField("Allocated Memory", FormatMegabytes(allocatedBytes) + " MB");
        }

        void DrawPrompting(LetterUiController controller)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Prompt + Response", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawTextArea("Last System Prompt", controller.DiagnosticsLastSystemPrompt);
            DrawTextArea("Last User Prompt", controller.DiagnosticsLastUserPrompt);
            DrawTextArea("Last Response", controller.DiagnosticsLastResponse);
            DrawTextArea("Retrieval Trace", controller.DiagnosticsLastRetrievalTrace);
            DrawTextArea("Source Framing", controller.DiagnosticsLastSourceFraming);
            DrawTextArea("Weather Context", controller.DiagnosticsLastWeatherContext);

            LetterPromptBuilder promptBuilder = controller.letterPromptBuilder != null
                ? controller.letterPromptBuilder
                : FindAnyObjectByType<LetterPromptBuilder>();
            if (promptBuilder != null)
            {
                // Keep these as fallbacks if the controller has not populated fields yet.
                if (string.IsNullOrWhiteSpace(controller.DiagnosticsLastRetrievalTrace))
                {
                    DrawTextArea("Retrieval Trace (fallback)", promptBuilder.LastRetrievalTrace);
                }

                if (string.IsNullOrWhiteSpace(controller.DiagnosticsLastSourceFraming))
                {
                    DrawTextArea("Source Framing (fallback)", promptBuilder.LastSourceFraming);
                }

                if (string.IsNullOrWhiteSpace(controller.DiagnosticsLastWeatherContext))
                {
                    DrawTextArea("Weather Context (fallback)", promptBuilder.LastWeatherContext);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawConsistency(LetterUiController controller)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Consistency", EditorStyles.boldLabel);
            DrawTextArea("Last Validation Report", controller.DiagnosticsLastValidationReport);
        }

        static void DrawTextArea(string label, string content)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(string.IsNullOrWhiteSpace(content) ? "n/a" : content, EditorStyles.textArea, GUILayout.Height(TextAreaHeight));
            EditorGUILayout.Space(4);
        }

        static string FormatMegabytes(long bytes)
        {
            double mb = bytes / (1024d * 1024d);
            return mb.ToString("0.0");
        }
    }
}
