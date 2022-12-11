using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BLINK.RPGBuilder.Combat;
using BLINK.RPGBuilder.Templates;

namespace PixelCrushers.DialogueSystem.RPGBuilderSupport
{

    /// <summary>
    /// Adds extra Dialogue System functionality to a CombatEntity.
    /// Currently handles Bark On Idle.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class DialogueSystemForEntity : MonoBehaviour
    {

        protected virtual void Start()
        {
            if (DialogueSystemRPGBuilderBridge.Instance.Entries == null) return;

            DialogueSystemNpcTemplate entry;
            CombatEntity entity = GetComponent<CombatEntity>();
            if (DialogueSystemRPGBuilderBridge.Instance.Entries.TryGetValue(entity.GetNPCData().entryName, out entry))
            {
                if (entry.HasIdleBark)
                {
                    var barkOnIdle = gameObject.AddComponent<BarkOnIdle>();
                    barkOnIdle.conversation = entry.IdleBarkConversation;
                    barkOnIdle.cacheBarkLines = entry.CacheBarks;
                    barkOnIdle.barkOrder = entry.BarkOrder;
                    barkOnIdle.minSeconds = entry.MinIdleBark;
                    barkOnIdle.maxSeconds = entry.MaxIdleBark;
                }
            }
        }

    }
}
