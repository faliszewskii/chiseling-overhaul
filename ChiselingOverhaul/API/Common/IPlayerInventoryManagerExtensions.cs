using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace ChiselingOverhaul.API.Common;

public static class IPlayerExtensions
{
    public static IEnumerable<ItemSlot> GetInventorySlotsWithCollectible(this IPlayer player, CollectibleObject collectible)
    {
        var slots = new List<ItemSlot>();
        foreach (var inventory in player.InventoryManager.InventoriesOrdered)
        {
            if(inventory.GetType() == typeof(InventoryPlayerCreative)) continue;
            foreach (var slot in inventory)
            {
                if(slot?.Itemstack == null) continue;
                var stack = slot.Itemstack;
                int id = stack.Id;
                if (id != collectible.Id) continue;
                slots.Add(slot);
            }
        }
        return slots;
    }
}