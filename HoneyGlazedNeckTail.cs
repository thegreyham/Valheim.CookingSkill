using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JotunnLib.Entities;

namespace CookingSkill
{
    public class HoneyGlazedNeckTailPrefab : PrefabConfig
    {
        public HoneyGlazedNeckTailPrefab() : base("HoneyGlazedNeckTail", "NeckTailGrilled")
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
            item.m_itemData.m_shared.m_name = "Honey Glazed Neck Tail";
            item.m_itemData.m_shared.m_description = "Roasted Neck Tail Glazed in Honey";
            item.m_itemData.m_dropPrefab = Prefab;
            item.m_itemData.m_shared.m_weight = .5f;
            item.m_itemData.m_shared.m_maxStackSize = 20;
            item.m_itemData.m_shared.m_variants = 1;
            item.m_itemData.m_shared.m_food = 40;
            item.m_itemData.m_shared.m_foodStamina = 25;
            item.m_itemData.m_shared.m_foodRegen = 5;
            item.m_itemData.m_shared.m_foodBurnTime = 1100;
        }
    }


    public class HoneyGlazedNeckTailRecipe : RecipeConfig
    {
        public static float SkillLevelRequirement = .1f;
        public HoneyGlazedNeckTailRecipe()
        {
            Name = "Recipe_HoneyGlazedNeckTail";
            Item = "HoneyGlazedNeckTail";
            CraftingStation = "piece_cauldron";
            Enabled = true;
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
            };
        }
    }
}
