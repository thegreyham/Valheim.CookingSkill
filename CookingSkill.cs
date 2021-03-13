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

        const int SKILL_TYPE = 745;

        void Awake()
        {
            SkillInjector.RegisterNewSkill(SKILL_TYPE, "Cooking", "Improves Cooked Food", 1.0f, null, Skills.SkillType.Unarmed);
        }
    ﻿}

}
