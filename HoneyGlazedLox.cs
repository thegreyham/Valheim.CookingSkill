using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JotunnLib.Entities;

namespace CookingSkill
{
    public class HoneyGlazedLoxPrefab : PrefabConfig
    {
        public HoneyGlazedLoxPrefab() : base("HoneyGlazedLox", "CookedLoxMeat")
        {
            // Nothing to do here
            // "Prefab" wil be set for us automatically after this is called
        }

        public override void Register()
        {
            // Configure item drop
            // ItemDrop is a component on GameObjects which determines info about the item when it's picked up in the inventory
            ItemDrop item = Prefab.GetComponent<ItemDrop>();
            item.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            item.m_itemData.m_shared.m_name = "Honey Glazed Lox";
            item.m_itemData.m_shared.m_description = "Roast Lox Glazed in Honey";
            item.m_itemData.m_dropPrefab = Prefab;
            item.m_itemData.m_shared.m_weight = 1f;
            item.m_itemData.m_shared.m_maxStackSize = 20;
            item.m_itemData.m_shared.m_variants = 1;
            item.m_itemData.m_shared.m_food = 80;
            item.m_itemData.m_shared.m_foodStamina = 50;
            item.m_itemData.m_shared.m_foodRegen = 6;
            item.m_itemData.m_shared.m_foodBurnTime = 2200;
        }
    }

    public class HoneyGlazedLoxRecipe : RecipeConfig
    {
        public static float SkillLevelRequirement = .5f;
        public HoneyGlazedLoxRecipe(bool enabled)
        {
            Name = "Recipe_HoneyGlazedHam";
            Item = "HoneyGlazedHam";
            CraftingStation = "piece_cauldron";
            Enabled = enabled;
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
            };
        }
    }

}

