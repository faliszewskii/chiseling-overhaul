using ChiselingOverhaul.Item;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using static ChiselingOverhaul.Utils.HarmonyUtils;

namespace ChiselingOverhaul.Common.Collectible
{
    [HarmonyPatch]
    internal class CollectiblePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.OnConsumedByCrafting))]
        public static bool OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, IRecipeBase recipe, ref IRecipeIngredient fromIngredient, IPlayer byPlayer, ref int quantity)
        {
            if (recipe.Name.Path == "chiselable_into_bits" || recipe.Name.Path == "ingot_into_bits")
            {
                if (stackInSlot.Itemstack.Collectible.Code.Path.Contains("chisel"))
                {
                    foreach(var slot in allInputSlots)
                    {
                        if (slot.Itemstack is null) continue;
                        if(!slot.Itemstack.Collectible.Code.Path.Contains("bitpouch") && !slot.Itemstack.Collectible.Code.Path.Contains("chisel"))
                        {
                            fromIngredient = (IRecipeIngredient)fromIngredient.Clone();
                            (fromIngredient as CraftingRecipeIngredient).DurabilityChange = -10 * slot.Itemstack.StackSize;
                        }
                    }
                }
                else if (!stackInSlot.Itemstack.Collectible.Code.Path.Contains("bitpouch"))
                {
                    quantity = stackInSlot.Itemstack.StackSize;
                }                    
            }
            return ContinueWithOriginal();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.OnHeldIdle))]
        public static bool OnHeldIdle(ICoreAPI ___api, CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity)
        {
            // ItemChisel or Item do not override OnHeldIdle so it has to be captured here
            if (__instance is ItemChisel && byEntity is EntityPlayer)
            {
                var player = (byEntity as EntityPlayer).Player;
                var system = byEntity.Api.ModLoader.GetModSystem<ChiselingOverhaulModSystem>();
                ItemStack[] pouches = ItemBitPouch.GetPlayerBitPouches(player);
                if(pouches.Length == 0)
                {
                    return ContinueWithOriginal();
                }
                var mainPouch = pouches.First();
                
                if(ItemBitPouch.GetCurrentMaterialBlockId(mainPouch) is null)
                {
                    return ContinueWithOriginal();
                }
                int blockId = (int)ItemBitPouch.GetCurrentMaterialBlockId(mainPouch);
                int quantity = ItemBitPouch.GetMaterialQuantity(mainPouch, blockId);

                string name = new ItemStack(___api.World.GetBlock(blockId)).GetName();
                system?.TriggerIngameInfo(byEntity, ItemBitPouch.GetMaterialInfoRow(name, quantity));
            }
            if (__instance is ItemBitPouch && byEntity is EntityPlayer)
            {
                var system = byEntity.Api.ModLoader.GetModSystem<ChiselingOverhaulModSystem>();
                var materials = ItemBitPouch.GetMaterials(slot.Itemstack);
                system?.TriggerIngameInfo(
                    byEntity,
                    (__instance as ItemBitPouch).GetContentInfo(materials),
                    materials.Count                    
                );
            }
            return ContinueWithOriginal();
        }
    }
}
