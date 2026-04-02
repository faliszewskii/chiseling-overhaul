//using System;
//using System.Collections.Generic;
//using System.Text;
//using Vintagestory.API.Common;

//namespace ChiselingOverhaul.Systems.Recipe
//{
//    internal class ChiselableCraftingRecipeIngredient : CraftingRecipeIngredient
//    {

//        public ChiselableCraftingRecipeIngredient()
//        {
//        }

//        public override bool SatisfiesAsIngredient(ItemStack inputStack, bool checkStackSize = true)
//        {
//            if(base.SatisfiesAsIngredient(inputStack, checkStackSize))
//            {
//                //bool test = ChiselableToBitsRecipe.IsChiselable(null, null, inputStack);
//                //return test;
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
