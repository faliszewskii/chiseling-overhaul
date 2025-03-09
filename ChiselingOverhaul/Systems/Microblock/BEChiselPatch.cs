using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
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
}