using ChiselingOverhaul.API.Common;
using ChiselingOverhaul.Items;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.Systems.Microblock
{
    public static class BlockEntityMicroBlockExtensions
    {

        public static Dictionary<int, int> getMaterialVolumes(List<uint> voxelCuboids, int[] blockIds)
        {
            Dictionary<int, int> volumeByBlockid = new Dictionary<int, int>();
            CuboidWithMaterial cwm = new CuboidWithMaterial();
            for (int i = 0; i < voxelCuboids.Count; i++)
            {
                BlockEntityMicroBlock.FromUint(voxelCuboids[i], cwm);

                if (blockIds.Length <= cwm.Material) continue;

                int blockId = blockIds[cwm.Material];

                if (volumeByBlockid.ContainsKey(blockId))
                {
                    volumeByBlockid[blockId] += cwm.SizeXYZ;
                }
                else
                {
                    volumeByBlockid[blockId] = cwm.SizeXYZ;
                }
            }

            return volumeByBlockid;
        }



        public static void SetEmptyData(this BlockEntityMicroBlock __instance)
        {
            BoolArray16x16x16 Voxels = new();
            byte[,,] VoxelMaterial = new byte[16, 16, 16];
            AccessTools.Method(
                typeof(BlockEntityMicroBlock),
                "RebuildCuboidList",
                new Type[] { typeof(BoolArray16x16x16), typeof(byte[,,]) })
                .Invoke(__instance, [Voxels, VoxelMaterial]);

            if (__instance.Api.Side == EnumAppSide.Client)
            {
                //RegenMesh();
                __instance.MarkMeshDirty();
            }

            __instance.RegenSelectionBoxes(__instance.Api.World, null);
            __instance.MarkDirty(true);
        }
    }

}
