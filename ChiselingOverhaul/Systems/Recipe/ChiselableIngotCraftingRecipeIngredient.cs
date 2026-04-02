//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Vintagestory.API.Common;

//namespace ChiselingOverhaul.Systems.Recipe
//{
//    internal class ChiselableIngotCraftingRecipeIngredient : CraftingRecipeIngredient
//    {

//        public ChiselableIngotCraftingRecipeIngredient()
//        {
//        }

//        public override bool SatisfiesAsIngredient(ItemStack inputStack, bool checkStackSize = true)
//        {
//            if (base.SatisfiesAsIngredient(inputStack, checkStackSize))
//            {
//                //string metal = inputStack.Collectible.Code.Path.Split('-')[1];
//                //return
//                //    (world.SearchBlocks(new AssetLocation("game", $"metalblock-new-plain-{metal}")).Count() == 0
//                //    || world.SearchBlocks(new AssetLocation("game", $"metalblock-new-riveted-{metal}")).Count() == 0);
//            }
//            return false;
//        }
//        public override CraftingRecipeIngredient Clone()
//        {
//            ChiselableCraftingRecipeIngredient result = new();

//            CloneTo(result);

//            return result;
//        }
//    }
//}
