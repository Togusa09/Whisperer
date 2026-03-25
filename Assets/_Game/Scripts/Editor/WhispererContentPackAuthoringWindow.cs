using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whisperer.Editor
{
    public class WhispererContentPackAuthoringWindow : EditorWindow
    {
        const string StoryEventsAssetPath = "Assets/_Game/Resources/Whisperer/story-events.json";
        static readonly string[] SourceTypeOptions =
        {
            StoryEventMetadataValidator.SourceCanon,
            StoryEventMetadataValidator.SourceLocal,
            StoryEventMetadataValidator.SourceScholarly,
            StoryEventMetadataValidator.SourceInUniverse,
            StoryEventMetadataValidator.SourceGeneratedLetter,
            StoryEventMetadataValidator.SourceSpeculative
        };

        readonly List<StoryEventEntry> workingEntries = new List<StoryEventEntry>();
        Vector2 listScroll;
        Vector2 detailScroll;
        int selectedIndex = -1;
        bool loaded;
        GUIStyle wrappedEntryButtonStyle;
        GUIStyle entryTitleLabelStyle;
        GUIStyle entryMetaLabelStyle;

        [MenuItem("Window/Whisperer/Content Pack Authoring")]
        static void Open()
        {
            WhispererContentPackAuthoringWindow window = GetWindow<WhispererContentPackAuthoringWindow>("Content Pack Authoring");
            window.minSize = new Vector2(900, 560);
            window.Show();
        }

        void OnEnable()
        {
            LoadFromDisk();
        }

        void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (!loaded)
            {
                EditorGUILayout.HelpBox("Unable to load story-events.json. Use Reload after confirming the asset path.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawEntryList();
                DrawEntryEditor();
            }

            DrawSourceTypeHelp();
            DrawMilestoneNote();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    LoadFromDisk();
                }

                if (GUILayout.Button("New Entry", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    AddNewEntry();
                }

                using (new EditorGUI.DisabledScope(selectedIndex < 0 || selectedIndex >= workingEntries.Count))
                {
                    if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        DuplicateSelectedEntry();
                    }

                    if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        DeleteSelectedEntry();
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Entries: {workingEntries.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
            }
        }

        void DrawEntryList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(330)))
            {
                EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                listScroll = EditorGUILayout.BeginScrollView(listScroll, GUI.skin.box, GUILayout.ExpandHeight(true));
                for (int i = 0; i < workingEntries.Count; i++)
                {
                    StoryEventEntry entry = workingEntries[i];
                    string title = string.IsNullOrWhiteSpace(entry.title) ? "(untitled)" : entry.title;
                    string id = string.IsNullOrWhiteSpace(entry.id) ? "(no-id)" : entry.id;
                    string source = string.IsNullOrWhiteSpace(entry.sourceType) ? "(no-source)" : entry.sourceType;

                    DrawEntryRow(i, title, id, source, entry.validFromYear, entry.validFromMonth, entry.validFromDay);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        void DrawEntryEditor()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Entry Details", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                if (selectedIndex < 0 || selectedIndex >= workingEntries.Count)
                {
                    EditorGUILayout.HelpBox("Select an entry to edit, or create a new one.", MessageType.Info);
                    return;
                }

                StoryEventEntry entry = workingEntries[selectedIndex];
                detailScroll = EditorGUILayout.BeginScrollView(detailScroll, GUI.skin.box, GUILayout.ExpandHeight(true));

                entry.id = EditorGUILayout.TextField("ID", entry.id ?? string.Empty);
                entry.title = EditorGUILayout.TextField("Title", entry.title ?? string.Empty);

                EditorGUILayout.LabelField("Description");
                entry.description = EditorGUILayout.TextArea(entry.description ?? string.Empty, GUILayout.MinHeight(100));

                int sourceIndex = GetSourceTypeIndex(entry.sourceType);
                int updatedSourceIndex = EditorGUILayout.Popup("Source Type", sourceIndex, SourceTypeOptions);
                entry.sourceType = SourceTypeOptions[Mathf.Clamp(updatedSourceIndex, 0, SourceTypeOptions.Length - 1)];
                entry.reliability = EditorGUILayout.IntSlider("Reliability", entry.reliability, 0, 100);

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Tags (comma separated)");
                string tagsAsText = string.Join(", ", entry.tags ?? new List<string>());
                string updatedTagsText = EditorGUILayout.TextField(tagsAsText);
                if (!string.Equals(tagsAsText, updatedTagsText, StringComparison.Ordinal))
                {
                    entry.tags = ParseTags(updatedTagsText);
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Valid From", EditorStyles.boldLabel);
                DrawDateFields(ref entry.validFromYear, ref entry.validFromMonth, ref entry.validFromDay);

                entry.hasEndDate = EditorGUILayout.Toggle("Has End Date", entry.hasEndDate);
                if (entry.hasEndDate)
                {
                    EditorGUILayout.LabelField("Valid To", EditorStyles.boldLabel);
                    DrawDateFields(ref entry.validToYear, ref entry.validToMonth, ref entry.validToDay);
                }
                else
                {
                    entry.validToYear = 0;
                    entry.validToMonth = 0;
                    entry.validToDay = 0;
                }

                workingEntries[selectedIndex] = entry;
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawSourceTypeHelp()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Source Type Help", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use one of: canon, local, scholarly, in-universe, generated-letter, speculative.\n" +
                "canon: hard chronology facts\n" +
                "local: regional/place observations\n" +
                "scholarly: academic framing\n" +
                "in-universe: setting-true occult material\n" +
                "generated-letter: runtime generated memory entries\n" +
                "speculative: low-confidence hypotheses",
                MessageType.None);
        }

        void DrawMilestoneNote()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Milestone M1 complete: authoring surface supports create/edit/delete. " +
                "Validation and save flow are added in M2.",
                MessageType.Info);
        }

        void DrawDateFields(ref int year, ref int month, ref int day)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDateField("Year", ref year, 42f, 88f);
                GUILayout.Space(10f);
                DrawDateField("Month", ref month, 48f, 88f);
                GUILayout.Space(10f);
                DrawDateField("Day", ref day, 32f, 76f);
            }
        }

        void DrawEntryRow(int index, string title, string id, string source, int year, int month, int day)
        {
            string metadata = $"{id} | {source} | {year:D4}-{month:D2}-{day:D2}";
            float viewWidth = Mathf.Max(220f, position.width - 610f);
            float contentWidth = Mathf.Max(200f, viewWidth - 34f);
            float titleHeight = entryTitleLabelStyle.CalcHeight(new GUIContent(title), contentWidth);
            float metadataHeight = entryMetaLabelStyle.CalcHeight(new GUIContent(metadata), contentWidth);
            float rowHeight = Mathf.Max(52f, titleHeight + metadataHeight + 16f);

            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight, GUILayout.ExpandWidth(true));
            DrawEntryRowBackground(rowRect, index == selectedIndex);

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                selectedIndex = index;
            }

            Rect contentRect = new Rect(rowRect.x + 8f, rowRect.y + 6f, rowRect.width - 16f, rowRect.height - 12f);
            Rect titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, titleHeight);
            Rect metadataRect = new Rect(contentRect.x, titleRect.yMax + 2f, contentRect.width, metadataHeight);

            GUI.Label(titleRect, title, entryTitleLabelStyle);
            GUI.Label(metadataRect, metadata, entryMetaLabelStyle);

            GUILayout.Space(4f);
        }

        void DrawEntryRowBackground(Rect rowRect, bool selected)
        {
            Color fill = selected
                ? new Color(0.42f, 0.42f, 0.42f, 1f)
                : new Color(0.24f, 0.24f, 0.24f, 1f);

            Color border = selected
                ? new Color(0.60f, 0.60f, 0.60f, 1f)
                : new Color(0.18f, 0.18f, 0.18f, 1f);

            EditorGUI.DrawRect(rowRect, border);

            Rect innerRect = new Rect(rowRect.x + 1f, rowRect.y + 1f, rowRect.width - 2f, rowRect.height - 2f);
            EditorGUI.DrawRect(innerRect, fill);
        }

        static void DrawDateField(string label, ref int value, float labelWidth, float fieldWidth)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(labelWidth + fieldWidth + 10f)))
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));
                value = EditorGUILayout.IntField(value, GUILayout.Width(fieldWidth));
            }
        }

        void EnsureStyles()
        {
            if (wrappedEntryButtonStyle != null) return;

            wrappedEntryButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(8, 8, 6, 6)
            };

            entryTitleLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Overflow
            };

            entryMetaLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Overflow
            };
        }

        static int GetSourceTypeIndex(string sourceType)
        {
            for (int i = 0; i < SourceTypeOptions.Length; i++)
            {
                if (string.Equals(SourceTypeOptions[i], sourceType, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return Array.IndexOf(SourceTypeOptions, StoryEventMetadataValidator.SourceLocal);
        }

        void LoadFromDisk()
        {
            loaded = false;
            workingEntries.Clear();
            selectedIndex = -1;

            if (!File.Exists(StoryEventsAssetPath))
            {
                return;
            }

            string json = File.ReadAllText(StoryEventsAssetPath);
            StoryEventLedgerFile parsed = JsonUtility.FromJson<StoryEventLedgerFile>(json);
            if (parsed?.entries == null)
            {
                loaded = true;
                return;
            }

            for (int i = 0; i < parsed.entries.Count; i++)
            {
                workingEntries.Add(parsed.entries[i].Clone());
            }

            if (workingEntries.Count > 0)
            {
                selectedIndex = 0;
            }

            loaded = true;
        }

        void AddNewEntry()
        {
            StoryEventEntry entry = new StoryEventEntry
            {
                id = "",
                title = "New Entry",
                description = "",
                sourceType = StoryEventMetadataValidator.SourceLocal,
                tags = new List<string>(),
                validFromYear = 1928,
                validFromMonth = 1,
                validFromDay = 1,
                hasEndDate = false,
                reliability = 60
            };

            workingEntries.Add(entry);
            selectedIndex = workingEntries.Count - 1;
        }

        void DuplicateSelectedEntry()
        {
            if (selectedIndex < 0 || selectedIndex >= workingEntries.Count) return;

            StoryEventEntry clone = workingEntries[selectedIndex].Clone();
            clone.id = string.IsNullOrWhiteSpace(clone.id) ? "" : clone.id + "-copy";
            workingEntries.Insert(selectedIndex + 1, clone);
            selectedIndex += 1;
        }

        void DeleteSelectedEntry()
        {
            if (selectedIndex < 0 || selectedIndex >= workingEntries.Count) return;

            workingEntries.RemoveAt(selectedIndex);
            if (workingEntries.Count == 0)
            {
                selectedIndex = -1;
            }
            else
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, workingEntries.Count - 1);
            }
        }

        static List<string> ParseTags(string csv)
        {
            List<string> tags = new List<string>();
            if (string.IsNullOrWhiteSpace(csv)) return tags;

            string[] pieces = csv.Split(',');
            for (int i = 0; i < pieces.Length; i++)
            {
                string tag = pieces[i].Trim();
                if (string.IsNullOrWhiteSpace(tag)) continue;
                if (!tags.Contains(tag)) tags.Add(tag);
            }

            return tags;
        }
    }
}
