using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Pipakin.SkillInjectorMod;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;

namespace CookingSkill
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.pipakin.SkillInjectorMod")]
    public class CookingSkill : BaseUnityPlugin
    {
        public const string PluginGUID = "thegreyham.valheim.CookingSkill";
        public const string PluginName = "Cooking Skill";
        public const string PluginVersion = "1.1.4";

        private static Harmony harmony;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        private static ConfigEntry<float> configCookingStationXPIncrease;
        private static ConfigEntry<float> configCauldronXPIncrease;
        private static ConfigEntry<float> configFermenterXPIncrease;

        private static ConfigEntry<float> configFoodHealthMulitplier;
        private static ConfigEntry<float> configFoodStaminaMulitplier;
        private static ConfigEntry<float> configFoodDurationMulitplier;
        private static ConfigEntry<float> configFermenterDuration;

        private static float SkillLevel = 0f;
        const int COOKING_SKILL_ID = 483;  // Nexus mod id :)
        private static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

        private void Awake()
        {
            nexusID = Config.Bind<int>("General", "NexusID", 483, "NexusMods ID for updates.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable the mod.");

            configCookingStationXPIncrease = Config.Bind<float>("Cooking Skill XP", "CookingStationXP", 1f, "Cooking skill xp gained when using the Cooking Station.");
            configCauldronXPIncrease = Config.Bind<float>("Cooking Skill XP", "CauldronXP", 2f, "Cooking skill xp gained when using the Cauldron.");
            configFermenterXPIncrease = Config.Bind<float>("Cooking Skill XP", "FermenterXP", 6f, "Cooking skill xp gained when fermenting mead.");

            configFoodHealthMulitplier = Config.Bind<float>("Food Effects", "HealthMultiplier", 0.5f, "Buff to Health given when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configFoodStaminaMulitplier = Config.Bind<float>("Food Effects", "StaminaMultiplier", 0.5f, "Buff to Stamina given when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configFoodDurationMulitplier = Config.Bind<float>("Food Effects", "DurationMultiplier", 1f, "Buff to Food Duration when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configFermenterDuration = Config.Bind<float>("Food Effects", "FermenterDuration", .66f, "Reduces Fermentation duration per Cooking Skill Level. 1f = -1% / Level");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (SkillInjector.GetSkillDef((Skills.SkillType)COOKING_SKILL_ID) == null)
                SkillInjector.RegisterNewSkill(COOKING_SKILL_ID, "Cooking", "Improves Health and Stamina buffs from consuming food", 1.0f, LoadCustomTexture("meat_cooked.png"), Skills.SkillType.Knives);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        private static void Log(object msg)
        {
            Debug.Log($"[{PluginName}] {msg.ToString()}");
        }

        private static void Warn(object msg)
        {
            Debug.LogWarning($"[{PluginName}] {msg.ToString()}");
        }

        private static void LogError(object msg)
        {
            Debug.LogError($"[{PluginName}] {msg.ToString()}");
        }

        private static Sprite LoadCustomTexture(string filename)
        {
            string filepath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "assets", filename);
            if (File.Exists(filepath))
                return Sprite.Create(LoadTexture(filepath), new Rect(0.0f, 0.0f, 64f, 64f), Vector2.zero);
            LogError($"Unable to load skill icon! Make sure you place the {filename} file in the 'Valheim/BepInEx/plugins/assets/' directory!");
            return null;
        }

        private static Texture2D LoadTexture(string filepath)
        {
            if (cachedTextures.ContainsKey(filepath))
                return cachedTextures[filepath];
            Texture2D texture2D = new Texture2D(0, 0);
            ImageConversion.LoadImage(texture2D, File.ReadAllBytes(filepath));
            return texture2D;
        }

        // ==================================================================== //
        //              SPAWN DETAILS                                           //
        // ==================================================================== //
        [HarmonyPatch(typeof(Player), "Update")]
        public static class GetSkillLevel
        {
            private static void Postfix(ref Player __instance)
            {

                SkillLevel = ((Player)__instance).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                Log($"Cooking Skill Level: {SkillLevel}");
            }
        }

        // ==================================================================== //
        //              COOKING STATION PATCHES                                 //
        // ==================================================================== //

        #region Cooking Station Patches

        // increase cooking skill when placing an item on the cooking station
        [HarmonyPatch(typeof(CookingStation), "UseItem")]
        internal class Patch_CookingStation_UseItem
        {
            static void Postfix(ref bool __result, Humanoid user)
            {
                if (__result)
                {
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configCookingStationXPIncrease.Value * 0.25f);
                    //Log($"[Add Item to Cook Station] Increase Cooking Skill by {configCookingStationXPIncrease.Value * 0.25f}");
                }
            }
        }

        // increase cooking skill when removing a successful cooked item from cooking station
        [HarmonyPatch(typeof(CookingStation), "Interact")]
        internal class Patch_CookingStation_Interact
        {
            static void Prefix(ref CookingStation __instance, ref ZNetView ___m_nview, Humanoid user, bool hold)
            {
                if (hold)
                    return;

                Traverse t_cookingStation = Traverse.Create(__instance);
                ZDO zdo = ___m_nview.GetZDO();

                for (int slot = 0; slot < __instance.m_slots.Length; ++slot)
                {
                    string itemName = zdo.GetString(nameof(slot) + slot);
                    bool isItemDone = t_cookingStation.Method("IsItemDone", itemName).GetValue<bool>();

                    if (itemName != "" && itemName != __instance.m_overCookedItem.name && isItemDone)
                    {
                        ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configCookingStationXPIncrease.Value * 0.75f);
                        //Log($"[Removed Cooked Item from Cook Station] Increase Cooking Skill by {configCookingStationXPIncrease.Value * 0.75f}");
                        break;
                    }
                }
            }
        }

        #endregion


        // ==================================================================== //
        //              CAULDRON PATCHES                                        //
        // ==================================================================== //

        #region Cauldron Patches

        // increase cooking skill when making food in the cauldron
        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        internal class Patch_InventoryGui_DoCrafting
        {
            static void Prefix(ref InventoryGui __instance, ref Recipe ___m_craftRecipe, Player player)
            {
                if (___m_craftRecipe == null)
                    return;

                bool isCauldronRecipe = ___m_craftRecipe.m_craftingStation?.m_name == "$piece_cauldron"; 
                bool haveRequirements = player.HaveRequirements(___m_craftRecipe, false, 1) || player.NoCostCheat();

                if (!isCauldronRecipe || !haveRequirements)  
                    return;

                player.RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configCauldronXPIncrease.Value);
                //Log($"[Cooked Item on Cauldron] Increase Cooking Skill by {configCauldronXPIncrease.Value}");
            }
        }

        #endregion


        // ==================================================================== //
        //              FERMENTER PATCHES                                       //
        // ==================================================================== //

        #region Fermenter Patches

        // increase cooking skill when placing a mead base into the fermenter
        [HarmonyPatch(typeof(Fermenter), "AddItem")]
        internal class Patch_Fermenter_AddItem
        {
            static void Postfix(ref bool __result, Humanoid user, ref Fermenter __instance)
            {
                if (__result)
                {
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configFermenterXPIncrease.Value * 0.5f);
                    //Log($"[Add Item to Fermenter] Increase Cooking Skill by {configFermenterXPIncrease.Value * 0.5f}");

                    // Set the fermenter duration when adding new item to fermenter.
                    if (configFermenterDuration.Value <= 0)
                        return;

                    float baseFermenterDuration = 2400f;                   
                    //float skillLevel = ((Player)user).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                    float FoodDurationMultiplier = (100f - ((SkillLevel * 100) * configFermenterDuration.Value)) / 100;
                    float newFermenterDuration = baseFermenterDuration * FoodDurationMultiplier;

                    if (newFermenterDuration <= 10)
                        newFermenterDuration = 10;

                    Log($"Fermenter Duration = {newFermenterDuration}");
                    __instance.m_fermentationDuration = newFermenterDuration;

                }
            }
        }

        // increase cooking skill when removing a fermented mead from the fermenter
        [HarmonyPatch(typeof(Fermenter), "Interact")]
        internal class Patch_Fermenter_Interact
        {
            static void Prefix(ref Fermenter __instance, Humanoid user, bool hold)
            {
                if (hold || !PrivateArea.CheckAccess(__instance.transform.position))
                    return;

                int status = Traverse.Create(__instance).Method("GetStatus").GetValue<int>();
                if (status == 3)    // 3 is the enum value for Ready
                {
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configFermenterXPIncrease.Value * 0.5f);
                    //Log($"[Removed Item from Fermenter] Increase Cooking Skill by {configFermenterXPIncrease.Value * 0.5f}");
                }
            }
        }

        #endregion

        // ==================================================================== //
        //              FOOD BUFF PATCHES                                       //
        // ==================================================================== //

        #region Health & Stamina Food Buff Patches

        // All Food will gain a % increase in HP & Stamina per level
        [HarmonyPatch(typeof(Player), "EatFood")]
        internal class Patch_Player_EatFood
        {
            static void Prefix(ref Player __instance, ref ItemDrop.ItemData item, ref float[] __state)
            {
                if (configFoodHealthMulitplier.Value == 0f && configFoodStaminaMulitplier.Value == 0f)
                    return;

                if (!Traverse.Create(__instance).Method("CanEat", item, false).GetValue<bool>())
                    return;

                //float skillLevel = __instance.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                float healthSkillModifier = 1f + (configFoodHealthMulitplier.Value * SkillLevel);
                float staminaSkillModifier = 1f + (configFoodStaminaMulitplier.Value * SkillLevel);
                float durationSkillModifier = 1f + (configFoodDurationMulitplier.Value * SkillLevel);

                __state = new float[] { item.m_shared.m_food, item.m_shared.m_foodStamina };
                item.m_shared.m_food *= healthSkillModifier;
                item.m_shared.m_foodStamina *= staminaSkillModifier;
                float newBurnTime = ((int)(item.m_shared.m_foodBurnTime * durationSkillModifier * 100)) / 100f;

                Log($"Cooking Skill buffed {item.m_dropPrefab.name}:\nHealth: {__state[0]} -> {item.m_shared.m_food}\nStamina: {__state[1]} -> {item.m_shared.m_foodStamina}\nDuration: {item.m_shared.m_foodBurnTime} sec -> {newBurnTime} sec");
            }

            static void Postfix(ref ItemDrop.ItemData item, float[] __state)
            {
                if (configFoodHealthMulitplier.Value == 0f && configFoodStaminaMulitplier.Value == 0f)
                    return;

                if (__state == null || __state.Length == 0)
                    return;
                
                item.m_shared.m_food = __state[0];
                item.m_shared.m_foodStamina = __state[1];
            }
        }

        #endregion

        #region Food Duration Buff Patches

        [HarmonyPatch(typeof(Player), "UpdateFood")]
        internal class Patch_Player_UpdateFood
        {
            private struct FoodState
            {
                public Player.Food food;
                public float originalBurnTime;
                public FoodState(ref Player.Food _food, float _originalBurnTime)
                {
                    this.food = _food;
                    this.originalBurnTime = _originalBurnTime;
                }
            }

            static void Prefix(ref Player __instance, ref bool forceUpdate, ref List<Player.Food> ___m_foods, ref Dictionary<string, FoodState> __state)
            {
                if (forceUpdate || configFoodDurationMulitplier.Value == 0f)
                    return;

                __state = new Dictionary<string, FoodState>(); ;

                //float skillLevel = __instance.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                float durationSkillModifier = 1f + (configFoodDurationMulitplier.Value * SkillLevel);

                for (int i = 0; i < ___m_foods.Count; i++)
                {
                    Player.Food food = ___m_foods[i];
                    float newBurnTime = food.m_item.m_shared.m_foodBurnTime * durationSkillModifier;

                    __state.Add(food.m_name, new FoodState(ref food, food.m_item.m_shared.m_foodBurnTime));
                    ___m_foods[i].m_item.m_shared.m_foodBurnTime = newBurnTime;
                }               
            }

            static void Postfix(ref List<Player.Food> ___m_foods, ref Dictionary<string, FoodState> __state)
            {
                if (configFoodDurationMulitplier.Value == 0f)
                    return;

                if (__state == null || __state.Count == 0)
                    return;

                List<string> eatenFoodNames = new List<string>();

                for (int i = 0; i < ___m_foods.Count; i++)
                {
                    eatenFoodNames.Add(___m_foods[i].m_name);

                    if (__state.ContainsKey(___m_foods[i].m_name))
                        ___m_foods[i].m_item.m_shared.m_foodBurnTime = __state[___m_foods[i].m_name].originalBurnTime;
                    else
                        Warn($"Player.UpdateFood().Postfix :: __state did not contain {___m_foods[i].m_name}.");
                }

                // Reset burnTime of expired food
                foreach (var stateItem in __state)
                    if (!eatenFoodNames.Contains(stateItem.Key))
                        stateItem.Value.food.m_item.m_shared.m_foodBurnTime = stateItem.Value.originalBurnTime;
            }
        }

        // used to set the Fermenter duration when the game is loaded.
        [HarmonyPatch(typeof(Fermenter), "Awake")]
        public static class ApplyFermenterChanges
        {
            static void Prefix(ref Fermenter __instance)
            {
                if (configFermenterDuration.Value <= 0)
                    return;

                float baseFermenterDuration = 2400f;
                //float skillLevel = ((Player)user).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                float FoodDurationMultiplier = (100f - ((SkillLevel * 100) * configFermenterDuration.Value)) / 100;
                float newFermenterDuration = baseFermenterDuration * FoodDurationMultiplier;

                if (newFermenterDuration <= 10)
                    newFermenterDuration = 10;

                Log($"Fermenter Duration = {newFermenterDuration}");
                __instance.m_fermentationDuration = newFermenterDuration;
            }
        }
        #endregion
    }
}
