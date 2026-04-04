using ChiselingOverhaul.API.Common;
using ChiselingOverhaul.GUI;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.Item;

using ChiselingOverhaul.Items;
using ChiselingOverhaul.Systems.Microblock;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;



public class ItemBitPouch : Item
{


    public static int? GetCurrentMaterialBlockId(ItemStack pouch)
    {
        return pouch.Attributes.TryGetInt("chiseling-overhaul-current-material-block-id");
    }

    public static void SetCurrentMaterialBlockId(ItemStack pouch, int blockId)
    {
        pouch.Attributes.SetInt("chiseling-overhaul-current-material-block-id", blockId);
    }

    public static IDictionary<int, int> GetMaterials(ItemStack pouch)
    {
        var attributes = pouch.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
        var materialQuantities = attributes.SortedCopy()
            .ToDictionary(attribute => int.Parse(attribute.Key), attribute => (int)attribute.Value.GetValue());
        return materialQuantities;
    }
    internal static int GetMaterialQuantity(ItemStack pouch, int blockId)
    {
        var attributes = pouch.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
        int quantity = attributes.GetInt(blockId.ToString(), 0);
        return quantity;
    }

    public static void UpdateMaterials(ItemStack pouch, IDictionary<int, int> materials)
    {
        var attributes = pouch.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
        foreach (var attribute in attributes)
        {
            attributes.RemoveAttribute(attribute.Key);
        }
        foreach (var material in materials)
        {
            if(material.Value != 0)
            {
                attributes.SetInt(material.Key.ToString(), material.Value);
            }
                
        }
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        var itemstack = inSlot?.Itemstack;
        if (itemstack == null) return;
        var materials = GetMaterials(itemstack);
        dsc.Append(Lang.Get(ChiselingOverhaulModSystem.ModID + ":bitpouch-desc"));
        if (materials.Count != 0)
        {
            dsc.Append(Lang.Get(ChiselingOverhaulModSystem.ModID + ":bitpouch-desc-content"));
        }
        dsc.Append(GetContentInfo(materials));
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }

    public static ItemStack[] GetPlayerBitPouches(IPlayer forPlayer)
    {
        var api = forPlayer.Entity.Api;
        var pouchItem = api.World.GetItem(new AssetLocation(ChiselingOverhaulModSystem.ModID, "bitpouch"));
        var pouchSlots = forPlayer.GetInventorySlotsWithCollectible(pouchItem).ToList();

        if (!pouchSlots.Any())
        {
            //(api as ICoreClientAPI)?.TriggerIngameError(forPlayer, "no-pouch", Lang.Get(ChiselingOverhaulModSystem.ModID + ":no-pouch"));
            return [];
        }
        return [.. pouchSlots.Select(slot => slot.Itemstack)];
    }

    public static bool IsChiselable(
        IPlayer forPlayer,
        IWorldAccessor world,
        ItemStack stack
    )
    {
        return stack.Block is not null && ItemChisel.IsChiselingAllowedFor(world.Api, new BlockPos(-1, -1, -1), stack.Block, forPlayer);
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe)
    {
        if (byRecipe.Name.Path == "chiselable_into_bits" || byRecipe.Name.Path == "ingot_into_bits")
        {
            ItemStack pouch = null;
            Dictionary<int, int> materialCounts = new();

            foreach (var slot in allInputSlots)
            {
                var stack = slot.Itemstack;
                if (stack == null) continue;

                if (stack.Collectible.Code.Path == "bitpouch")
                {
                    pouch = stack.Clone();
                }
                else if (stack.Block is not null && IsChiselable(null, api.World, stack))
                {
                    if (stack.Block is BlockChisel)
                    {
                        var microMaterials = BlockEntityMicroBlock.MaterialIdsFromAttributes(stack.Attributes, null);
                        var microCuboids = BlockEntityMicroBlock.GetVoxelCuboids(stack.Attributes).ToList();
                        var microVolumes = BlockEntityMicroBlockExtensions.getMaterialVolumes(microCuboids, microMaterials);
                        materialCounts = materialCounts.Concat(microVolumes).GroupBy(kvp => kvp.Key).ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value));
                    }
                    else
                    {
                        materialCounts.TryAdd(stack.Id, 0);
                        materialCounts[stack.Id] += 4096 * stack.StackSize;
                    }

                } else if (stack.Collectible.Code.Path.Contains("ingot"))
                {
                    string metal = stack.Collectible.Code.Path.Split('-')[1];
                    Block[] blocks = api.World.SearchBlocks(new AssetLocation("game", $"metalblock-new-plain-{metal}"));
                    if (blocks.Count() == 0)
                    {
                        blocks = api.World.SearchBlocks(new AssetLocation("game", $"metalblock-new-riveted-{metal}"));
                    }
                    if (blocks.Count() != 0)
                    {
                        Block block = blocks.First();
                        materialCounts.TryAdd(block.Id, 0);
                        materialCounts[block.Id] += 340 * stack.StackSize;
                    }
                }
            }

            if (pouch is null || materialCounts.Count == 0)
            {
                outputSlot.Itemstack = null;
                return;
            }

            var bitPouch = new BitPouch(pouch);

            foreach (var (id, material) in materialCounts)
            {
                bitPouch.AddMaterial(id, material);
            }
            bitPouch.CopyMaterials(pouch);
            outputSlot.Itemstack = pouch;
        }
        base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe);


    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
        ItemSlot hotbarSlot = byPlayer?.InventoryManager.ActiveHotbarSlot;
        if (byPlayer is not null && byPlayer.Entity.Controls.ShiftKey)
        {
            BlockPos atBlockPos = null;
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                atBlockPos = blockSel.Position.Offset(blockSel.Face);
            } else if (byEntity.Api.Side == EnumAppSide.Server)
            {
                atBlockPos = blockSel.Position;
            }
            Block atBlock = api.World.BlockAccessor.GetBlock(atBlockPos);


            if (atBlock.Replaceable < 6000) return;           

            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                var system = byEntity.Api.ModLoader.GetModSystem<ChiselingOverhaulModSystem>();
                system?.TriggerPouchMaterialList(atBlockPos, slot.Itemstack, byPlayer as IClientPlayer, blockSel.Face);
            }
            
            handling = EnumHandHandling.PreventDefaultAction;
        }
    }

    public static void PlacePouchAsBlock(IPlayer byPlayer, BlockPos atPos, int blockId, BlockFacing face)
    {

        var pouch = GetPlayerBitPouches(byPlayer).First();
        var quantity = GetMaterialQuantity(pouch, blockId);

        BlockEntityChisel be = null;
        if (quantity == 0 || !TryPlaceBEChisel(byPlayer.Entity, blockId, atPos, out be))
        {
            return;
        }

        int cubeSide = quantity >= 4 * 4 * 4 ? 4 : quantity >= 2 * 2 * 2 ? 2 : 1;
        BoolArray16x16x16 data = new();
        FillSubCube(data, face.Opposite.PlaneCenter.Clone().Mul(16 - cubeSide).AsVec3i, cubeSide);
        byte[,,] materials = new byte[16, 16, 16];
        be.SetData(data, materials);

        TakeoutMaterial(pouch, blockId, cubeSide * cubeSide * cubeSide);
        SetCurrentMaterialBlockId(pouch, blockId);
    }


    public static bool TryPlaceBEChisel(Entity byEntity, int blockId, BlockPos atPos, out BlockEntityChisel be)
    {
        be = null;
        Block atBlock = byEntity.World.BlockAccessor.GetBlock(atPos);
        if (atBlock.Replaceable < 6000) return false;

        Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiseledblock"));
        byEntity.World.BlockAccessor.SetBlock(chiseledblock.BlockId, atPos);
        be = byEntity.Api.World.BlockAccessor.GetBlockEntity(atPos) as BlockEntityChisel;
        if (be == null) return false;

        be.WasPlaced(byEntity.Api.World.GetBlock(blockId), null);
        be.SetEmptyData();
        be.SetNowMaterialId(0);

        return true;
    }

    public static bool isBEChiselEmpty(BlockEntityChisel be)
    {
        return be.VoxelCuboids.Count == 0;
    }

    private static void FillSubCube(BoolArray16x16x16 data, Vec3i start, int size)
    {
        for (int x = start.X; x < start.X + size; x++)
        {
            for (int y = start.Y; y < start.Y + size; y++)
            {
                for (int z = start.Z; z < start.Z + size; z++)
                {
                    data[x, y, z] = true;
                }
            }
        }
    }

    public static bool IsEnoughMaterial(ItemStack pouch, int blockId, int requiredQuantity)
    {
        return GetMaterialQuantity(pouch, blockId) >= requiredQuantity;
    }

    public static bool TryTakeoutMaterial(ItemStack pouch, int blockId, int quantity)
    {
        if(!IsEnoughMaterial(pouch, blockId, quantity))
        {
            return false;
        }
        var materials = GetMaterials(pouch);
        materials[blockId] -= quantity;
        UpdateMaterials(pouch, materials);
        return true;
    }

    private static void TakeoutMaterial(ItemStack pouch, int blockId, int quantity)
    {
        if (!IsEnoughMaterial(pouch, blockId, quantity))
        {
            throw new Exception("Tried to take out more material than available!");
        }
        var materials = GetMaterials(pouch);
        materials[blockId] -= quantity;
        UpdateMaterials(pouch, materials);
    }

    public static void AddMaterial(ItemStack pouch, int blockId, int quantity)
    {
        var materials = GetMaterials(pouch);
        if(materials.Count == 0)
        {
            SetCurrentMaterialBlockId(pouch, blockId);
        }
        materials.TryAdd(blockId, 0);
        materials[blockId] += quantity;
        UpdateMaterials(pouch, materials);
    }

    public override void OnHandbookRecipeRender(ICoreClientAPI capi, IRecipeBase recipe, ItemSlot slot, double x, double y, double z, double size)
    {
        base.OnHandbookRecipeRender(capi, recipe, slot, x, y, z, size);
    }

    public string GetContentInfo(IDictionary<int, int> materials)
    {
        StringBuilder sb = new();
        foreach(var (blockId, quantity) in materials) {
            string name = new ItemStack(api.World.GetBlock(blockId)).GetName();
            sb.Append($"- {GetMaterialInfoRow(name, quantity)}\n");
        }
        return sb.ToString();
    }

    public static string GetMaterialInfoRow(string name, int quantity)
    {
        return $"{GetPrefix(quantity)} {name}";
    }

    public static string GetPrefix(int quantity)
    {
        return quantity == 1 ?
            Lang.Get(ChiselingOverhaulModSystem.ModID + ":bit-of") :
            quantity < 4096 ?
            Lang.Get(ChiselingOverhaulModSystem.ModID + ":bits-of", quantity) :
            quantity == 4096 ?
            Lang.Get(ChiselingOverhaulModSystem.ModID + ":block-of") :
            Lang.Get(ChiselingOverhaulModSystem.ModID + ":blocks-of", quantity / 4096.0);
    }

    public static void SetCurrentMaterialToBEC(ItemStack pouch, BlockEntityChisel bec)
    {
        int blockId = (int)GetCurrentMaterialBlockId(pouch);
        Block block = bec.Api.World.GetBlock(blockId);
        if (!bec.BlockIds.Contains(blockId))
        {
            bec.AddMaterial(block, out _, false);
            if(bec.Api.Side == EnumAppSide.Client)
            {
                bec.MarkDirty();
            }
        }
        bec.SetNowMaterialId(blockId);
    }
}