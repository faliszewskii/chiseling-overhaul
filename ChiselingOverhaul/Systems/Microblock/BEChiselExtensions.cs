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

namespace ChiselingOverhaul.Systems.Microblock;

public static class BlockEntityChiselExtensions
{

    public static bool HandlePouch(this BlockEntityChisel chiselEntity, Vec3i voxelPos, bool add, IPlayer byPlayer, byte materialId)
    {
        var api = chiselEntity.Api;
        int chiselSize = chiselEntity.GetChiselSize(byPlayer);
        
        var pouchItem = api.World.GetItem(new AssetLocation(ChiselingOverhaulModSystem.ModID, "bitpouch"));
        var pouchSlots = byPlayer.GetInventorySlotsWithCollectible(pouchItem).ToList();

        // Check if player has any pouches.
        if (!pouchSlots.Any())
        {
            (api as ICoreClientAPI)?.TriggerIngameError(chiselEntity, "no-pouch", Lang.Get(ChiselingOverhaulModSystem.ModID + ":no-pouch"));
            return false;
        }
            
        var pouches = pouchSlots.Select(slot => new BitPouch(slot.Itemstack)).ToList();
            
        if (add)
        {
            // TODO Check if it is possible to add less voxels than n^3. 
            int requiredQuantity = chiselSize*chiselSize*chiselSize;
            int materialBlockId = chiselEntity.BlockIds[materialId];
            var pouch = pouches.Find(pouch => pouch.ContainsMaterial(materialBlockId, requiredQuantity));
            // TODO Add possibility for material to be spread among different pouches. and fix line 46
            if (pouch == null)
            {
                var block = api.World.GetBlock(materialBlockId);
                var stack = new ItemStack(block); 
                (api as ICoreClientAPI)?.TriggerIngameError(chiselEntity, "not-enough-bits",
                    Lang.Get(ChiselingOverhaulModSystem.ModID + ":not-enough-bits", requiredQuantity, stack.GetName()));
                return false;
            }

            pouch.TakeMaterial(materialBlockId, requiredQuantity);
            pouch.UpdateItemStack();
        }
        else
        {
            var materialQuantities = chiselEntity.GetMaterialVoxels(voxelPos, chiselSize);
                
            // n pouches with m dictionaries of contained materials
            // k materials to put in these pouches
            // Pouches have LIMITED CAPACITY.
            // TODO For now infinite pouch capacity to ignore Backpack Problem. Choose the one that has already the material
            var updatedPouches = new HashSet<BitPouch>();
            foreach (var material in materialQuantities)
            {
                var pouch = pouches.Find(pouch => pouch.ContainsMaterial(material.Key));
                if (pouch == null)
                {
                    pouch = pouches.First();
                }

                pouch.AddMaterial(material.Key, material.Value);
                updatedPouches.Add(pouch);
            }

            foreach (var pouch in updatedPouches)
            {
                pouch.UpdateItemStack();
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