using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using Pipakin.SkillInjectorMod;

namespace CookingSkill
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.pipakin.SkillInjectorMod")]
    public class CookingSkill : BaseUnityPlugin
    {
        public const string PluginGUID = "thegreyham.valheim.CookingSkill";
        public const string PluginName = "Cooking Skill";
        public const string PluginVersion = "1.0.0";

        private static Harmony harmony;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        private static ConfigEntry<float> configCookingStationXPIncrease;
        private static ConfigEntry<float> configCauldronXPIncrease;
        private static ConfigEntry<float> configFermenterXPIncrease;

        private static ConfigEntry<float> configFoodHealthMulitplier;
        private static ConfigEntry<float> configFoodStaminaMulitplier;

        const int COOKING_SKILL_ID = 483;  //nexus mod id :)




        private void Awake()
        {
            nexusID = Config.Bind<int>("General", "NexusID", 483, "NexusMods ID for updates.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable the mod.");

            configCookingStationXPIncrease = Config.Bind<float>("Cooking Skill XP", "CookingStationXP", 1f, "Cooking skill xp gained when using the Cooking Station.");
            configCauldronXPIncrease = Config.Bind<float>("Cooking Skill XP", "CauldronXP", 2f, "Cooking skill xp gained when using the Cauldron.");
            configFermenterXPIncrease = Config.Bind<float>("Cooking Skill XP", "FermenterXP", 6f, "Cooking skill xp gained when fermenting mead.");

            configFoodHealthMulitplier = Config.Bind<float>("Food Effects", "HealthMultiplier", 0.5f, "Buff to Health given when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configFoodStaminaMulitplier = Config.Bind<float>("Food Effects", "StaminaMultiplier", 0.5f, "Buff to Stamina given when consuming food per Cooking Skill Level. 1f = +1% / Level");

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

        private static void Log(string msg)
        {
            Debug.Log($"[{PluginName}] {msg}");
        }

        private static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

        private static Sprite LoadCustomTexture(string filename)
        {
            string str = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filename);
            Log(str);
            if (File.Exists(str))
                return Sprite.Create(LoadTexture(str), new Rect(0.0f, 0.0f, 32f, 32f), Vector2.zero);
            Debug.LogError($"Unable to load skill icon! Make sure you place the {filename} file in the plugins directory!");
            return (Sprite)null;
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
        //              FERMENTER PATCHES                                       //
        // ==================================================================== //

        #region Fermenter Patches

        [HarmonyPatch(typeof(Fermenter), "AddItem")]
        internal class Patch_Fermenter_AddItem
        {
            static void Postfix(ref bool __result, Humanoid user)
            {
                if (__result)
                {
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configFermenterXPIncrease.Value * 0.5f);
                    //Log($"[Add Item to Fermenter] Increase Cooking Skill by {configFermenterXPIncrease.Value * 0.5f}");
                }
            }
        }

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
        //              CAULDRON PATCHES                                        //
        // ==================================================================== //

        #region Cauldron Patches

        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        internal class Patch_InventoryGui_DoCrafting
        {
            static void Postfix(ref InventoryGui __instance, ref Recipe ___m_craftRecipe, Player player)
            {
                if (___m_craftRecipe == null)
                    return;

                bool isCauldron = __instance.m_craftingStationName.text == "Cauldron";
                bool isCauldronRecipe = ___m_craftRecipe.m_craftingStation.m_name == "$piece_cauldron";

                if (!isCauldron || !isCauldronRecipe || (!player.HaveRequirements(___m_craftRecipe, false, 1) && !player.NoCostCheat()))
                    return;

                ((Player)player).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configCauldronXPIncrease.Value);
                //Log($"[Cooked Item on Cauldron] Increase Cooking Skill by {configCauldronXPIncrease.Value}");
            }
        }
        #endregion

        // ==================================================================== //
        //              FOOD BUFF PATCHES                                       //
        // ==================================================================== //

        #region Food Buff Patches

        // All Food will gain a % increase in HP & Stamina per level
        [HarmonyPatch(typeof(Player), "EatFood")]
        internal class Patch_Player_EatFood
        {
            static void Prefix(ref Player __instance, ref ItemDrop.ItemData item, ref float[] __state)
            {
                if (!Traverse.Create(__instance).Method("CanEat", item, false).GetValue<bool>())
                    return;

                float skillLevel = __instance.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                float healthSkillModifier = 1f + (configFoodHealthMulitplier.Value * skillLevel);
                float staminaSkillModifier = 1f + (configFoodStaminaMulitplier.Value * skillLevel);

                __state = new float[] { item.m_shared.m_food, item.m_shared.m_foodStamina};
                item.m_shared.m_food *= healthSkillModifier;
                item.m_shared.m_foodStamina *= staminaSkillModifier;
            }

            static void Postfix(ref ItemDrop.ItemData item, float[] __state)
            {
                if (__state == null || __state.Length == 0)
                    return;
                
                item.m_shared.m_food = __state[0];
                item.m_shared.m_foodStamina = __state[1];
            }
        }

        #endregion
    }
}
