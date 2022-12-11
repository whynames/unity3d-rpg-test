using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BLINK.RPGBuilder.Combat;
using BLINK.Controller;
using BLINK.RPGBuilder.Templates;
using BLINK.RPGBuilder.Managers;
using BLINK.RPGBuilder.Characters;
using static GameActionsData;
using static RequirementsData;
using BLINK.RPGBuilder.AI;

namespace PixelCrushers.DialogueSystem.RPGBuilderSupport
{

    /// <summary>
    /// Add to Dialogue Manager.
    /// - Integrates with save system.
    /// - Handles NPC dialogue.
    /// - Adds Lua functions.
    /// </summary>
    [RequireComponent(typeof(DialogueSystemController))]
    public class DialogueSystemRPGBuilderBridge : MonoBehaviour
    {

        #region Variables

        protected Dictionary<string, DialogueSystemNpcTemplate> entries = null;

        protected bool didIRegister = false;
        protected static bool s_registeredLua = false;

        protected const string DialogueSystemRPGBuilderSaveDataKey = "dialogue_system";
        protected const string DefaultUnknownValue = "none";

        public Dictionary<string, DialogueSystemNpcTemplate> Entries { get { return entries; } }

        public static DialogueSystemRPGBuilderBridge Instance { get; protected set; }

        #endregion

        #region Unity

        protected virtual void Awake()
        {
            Instance = this;

            // Load entries:
            var editorDATA = Resources.Load<RPGBuilderEditorDATA>("EditorData/RPGBuilderEditorData");
            entries = Resources.LoadAll<DialogueSystemNpcTemplate>(editorDATA.RPGBDatabasePath + "DialogueSystemNpcTemplates").ToDictionary(t => t.entryName, t => t);

            // Register with RPG Builder's save system, dialogue, and Lua functions:
            GameEvents.SaveCharacterData += OnSaveCharacterData;
            GameEvents.LoadCharacterData += OnLoadCharacterData;
            GameEvents.SceneEntered += OnSceneEntered;
            WorldEvents.PlayerInitDialogue += OnPlayerInitDialogue;
            RegisterLuaFunctions();
        }

        protected virtual void OnDestroy()
        {
            GameEvents.SaveCharacterData -= OnSaveCharacterData;
            GameEvents.LoadCharacterData -= OnLoadCharacterData;
            GameEvents.SceneEntered -= OnSceneEntered;
            WorldEvents.PlayerInitDialogue -= OnPlayerInitDialogue;
            UnregisterLuaFunctions();
        }

        #endregion

        #region Save

        protected virtual void OnSaveCharacterData()
        {
            try // Don't let a failure here stop RPG Builder.
            {
                string s = SaveSystem.Serialize(SaveSystem.RecordSavedGameData());
                Character.Instance.CharacterData.CustomStringData[DialogueSystemRPGBuilderSaveDataKey] = s;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Dialogue System: Error saving character data: " + e.Message);
            }
        }

        protected virtual void OnLoadCharacterData()
        {
            try // Don't let a failure here stop RPG Builder.
            {
                string s;
                if (Character.Instance.CharacterData.CustomStringData.TryGetValue(DialogueSystemRPGBuilderSaveDataKey, out s))
                {
                    SaveSystem.ApplySavedGameData(SaveSystem.Deserialize<SavedGameData>(s));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Dialogue System: Error loading character data: " + e.Message);
            }
        }

        // Apply saved game data, and add component that monitors scene unloads so we know when to save data:
        protected virtual void OnSceneEntered(string sceneName)
        {
            SaveSystem.ApplySavedGameData();
            if (FindObjectOfType<SceneUnloadMonitor>() == null)
            {
                new GameObject("DialogueSystemMonitorSceneUnload", typeof(SceneUnloadMonitor));
            }
        }

        #endregion

        #region Dialogue

        protected virtual void OnPlayerInitDialogue(CombatEntity entity)
        {
            if (entries == null) return;

            DialogueSystemNpcTemplate entry;
            if (entries.TryGetValue(entity.GetNPCData().entryName, out entry))
            {
                if (entry.HasInteractionConversation)
                {
                    UIEvents.Instance.OnClosePanel("NPC_Interactions");
                    DialogueManager.StartConversation(entry.Conversation, GameState.playerEntity.transform, entity.transform);
                }
                else if (entry.HasInteractionBark)
                {
                    DialogueManager.Bark(entry.BarkConversation, entity.transform);
                }
            }
        }

        #endregion

        #region Lua Registration

        protected virtual void RegisterLuaFunctions()
        {
            if (s_registeredLua) return;
            s_registeredLua = true;
            didIRegister = true;

            // Player:
            Lua.RegisterFunction(nameof(rpgGetPlayerName), this, SymbolExtensions.GetMethodInfo(() => rpgGetPlayerName()));
            Lua.RegisterFunction(nameof(rpgGetPlayerRace), this, SymbolExtensions.GetMethodInfo(() => rpgGetPlayerRace()));
            Lua.RegisterFunction(nameof(rpgGetPlayerClass), this, SymbolExtensions.GetMethodInfo(() => rpgGetPlayerClass()));
            Lua.RegisterFunction(nameof(rpgGetPlayerLevel), this, SymbolExtensions.GetMethodInfo(() => rpgGetPlayerLevel()));

            // Faction:
            Lua.RegisterFunction(nameof(rpgGetEntityStanceToFaction), this, SymbolExtensions.GetMethodInfo(() => rpgGetEntityStanceToFaction(string.Empty, string.Empty)));
            Lua.RegisterFunction(nameof(rpgChangeFaction), this, SymbolExtensions.GetMethodInfo(() => rpgChangeFaction(string.Empty, string.Empty)));
            Lua.RegisterFunction(nameof(rpgAddFactionPoints), this, SymbolExtensions.GetMethodInfo(() => rpgAddFactionPoints(string.Empty, 0)));
            Lua.RegisterFunction(nameof(rpgResetFactionPoints), this, SymbolExtensions.GetMethodInfo(() => rpgResetFactionPoints(string.Empty)));

            // Inventory:
            Lua.RegisterFunction(nameof(rpgGetEmptySlotsCount), this, SymbolExtensions.GetMethodInfo(() => rpgGetEmptySlotsCount()));
            Lua.RegisterFunction(nameof(rpgGetItemAmount), this, SymbolExtensions.GetMethodInfo(() => rpgGetItemAmount(string.Empty)));
            Lua.RegisterFunction(nameof(rpgAddItem), this, SymbolExtensions.GetMethodInfo(() => rpgAddItem(string.Empty, 0, false)));
            Lua.RegisterFunction(nameof(rpgRemoveItem), this, SymbolExtensions.GetMethodInfo(() => rpgRemoveItem(string.Empty, 0)));
            Lua.RegisterFunction(nameof(rpgAddCurrency), this, SymbolExtensions.GetMethodInfo(() => rpgAddCurrency(string.Empty, 0)));
            Lua.RegisterFunction(nameof(rpgRemoveCurrency), this, SymbolExtensions.GetMethodInfo(() => rpgRemoveCurrency(string.Empty, 0)));

            // Abilities
            Lua.RegisterFunction(nameof(rpgRankUpAbility), this, SymbolExtensions.GetMethodInfo(() => rpgRankUpAbility(string.Empty, string.Empty)));
            Lua.RegisterFunction(nameof(rpgAddAbility), this, SymbolExtensions.GetMethodInfo(() => rpgAddAbility(string.Empty)));
            Lua.RegisterFunction(nameof(rpgIsAbilityKnown), this, SymbolExtensions.GetMethodInfo(() => rpgIsAbilityKnown(string.Empty)));

            // Tree:
            Lua.RegisterFunction(nameof(rpgAddTreePoints), this, SymbolExtensions.GetMethodInfo(() => rpgAddTreePoints(string.Empty, 0)));

            // Skills:
            Lua.RegisterFunction(nameof(rpgIsSkillKnown), this, SymbolExtensions.GetMethodInfo(() => rpgIsSkillKnown(string.Empty)));
            Lua.RegisterFunction(nameof(rpgAddSkill), this, SymbolExtensions.GetMethodInfo(() => rpgAddSkill(string.Empty)));
            Lua.RegisterFunction(nameof(rpgGainLevel), this, SymbolExtensions.GetMethodInfo(() => rpgGainLevel(string.Empty, 0)));
            Lua.RegisterFunction(nameof(rpgGainExperience), this, SymbolExtensions.GetMethodInfo(() => rpgGainExperience(string.Empty, 0)));

            // Recipes:
            Lua.RegisterFunction(nameof(rpgGetRecipeRank), this, SymbolExtensions.GetMethodInfo(() => rpgGetRecipeRank(string.Empty)));
            Lua.RegisterFunction(nameof(rpgRankUpRecipe), this, SymbolExtensions.GetMethodInfo(() => rpgRankUpRecipe(string.Empty, string.Empty)));

            // Combat:
            Lua.RegisterFunction(nameof(rpgEnterCombat), this, SymbolExtensions.GetMethodInfo(() => rpgEnterCombat(string.Empty)));
        }

        protected virtual void UnregisterLuaFunctions()
        {
            if (!(s_registeredLua && didIRegister)) return;
            s_registeredLua = false;
            didIRegister = false;

            Lua.UnregisterFunction(nameof(rpgGetPlayerName));
            Lua.UnregisterFunction(nameof(rpgGetPlayerRace));
            Lua.UnregisterFunction(nameof(rpgGetPlayerClass));
            Lua.UnregisterFunction(nameof(rpgGetPlayerLevel));

            Lua.UnregisterFunction(nameof(rpgGetEntityStanceToFaction));
            Lua.UnregisterFunction(nameof(rpgChangeFaction));
            Lua.UnregisterFunction(nameof(rpgAddFactionPoints));
            Lua.UnregisterFunction(nameof(rpgResetFactionPoints));

            Lua.UnregisterFunction(nameof(rpgGetEmptySlotsCount));
            Lua.UnregisterFunction(nameof(rpgGetItemAmount));
            Lua.UnregisterFunction(nameof(rpgAddItem));
            Lua.UnregisterFunction(nameof(rpgRemoveItem));
            Lua.UnregisterFunction(nameof(rpgAddCurrency));
            Lua.UnregisterFunction(nameof(rpgRemoveCurrency));

            Lua.UnregisterFunction(nameof(rpgRankUpAbility));

            Lua.UnregisterFunction(nameof(rpgAddTreePoints));

            Lua.UnregisterFunction(nameof(rpgGainLevel));
            Lua.UnregisterFunction(nameof(rpgGainExperience));

            Lua.UnregisterFunction(nameof(rpgGetRecipeRank));
            Lua.UnregisterFunction(nameof(rpgRankUpRecipe));

            Lua.UnregisterFunction(nameof(rpgEnterCombat));
        }

        #endregion

        #region Lua Utility

        protected virtual CombatEntity GetEntity(string entityName)
        {
            foreach (CombatEntity entity in GameState.combatEntities)
            {
                if (entity == null) continue;
                if (string.IsNullOrEmpty(entityName) && entity is PlayerCombatEntity)
                {
                    return entity;
                }
                else
                {
                    var aiEntity = entity.GetComponent<AIEntity>();
                    if (aiEntity != null && string.Equals(aiEntity.BehaviorTemplate.entryName, entityName))
                    {
                        return entity;
                    }
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder entity named '{entityName}'");
            return null;
        }

        #endregion

        #region Lua: Player

        public virtual string rpgGetPlayerName()
        {
            return Character.Instance.CharacterData.CharacterName;
        }

        public virtual string rpgGetPlayerRace() // Note: Returns display name.
        {
            RPGRace raceREF = GameDatabase.Instance.GetRaces()[Character.Instance.CharacterData.RaceID];
            return raceREF.displayName;
        }

        public virtual string rpgGetPlayerClass() // Note: Returns display name.
        {
            RPGClass classREF = GameDatabase.Instance.GetClasses()[Character.Instance.CharacterData.ClassID];
            return classREF.displayName;
        }

        public virtual double rpgGetPlayerLevel()
        {
            return Character.Instance.CharacterData.Level;
        }

        #endregion

        #region Lua: Faction

        public virtual string rpgGetEntityStanceToFaction(string entityName, string otherFactionName)
        {
            CombatEntity entity = GetEntity(entityName);
            if (entity == null) return DefaultUnknownValue;
            RPGFaction otherFaction = GetFaction(otherFactionName);
            if (otherFaction == null) return DefaultUnknownValue;
            RPGBFactionStance stance = FactionManager.Instance.GetEntityStanceToFaction(entity, otherFaction);
            return (stance != null) ? stance.entryName : DefaultUnknownValue;
        }

        public virtual void rpgChangeFaction(string entityName, string factionName)
        {
            var entity = GetEntity(entityName);
            if (entity == null) return;
            var faction = GetFaction(factionName);
            if (faction == null) return;
            entity.SetFaction(faction);
        }

        public virtual void rpgAddFactionPoints(string factionName, double amount)
        {
            int factionID = GetFactionID(factionName);
            if (factionID == -1) return;
            FactionManager.Instance.AddFactionPoint(factionID, (int)amount);
        }

        public virtual void rpgResetFactionPoints(string factionName)
        {
            var factionID = GetFactionID(factionName);
            if (factionID == -1) return;
            foreach (CharacterEntries.FactionEntry faction in Character.Instance.CharacterData.Factions)
            {
                if (faction.ID == factionID)
                {
                    faction.currentPoint = 0;
                }
            }
        }

        protected virtual RPGFaction GetFaction(string factionName)
        {
            foreach (var faction in GameDatabase.Instance.GetFactions().Values)
            {
                if (faction != null && string.Equals(faction.entryName, factionName))
                {
                    return faction;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder faction named '{factionName}'");
            return null;
        }

        protected virtual int GetFactionID(string factionName)
        {
            var faction = GetFaction(factionName);
            return (faction != null) ? faction.ID : -1;
        }

        #endregion

        #region Lua: Inventory

        public virtual double rpgGetEmptySlotsCount()
        {
            return InventoryManager.Instance.getEmptySlotsCount();
        }

        public virtual double rpgGetItemAmount(string itemName)
        {
            int itemID = GetItemID(itemName);
            if (itemID == -1) return 0;
            return EconomyUtilities.GetTotalItemCount(itemID);
        }

        public virtual void rpgAddItem(string itemName, double amount, bool automaticallyEquip)
        {
            int itemID = GetItemID(itemName);
            if (itemID == -1) return;
            InventoryManager.Instance.AddItem(itemID, (int)amount, automaticallyEquip, -1);
        }

        public virtual void rpgRemoveItem(string itemName, double amount)
        {
            int itemID = GetItemID(itemName);
            InventoryManager.Instance.RemoveItem(itemID, -1, (int)amount, -1, -1, false);
        }

        protected virtual int GetItemID(string itemName)
        {
            foreach (RPGItem item in GameDatabase.Instance.GetItems().Values)
            {
                if (item != null && string.Equals(item.entryName, itemName))
                {
                    return item.ID;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder item named '{itemName}'");
            return -1;
        }

        public virtual double rpgGetCurrencyAmount(string currencyName)
        {
            RPGCurrency currency = GetCurrency(currencyName);
            if (currency == null) return -1;
            return EconomyUtilities.GetTotalCurrencyOfGroup(currency);
        }

        public virtual void rpgAddCurrency(string currencyName, double amount)
        {
            int currencyID = GetCurrencyID(currencyName);
            if (currencyID == -1) return;
            InventoryManager.Instance.AddCurrency(currencyID, (int)amount);
        }

        public virtual void rpgRemoveCurrency(string currencyName, double amount)
        {
            int currencyID = GetCurrencyID(currencyName);
            if (currencyID == -1) return;
            InventoryManager.Instance.RemoveCurrency(currencyID, (int)amount);
        }

        protected virtual RPGCurrency GetCurrency(string currencyName)
        {
            foreach (RPGCurrency currency in GameDatabase.Instance.GetCurrencies().Values)
            {
                if (currency != null && string.Equals(currency.entryName, currencyName))
                {
                    return currency;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder currency named '{currencyName}'");
            return null;
        }

        protected virtual int GetCurrencyID(string currencyName)
        {
            RPGCurrency currency = GetCurrency(currencyName);
            return (currency != null) ? currency.ID : -1;
        }

        #endregion

        #region Lua: Abilities

        public virtual bool rpgIsAbilityKnown(string abilityName)
        {
            RPGAbility ability = GetAbility(abilityName);
            if (ability == null) return false;
            return CombatUtilities.IsAbilityKnown(ability.ID);
        }

        public virtual void rpgAddAbility(string abilityName)
        {
            RPGAbility ability = GetAbility(abilityName);
            if (ability == null) return;
            var ID = ability.ID;
            var newEntry = new CharacterEntries.AbilityEntry();
            RPGAbility entryREF = GameDatabase.Instance.GetAbilities()[ID];
            newEntry.name = entryREF.entryName;
            newEntry.ID = ID;
            if (entryREF.learnedByDefault)
            {
                newEntry.known = true;
                newEntry.rank = 1;
            }

            if (!GameDatabase.Instance.GetCharacterSettings().NoClasses)
            {
                foreach (var spellbook in GameDatabase.Instance.GetClasses()[Character.Instance.CharacterData.ClassID]
                    .spellbooks)
                {
                    RPGSpellbook spellbookREF = GameDatabase.Instance.GetSpellbooks()[spellbook.spellbookID];
                    foreach (var spellbookNode in spellbookREF.nodeList)
                    {
                        if (spellbookNode.nodeType != RPGSpellbook.SpellbookNodeType.ability) continue;
                        if (spellbookNode.abilityID != ID) continue;
                        if (Character.Instance.CharacterData.Level < spellbookNode.unlockLevel) continue;
                        newEntry.known = true;
                        newEntry.rank = 1;
                    }
                }
            }

            foreach (var t in Character.Instance.CharacterData.WeaponTemplates)
            {
                foreach (var spellbook in GameDatabase.Instance.GetWeaponTemplates()[t.weaponTemplateID].spellbooks)
                {
                    RPGSpellbook spellbookREF = GameDatabase.Instance.GetSpellbooks()[spellbook.spellbookID];
                    foreach (var spellbookNode in spellbookREF.nodeList)
                    {
                        if (spellbookNode.nodeType != RPGSpellbook.SpellbookNodeType.ability) continue;
                        if (spellbookNode.abilityID != ID) continue;
                        if (RPGBuilderUtilities.getWeaponTemplateLevel(t.weaponTemplateID) <
                            spellbookNode.unlockLevel) continue;
                        newEntry.known = true;
                        newEntry.rank = 1;
                    }
                }
            }

            Character.Instance.CharacterData.Abilities.Add(newEntry);
        }

        public virtual void rpgRankUpAbility(string abilityName, string talentTreeName)
        {
            RPGAbility ability = GetAbility(abilityName);
            if (ability == null) return;
            RPGTalentTree tree = GetTree(talentTreeName);
            if (tree == null) return;
            AbilityManager.Instance.RankUpAbility(ability, tree);
        }

        protected virtual RPGAbility GetAbility(string abilityName)
        {
            foreach (var ability in GameDatabase.Instance.GetAbilities().Values)
            {
                if (ability != null && string.Equals(ability.entryName, abilityName))
                {
                    return ability;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder ability named '{abilityName}'");
            return null;
        }

        #endregion

        #region Lua: Tree

        public virtual void rpgAddTreePoints(string treeName, double amount)
        {
            int treeID = GetTreeID(treeName);
            if (treeID == -1) return;
            TreePointsManager.Instance.AddTreePoint((int)treeID, (int)amount);
        }

        protected virtual RPGTalentTree GetTree(string treeName)
        {
            foreach (var tree in GameDatabase.Instance.GetTalentTrees().Values)
            {
                if (tree != null && string.Equals(tree.entryName, treeName))
                {
                    return tree;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder talent tree named '{treeName}'");
            return null;
        }

        protected virtual int GetTreeID(string treeName)
        {
            RPGTalentTree tree = GetTree(treeName);
            return (tree != null) ? tree.treePointAcceptedID : -1;
        }

        #endregion

        #region Lua: Skills

        public virtual bool rpgIsSkillKnown(string skillName)
        {
            int skillID = GetSkillID(skillName);
            if (skillID == -1) return false;
            return Character.Instance.CharacterData.Skills.Find(skill => skill.skillID == skillID) != null;
        }

        public virtual void rpgAddSkill(string skillName)
        {
            var skill = GetSkill(skillName);
            if (skill == null) return;
            var newSkillData = new CharacterEntries.SkillEntry();
            newSkillData.currentSkillLevel = 1;
            newSkillData.currentSkillXP = 0;
            newSkillData.skillID = skill.ID;
            newSkillData.maxSkillXP = GameDatabase.Instance.GetLevels()[skill.levelTemplateID].allLevels[0].XPRequired;
            Character.Instance.CharacterData.Skills.Add(newSkillData);
        }

        public virtual void rpgGainLevel(string skillName, double amount)
        {
            int skillID = GetSkillID(skillName);
            if (skillID == -1) return;
            LevelingManager.Instance.AddSkillLevel(skillID, (int)amount);
        }

        public virtual void rpgGainExperience(string skillName, double amount)
        {
            int skillID = GetSkillID(skillName);
            if (skillID == -1) return;
            LevelingManager.Instance.AddSkillEXP(skillID, (int)amount);
        }

        protected virtual int GetSkillID(string skillName)
        {
            var skill = GetSkill(skillName);
            return (skill != null) ? skill.ID : -1;
        }

        protected virtual RPGSkill GetSkill(string skillName)
        {
            foreach (RPGSkill skill in GameDatabase.Instance.GetSkills().Values)
            {
                if (skill != null && (string.Equals(skill._name.Replace("_SKILL", ""), skillName)))
                {
                    return skill;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder skill named '{skillName}'");
            return null;
        }

        #endregion

        #region Lua: Recipes

        public virtual double rpgGetRecipeRank(string recipeName)
        {
            RPGCraftingRecipe recipe = GetRecipe(recipeName);
            if (recipe == null) return 0;
            foreach (var playerRecipe in Character.Instance.CharacterData.Recipes)
            {
                if (playerRecipe.ID == recipe.ID)
                {
                    return playerRecipe.rank;
                }
            }
            return 0;
        }

        public virtual void rpgRankUpRecipe(string recipeName, string treeName)
        {
            RPGCraftingRecipe recipe = GetRecipe(recipeName);
            if (recipe == null) return;
            RPGTalentTree tree = GetTree(treeName);
            if (tree == null) return;
            CraftingManager.Instance.RankUpRecipe(recipe, tree);
        }

        protected virtual RPGCraftingRecipe GetRecipe(string recipeName)
        {
            foreach (RPGCraftingRecipe recipe in GameDatabase.Instance.GetRecipes().Values)
            {
                if (recipe != null && string.Equals(recipe.entryName, recipeName))
                {
                    return recipe;
                }
            }
            Debug.LogWarning($"Dialogue System: Can't find RPG Builder recipe named '{recipeName}'");
            return null;
        }

        #endregion

        #region Lua: Combat

        public virtual void rpgEnterCombat(string entityName)
        {
            CombatEntity entity = GetEntity(entityName);
            if (entity == null) return;
            entity.EnterCombat();
        }

        #endregion

    }
}
