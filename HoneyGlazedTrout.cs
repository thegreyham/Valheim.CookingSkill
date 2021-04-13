using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JotunnLib.Entities;

namespace CookingSkill
{
    public class HoneyGlazedTrout : PrefabConfig
    {
        public HoneyGlazedTrout() : base("HoneyGlazedTrout", "FishCooked")
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
            item.m_itemData.m_shared.m_name = "Honey Glazed Trout";
            item.m_itemData.m_shared.m_description = "Grilled Trout Glazed in Honey";
            item.m_itemData.m_dropPrefab = Prefab;
            item.m_itemData.m_shared.m_weight = .5f;
            item.m_itemData.m_shared.m_maxStackSize = 20;
            item.m_itemData.m_shared.m_variants = 1;
            item.m_itemData.m_shared.m_food = 50;
            item.m_itemData.m_shared.m_foodStamina = 30;
            item.m_itemData.m_shared.m_foodRegen = 5;
            item.m_itemData.m_shared.m_foodBurnTime = 1300;
        }
    }
}

