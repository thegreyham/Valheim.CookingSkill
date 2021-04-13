using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Pipakin.SkillInjectorMod;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using UnityEngine;
using System.Linq;
using JotunnLib.Entities;
using JotunnLib.Managers;

namespace CookingSkill
{

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("com.pipakin.SkillInjectorMod")]
    [BepInDependency("com.bepinex.plugins.jotunnlib")]
    public class CookingSkill : BaseUnityPlugin
    {
        public const string PluginGUID = "thegreyham.valheim.CookingSkill";
        public const string PluginName = "Cooking Skill";
        public const string PluginVersion = "1.3.0";

        private static Harmony harmony;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<bool> configEnableRecipes;
        private static ConfigEntry<bool> configLevelGateRecipes;


        private static ConfigEntry<float> configCookingStationXPIncrease;
        private static ConfigEntry<float> configCauldronXPIncrease;
        private static ConfigEntry<float> configFermenterXPIncrease;

        private static ConfigEntry<float> configFoodHealthMulitplier;
        private static ConfigEntry<float> configFoodStaminaMulitplier;
        private static ConfigEntry<float> configFoodDurationMulitplier;
        private static ConfigEntry<float> configFermenterDuration;
        private static ConfigEntry<string> configFermenterDropLevels;
        private static ConfigEntry<int> configFermenterDropAmount;
        private static ConfigEntry<float> configCookingStationDuration;
        private static ConfigEntry<int> configCookingStationCoalPreventionLevel;        

        private static float SkillLevel = 0f;

        const int COOKING_SKILL_ID = 483;  // Nexus mod id :)
        
        private static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

        private void Awake()
        {
            nexusID = Config.Bind<int>("General", "NexusID", 483, "NexusMods ID for updates.");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable the mod.");
            configEnableRecipes = Config.Bind<bool>("General", "Recipe Enabled", true, "Enable Extra Recipes that this mod grants.");
            configLevelGateRecipes = Config.Bind<bool>("General", "Recipe Level Gated", true, "Extra Recipes are unlocked at certain levels.");


            configCookingStationXPIncrease = Config.Bind<float>("Cooking Skill XP", "CookingStationXP", 1f, "Cooking skill xp gained when using the Cooking Station.");
            configCauldronXPIncrease = Config.Bind<float>("Cooking Skill XP", "CauldronXP", 2f, "Cooking skill xp gained when using the Cauldron.");
            configFermenterXPIncrease = Config.Bind<float>("Cooking Skill XP", "FermenterXP", 6f, "Cooking skill xp gained when fermenting mead.");

            configFoodHealthMulitplier = Config.Bind<float>("Food Effects", "HealthMultiplier", 0.5f, "Buff to Health given when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configFoodStaminaMulitplier = Config.Bind<float>("Food Effects", "StaminaMultiplier", 0.5f, "Buff to Stamina given when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configFoodDurationMulitplier = Config.Bind<float>("Food Effects", "DurationMultiplier", 1f, "Buff to Food Duration when consuming food per Cooking Skill Level. 1f = +1% / Level");
            configCookingStationDuration = Config.Bind<float>("Cooking Effects", "CookingStationDuration", .5f, "Reduces CookingStation cooking time per Cooking Skill Level. 1f = -1% / Level");
            configCookingStationCoalPreventionLevel = Config.Bind<int>("Cooking Effects", "CookingStationCoalPreventionLevel", 75, "The level at which food on the Cooking Station will never burn");
            configFermenterDuration = Config.Bind<float>("Fermenter Effects", "FermenterDuration", .66f, "Reduces Fermentation duration per Cooking Skill Level. 1f = -1% / Level");
            configFermenterDropLevels = Config.Bind<string>("Fermenter Effects", "FermenterDropLevels", "50,75,100", "The levels at which extra fermenter drops are granted Default 50,75,100 (Meaning fermenter will drop additional items at lv50 lv75 & lv100 ");
            configFermenterDropAmount = Config.Bind<int>("Fermenter Effects", "FermenterDropAmount", 1, "The amount of extra potions a fermenter will drop when a level requirement is met. Default 1");

            if (!modEnabled.Value)
                return;

            //if (configLevelGateRecipes.Value == true) configLevelGateRecipes.Value = false;
            //if (configLevelGateRecipes.Value == false) configLevelGateRecipes.Value = true;

            configCookingStationCoalPreventionLevel.Value = Mathf.Clamp(configCookingStationCoalPreventionLevel.Value, 0, 100);

            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (SkillInjector.GetSkillDef((Skills.SkillType)COOKING_SKILL_ID) == null)
                SkillInjector.RegisterNewSkill(COOKING_SKILL_ID, "Cooking", "Improves Health and Stamina buffs from consuming food", 1.0f, LoadCustomTexture("meat_cooked.png"), Skills.SkillType.Knives);

            if (configEnableRecipes.Value)
            {
                PrefabManager.Instance.PrefabRegister += registerPrefabs;
                ObjectManager.Instance.ObjectRegister += initObjects;
            }
        }

        private void registerPrefabs(object sender, EventArgs e)
        {
            // Create a new instance of our TestPrefab
            PrefabManager.Instance.RegisterPrefab(new HoneyGlazedNeckTail());
            PrefabManager.Instance.RegisterPrefab(new HoneyGlazedHam());
            PrefabManager.Instance.RegisterPrefab(new HoneyGlazedTrout());
            PrefabManager.Instance.RegisterPrefab(new HoneyGlazedSerpent());
            PrefabManager.Instance.RegisterPrefab(new HoneyGlazedLox());
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
        //              Recipe Stuff                                            //
        // ==================================================================== //

        #region load recipes
        private void initObjects(object sender, EventArgs e)
        {

            // Recipes
            ObjectManager.Instance.RegisterItem("HoneyGlazedNeckTail");
            ObjectManager.Instance.RegisterItem("HoneyGlazedHam");
            ObjectManager.Instance.RegisterItem("HoneyGlazedTrout");
            ObjectManager.Instance.RegisterItem("HoneyGlazedSerpent");
            ObjectManager.Instance.RegisterItem("HoneyGlazedLox");

            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                Name = "Recipe_HoneyGlazedHam",
                Item = "HoneyGlazedHam",
                CraftingStation = "piece_cauldron",
                Enabled = configLevelGateRecipes.Value,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        Item = "CookedMeat",
                        Amount = 1
                    },
                    new PieceRequirementConfig()
                    {
                        Item = "Honey",
                        Amount = 5
                    }
                }
            });
            Log("Loaded HoneyGlazedHam");
            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                Name = "Recipe_HoneyGlazedNeckTail",
                Item = "HoneyGlazedNeckTail",
                CraftingStation = "piece_cauldron",
                Enabled = configLevelGateRecipes.Value,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        Item = "NeckTailGrilled",
                        Amount = 1
                    },
                    new PieceRequirementConfig()
                    {
                        Item = "Honey",
                        Amount = 2
                    }
                }
            });
            Log("Loaded HoneyGlazedNeck");
            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                Name = "Recipe_HoneyGlazedTrout",
                Item = "HoneyGlazedTrout",
                CraftingStation = "piece_cauldron",
                Enabled = configLevelGateRecipes.Value,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        Item = "FishCooked",
                        Amount = 1
                    },
                    new PieceRequirementConfig()
                    {
                        Item = "Honey",
                        Amount = 4
                    }
                }
            });
            Log("Loaded HoneyGlazedTrout");
            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                Name = "Recipe_HoneyGlazedSerpent",
                Item = "HoneyGlazedSerpent",
                CraftingStation = "piece_cauldron",
                Enabled = configLevelGateRecipes.Value,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        Item = "SerpentMeatCooked",
                        Amount = 1
                    },
                    new PieceRequirementConfig()
                    {
                        Item = "Honey",
                        Amount = 6
                    }
                }
            });
            Log("Loaded HoneyGlazedSerpent");
            ObjectManager.Instance.RegisterRecipe(new RecipeConfig()
            {
                Name = "Recipe_HoneyGlazedLox",
                Item = "HoneyGlazedLox",
                CraftingStation = "piece_cauldron",
                Enabled = configLevelGateRecipes.Value,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig()
                    {
                        Item = "CookedLoxMeat",
                        Amount = 1
                    },
                    new PieceRequirementConfig()
                    {
                        Item = "Honey",
                        Amount = 10
                    }
                }
            });
            Log("Loaded HoneyGlazedLox");
        }
        #endregion

        #region enable recipe in cauldron
        [HarmonyPatch(typeof(InventoryGui), "AddRecipeToList")]
        internal class Patch_InventoryGui_AddRecipeToList
        {
            static void Prefix(Player player, Recipe recipe, ItemDrop.ItemData item,ref bool canCraft, ref InventoryGui __instance)
            {               
                bool isCauldronRecipe = recipe.m_craftingStation?.m_name == "$piece_cauldron";

                if (!isCauldronRecipe) return;
                Log($"{recipe.name}");
                Log($"Skill Level = {SkillLevel}");
                if (recipe.name == "Recipe_HoneyGlazedHam")
                {
                    if (SkillLevel < .2)
                    {
                        Log("Disable Recipe_HoneyGlazedHam");
                        canCraft = false;
                        //TODO move this to inventorygui update postfix to disable the crafting of the items. They are still craftable even if greyed out.
                        //__instance.m_craftButton.interactable = false;
                        //__instance.m_craftButton.GetComponent<UITooltip>().m_text = "Unable to craft until lv20";
                    }
                    else
                        canCraft = true;
                }

                if (recipe.name == "Recipe_HoneyGlazedNeckTail")
                {
                    if (SkillLevel < .1)
                    {
                        Log("Disable Recipe_HoneyGlazedNeckTail");
                        canCraft = false;
                    }
                    else
                        canCraft = true;
                }

                if (recipe.name == "Recipe_HoneyGlazedTrout")
                {
                    if (SkillLevel < .3)
                    {
                        Log("Disable Recipe_HoneyGlazedTrout");
                        canCraft = false;
                    }
                    else
                        canCraft = true;
                }

                if (recipe.name == "Recipe_HoneyGlazedSerpent")
                {
                    if (SkillLevel < .5)
                    {
                        Log("Disable Recipe_HoneyGlazedSerpent");
                        canCraft = false;
                    }
                    else
                        canCraft = true;
                }

                if (recipe.name == "Recipe_HoneyGlazedLox")
                {
                    if (SkillLevel < .7)
                    {
                        Log("Disable Recipe_HoneyGlazedLox");
                        canCraft = false;
                    }
                    else
                        canCraft = true;
                }


                //return true;

                //canCraft = false;


                //player.RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configCauldronXPIncrease.Value);
                //SkillLevel = player.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                //Log($"[Cooked Item on Cauldron] Increase Cooking Skill by {configCauldronXPIncrease.Value} | [Level:{SkillLevel}]");
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
        internal class Patch_InventoryGui_UpdateRecipe
        {
            static void Postfix(ref InventoryGui __instance, Player player)
            {
                Recipe selectedRecipe = ((KeyValuePair<Recipe, ItemDrop.ItemData>)AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe").GetValue(__instance)).Key;
                if (selectedRecipe.name == "Recipe_HoneyGlazedNeckTail" && SkillLevel < .1)
                {
                    if (__instance.m_craftButton.interactable)
                    {
                        __instance.m_craftButton.GetComponent<UITooltip>().m_text = "Requires Cooking Skill lv 10 to Craft";
                        __instance.m_craftButton.interactable = false;
                    }
                }
                if (selectedRecipe.name == "Recipe_HoneyGlazedHam" && SkillLevel < .2)
                {
                    if (__instance.m_craftButton.interactable)
                    {
                        __instance.m_craftButton.GetComponent<UITooltip>().m_text = "Requires Cooking Skill lv 20 to Craft";
                        __instance.m_craftButton.interactable = false;
                    }
                }
                if (selectedRecipe.name == "Recipe_HoneyGlazedTrout" && SkillLevel < .3)
                {
                    if (__instance.m_craftButton.interactable)
                    {
                        __instance.m_craftButton.GetComponent<UITooltip>().m_text = "Requires Cooking Skill lv 30 to Craft";
                        __instance.m_craftButton.interactable = false;
                    }
                }
                if (selectedRecipe.name == "Recipe_HoneyGlazedSerpent" && SkillLevel < .5)
                {
                    if (__instance.m_craftButton.interactable)
                    {
                        __instance.m_craftButton.GetComponent<UITooltip>().m_text = "Requires Cooking Skill lv 50 to Craft";
                        __instance.m_craftButton.interactable = false;
                    }
                }
                if (selectedRecipe.name == "Recipe_HoneyGlazedLox" && SkillLevel < .5)
                {
                    if (__instance.m_craftButton.interactable)
                    {
                        __instance.m_craftButton.GetComponent<UITooltip>().m_text = "Requires Cooking Skill lv 50 to Craft";
                        __instance.m_craftButton.interactable = false;
                    }
                }
                //Log($"[update recipe post] Recipe: {selectedRecipe}");
                //if (selectedRecipe && SkillRequirement.skillRequirements.ContainsKey(selectedRecipe.name))
                //{
                //    SkillRequirement skillRecipe = SkillRequirement.skillRequirements[selectedRecipe.name];
                //    Dictionary<Skills.SkillType, Skills.Skill> m_skillData = AccessTools.Field(typeof(Skills), "m_skillData").GetValue(player.GetSkills()) as Dictionary<Skills.SkillType, Skills.Skill>;
                //    if (m_skillData.ContainsKey(skillRecipe.m_skill))
                //    {
                //        string message = $"Requires Lvl {skillRecipe.m_requiredLevel} in {SkillRequirement.GetSkillName(skillRecipe.m_skill)}";
                //        __instance.m_recipeDecription.text += Localization.instance.Localize($"\n\n{message}\n");
                //        if (__instance.m_craftButton.interactable && !SkillRequirement.GoodEnough(player, selectedRecipe))
                //        {
                //            __instance.m_craftButton.GetComponent<UITooltip>().m_text = message;
                //            __instance.m_craftButton.interactable = false;
                //        }
                //    }
                //}e
            }
        }

        #endregion
        // ==================================================================== //
        //              SPAWN DETAILS                                           //
        // ==================================================================== //

        #region onloadskill
        [HarmonyPatch(typeof(PlayerProfile), "LoadPlayerData")]
        public static class PlayerProfileCheck
        {
            private static void Postfix(PlayerProfile __instance, Player player)
            {
                SkillLevel = player.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                //Log($"[PlayerProfileCheck] Cooking Skill: {SkillLevel}");
            }
        }
        #endregion

        // ==================================================================== //
        //              COOKING STATION XP PATCHES                              //
        // ==================================================================== //

        #region Cooking Station XP Patches

        // increase cooking skill when placing an item on the cooking station
        [HarmonyPatch(typeof(CookingStation), "UseItem")]
        internal class Patch_CookingStation_UseItem
        {
            static void Postfix(ref bool __result, Humanoid user)
            {
                if (__result)
                {
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configCookingStationXPIncrease.Value * 0.25f);
                    SkillLevel = ((Player)user).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                    //Log($"[Add Item to Cook Station] Increase Cooking Skill by {configCookingStationXPIncrease.Value * 0.25f} | [Level:{SkillLevel}]");
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
                        SkillLevel = ((Player)user).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                        //Log($"[Removed Cooked Item from Cook Station] Increase Cooking Skill by {configCookingStationXPIncrease.Value * 0.75f} | [Level:{SkillLevel}]");
                        break;
                    }
                }
            }
        }

        #endregion


        // ==================================================================== //
        //              CAULDRON  XP PATCHES                                    //
        // ==================================================================== //

        #region Cauldron XP Patches

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
                SkillLevel = player.GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                //Log($"[Cooked Item on Cauldron] Increase Cooking Skill by {configCauldronXPIncrease.Value} | [Level:{SkillLevel}]");
            }
        }

        #endregion


        // ==================================================================== //
        //              FERMENTER XP PATCHES                                    //
        // ==================================================================== //

        #region Fermenter XP Patches

        // increase cooking skill when placing a mead base into the fermenter
        [HarmonyPatch(typeof(Fermenter), "AddItem")]
        internal class Patch_Fermenter_AddItem
        {
            static void Postfix(ref bool __result, Humanoid user, ref Fermenter __instance)
            {
                if (__result)
                {
                    ((Player)user).RaiseSkill((Skills.SkillType)COOKING_SKILL_ID, configFermenterXPIncrease.Value * 0.5f);
                    SkillLevel = ((Player)user).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                    Log($"[Add Item to Fermenter] Increase Cooking Skill by {configFermenterXPIncrease.Value * 0.5f} | [Level:{SkillLevel}]");

                    // Set the fermenter duration when adding new item to fermenter.
                    if (configFermenterDuration.Value <= 0)
                        return;


                    float currentFermenterDuration = __instance.m_fermentationDuration;
                    //Log($"[Add Item to Fermenter] Current Fermenter Duration = {currentFermenterDuration}");
                    float baseFermenterDuration = 2400f;                   
                    float FermenterDurationMultiplier = (100f - ((SkillLevel * 100) * configFermenterDuration.Value)) / 100;
                    float newFermenterDuration = baseFermenterDuration * FermenterDurationMultiplier;

                    if (newFermenterDuration <= 10)
                        newFermenterDuration = 10;  

                    // if your ferment duration calculation is less than the current one update it, otherwise leave it.
                    if (newFermenterDuration < currentFermenterDuration)
                    {
                        //Log($"[Add Item to Fermenter] Updating Fermenter Duration to {newFermenterDuration}");
                        __instance.m_fermentationDuration = newFermenterDuration;
                    }
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
                    SkillLevel = ((Player)user).GetSkillFactor((Skills.SkillType)COOKING_SKILL_ID);
                    //Log($"[Removed Item from Fermenter] Increase Cooking Skill by {configFermenterXPIncrease.Value * 0.5f} | [Level:{SkillLevel}]");
                }
            }
        }

        #endregion

        // ==================================================================== //
        //              BUFF PATCHES                                            //
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
        #endregion

        #region fermenter buffs
        // used to set the Fermenter duration when the game is loaded.
        [HarmonyPatch(typeof(Fermenter), "Awake")]
        internal class ApplyFermenterChanges
        {
            static void Prefix(ref Fermenter __instance)
            {
                if (configFermenterDuration.Value <= 0)
                    return;
                float onLoadFermenterDuration = __instance.m_fermentationDuration;
                //Log($"[Fermenter Awake] On Load Fermenter Duration = {onLoadFermenterDuration}");
                float baseFermenterDuration = 2400f;
                float FermentDurationMultiplier = (100f - ((SkillLevel * 100) * configFermenterDuration.Value)) / 100;
                float newFermenterDuration = baseFermenterDuration * FermentDurationMultiplier;

                if (newFermenterDuration <= 10) //minimum duration of fermenter is 10 seconds
                    newFermenterDuration = 10;

                if (newFermenterDuration < onLoadFermenterDuration)
                {
                    //Log($"[Fermenter Awake] Update Fermenter Duration = {newFermenterDuration}");
                    __instance.m_fermentationDuration = newFermenterDuration;
                }
            }
        }

        [HarmonyPatch(typeof(Fermenter), "GetItemConversion")]
        internal class ApplyFermenterItemCountChanges
        {
            public static void Postfix(ref Fermenter.ItemConversion __result)
            {
                // check droplevels length > 0 and dropamount > 0
                if (configFermenterDropLevels.Value.Length > 0 && configFermenterDropAmount.Value > 0)
                {
                    // split fermenterDrops by comma to array
                    var SkillLevelDrops = configFermenterDropLevels.Value.Split(',')
                                                                         .Where(m => int.TryParse(m, out _))
                                                                         .Select(m => int.Parse(m))
                                                                         .ToList();
                    // base fermenter count & skill level
                    int fermenterItemCount = 6;
                    float currentSkillLevel = SkillLevel * 100;
                    // iterate over list
                    for (var i = 0; i < SkillLevelDrops.Count; i++)
                        if (SkillLevelDrops[i] > 0 && currentSkillLevel >= SkillLevelDrops[i])
                            fermenterItemCount += configFermenterDropAmount.Value;     
                    
                    if (fermenterItemCount > 6)
                        __result.m_producedItems = fermenterItemCount;
                }   
            }
        }
        #endregion

        # region Cooking Station Duration changes
        [HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
        static class CookingStation_UpdateCooking_Patch
        {

            static void Prefix(CookingStation __instance, ZNetView ___m_nview)
            {
                if (configCookingStationDuration.Value == 0 || !___m_nview.IsValid() || !___m_nview.IsOwner() || !EffectArea.IsPointInsideArea(__instance.transform.position, EffectArea.Type.Burning, 0.25f))
                    return;

                ZDO zdo = ___m_nview.GetZDO();
                Traverse cookingStationTraverse = Traverse.Create(__instance);
                for (int i = 0; i < __instance.m_slots.Length; i++)
                {
                    string itemName = zdo.GetString("slot" + i, "");
                    float ticks = zdo.GetFloat("slot" + i, 0f);
                    float originalCookTime = 0f;
                    //Log($"Num: {ticks}");
                    if (itemName != null && itemName != "" && itemName != __instance.m_overCookedItem.name)
                    {
                        CookingStation.ItemConversion itemConversion = cookingStationTraverse.Method("GetItemConversion", new object[] { itemName }).GetValue<CookingStation.ItemConversion>();

                        if (ticks == 1)
                        {
                            originalCookTime = itemConversion.m_cookTime;
                            // we set a -1 as we need to wait 1 second before the first tick
                            // so the item that is cooking has already cooked for 1 second.
                            float newCookTime = originalCookTime * (1f - (configCookingStationDuration.Value * SkillLevel)) - 1;
                            // set it so cooktime can't go below 3 seconds
                            if (newCookTime < 3) newCookTime = 3;
                            //Log($"[{SkillLevel}] Set Cooktime for {itemName} to {newCookTime}");
                            ___m_nview.GetZDO().Set("slot" + i, originalCookTime - newCookTime);
                        }

                        if (configCookingStationCoalPreventionLevel.Value > 0 && SkillLevel * 100 >= configCookingStationCoalPreventionLevel.Value && ticks > itemConversion.m_cookTime && itemName == itemConversion.m_to.name)
                            ___m_nview.GetZDO().Set("slot" + i, itemConversion.m_cookTime);
                    }
                }
            }
        }
        #endregion
    }
}
