using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChiselingOverhaul.API.Common;
using ChiselingOverhaul.Items;
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
    }
}
