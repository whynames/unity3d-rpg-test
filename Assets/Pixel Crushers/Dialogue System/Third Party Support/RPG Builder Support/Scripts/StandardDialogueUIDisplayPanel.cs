using System.Collections;
using BLINK.RPGBuilder.Characters;
using BLINK.RPGBuilder.LogicMono;
using UnityEngine;
using BLINK.RPGBuilder.UI;
using BLINK.RPGBuilder.Managers;

namespace PixelCrushers.DialogueSystem.RPGBuilderSupport
{

    /// <summary>
    /// Adds an RPG Builder DisplayPanel to a dialogue UI so RPG Builder knows
    /// to pause the player and show the mouse while the dialogue UI is open.
    /// </summary>
    [RequireComponent(typeof(StandardDialogueUI))]
    public class StandardDialogueUIDisplayPanel : DisplayPanel
    {

        [SerializeField] private CanvasGroup thisCG;

        private WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

        private void OnEnable()
        {
            var dialogueUI = GetComponent<StandardDialogueUI>();
            var mainPanel = dialogueUI.conversationUIElements.mainPanel;
            mainPanel.onOpen.AddListener(Show);
            mainPanel.onClose.AddListener(Hide);

            GameEvents.NewGameSceneLoaded += Register;

            if (GameState.IsInGame())
            {
                Register();
            }
        }

        private void OnDisable()
        {
            GameEvents.NewGameSceneLoaded -= Register;
            Unregister();
        }

        protected override void Register()
        {
            UIEvents.Instance.AddPanelEntry(this, gameObject.name, null);
        }

        protected override void Unregister()
        {
            UIEvents.Instance.RemovePanelEntry(this, gameObject.name);
        }

        public override bool IsOpen()
        {
            return opened;
        }

        public override void Show()
        {
            // Wait for end of frame in case NPCInteractionsPanel closes and hides cursor after this Show().
            StartCoroutine(ShowAtEndOfFrame());
        }

        protected virtual IEnumerator ShowAtEndOfFrame()
        {
            yield return endOfFrame;
            base.Show();
            RPGBuilderUtilities.EnableCG(thisCG);
            transform.SetAsLastSibling();
            if (GameState.playerEntity != null) GameState.playerEntity.controllerEssentials.GameUIPanelAction(opened);
        }

        public override void Hide()
        {
            base.Hide();
            gameObject.transform.SetAsFirstSibling();
            RPGBuilderUtilities.DisableCG(thisCG);
            if (CustomInputManager.Instance != null) CustomInputManager.Instance.HandleUIPanelClose(thisCG);
        }

    }
}
