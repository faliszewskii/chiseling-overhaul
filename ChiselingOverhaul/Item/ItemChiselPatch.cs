using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
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

        if (toolMode > __instance.ToolModes.Length - 1)
        {
            //Assume there is at least one material for this branch to be reached.
            ItemStack pouch = ItemBitPouch.GetPlayerBitPouches(byPlayer).First();
            var pouchMaterials = ItemBitPouch.GetMaterials(pouch);

            int matNum = toolMode - __instance.ToolModes.Length;
            var (blockId, quantity) = pouchMaterials.ToArray()[matNum];
            ItemBitPouch.SetCurrentMaterialBlockId(pouch, blockId);
            BlockEntityChisel be = ___api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
            if (be != null)
            {
                Block block = ___api.World.GetBlock(blockId);
                if (!be.BlockIds.Contains(blockId))
                {
                    be.AddMaterial(block, out _, false);
                }
            }

            return ShortCircuitVoid();
        }

        slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        return ShortCircuitVoid();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemChisel), nameof(ItemChisel.GetToolModes))]
    public static bool GetToolModes(ref SkillItem[] __result, ICoreAPI ___api, ItemChisel __instance, ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        var addMatItem = (SkillItem)AccessTools.Field(typeof(ItemChisel), "addMatItem").GetValue(__instance);

        if (blockSel == null)
        {
            return ShortCircuitReturn(ref __result, null);
        }

        ItemStack[] pouches = ItemBitPouch.GetPlayerBitPouches(forPlayer);
        if (pouches.Length == 0)
        {
            addMatItem.Linebreak = true;
            return ShortCircuitReturn(ref __result, __instance.ToolModes);

        }
        var pouchMaterials = ItemBitPouch.GetMaterials(pouches.First());      
        if (pouchMaterials.Count == 0)
        {
            addMatItem.Linebreak = true;
            return ShortCircuitReturn(ref __result, __instance.ToolModes);
        }

        SkillItem[] mats = new SkillItem[pouchMaterials.Count];
        for (int i = 0; i < pouchMaterials.Count; i++)
        {
            var (blockId, quantity) = pouchMaterials.ToArray()[i];
            Block block = ___api.World.GetBlock(blockId);
            ItemSlot dummySlot = new DummySlot
            {
                Itemstack = new ItemStack(block)
            };
            mats[i] = new SkillItem()
            {
                Code = block.Code,
                Data = blockId,
                Linebreak = i % 7 == 0,
                Name = $"{ItemBitPouch.GetPrefix(quantity)} {block.GetInterface<ICustomChiselMaterialName>(___api.World, null)?.GetName(dummySlot.Itemstack) ?? block.GetHeldItemName(dummySlot.Itemstack)}",
                RenderHandler = (AssetLocation code, float dt, double atPosX, double atPosY) =>
                {
                    float wdt = (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledSlotSize);
                    ICoreClientAPI capi = ___api as ICoreClientAPI;
                    capi.Render.RenderItemstackToGui(dummySlot, atPosX + wdt / 2, atPosY + wdt / 2, 50, wdt / 2, ColorUtil.WhiteArgb, true, false, false);
                }
            };
        }

        addMatItem.Linebreak = (mats.Length - 1) % 7 == 0;

        return ShortCircuitReturn(ref __result, __instance.ToolModes.Append(mats));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemChisel), nameof(ItemChisel.OnBlockInteract))]
    public static bool OnBlockInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, bool isBreak, ref EnumHandHandling handling)
    {
        if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        {
            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            return ShortCircuitVoid();
        }

        BlockEntityChisel bec = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
        if (bec != null)
        {
            ItemStack[] pouches = ItemBitPouch.GetPlayerBitPouches(byPlayer);
            if(pouches.Length == 0)
            {
                return ShortCircuitVoid();
            }
            if(!isBreak)
            {
                if(ItemBitPouch.GetCurrentMaterialBlockId(pouches.First()) == null)
                {
                    (world.Api as ICoreClientAPI)?.TriggerIngameError(byPlayer, "no-material-chosen", Lang.Get(ChiselingOverhaulModSystem.ModID + ":no-material-chosen"));
                    return ShortCircuitVoid();
                }
                int blockId = (int)ItemBitPouch.GetCurrentMaterialBlockId(pouches.First());
                ItemBitPouch.SetCurrentMaterialToBEC(pouches.First(), bec);
                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Attributes.SetInt("materialId", blockId);
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            }            

            AccessTools.Method(typeof(BlockEntityChisel), "OnBlockInteract").Invoke(bec, [byPlayer, blockSel, isBreak]);
            handling = EnumHandHandling.PreventDefaultAction;
        }

        return ShortCircuitVoid();
    }

}