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

        // increase cooking skill when placing an item on the cooking station
        [HarmonyPatch(typeof(CookingStation), "CookItem")]
        internal class Patch_CookingStation_CookItem
        {
            static void Postfix(ref bool __result)
            {
                if (__result)
                    Player.m_localPlayer.RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, 0.25f);
            }
        }

        // increase cooking skill when removing a successful cooked item from cooking station
        [HarmonyPatch(typeof(CookingStation), "RPC_RemoveDoneItem")]
        internal class Patch_CookingStation_RPC_RemoveDoneItem
        {
            static void Prefix(ref CookingStation __instance, ref ZNetView ___m_nview)
            {
                Traverse t_cookingStation = Traverse.Create(__instance);
                ZDO zdo = ___m_nview.GetZDO();
                for (int slot = 0; slot < __instance.m_slots.Length; ++slot)
                {
                    string itemName = zdo.GetString(nameof(slot) + slot);
                    bool isItemDone = t_cookingStation.Method("IsItemDone", itemName).GetValue<bool>();
                    if (itemName != "" && itemName != __instance.m_overCookedItem.name && isItemDone)
                    {
                        Player.m_localPlayer.RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, 0.75f);
                        break;
                    } 
                }
            }
        }
        // float skillLevel = Player.m_localPlayer.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID)
    }
}
