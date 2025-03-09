using System.Text;
using ChiselingOverhaul.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.Item;

using Vintagestory.API.Common;

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
}