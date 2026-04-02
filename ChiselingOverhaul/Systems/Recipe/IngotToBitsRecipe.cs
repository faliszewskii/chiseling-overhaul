using ChiselingOverhaul.Items;
using ChiselingOverhaul.Systems.Microblock;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace ChiselingOverhaul.Systems.Recipe
{
    internal class IngotToBitsRecipe : GridRecipe
    {
        private IWorldAccessor world;

        // TODO Crashes bitpouch handbook page
        public IngotToBitsRecipe()
        {
            Width = 2;
            Height = 2;
            Shapeless = true;
            RecipeGroup = 2;
            IngredientPattern = "_C,AB";

            Ingredients = new Dictionary<string, CraftingRecipeIngredient>()
            {
                ["A"] = new CraftingRecipeIngredient()
                {
                    Code = new AssetLocation("chiselingoverhaul", "bitpouch"),
                    Type = EnumItemClass.Item
                },
                ["B"] = new CraftingRecipeIngredient()
                {
                    Code = new AssetLocation("game", "ingot-*"),
                    Type = EnumItemClass.Item
                },
                ["C"] = new CraftingRecipeIngredient()
                {
                    Code = new AssetLocation("game", "chisel-*"),
                    Type = EnumItemClass.Item,
                    Consume = false,
                    IsTool = true,
                    DurabilityChange = -10,
                    ToolDurabilityCost = 10 // TODO Does not work
                }
            };

            Output = new CraftingRecipeIngredient()
            {
                Code = new AssetLocation("chiselingoverhaul", "bitpouch"),
                Type = EnumItemClass.Item,
                Quantity = 1
            };
        }
       // public override bool Matches(
       //IPlayer forPlayer,
       //IWorldAccessor world,
       //ItemSlot[] ingredients,
       //int gridWidth)
       // {
       //     foreach (var slot in ingredients)
       //     {
       //         var stack = slot.Itemstack;
       //         if (stack == null) continue;
       //         if (stack.Collectible.Code.Path.Contains("ingot"))
       //         {
       //             string metal = stack.Collectible.Code.Path.Split('-')[1];
       //             return
       //                 (world.SearchBlocks(new AssetLocation("game", $"metalblock-new-plain-{metal}")).Count() == 0
       //                 || world.SearchBlocks(new AssetLocation("game", $"metalblock-new-riveted-{metal}")).Count() == 0)
       //                 && base.Matches(forPlayer, world, ingredients, gridWidth);
       //         }
       //     }

       //     return false;
       // }


       // public override void GenerateOutputStack(ItemSlot[] inputSlots, ItemSlot outputSlot)
       // {
       //     ItemStack pouch = null;
       //     Dictionary<int, int> materialCounts = new();

       //     foreach (var slot in inputSlots)
       //     {
       //         var stack = slot.Itemstack;
       //         if (stack == null) continue;

       //         if (stack.Collectible.Code.Path == "bitpouch")
       //         {
       //             pouch = stack.Clone();
       //         }
       //         else if (stack.Collectible.Code.Path.Contains("ingot"))
       //         {
       //             string metal = stack.Collectible.Code.Path.Split('-')[1];
       //             Block[] blocks = world.SearchBlocks(new AssetLocation("game", $"metalblock-new-plain-{metal}"));
       //             if (blocks.Count() == 0)
       //             {
       //                 blocks = world.SearchBlocks(new AssetLocation("game", $"metalblock-new-riveted-{metal}"));
       //             }
       //             if (blocks.Count() != 0)
       //             {
       //                 Block block = blocks.First();
       //                 materialCounts.TryAdd(block.Id, 0);
       //                 materialCounts[block.Id] += 340;
       //             }
       //         }
       //     }

       //     if (pouch is null || materialCounts.Count == 0)
       //     {
       //         outputSlot.Itemstack = pouch;
       //         outputSlot.Itemstack.Collectible.OnCreatedByCrafting(inputSlots, outputSlot, this);
       //         return;
       //     }

       //     var bitPouch = new BitPouch(pouch);

       //     foreach (var (id, material) in materialCounts)
       //     {
       //         bitPouch.AddMaterial(id, material);
       //     }
       //     bitPouch.CopyMaterials(pouch);
       //     outputSlot.Itemstack = pouch;
       //     outputSlot.Itemstack.Collectible.OnCreatedByCrafting(inputSlots, outputSlot, this);
       // }

       // public override bool ConsumeInput(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth)
       // {
       //     // TODO Consume whole stack
       //     return base.ConsumeInput(byPlayer, inputSlots, gridWidth);
       // }

       // /// <summary>
       // /// Serialized the recipe
       // /// </summary>
       // /// <param name="writer"></param>
       // public override void ToBytes(BinaryWriter writer)
       // {
       //     if (Ingredients == null || ResolvedIngredients == null || IngredientPattern == null || Output == null)
       //     {
       //         throw new InvalidOperationException("Some of required grid recipe fields are null, cant serialize it to bytes");
       //     }

       //     base.ToBytes(writer);

       //     writer.Write(Width);
       //     writer.Write(Height);
       //     writer.Write(Shapeless);

       //     writer.Write(Output.GetType().FullName);
       //     Output.ToBytes(writer);

       //     for (int i = 0; i < ResolvedIngredients.Length; i++)
       //     {
       //         if (ResolvedIngredients[i] == null)
       //         {
       //             writer.Write(true);
       //             continue;
       //         }

       //         writer.Write(false);
       //         writer.Write(ResolvedIngredients[i]?.GetType().FullName);
       //         ResolvedIngredients[i]?.ToBytes(writer);
       //     }

       //     writer.Write(RecipeGroup);

       //     writer.Write(IngredientPattern);
       // }

       // /// <summary>
       // /// Deserializes the recipe
       // /// </summary>
       // /// <param name="reader"></param>
       // /// <param name="resolver"></param>
       // public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
       // {
       //     base.FromBytes(reader, resolver);

       //     Width = reader.ReadInt32();
       //     Height = reader.ReadInt32();
       //     Shapeless = reader.ReadBoolean();

       //     Type outputType = Type.GetType(reader.ReadString());
       //     Output = (CraftingRecipeIngredient)Activator.CreateInstance(outputType);
       //     Output.FromBytes(reader, resolver);

       //     ResolvedIngredients = new CraftingRecipeIngredient[Width * Height];
       //     for (int i = 0; i < ResolvedIngredients.Length; i++)
       //     {
       //         bool isNull = reader.ReadBoolean();
       //         if (isNull) continue;

       //         Type inputType = Type.GetType(reader.ReadString());
       //         CraftingRecipeIngredient ingredient = (CraftingRecipeIngredient)Activator.CreateInstance(inputType);
       //         ingredient.FromBytes(reader, resolver);
       //         ingredient.Resolve(resolver, "Grid recipes deserialized", this);
       //         ResolvedIngredients[i] = ingredient;
       //     }

       //     RecipeGroup = reader.ReadInt32();

       //     IngredientPattern = reader.ReadString().DeDuplicate();
       // }

       // /// <summary>
       // /// Creates a deep copy
       // /// </summary>
       // /// <returns></returns>
       // public override GridRecipe Clone()
       // {
       //     IngotToBitsRecipe recipe = new();

       //     CloneTo(recipe);

       //     return recipe;
       // }

    }
}
