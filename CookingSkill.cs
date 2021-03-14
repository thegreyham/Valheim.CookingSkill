using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Pipakin.SkillInjectorMod;

namespace CookingSkill
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.pipakin.SkillInjectorMod")]
    public class CookingSkill : BaseUnityPlugin
    {
        public const string PluginGUID = "thegreyham.valheim.CookingSkill";
        public const string PluginName = "CookingSkill";
        public const string PluginVersion = "1.0.0";

        private static Harmony harmony;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;

        const int COOKING_SKILL_ID = 483;  //nexus mod id :)

        private void Awake()
        {
            nexusID = Config.Bind<int>("General", "NexusID", 483, "NexusMods ID for updates.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable the mod.");

            if (!modEnabled.Value)
                return;

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // try/catch For debuging via script engine
            try
            {
                SkillInjector.RegisterNewSkill(COOKING_SKILL_ID, "Cooking", "Improves Cooked Food", 1.0f, null, Skills.SkillType.Unarmed);
            }
            catch { }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        private static void Log(string msg)
        {
            Debug.Log($"[{PluginName}] {msg}");
        }

        // ==================================================================== //
        //              COOKING STATION LOGIC                                   //
        // ==================================================================== //

        #region Cooking Station Logic
        // increase cooking skill when placing an item on the cooking station
        [HarmonyPatch(typeof(CookingStation), "UseItem")]
        internal class Patch_CookingStation_UseItem
        {
            static void Postfix(ref bool __result, Humanoid user)
            {
                if (__result)
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, 0.25f);
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
                        ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, 0.75f);
                        break;
                    }
                }
            }
        }
        #endregion


        // ==================================================================== //
        //              FERMENTER STATION LOGIC                                 //
        // ==================================================================== //

        #region Fermenter Logic
        [HarmonyPatch(typeof(Fermenter), "AddItem")]
        internal class Patch_Fermenter_AddItem
        {
            static void Postfix(ref bool __result, Humanoid user)
            {
                if (__result)
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, 0.5f);
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
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, 0.5f);
            }
        }
        #endregion


        // ==================================================================== //
        //              COULDRON STATION LOGIC                                  //
        // ==================================================================== //



        // ==================================================================== //
        //              FOOD BUFF LOGIC                                         //
        // ==================================================================== //

        // All Food will gain a % increase in Duration/HP & Stamina per level
        #region Food Buff Stuff
        [HarmonyPatch(typeof(Player), "EatFood")]
        internal class Patch_Player_EatFood
        {
            static void Prefix(ref Player __instance, ref ItemDrop.ItemData item, ref float[] __state)
            {
                Traverse t_player = Traverse.Create(__instance);
                if (!t_player.Method("CanEat", item, false).GetValue<bool>())
                    return;

                float skillLevel = Player.m_localPlayer.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                float skillModifier = 1f + (0.5f * skillLevel);
                __state = new float[] { item.m_shared.m_food, item.m_shared.m_foodStamina};
                item.m_shared.m_food *= skillModifier;
                item.m_shared.m_foodStamina *= skillModifier;
                Log($"HP Increase from Food :{item.m_shared.m_food}");
                Log($"Stamina Increase from Food :{item.m_shared.m_foodStamina}");
            }

            static void Postfix(ref bool __result, ref ItemDrop.ItemData item, float[] __state)
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
