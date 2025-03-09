// using System.Collections.Generic;
// using System.Linq;
// using ChiselingOverhaul.API.Common;
// using ChiselingOverhaul.Items;
// using HarmonyLib;
// using Vintagestory.API.Client;
// using Vintagestory.API.Common;
// using Vintagestory.API.Config;
// using Vintagestory.API.MathTools;
// using Vintagestory.API.Server;
// using Vintagestory.GameContent;
// using static ChiselingOverhaul.Utils.HarmonyUtils;
//
// namespace ChiselingOverhaul.Systems.Microblock.ChiselMode;
//
// using VSSurvivalMod.Systems.ChiselModes;
//
// [HarmonyPatch]
// public class ChiselModePatches
// {
//     [HarmonyPrefix]
//     [HarmonyPatch(typeof(ChiselMode), nameof(ChiselMode.Apply))]
//     // TODO Move to other class!!! For compatibility with other chisel modes.
//     public static bool Apply(ref bool __result, ChiselMode __instance, BlockEntityChisel chiselEntity, IPlayer byPlayer, Vec3i voxelPos, BlockFacing facing, bool isBreak, byte currentMaterialIndex)
//         {
//             var api = chiselEntity.Api;
//             var addAtPos = voxelPos.Clone().Add(__instance.ChiselSize * facing.Normali.X, __instance.ChiselSize * facing.Normali.Y, __instance.ChiselSize * facing.Normali.Z);
//             int materialToPlaceId = chiselEntity.BlockIds[currentMaterialIndex];
//             int fullCubeVoxels = __instance.ChiselSize * __instance.ChiselSize * __instance.ChiselSize;
//             
//             var pouchItem = api.World.GetItem(new AssetLocation(ChiselingOverhaulModSystem.ModID, "bitpouch"));
//             var pouchSlots = byPlayer.GetInventorySlotsWithCollectible(pouchItem).ToList();
//
//             // Check if player has any pouches.
//             if (!pouchSlots.Any())
//             {
//                 (api as ICoreClientAPI)?.TriggerIngameError(__instance, "no-pouch", Lang.Get(ChiselingOverhaulModSystem.ModID + ":no-pouch"));
//                 return ShortCircuitReturn(ref __result, false);
//             }
//             
//             var pouches = pouchSlots.Select(slot => new BitPouch(slot.Itemstack)).ToList();
//             
//             if (isBreak)
//             {
//                 var materialQuantities = chiselEntity.GetMaterialVoxels(voxelPos, __instance.ChiselSize);
//                 
//                 // n pouches with m dictionaries of contained materials
//                 // k materials to put in these pouches
//                 // Pouches have LIMITED CAPACITY.
//                 // TODO For now infinite pouch capacity to ignore Backpack Problem. Choose the one that has already the material
//                 var updatedPouches = new HashSet<BitPouch>();
//                 foreach (var material in materialQuantities)
//                 {
//                     var pouch = pouches.Find(pouch => pouch.ContainsMaterial(material.Key));
//                     if (pouch == null)
//                     {
//                         pouch = pouches.First();
//                     }
//
//                     pouch.AddMaterial(material.Key, material.Value);
//                     updatedPouches.Add(pouch);
//                 }
//
//                 foreach (var pouch in updatedPouches)
//                 {
//                     pouch.UpdateItemStack();
//                 }
//                 
//                 return ShortCircuitReturn(ref __result, chiselEntity.SetVoxel(voxelPos, false, byPlayer, currentMaterialIndex));
//             }
//
//             if (addAtPos.X >= 0 && addAtPos.X < 16 && addAtPos.Y >= 0 && addAtPos.Y < 16 && addAtPos.Z >= 0 && addAtPos.Z < 16)
//             {
//                 int placedVoxelCount = chiselEntity.GetVoxels(addAtPos, __instance.ChiselSize);
//                 int voxelQuantity = fullCubeVoxels - placedVoxelCount;
//                 int requiredQuantity = voxelQuantity;
//                 // foreach (var inventory in byPlayer.InventoryManager.InventoriesOrdered)
//                 // {
//                 //     for (int i = 0; i < inventory.Count && requiredQuantity > 0; i++)
//                 //     {
//                 //         var slot = inventory[i];
//                 //         if(slot?.Itemstack == null) continue;
//                 //         var stack = slot.Itemstack;
//                 //         int id = stack.Id;
//                 //         if (id != bitsId) continue;
//                 //         var requiredMaterial = stack.Attributes.GetInt("MaterialId", 0);
//                 //         if(requiredMaterial != materialToPlaceId) continue;
//                 //         requiredQuantity -= slot.Itemstack.StackSize;
//                 //     }
//                 //     if (requiredQuantity <= 0) break;
//                 // }
//                 // bool isEnoughMaterial = requiredQuantity <= 0;
//                 //
//                 // if (isEnoughMaterial)
//                 // {
//                 //     requiredQuantity = voxelQuantity;
//                 //     foreach (var inventory in byPlayer.InventoryManager.InventoriesOrdered)
//                 //     {
//                 //         for (int i = 0; i < inventory.Count && requiredQuantity > 0; i++)
//                 //         {
//                 //             var slot = inventory[i];
//                 //             if(slot?.Itemstack == null) continue;
//                 //             var stack = slot.Itemstack;
//                 //             int id = stack.Id;
//                 //             if (id != bitsId) continue;
//                 //             var requiredMaterial = stack.Attributes.GetInt("MaterialId", 0);
//                 //             if(requiredMaterial != materialToPlaceId) continue;
//                 //             int stackSize = slot.Itemstack.StackSize;
//                 //             slot.TakeOut(requiredQuantity);
//                 //             requiredQuantity -= stackSize;
//                 //         }
//                 //         if (requiredQuantity <= 0) break;
//                 //     }
//                 //
//                 // }
//                 
//                 // if (isEnoughMaterial)
//                 // {
//                     // if (api is ICoreServerAPI sapi)
//                     // {
//                     //     string message = $"Added {voxelQuantity} voxels...";
//                     //     sapi.BroadcastMessageToAllGroups(message, EnumChatType.AllGroups);
//                     // }
//                     return ShortCircuitReturn(ref __result, chiselEntity.SetVoxel(addAtPos, true, byPlayer, currentMaterialIndex));
//                 // }
//                 // else
//                 // {
//                 //     var block = api.World.GetBlock(materialToPlaceId);
//                 //     var stack = new ItemStack(block); 
//                 //     (api as ICoreClientAPI)?.TriggerIngameError(__instance, "not-enough-bits",
//                 //         Lang.Get(ChiselingOverhaulModSystem.ModID + ":not-enough-bits", requiredQuantity, stack.GetName()));
//                 // }
//
//             }
//
//             return ShortCircuitReturn(ref __result, false);
//         }
// }