using ChiselingOverhaul.GUI;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.Item;

using ChiselingOverhaul.Items;
using ChiselingOverhaul.Systems.Microblock;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods.WorldEdit;


public class ItemBitPouch : Item
{

    private GuiDialogPouchAddMaterial pouchMaterialSelector;
	
	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		var itemstack = inSlot?.Itemstack;
		if (itemstack == null) return;
		var attributes = itemstack.Attributes.GetOrAddTreeAttribute("chiseling-overhaul-pouch-content");
		dsc.Append(Lang.Get(ChiselingOverhaulModSystem.ModID + ":bitpouch-desc"));
		if (attributes.Count != 0)
		{
			dsc.Append(Lang.Get(ChiselingOverhaulModSystem.ModID + ":bitpouch-desc-content"));
		}
		foreach (var attribute in attributes)
		{
			string name = new ItemStack(api.World.GetBlock(int.Parse(attribute.Key))).GetName();
			dsc.Append("- " + attribute.Value + "x\t " + name + "\n");
		}
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public void OpenMaterialSelectionDialog(BlockEntityChisel chiselEntity, ItemStack pouchItemstack, ICoreAPI Api)
	{
		if (Api.Side != EnumAppSide.Client) return;
		pouchMaterialSelector = new GuiDialogPouchAddMaterial(pouchItemstack, chiselEntity,  Api as ICoreClientAPI);
		pouchMaterialSelector.TryOpen();
		pouchMaterialSelector.OnClosed += () => pouchMaterialSelector = null;
	}

    public static bool IsChiselable(
        IPlayer forPlayer,
        IWorldAccessor world,
        ItemStack stack
    )
    {
        return stack.Block is not null && ItemChisel.IsChiselingAllowedFor(world.Api, new BlockPos(-1, -1, -1), stack.Block, forPlayer);
    }

    //public override bool MatchesForCrafting(ItemStack inputStack, IRecipeBase recipe, IRecipeIngredient ingredient)
    //{
    //    if (recipe.Name != new AssetLocation(ChiselingOverhaulModSystem.ModID, "bitpouch_chisel"))
    //    {
    //        return base.MatchesForCrafting(inputStack, recipe, ingredient);
    //    }
    //    int pouches = 0;
    //    int chisels = 0;
    //    bool foundChiselable = false;

    //    foreach (var slot in recipe.RecipeIngredients)
    //    {
    //        var stack = slot.ResolvedItemStack;
    //        if (stack == null) continue;

    //        if (stack.Collectible.Code.Path == "bitpouch")
    //        {
    //            pouches++;
    //        }
    //        else if (stack.Item is not null && api.World.SearchItems(new AssetLocation("game:chisel-*")).ToList().Contains(stack.Item))
    //        {
    //            chisels++;
    //        }
    //        else if (IsChiselable(null, api.World, stack))
    //        {
    //            foundChiselable = true;
    //        }
    //        else
    //        {
    //            return false;
    //        }
    //    }

    //    return (pouches == 1 && chisels == 1 && foundChiselable) && base.MatchesForCrafting(inputStack, recipe, ingredient);
    //}

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
            Block atBlock = null;
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                BlockPos abovePos = blockSel.Position.Offset(blockSel.Face);
                atBlock = api.World.BlockAccessor.GetBlock(abovePos);
            } else if (byEntity.Api.Side == EnumAppSide.Server)
            {
                atBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            }


            if (atBlock.Replaceable < 6000) return;

            Block chiseledblock = byEntity.World.GetBlock(new AssetLocation("chiseledblock"));

            //if (byEntity.Api.Side == EnumAppSide.Client)
            { 
                byEntity.World.BlockAccessor.SetBlock(chiseledblock.BlockId, blockSel.Position); 
                
            }

            BlockEntityChisel be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
            if (be == null) return;

            be.WasPlaced(byEntity.World.GetBlock(new AssetLocation("log-placed-oak-ud")), null);

            BoolArray16x16x16 data = new();
            data[0, 0, 0] = true;
            data[0, 0, 1] = true;
            byte[,,] materials = new byte[16,16,16];
            be.SetData(data, materials);
            be.SetNowMaterialId(0);


            handling = EnumHandHandling.PreventDefaultAction;
            // Wywo³uje siê tez  na serwerze i tylko na serwerze poprawnie dzia³a
            // Clienta naprawiæ
        }
    }

    public override void OnHandbookRecipeRender(ICoreClientAPI capi, IRecipeBase recipe, ItemSlot slot, double x, double y, double z, double size)
    {
        base.OnHandbookRecipeRender(capi, recipe, slot, x, y, z, size);
    }
}