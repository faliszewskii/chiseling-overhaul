using Cairo.Freetype;
using ChiselingOverhaul.Events;
using ChiselingOverhaul.Item;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using VSSurvivalMod.Systems.ChiselModes;
using static ChiselingOverhaul.Utils.HarmonyUtils;

namespace ChiselingOverhaul.Systems.Microblock;

[HarmonyPatch]
public class BEChiselPatch
{
    // There is a SetVoxel method in BEMicroBlock that returns false if nothing has changed
    // but the thing is.. it is not possible for it to return false as you can't place a bit
    // where there is something there already and you can't remove an area where there is nothing there.
    
    // Move it to Harmony Transpiler before calling
    // IL_00bc: call         instance bool Vintagestory.GameContent.BlockEntityMicroBlock::SetVoxel(class [VintagestoryAPI]Vintagestory.API.MathTools.Vec3i, bool, unsigned int8, int32)
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityChisel), nameof(BlockEntityChisel.SetVoxel))]
    public static bool SetVoxel(ref bool __result, BlockEntityChisel __instance, Vec3i voxelPos, bool add, IPlayer byPlayer, byte materialId)
    {
        int size = __instance.GetChiselSize(byPlayer);

        if (add && BlockEntityChisel.ConstrainToAvailableMaterialQuantity && __instance.AvailMaterialQuantities != null)
        {
            int availableSumMaterial = __instance.AvailMaterialQuantities[materialId];
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            int usedSumMaterial = 0;
            foreach (var cubint in __instance.VoxelCuboids)
            {
                BlockEntityMicroBlock.FromUint(cubint, cwm);
                if (cwm.Material == materialId)
                {
                    usedSumMaterial += cwm.SizeXYZ;
                }
            }
            usedSumMaterial += size * size * size;

            if (usedSumMaterial > availableSumMaterial)
            {
                (__instance.Api as ICoreClientAPI)?.TriggerIngameError(__instance, "outofmaterial", Lang.Get("Out of material, add more material to continue adding voxels"));
                return ShortCircuitReturn(ref __result, false);
            }
        }

        // Chiseling Overhaul {
        if (!__instance.HandlePouch(voxelPos, add, byPlayer, materialId))
            return ShortCircuitReturn(ref __result, false);
        // } Chiseling Overhaul
        
        bool wasChanged = __instance.SetVoxel(voxelPos, add, materialId, size);
        // TODO Chiseling Overhaul: It is faster to revert pouch if SetVoxel returned false. But look for reproducible cases first.
        if (!wasChanged) return ShortCircuitReturn(ref __result, false);

        if (__instance.Api.Side == EnumAppSide.Client && !add)
        {
            Vec3d basepos = __instance.Pos
                .ToVec3d()
                .Add(voxelPos.X / 16.0, voxelPos.Y / 16.0, voxelPos.Z / 16.0)
                .Add(size / 4f / 16.0, size / 4f / 16.0, size / 4f / 16.0)
            ;

            int q = size * 5 - 2 + __instance.Api.World.Rand.Next(5);
            Block block = __instance.Api.World.GetBlock(__instance.BlockIds[materialId]);

            while (q-- > 0)
            {
                __instance.Api.World.SpawnParticles(
                    1,
                    block.GetRandomColor(__instance.Api as ICoreClientAPI, __instance.Pos, BlockFacing.UP) | (0xff << 24),
                    basepos,
                    basepos.Clone().Add(size / 4f / 16.0, size / 4f / 16.0, size / 4f / 16.0),
                    new Vec3f(-1, -0.5f, -1),
                    new Vec3f(1, 1 + size/3f, 1),
                    1, 1, size/30f + 0.1f + (float)__instance.Api.World.Rand.NextDouble() * 0.25f, EnumParticleModel.Cube
                );
            }
        }

        return ShortCircuitReturn(ref __result, true);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockEntityChisel), "OnBlockInteract")]
    public static bool OnBlockInteract(BlockEntityChisel __instance, IPlayer byPlayer, BlockSelection blockSel, bool isBreak)
    {
        if (__instance.Api.World.Side == EnumAppSide.Client && __instance.DetailingMode)
        {
           
            var boxes = (Cuboidf[])AccessTools.Method(typeof(BlockEntityChisel), "GetOrCreateVoxelSelectionBoxes").Invoke(__instance, [byPlayer]);
            if (blockSel.SelectionBoxIndex >= boxes.Length) return ShortCircuitVoid();

            Cuboidf box = boxes[blockSel.SelectionBoxIndex];
            Vec3i voxelPos = new Vec3i((int)(16 * box.X1), (int)(16 * box.Y1), (int)(16 * box.Z1));

            // blockSel.SelectionBoxIndex is strictly tied to the boxes from this and only this BEChisel.
            // If we want to get voxelPos for different BEChisel we have to calculate it first in original BEChisel
            // and only here pass it to correct one
            var facing = blockSel.Face;
            var modeData = __instance.GetChiselModeData(byPlayer);
            Vec3i addAtPos = voxelPos.Clone().Add(modeData.ChiselSize * facing.Normali.X, modeData.ChiselSize * facing.Normali.Y, modeData.ChiselSize * facing.Normali.Z);           

            if (!isBreak && modeData is OneByChiselMode or TwoByChiselMode or FourByChiselMode or EightByChiselModeData 
                && (addAtPos.X < 0 || addAtPos.X >= 16 || addAtPos.Y < 0 || addAtPos.Y >= 16 || addAtPos.Z < 0 || addAtPos.Z >= 16))
            {
                // We need to overcompensate for addAtPos calculation in ChiselMode.Apply
                int offset = 16 + modeData.ChiselSize;
                addAtPos.X += addAtPos.X < 0 ? offset : addAtPos.X >= 16 ? -offset : 0;
                addAtPos.Y += addAtPos.Y < 0 ? offset : addAtPos.Y >= 16 ? -offset : 0;
                addAtPos.Z += addAtPos.Z < 0 ? offset : addAtPos.Z >= 16 ? -offset : 0;                

                BlockPos atBlockPos = __instance.Pos.Copy().Offset(facing);
                Block atBlock = byPlayer.Entity.World.BlockAccessor.GetBlock(atBlockPos);

                var pouches = ItemBitPouch.GetPlayerBitPouches(byPlayer);
                if (pouches.Length == 0 || ItemBitPouch.GetCurrentMaterialBlockId(pouches.First()) is null)
                {
                    return ShortCircuitVoid();
                }
                int blockId = (int)ItemBitPouch.GetCurrentMaterialBlockId(pouches.First());

                if (atBlock is BlockChisel)
                {
                    var bec = byPlayer.Entity.Api.World.BlockAccessor.GetBlockEntity(atBlockPos) as BlockEntityChisel;
                    ItemBitPouch.SetCurrentMaterialToBEC(ItemBitPouch.GetPlayerBitPouches(byPlayer).First(), bec);
                    AccessTools.Method(typeof(BlockEntityChisel), "UpdateVoxel").Invoke(bec, [byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, addAtPos, facing, isBreak]);
                }
                else 
                {                   
                    BlockEntityChisel bec = null;
                    if (!ItemBitPouch.TryPlaceBEChisel(byPlayer.Entity, blockId, atBlockPos, out bec))
                    {
                        throw new Exception($"Failed to put a new BEChisel at the {facing.Code} side of current BEChisel");
                    }
                    ChiselingOverhaulModSystem.ClientNetworkChannel.SendPacket(new PlaceBEChiselPacket { blockId = blockId, atPos = atBlockPos });

                    AccessTools.Method(typeof(BlockEntityChisel), "UpdateVoxel").Invoke(bec, [byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, addAtPos, facing, isBreak]);

                    if (ItemBitPouch.isBEChiselEmpty(bec))
                    {
                        byPlayer.Entity.Api.World.BlockAccessor.SetBlock(0, atBlockPos);
                        ChiselingOverhaulModSystem.ClientNetworkChannel.SendPacket(new SetBlockPacket { blockId = 0, atPos = atBlockPos });
                    }
                }

            }
            else
            {
                AccessTools.Method(typeof(BlockEntityChisel), "UpdateVoxel").Invoke(__instance, [byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot, voxelPos, facing, isBreak]);               
            }
        }
        return ShortCircuitVoid();
    }
}