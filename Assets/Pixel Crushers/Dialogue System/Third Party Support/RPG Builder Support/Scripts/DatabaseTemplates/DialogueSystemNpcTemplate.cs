using System.Collections.Generic;
using BLINK.RPGBuilder.AI;
using PixelCrushers.DialogueSystem;

namespace BLINK.RPGBuilder.Templates
{
    public enum BarkSource { Conversation, Text }

    public class DialogueSystemNpcTemplate : RPGBuilderDatabaseEntry
    {

        // Conversation:
        public bool HasInteractionConversation;
        [ConversationPopup] public string Conversation;

        // Interaction Bark:
        public bool HasInteractionBark;
        public BarkSource BarkSource;
        [ConversationPopup] public string BarkConversation;
        public string BarkText;

        // Idle Bark:
        public bool HasIdleBark;
        public BarkSource IdleBarkSource;
        [ConversationPopup] public string IdleBarkConversation;
        public string IdleBarkText;
        public float MinIdleBark = 5;
        public float MaxIdleBark = 10;
        public BarkOrder BarkOrder = BarkOrder.Random;
        public bool CacheBarks = false;

        public void UpdateEntryData(DialogueSystemNpcTemplate newEntryData)
        {
            entryName = newEntryData.entryName;
            entryFileName = newEntryData.entryFileName;

            HasInteractionConversation = newEntryData.HasInteractionConversation;
            Conversation = newEntryData.Conversation;

            HasInteractionBark = newEntryData.HasInteractionBark;
            BarkSource = newEntryData.BarkSource;
            BarkConversation = newEntryData.BarkConversation;
            BarkText = newEntryData.BarkText;

            HasIdleBark = newEntryData.HasIdleBark;
            IdleBarkSource = newEntryData.IdleBarkSource;
            IdleBarkConversation = newEntryData.IdleBarkConversation;
            IdleBarkText = newEntryData.IdleBarkText;
            MinIdleBark = newEntryData.MinIdleBark;
            MaxIdleBark = newEntryData.MaxIdleBark;
        }
    }
}
