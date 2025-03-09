using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using static ChiselingOverhaul.Utils.HarmonyUtils;

namespace ChiselingOverhaul.Item;

[HarmonyPatch]
public class ItemChiselPatch
{
    // TODO | Open UI for choosing the block from pouch
    // TODO | Allow only pouches to be used here for survival
    // TODO | Pouches should not be eaten.
    // TODO | Learn what MarkDirty means
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemChisel), nameof(ItemChisel.SetToolMode))]
    public static bool SetToolMode(ICoreAPI ___api, ItemChisel __instance, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
    {
        if (blockSel == null) return ShortCircuitVoid();
        
        var pos = blockSel.Position;
        var mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        if (!mouseslot.Empty && mouseslot.Itemstack.Block is not BlockChisel)
        {
            BlockEntityChisel be = ___api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
            
            // ===== Chiseling Overhaul
            // Now we check if the player holds a pouch
            if (mouseslot.Itemstack.Item is ItemBitPouch pouch)
            {
                
                pouch.OpenMaterialSelectionDialog(be, mouseslot.Itemstack, ___api);
                mouseslot.MarkDirty();
            }
            else // If not, check if it is a block. Nothing after changes for creative player.
            {
                if (mouseslot.Itemstack.Block != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    if (ItemChisel.IsValidChiselingMaterial(___api, pos, mouseslot.Itemstack.Block, byPlayer))
                    {
                        be.AddMaterial(mouseslot.Itemstack.Block, out _, false);
                    }
                    be.MarkDirty();
                    ___api.Event.PushEvent("keepopentoolmodedlg");
                }
            }
            return ShortCircuitVoid();
            // ===== Chiseling Overhaul
        }

        if (toolMode > __instance.ToolModes.Length - 1)
        {
            int matNum = toolMode - __instance.ToolModes.Length;
            BlockEntityChisel be = ___api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
            if (be != null && be.BlockIds.Length > matNum)
            {
                slot.Itemstack.Attributes.SetInt("materialId", be.BlockIds[matNum]);
                slot.MarkDirty();
            }

            return ShortCircuitVoid();
        }

        slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        return ShortCircuitVoid();
    }
}