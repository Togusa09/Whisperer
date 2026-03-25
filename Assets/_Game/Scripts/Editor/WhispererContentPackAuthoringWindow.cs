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
        Vector2 importPreviewScroll;
        int selectedIndex = -1;
        bool loaded;
        GUIStyle wrappedEntryButtonStyle;
        GUIStyle entryTitleLabelStyle;
        GUIStyle entryMetaLabelStyle;
        GUIStyle entryStatusLabelStyle;
        DraftValidationState draftValidationState;
        string saveStatusMessage = string.Empty;
        MessageType saveStatusMessageType = MessageType.Info;
        ImportPreviewState importPreviewState;

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
            draftValidationState = ValidateDraftEntries();
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

            DrawValidationPanel();
            DrawImportPreviewPanel();
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

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    SaveToDisk();
                }

                if (GUILayout.Button("Import JSON", EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    BeginImportJson();
                }

                if (GUILayout.Button("Export Backup", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    ExportBackup();
                }

                if (GUILayout.Button("CSV Template", EditorStyles.toolbarButton, GUILayout.Width(95)))
                {
                    ExportCsvTemplate();
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
                    EntryValidationState validation = draftValidationState?.GetEntry(i);

                    DrawEntryRow(i, title, id, source, entry.validFromYear, entry.validFromMonth, entry.validFromDay, validation);
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
                DrawSelectedEntryValidation(draftValidationState?.GetEntry(selectedIndex));
                EditorGUILayout.EndScrollView();
                draftValidationState = ValidateDraftEntries();
            }
        }

        void DrawValidationPanel()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (draftValidationState == null)
            {
                EditorGUILayout.HelpBox("Validation state unavailable.", MessageType.Warning);
                return;
            }

            MessageType summaryType = draftValidationState.HasErrors
                ? MessageType.Error
                : draftValidationState.WarningCount > 0
                    ? MessageType.Warning
                    : MessageType.Info;

            EditorGUILayout.HelpBox(draftValidationState.SummaryText, summaryType);

            if (!string.IsNullOrWhiteSpace(saveStatusMessage))
            {
                EditorGUILayout.HelpBox(saveStatusMessage, saveStatusMessageType);
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

        void DrawImportPreviewPanel()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Import Preview", EditorStyles.boldLabel);

            if (importPreviewState == null)
            {
                EditorGUILayout.HelpBox("No import file selected. Use Import JSON to preview an external content pack before applying it.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Source File", importPreviewState.SourcePath, EditorStyles.wordWrappedMiniLabel);

            ImportMergeMode updatedMergeMode = (ImportMergeMode)EditorGUILayout.EnumPopup("Merge Mode", importPreviewState.MergeMode);
            if (updatedMergeMode != importPreviewState.MergeMode)
            {
                importPreviewState.MergeMode = updatedMergeMode;
                RebuildImportMergeSummary(importPreviewState);
            }

            MessageType previewMessageType = importPreviewState.HasErrors
                ? MessageType.Error
                : importPreviewState.WarningCount > 0
                    ? MessageType.Warning
                    : MessageType.Info;

            EditorGUILayout.HelpBox(importPreviewState.SummaryText, previewMessageType);
            EditorGUILayout.HelpBox(importPreviewState.MergeSummaryText, MessageType.None);

            using (new EditorGUI.DisabledScope(importPreviewState.HasErrors || importPreviewState.AcceptedCount == 0))
            {
                if (GUILayout.Button("Apply Import", GUILayout.Width(110)))
                {
                    ApplyImportPreview();
                }
            }

            importPreviewScroll = EditorGUILayout.BeginScrollView(importPreviewScroll, GUI.skin.box, GUILayout.MinHeight(90), GUILayout.MaxHeight(180));
            EditorGUILayout.SelectableLabel(importPreviewState.Report, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void DrawMilestoneNote()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Milestones M1-M3 complete: authoring surface now supports create/edit/delete, live validation, deterministic save flow, and content-pack import/export preview workflows. " +
                "Pause here for verification before M4 diagnostics integration.",
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

        void DrawEntryRow(int index, string title, string id, string source, int year, int month, int day, EntryValidationState validation)
        {
            string metadata = $"{id} | {source} | {year:D4}-{month:D2}-{day:D2}";
            string statusText = BuildEntryStatusText(validation);
            float viewWidth = Mathf.Max(220f, position.width - 610f);
            float contentWidth = Mathf.Max(200f, viewWidth - 34f);
            float titleHeight = entryTitleLabelStyle.CalcHeight(new GUIContent(title), contentWidth);
            float metadataHeight = entryMetaLabelStyle.CalcHeight(new GUIContent(metadata), contentWidth);
            float statusHeight = string.IsNullOrWhiteSpace(statusText)
                ? 0f
                : entryStatusLabelStyle.CalcHeight(new GUIContent(statusText), contentWidth);
            float rowHeight = Mathf.Max(52f, titleHeight + metadataHeight + statusHeight + 18f);

            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight, GUILayout.ExpandWidth(true));
            DrawEntryRowBackground(rowRect, index == selectedIndex, validation);

            if (GUI.Button(rowRect, GUIContent.none, GUIStyle.none))
            {
                selectedIndex = index;
            }

            Rect contentRect = new Rect(rowRect.x + 8f, rowRect.y + 6f, rowRect.width - 16f, rowRect.height - 12f);
            Rect titleRect = new Rect(contentRect.x, contentRect.y, contentRect.width, titleHeight);
            Rect metadataRect = new Rect(contentRect.x, titleRect.yMax + 2f, contentRect.width, metadataHeight);
            Rect statusRect = new Rect(contentRect.x, metadataRect.yMax + 2f, contentRect.width, statusHeight);

            GUI.Label(titleRect, title, entryTitleLabelStyle);
            GUI.Label(metadataRect, metadata, entryMetaLabelStyle);
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                Color previousColor = GUI.contentColor;
                GUI.contentColor = GetEntryStatusColor(validation);
                GUI.Label(statusRect, statusText, entryStatusLabelStyle);
                GUI.contentColor = previousColor;
            }

            GUILayout.Space(4f);
        }

        void DrawEntryRowBackground(Rect rowRect, bool selected, EntryValidationState validation)
        {
            Color fill = selected
                ? new Color(0.42f, 0.42f, 0.42f, 1f)
                : new Color(0.24f, 0.24f, 0.24f, 1f);

            Color border;
            if (validation != null && validation.Errors.Count > 0)
            {
                border = new Color(0.62f, 0.23f, 0.20f, 1f);
            }
            else if (validation != null && validation.Warnings.Count > 0)
            {
                border = new Color(0.68f, 0.52f, 0.18f, 1f);
            }
            else
            {
                border = selected
                    ? new Color(0.60f, 0.60f, 0.60f, 1f)
                    : new Color(0.18f, 0.18f, 0.18f, 1f);
            }

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

            entryStatusLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                clipping = TextClipping.Clip,
                fontStyle = FontStyle.Italic
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

        DraftValidationState ValidateDraftEntries()
        {
            return ValidateEntries(workingEntries, "authoring draft");
        }

        void DrawSelectedEntryValidation(EntryValidationState entryState)
        {
            if (entryState == null) return;

            if (entryState.Errors.Count == 0 && entryState.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("This entry is valid and ready to save.", MessageType.Info);
                return;
            }

            for (int i = 0; i < entryState.Errors.Count; i++)
            {
                EditorGUILayout.HelpBox(entryState.Errors[i], MessageType.Error);
            }

            for (int i = 0; i < entryState.Warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(entryState.Warnings[i], MessageType.Warning);
            }
        }

        void SaveToDisk()
        {
            draftValidationState = ValidateDraftEntries();
            if (draftValidationState.HasErrors)
            {
                saveStatusMessage = draftValidationState.Report;
                saveStatusMessageType = MessageType.Error;
                return;
            }

            List<StoryEventEntry> sortedEntries = new List<StoryEventEntry>(draftValidationState.NormalizedEntries);
            sortedEntries.Sort(CompareEntriesForSave);

            StoryEventLedgerFile file = new StoryEventLedgerFile
            {
                entries = sortedEntries
            };

            WriteEntriesToPath(GetStoryEventsAbsolutePath(), file.entries, sortEntries: false);

            workingEntries.Clear();
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                workingEntries.Add(sortedEntries[i].Clone());
            }

            if (workingEntries.Count == 0)
            {
                selectedIndex = -1;
            }
            else
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, workingEntries.Count - 1);
            }

            saveStatusMessage = draftValidationState.Report;
            saveStatusMessageType = draftValidationState.WarningCount > 0 ? MessageType.Warning : MessageType.Info;
            draftValidationState = ValidateDraftEntries();
        }

        void LoadFromDisk()
        {
            loaded = false;
            workingEntries.Clear();
            selectedIndex = -1;
            saveStatusMessage = string.Empty;
            importPreviewState = null;

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

        void BeginImportJson()
        {
            string startDirectory = Path.GetDirectoryName(GetStoryEventsAbsolutePath());
            string selectedPath = EditorUtility.OpenFilePanel("Import Story Events JSON", startDirectory, "json");
            if (string.IsNullOrWhiteSpace(selectedPath)) return;

            importPreviewState = BuildImportPreview(selectedPath);
            saveStatusMessage = importPreviewState != null && importPreviewState.HasErrors ? importPreviewState.Report : string.Empty;
            saveStatusMessageType = MessageType.Info;
        }

        ImportPreviewState BuildImportPreview(string path)
        {
            ImportPreviewState preview = new ImportPreviewState
            {
                SourcePath = path,
                MergeMode = ImportMergeMode.UpdateById
            };

            if (!TryLoadLedgerFile(path, out StoryEventLedgerFile importedFile, out string loadError))
            {
                preview.Report = loadError;
                preview.SummaryText = "Import failed: the selected file could not be parsed as a story-event ledger.";
                preview.HasHardFailure = true;
                RebuildImportMergeSummary(preview);
                return preview;
            }

            List<StoryEventEntry> importedEntries = importedFile?.entries ?? new List<StoryEventEntry>();
            DraftValidationState importedValidation = ValidateEntries(importedEntries, "import preview");
            preview.SourceCount = importedValidation.SourceCount;
            preview.AcceptedCount = importedValidation.AcceptedCount;
            preview.WarningCount = importedValidation.WarningCount;
            preview.ValidEntries.AddRange(importedValidation.NormalizedEntries);
            preview.Report = importedValidation.Report;

            Dictionary<string, StoryEventEntry> currentById = BuildEntryLookup(workingEntries);
            Dictionary<string, int> incomingIdCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < preview.ValidEntries.Count; i++)
            {
                StoryEventEntry entry = preview.ValidEntries[i];
                if (!string.IsNullOrWhiteSpace(entry.id))
                {
                    incomingIdCounts.TryGetValue(entry.id, out int existingCount);
                    incomingIdCounts[entry.id] = existingCount + 1;
                }

                if (currentById.TryGetValue(entry.id ?? string.Empty, out StoryEventEntry existingEntry))
                {
                    if (AreEntriesEquivalent(existingEntry, entry))
                    {
                        preview.UnchangedCount += 1;
                    }
                    else
                    {
                        preview.UpdatedCount += 1;
                    }
                }
                else
                {
                    preview.AddedCount += 1;
                }
            }

            foreach (KeyValuePair<string, int> pair in incomingIdCounts)
            {
                if (pair.Value > 1)
                {
                    preview.DuplicateIncomingIds += pair.Value - 1;
                }
            }

            preview.SummaryText = preview.HasErrors
                ? $"Import preview found {preview.SourceCount - preview.AcceptedCount} invalid entries. Fix the source file before applying the import."
                : preview.WarningCount > 0
                    ? $"Import preview accepted {preview.AcceptedCount} entries with {preview.WarningCount} warning(s)."
                    : $"Import preview accepted all {preview.AcceptedCount} entries cleanly.";

            RebuildImportMergeSummary(preview);
            return preview;
        }

        void RebuildImportMergeSummary(ImportPreviewState preview)
        {
            if (preview == null) return;

            if (preview.HasHardFailure)
            {
                preview.MergeSummaryText = "No merge preview available because the file could not be parsed.";
                return;
            }

            int resultCount;
            switch (preview.MergeMode)
            {
                case ImportMergeMode.Append:
                    resultCount = workingEntries.Count + preview.AcceptedCount;
                    preview.MergeSummaryText = $"Append will add {preview.AcceptedCount} validated imported entries to the current draft, resulting in {resultCount} total entries.";
                    break;
                case ImportMergeMode.ReplaceAll:
                    resultCount = preview.AcceptedCount;
                    preview.MergeSummaryText = $"Replace All will discard the current draft and replace it with {resultCount} imported entries.";
                    break;
                default:
                    int updateByIdCount = BuildUpdateByIdMerge(preview.ValidEntries).Count;
                    preview.MergeSummaryText = $"Update By ID will add {preview.AddedCount}, update {preview.UpdatedCount}, leave {preview.UnchangedCount} unchanged, and result in {updateByIdCount} total entries.";
                    break;
            }

            if (preview.DuplicateIncomingIds > 0)
            {
                preview.MergeSummaryText += $" Incoming data contains {preview.DuplicateIncomingIds} duplicate id occurrence(s); later entries win during Update By ID.";
            }
        }

        void ApplyImportPreview()
        {
            if (importPreviewState == null || importPreviewState.HasErrors || importPreviewState.AcceptedCount == 0)
            {
                saveStatusMessage = "Import could not be applied because the preview contains errors or no valid entries.";
                saveStatusMessageType = MessageType.Error;
                return;
            }

            List<StoryEventEntry> mergedEntries;
            switch (importPreviewState.MergeMode)
            {
                case ImportMergeMode.Append:
                    mergedEntries = CloneEntries(workingEntries);
                    mergedEntries.AddRange(CloneEntries(importPreviewState.ValidEntries));
                    break;
                case ImportMergeMode.ReplaceAll:
                    mergedEntries = CloneEntries(importPreviewState.ValidEntries);
                    break;
                default:
                    mergedEntries = BuildUpdateByIdMerge(importPreviewState.ValidEntries);
                    break;
            }

            workingEntries.Clear();
            workingEntries.AddRange(mergedEntries);
            if (workingEntries.Count == 0)
            {
                selectedIndex = -1;
            }
            else
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, workingEntries.Count - 1);
            }

            draftValidationState = ValidateDraftEntries();
            saveStatusMessage = $"Applied import from '{Path.GetFileName(importPreviewState.SourcePath)}'. {importPreviewState.MergeSummaryText}";
            saveStatusMessageType = MessageType.Info;
        }

        void ExportBackup()
        {
            string targetDirectory = Path.GetDirectoryName(GetStoryEventsAbsolutePath());
            string defaultFileName = $"story-events-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            string selectedPath = EditorUtility.SaveFilePanel("Export Story Events Backup", targetDirectory, defaultFileName, "json");
            if (string.IsNullOrWhiteSpace(selectedPath)) return;

            WriteEntriesToPath(selectedPath, workingEntries, sortEntries: true);
            saveStatusMessage = $"Exported backup to '{selectedPath}'.";
            saveStatusMessageType = MessageType.Info;
        }

        void ExportCsvTemplate()
        {
            string targetDirectory = Path.GetDirectoryName(GetStoryEventsAbsolutePath());
            string selectedPath = EditorUtility.SaveFilePanel("Export Story Events CSV Template", targetDirectory, "story-events-template", "csv");
            if (string.IsNullOrWhiteSpace(selectedPath)) return;

            string csv = string.Join(Environment.NewLine, new[]
            {
                "id,title,description,sourceType,tags,validFromYear,validFromMonth,validFromDay,hasEndDate,validToYear,validToMonth,validToDay,reliability",
                "example-townshend,Example Townshend note,Short description here,local,terrain|roads|vermont,1928,5,1,false,0,0,0,85"
            });

            File.WriteAllText(selectedPath, csv + Environment.NewLine);
            saveStatusMessage = $"Exported CSV template to '{selectedPath}'. Use pipe-separated tags in the tags column when preparing external content.";
            saveStatusMessageType = MessageType.Info;
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
            saveStatusMessage = string.Empty;
        }

        void DuplicateSelectedEntry()
        {
            if (selectedIndex < 0 || selectedIndex >= workingEntries.Count) return;

            StoryEventEntry clone = workingEntries[selectedIndex].Clone();
            clone.id = string.IsNullOrWhiteSpace(clone.id) ? "" : clone.id + "-copy";
            workingEntries.Insert(selectedIndex + 1, clone);
            selectedIndex += 1;
            saveStatusMessage = string.Empty;
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

            saveStatusMessage = string.Empty;
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

        static List<StoryEventEntry> CloneEntries(List<StoryEventEntry> source)
        {
            List<StoryEventEntry> clones = new List<StoryEventEntry>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                clones.Add(source[i].Clone());
            }

            return clones;
        }

        List<StoryEventEntry> BuildUpdateByIdMerge(List<StoryEventEntry> importedEntries)
        {
            List<StoryEventEntry> merged = CloneEntries(workingEntries);
            Dictionary<string, int> existingById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < merged.Count; i++)
            {
                string id = merged[i].id ?? string.Empty;
                if (!existingById.ContainsKey(id))
                {
                    existingById[id] = i;
                }
            }

            for (int i = 0; i < importedEntries.Count; i++)
            {
                StoryEventEntry imported = importedEntries[i].Clone();
                string id = imported.id ?? string.Empty;
                if (existingById.TryGetValue(id, out int existingIndex))
                {
                    merged[existingIndex] = imported;
                }
                else
                {
                    existingById[id] = merged.Count;
                    merged.Add(imported);
                }
            }

            return merged;
        }

        static Dictionary<string, StoryEventEntry> BuildEntryLookup(List<StoryEventEntry> entries)
        {
            Dictionary<string, StoryEventEntry> lookup = new Dictionary<string, StoryEventEntry>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entries.Count; i++)
            {
                StoryEventEntry entry = entries[i];
                string id = entry?.id ?? string.Empty;
                if (!lookup.ContainsKey(id))
                {
                    lookup[id] = entry;
                }
            }

            return lookup;
        }

        static bool AreEntriesEquivalent(StoryEventEntry left, StoryEventEntry right)
        {
            if (left == null || right == null) return left == right;
            if (!string.Equals(left.id, right.id, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(left.title, right.title, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.description, right.description, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.sourceType, right.sourceType, StringComparison.OrdinalIgnoreCase)) return false;
            if (left.validFromYear != right.validFromYear || left.validFromMonth != right.validFromMonth || left.validFromDay != right.validFromDay) return false;
            if (left.hasEndDate != right.hasEndDate) return false;
            if (left.validToYear != right.validToYear || left.validToMonth != right.validToMonth || left.validToDay != right.validToDay) return false;
            if (left.reliability != right.reliability) return false;

            int leftTagCount = left.tags?.Count ?? 0;
            int rightTagCount = right.tags?.Count ?? 0;
            if (leftTagCount != rightTagCount) return false;

            for (int i = 0; i < leftTagCount; i++)
            {
                if (!string.Equals(left.tags[i], right.tags[i], StringComparison.Ordinal)) return false;
            }

            return true;
        }

        DraftValidationState ValidateEntries(List<StoryEventEntry> entries, string sourceName)
        {
            DraftValidationState state = new DraftValidationState(entries.Count);
            List<string> allMessages = new List<string>();

            for (int i = 0; i < entries.Count; i++)
            {
                List<string> entryMessages = new List<string>();
                bool isValid = StoryEventMetadataValidator.TryNormalize(entries[i], i, entryMessages, out StoryEventEntry normalizedEntry);
                EntryValidationState entryState = EntryValidationState.Create(isValid, normalizedEntry, entryMessages);
                state.Entries.Add(entryState);

                if (isValid && normalizedEntry != null)
                {
                    state.NormalizedEntries.Add(normalizedEntry);
                }

                allMessages.AddRange(entryMessages);
            }

            state.SourceCount = entries.Count;
            state.AcceptedCount = state.NormalizedEntries.Count;
            state.Report = StoryEventMetadataValidator.BuildSummary(sourceName, state.SourceCount, state.AcceptedCount, allMessages);
            state.WarningCount = 0;
            for (int i = 0; i < state.Entries.Count; i++)
            {
                state.WarningCount += state.Entries[i].Warnings.Count;
            }

            state.SummaryText = state.HasErrors
                ? $"{state.AcceptedCount} of {state.SourceCount} entries are currently valid. Fix invalid entries before saving."
                : state.WarningCount > 0
                    ? $"All {state.SourceCount} entries are valid. {state.WarningCount} warning(s) will be normalized on save."
                    : $"All {state.SourceCount} entries are valid and ready to save.";

            return state;
        }

        static bool TryLoadLedgerFile(string path, out StoryEventLedgerFile file, out string error)
        {
            file = null;
            error = string.Empty;

            try
            {
                string json = File.ReadAllText(path);
                file = JsonUtility.FromJson<StoryEventLedgerFile>(json);
                if (file == null)
                {
                    error = "JSON did not deserialize into a story-event ledger.";
                    return false;
                }

                if (file.entries == null)
                {
                    file.entries = new List<StoryEventEntry>();
                }

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        string GetStoryEventsAbsolutePath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            return Path.Combine(projectRoot, StoryEventsAssetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        void WriteEntriesToPath(string absolutePath, List<StoryEventEntry> sourceEntries, bool sortEntries)
        {
            List<StoryEventEntry> entriesToWrite = CloneEntries(sourceEntries);
            if (sortEntries)
            {
                entriesToWrite.Sort(CompareEntriesForSave);
            }

            StoryEventLedgerFile file = new StoryEventLedgerFile
            {
                entries = entriesToWrite
            };

            string json = JsonUtility.ToJson(file, true);
            File.WriteAllText(absolutePath, json + Environment.NewLine);
            AssetDatabase.Refresh();
        }

        static int CompareEntriesForSave(StoryEventEntry left, StoryEventEntry right)
        {
            int sourceComparison = GetSourceTypeOrder(left?.sourceType).CompareTo(GetSourceTypeOrder(right?.sourceType));
            if (sourceComparison != 0) return sourceComparison;

            int yearComparison = left.validFromYear.CompareTo(right.validFromYear);
            if (yearComparison != 0) return yearComparison;

            int monthComparison = left.validFromMonth.CompareTo(right.validFromMonth);
            if (monthComparison != 0) return monthComparison;

            int dayComparison = left.validFromDay.CompareTo(right.validFromDay);
            if (dayComparison != 0) return dayComparison;

            int idComparison = string.Compare(left.id, right.id, StringComparison.OrdinalIgnoreCase);
            if (idComparison != 0) return idComparison;

            return string.Compare(left.title, right.title, StringComparison.OrdinalIgnoreCase);
        }

        static int GetSourceTypeOrder(string sourceType)
        {
            int index = Array.FindIndex(SourceTypeOptions, option => string.Equals(option, sourceType, StringComparison.OrdinalIgnoreCase));
            return index >= 0 ? index : int.MaxValue;
        }

        static string BuildEntryStatusText(EntryValidationState validation)
        {
            if (validation == null) return string.Empty;
            if (validation.Errors.Count > 0) return validation.Errors[0];
            if (validation.Warnings.Count > 0) return validation.Warnings[0];
            return "Validated";
        }

        static Color GetEntryStatusColor(EntryValidationState validation)
        {
            if (validation != null && validation.Errors.Count > 0)
            {
                return new Color(1f, 0.72f, 0.72f, 1f);
            }

            if (validation != null && validation.Warnings.Count > 0)
            {
                return new Color(1f, 0.90f, 0.62f, 1f);
            }

            return new Color(0.78f, 0.92f, 0.78f, 1f);
        }

        sealed class DraftValidationState
        {
            public readonly List<EntryValidationState> Entries;
            public readonly List<StoryEventEntry> NormalizedEntries;
            public int SourceCount;
            public int AcceptedCount;
            public int WarningCount;
            public string SummaryText;
            public string Report;

            public bool HasErrors => AcceptedCount != SourceCount;

            public DraftValidationState(int capacity)
            {
                Entries = new List<EntryValidationState>(capacity);
                NormalizedEntries = new List<StoryEventEntry>(capacity);
            }

            public EntryValidationState GetEntry(int index)
            {
                if (index < 0 || index >= Entries.Count) return null;
                return Entries[index];
            }
        }

        enum ImportMergeMode
        {
            Append,
            ReplaceAll,
            UpdateById
        }

        sealed class ImportPreviewState
        {
            public string SourcePath;
            public ImportMergeMode MergeMode;
            public readonly List<StoryEventEntry> ValidEntries = new List<StoryEventEntry>();
            public int SourceCount;
            public int AcceptedCount;
            public int WarningCount;
            public int AddedCount;
            public int UpdatedCount;
            public int UnchangedCount;
            public int DuplicateIncomingIds;
            public string SummaryText;
            public string MergeSummaryText;
            public string Report;
            public bool HasHardFailure;

            public bool HasErrors => HasHardFailure || AcceptedCount != SourceCount;
        }

        sealed class EntryValidationState
        {
            public bool IsValid;
            public StoryEventEntry NormalizedEntry;
            public List<string> Errors;
            public List<string> Warnings;

            public static EntryValidationState Create(bool isValid, StoryEventEntry normalizedEntry, List<string> messages)
            {
                EntryValidationState state = new EntryValidationState
                {
                    IsValid = isValid,
                    NormalizedEntry = normalizedEntry,
                    Errors = new List<string>(),
                    Warnings = new List<string>()
                };

                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i].IndexOf("rejected:", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        state.Errors.Add(messages[i]);
                    }
                    else
                    {
                        state.Warnings.Add(messages[i]);
                    }
                }

                if (!isValid && state.Errors.Count == 0)
                {
                    state.Errors.Add("Entry is invalid.");
                }

                return state;
            }
        }
    }
}
