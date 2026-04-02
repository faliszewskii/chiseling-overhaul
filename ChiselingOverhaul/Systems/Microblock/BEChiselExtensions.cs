using ChiselingOverhaul.API.Common;
using ChiselingOverhaul.Item;
using ChiselingOverhaul.Items;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.Systems.Microblock;

public static class BlockEntityChiselExtensions
{

    public static bool HandlePouch(this BlockEntityChisel chiselEntity, Vec3i voxelPos, bool add, IPlayer byPlayer, byte materialId)
    {
        var api = chiselEntity.Api;
        int chiselSize = chiselEntity.GetChiselSize(byPlayer);

        var pouches = ItemBitPouch.GetPlayerBitPouches(byPlayer);
        
        if (pouches.Length == 0)
        {
            (api as ICoreClientAPI)?.TriggerIngameError(chiselEntity, "no-pouch", Lang.Get(ChiselingOverhaulModSystem.ModID + ":no-pouch"));
            return false;
        }

        var mainPouch = pouches.First();
            
        if (add)
        {
            int requiredQuantity = chiselSize*chiselSize*chiselSize;
            int materialBlockId = chiselEntity.BlockIds[materialId];           
            if (!ItemBitPouch.TryTakeoutMaterial(mainPouch, materialBlockId, requiredQuantity))
            {
                (api as ICoreClientAPI)?.TriggerIngameError(chiselEntity, "not-enough-bits",
                    Lang.Get(ChiselingOverhaulModSystem.ModID + ":not-enough-bits", requiredQuantity));
                return false;
            }            
        }
        else
        {
            var materialQuantities = chiselEntity.GetMaterialVoxels(voxelPos, chiselSize);
                
            foreach (var (blockId, quantity) in materialQuantities)
            {
                ItemBitPouch.AddMaterial(mainPouch, blockId, quantity);
            }
        }

        return true;
    }
    public static int GetVoxels(this BlockEntityChisel chiselEntity, Vec3i voxelPos, int size)
    {
        int count = 0;
        BoolArray16x16x16 voxels;
        chiselEntity.ConvertToVoxels(out voxels, out _);
        int num1 = voxelPos.X + size;
        int num2 = voxelPos.Y + size;
        int num3 = voxelPos.Z + size;
        for (int x = voxelPos.X; x < num1; ++x)
        {
            for (int y = voxelPos.Y; y < num2; ++y)
            {
                for (int z = voxelPos.Z; z < num3; ++z)
                {
                    if (x < 16 && y < 16 && z < 16)
                    {
                        count += voxels[x, y, z] ? 1 : 0;
                    }
                }
            }
        }
        return count;
    }
    
    public static IDictionary<int, int> GetMaterialVoxels(this BlockEntityChisel chiselEntity, Vec3i voxelPos, int size)
    {
        var materialQuantities = new Dictionary<int, int>();
        chiselEntity.ConvertToVoxels(out var voxels, out _);
        int num1 = voxelPos.X + size;
        int num2 = voxelPos.Y + size;
        int num3 = voxelPos.Z + size;
        for (int x = voxelPos.X; x < num1; ++x)
        {
            for (int y = voxelPos.Y; y < num2; ++y)
            {
                for (int z = voxelPos.Z; z < num3; ++z)
                {
                    if (x >= 16 || y >= 16 || z >= 16) continue;
                    int materialId = chiselEntity.GetVoxelMaterialAt(new Vec3i(x, y, z));
                    if (!voxels[x, y, z]) continue;
                    materialQuantities.TryAdd(materialId, 0);
                    materialQuantities[materialId]++;
                }
            }
        }
        return materialQuantities;
    }
}