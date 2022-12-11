using System.Collections.Generic;
using BLINK.RPGBuilder.AI;
using BLINK.RPGBuilder.Templates;
using UnityEditor;
using UnityEngine;
using PixelCrushers.DialogueSystem;

//[CreateAssetMenu]
public class RPGBuilderEditorDialogueSystemModule : RPGBuilderEditorModule
{
    private Dictionary<int, DialogueSystemNpcTemplate> entries = new Dictionary<int, DialogueSystemNpcTemplate>();
    private DialogueSystemNpcTemplate currentEntry;

    private ConversationPicker conversationPicker = null;
    private ConversationPicker barkConversationPicker = null;
    private ConversationPicker idleBarkConversationPicker = null;
    private string[] BarkOrderOptions = new string[] { "Random", "Sequential", "First Valid" };

    public override void Initialize()
    {
        LoadEntries();
        if (entries.Count != 0)
        {
            currentEntry = Instantiate(entries[RPGBuilderEditor.Instance.CurrentEntryIndex]);
            RPGBuilderEditor.Instance.CurrentEntry = currentEntry;
        }
        else
        {
            CreateNewEntry();
        }
    }

    public override void InstantiateCurrentEntry(int index)
    {
        if (entries.Count == 0) return;
        currentEntry = Instantiate(entries[index]);
        RPGBuilderEditor.Instance.CurrentEntry = currentEntry;
    }

    public override void LoadEntries()
    {
        Dictionary<int, DialogueSystemNpcTemplate> dictionary = new Dictionary<int, DialogueSystemNpcTemplate>();
        databaseEntries.Clear();
        var allEntries =
            Resources.LoadAll<DialogueSystemNpcTemplate>(RPGBuilderEditor.Instance.EditorData.RPGBDatabasePath +
                                                  AssetFolderName);
        for (var index = 0; index < allEntries.Length; index++)
        {
            var entry = allEntries[index];
            dictionary.Add(index, entry);
            databaseEntries.Add(entry);
        }

        entries = dictionary;
    }

    public override void CreateNewEntry()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.LogError("You cannot interact with the RPG Builder while the editor is compiling");
            return;
        }

        currentEntry = CreateInstance<DialogueSystemNpcTemplate>();
        RPGBuilderEditor.Instance.CurrentEntry = currentEntry;
        RPGBuilderEditor.Instance.CurrentEntryIndex = -1;
    }

    public override bool SaveConditionsMet()
    {
        if (ContainsInvalidCharacters(currentEntry.entryName))
        {
            RPGBuilderEditorUtility.DisplayDialogueWindow("Invalid Characters", "The Name contains invalid characters", "OK");
            return false;
        }
        if (string.IsNullOrEmpty(currentEntry.entryName))
        {
            RPGBuilderEditorUtility.DisplayDialogueWindow("Invalid Name", "Enter a valid name", "OK");
            return false;
        }
        return true;
    }

    public override void UpdateEntryData(RPGBuilderDatabaseEntry updatedEntry)
    {
        DialogueSystemNpcTemplate entryFile = (DialogueSystemNpcTemplate)updatedEntry;
        entryFile.UpdateEntryData(currentEntry);
    }

    public override void ClearEntries()
    {
        databaseEntries.Clear();
        entries.Clear();
        currentEntry = null;
    }

    public override void DrawView()
    {
        if (currentEntry == null)
        {
            if (entries.Count > 0 && entries[0] != null)
            {
                RPGBuilderEditor.Instance.SelectDatabaseEntry(0, true);
            }
            else
            {
                CreateNewEntry();
            }
        }

        RPGBuilderEditorUtility.UpdateViewAndFieldData();

        ScriptableObject scriptableObj = currentEntry;
        var serialObj = new SerializedObject(scriptableObj);

        float topSpace = RPGBuilderEditor.Instance.ButtonHeight + 5;
        GUILayout.Space(topSpace);

        RPGBuilderEditor.Instance.ViewScroll = EditorGUILayout.BeginScrollView(RPGBuilderEditor.Instance.ViewScroll,
            false, false,
            GUILayout.Width(RPGBuilderEditor.Instance.ViewWidth),
            GUILayout.MaxWidth(RPGBuilderEditor.Instance.ViewWidth),
            GUILayout.ExpandHeight(true));

        RPGBuilderEditorUtility.StartHorizontalMargin(RPGBuilderEditor.Instance.LongHorizontalMargin, true);
        currentEntry.entryName = RPGBuilderEditorFields.DrawHorizontalTextField("Name",
            "", RPGBuilderEditor.Instance.FieldHeight + 15, currentEntry.entryName);
        currentEntry.entryFileName = currentEntry.entryName + AssetNameSuffix;

        RPGBuilderEditorFields.DrawTitleLabelExpanded("Conversation", "", true);
        EditorGUI.BeginDisabledGroup(currentEntry.HasInteractionBark);
        currentEntry.HasInteractionConversation =
            RPGBuilderEditorFields.DrawHorizontalToggle("Has Interaction Conversation",
            "NPC plays conversation when player interacts with it. AI > NPCs > FUNCTIONS > Dialogue? must be ticked.",
            RPGBuilderEditor.Instance.FieldHeight, currentEntry.HasInteractionConversation);
        EditorGUI.EndDisabledGroup();
        if (currentEntry.HasInteractionConversation)
        {
            if (conversationPicker == null)
            {
                conversationPicker = new ConversationPicker(null, currentEntry.Conversation, true);
            }
            conversationPicker.Draw(true);
            currentEntry.Conversation = conversationPicker.currentConversation;
        }

        RPGBuilderEditorFields.DrawTitleLabelExpanded("Bark", "", true);
        EditorGUI.BeginDisabledGroup(currentEntry.HasInteractionConversation);
        currentEntry.HasInteractionBark =
            RPGBuilderEditorFields.DrawHorizontalToggle("Has Interaction Bark",
            "NPC plays bark when player interacts with it. AI > NPCs > FUNCTIONS > Dialogue? must be ticked.",
            RPGBuilderEditor.Instance.FieldHeight, currentEntry.HasInteractionBark);
        EditorGUI.EndDisabledGroup();
        if (currentEntry.HasInteractionBark)
        {
            RPGBuilderEditorFields.DrawLabelExpanded("Reminder: NPC's prefab must have a bark UI.", "");
            if (barkConversationPicker == null)
            {
                barkConversationPicker = new ConversationPicker(null, currentEntry.BarkConversation, true);
            }
            barkConversationPicker.Draw(true);
            currentEntry.BarkConversation = barkConversationPicker.currentConversation;
        }

        RPGBuilderEditorFields.DrawTitleLabelExpanded("Idle Bark", "", true);
        currentEntry.HasIdleBark =
            RPGBuilderEditorFields.DrawHorizontalToggle("Has Idle Bark",
            "NPC barks automatically at a specified frequency.",
            RPGBuilderEditor.Instance.FieldHeight, currentEntry.HasIdleBark);
        if (currentEntry.HasIdleBark)
        {
            RPGBuilderEditorFields.DrawLabelExpanded("Reminder: NPC's prefab must have a bark UI & DialogueSystemForEntity component.", "");
            if (idleBarkConversationPicker == null)
            {
                idleBarkConversationPicker = new ConversationPicker(null, currentEntry.IdleBarkConversation, true);
            }
            idleBarkConversationPicker.Draw(true);
            currentEntry.IdleBarkConversation = idleBarkConversationPicker.currentConversation;
            currentEntry.MinIdleBark = RPGBuilderEditorFields.DrawHorizontalFloatField("Min Seconds", "Wait at least this many seconds between barks.",
                RPGBuilderEditor.Instance.FieldHeight, currentEntry.MinIdleBark);
            currentEntry.MaxIdleBark = RPGBuilderEditorFields.DrawHorizontalFloatField("Max Seconds", "Wait no more than this many seconds between barks.",
                RPGBuilderEditor.Instance.FieldHeight, currentEntry.MaxIdleBark);
            currentEntry.BarkOrder = (BarkOrder)RPGBuilderEditorFields.DrawHorizontalEnum("Order", "Order in which to bark lines in conversation",
                (int)currentEntry.BarkOrder, BarkOrderOptions);
            currentEntry.CacheBarks =
                RPGBuilderEditorFields.DrawHorizontalToggle("Cache Barks",
                "Cache bark text for performance. Cached barks will NOT re-evaluate their Conditions.",
                RPGBuilderEditor.Instance.FieldHeight, currentEntry.CacheBarks);
        }

        RPGBuilderEditorUtility.EndHorizontalMargin(RPGBuilderEditor.Instance.LongHorizontalMargin, true);

        serialObj.ApplyModifiedProperties();

        GUILayout.Space(25);
        GUILayout.EndScrollView();
    }
}
