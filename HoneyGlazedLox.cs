using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JotunnLib.Entities;

public class HoneyGlazedLox : PrefabConfig
{
    public HoneyGlazedLox() : base("HoneyGlazedLox", "CookedLoxMeat")
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
        item.m_itemData.m_shared.m_foodStamina = 60;
        item.m_itemData.m_shared.m_foodRegen = 8;
        item.m_itemData.m_shared.m_foodBurnTime = 2500;
    }
}

