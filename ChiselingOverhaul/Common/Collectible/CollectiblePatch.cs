using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;

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
            return true;
        }
    }
}
