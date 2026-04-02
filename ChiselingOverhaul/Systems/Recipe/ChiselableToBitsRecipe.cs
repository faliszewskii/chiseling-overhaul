
//using ChiselingOverhaul.Items;
//using ChiselingOverhaul.Systems.Microblock;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using Vintagestory.API.Client;
//using Vintagestory.API.Common;
//using Vintagestory.API.Config;
//using Vintagestory.API.Datastructures;
//using Vintagestory.API.MathTools;
//using Vintagestory.API.Util;
//using Vintagestory.GameContent;

//namespace ChiselingOverhaul.Systems.Recipe;

//public sealed class ChiselableToBitsRecipe : GridRecipe
//{
//    private IWorldAccessor world;

//    // TODO: Limit recipe to chiselable
//    // TODO: Add chunks and gems
//    // TODO: Add support for ChiseledBlocks
//    // TODO: Lower chisel durability on consume
//    // TODO: Handbook seems to discard my derived classes and use only json recipe data.
//    // ingot: 340, bit: 17
//    public ChiselableToBitsRecipe()
//    {
//        //this.world = world;
//        Width = 2;
//        Height = 2;
//        Shapeless = true;
//        RecipeGroup = 1;
//        IngredientPattern = "_C,AB";

//        Ingredients = new Dictionary<string, CraftingRecipeIngredient>()
//        {
//            ["A"] = new CraftingRecipeIngredient()
//            {
//                Code = new AssetLocation("chiselingoverhaul", "bitpouch"),
//                Type = EnumItemClass.Item
//            },
//            ["B"] = new CraftingRecipeIngredient()
//            {
//                Code = new AssetLocation("game", "*"),
//                Type = EnumItemClass.Block
//            },
//            ["C"] = new CraftingRecipeIngredient()
//            {
//                Code = new AssetLocation("game", "chisel-*"),
//                Type = EnumItemClass.Item,
//                Consume = false,
//                IsTool = true,
//                DurabilityChange = -10,
//                ToolDurabilityCost = 10 // TODO Does not work
//            }
//        };

//        Output = new CraftingRecipeIngredient()
//        {
//            Code = new AssetLocation("chiselingoverhaul", "bitpouch"),
//            Type = EnumItemClass.Item,
//            Quantity = 1
//        };
//    }


//}