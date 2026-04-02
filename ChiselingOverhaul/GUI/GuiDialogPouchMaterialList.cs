using Cairo;
using ChiselingOverhaul.Events;
using ChiselingOverhaul.Item;
using ChiselingOverhaul.Items;
using ChiselingOverhaul.Utils;
using System.Collections.Generic;
using System.Net.Sockets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.GUI;

public class GuiDialogPouchMaterialList : GuiDialogGeneric
{
    long lastRedrawMs;

    private BlockFacing face;
    private BlockPos atBlock;
    private ItemStack pouch;
    private IClientPlayer player;
    
    public GuiDialogPouchMaterialList(ICoreClientAPI capi, ChiselingOverhaulEventBus eventBus) :
        base(Lang.Get(ChiselingOverhaulModSystem.ModID + ":dialog-title-pouch-material-list"), capi)
    {
        eventBus.pouchMaterialList += Event_TryOpen;
    }
    
    private void Event_TryOpen(BlockPos atBlock, ItemStack pouch, IClientPlayer player, BlockFacing face)
    {
        this.face = face;
        this.player = player;
        this.atBlock = atBlock;
        this.pouch = pouch;
        SetupDialog();
        TryOpen();
    }

    ElementBounds containerBounds;
     void SetupDialog()
     {
            var materials = BitPouch.GetMaterials(pouch);
            
           double elemToDlgPad = GuiStyle.ElementToDialogPadding;

            ElementBounds button = ElementBounds.Fixed(3, 3, 283, 25).WithFixedPadding(10, 2);

            ElementBounds lorelistBounds = ElementBounds.Fixed(0, 32, 285, 500);

            ElementBounds clippingBounds = lorelistBounds.ForkBoundingParent();
            ElementBounds insetBounds = lorelistBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(lorelistBounds.fixedWidth + 7).WithFixedWidth(20);


            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(6);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(insetBounds, clippingBounds, scrollbarBounds);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(5,0);

            ClearComposers();

            Composers["loreList"] =
                capi.Gui
                .CreateCompo("loreList", dialogBounds)
                .AddShadedDialogBG(ElementBounds.Fill)
                .AddDialogTitleBar(Lang.Get(ChiselingOverhaulModSystem.ModID + ":dialog-title-pouch-material-list"), OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddInset(insetBounds, 3)
                    .BeginClip(clippingBounds)
                    .AddContainer(containerBounds = clippingBounds.ForkContainingChild(0, 0, 0, -3), "journallist")
            ;

            var container = Composers["loreList"].GetContainer("journallist");

            CairoFont hoverFont = CairoFont.WhiteSmallText().Clone().WithColor(GuiStyle.ActiveButtonTextColor);

            foreach (int id in materials.Keys)
            {
                string materialName = new ItemStack(capi.World.GetBlock(id)).GetName(); 
                GuiElementTextButton elem = new GuiElementTextButton(capi, Lang.Get(materialName), CairoFont.WhiteSmallText(), hoverFont, () => { return OnClickItem(id); }, button, EnumButtonStyle.Small);
                elem.SetOrientation(EnumTextOrientation.Left);
                container.Add(elem);
                button = button.BelowCopy();
            }

            if (materials.Count == 0)
            {
                string vtmlCode = "<i>" + Lang.Get(ChiselingOverhaulModSystem.ModID + ":dialog-no-material-in-pouch") + "</i>";
                container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, vtmlCode, CairoFont.WhiteSmallText()), button));
            }


            Composers["loreList"]
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
                .EndChildElements()
                .Compose()
            ;

            containerBounds.CalcWorldBounds();
            clippingBounds.CalcWorldBounds();

            Composers["loreList"].GetScrollbar("scrollbar").SetHeights(
                (float)(clippingBounds.fixedHeight),
                (float)(containerBounds.fixedHeight)
            );
        }
     
        private bool OnClickItem(int id) 
        {                      
            ChiselingOverhaulModSystem.ClientNetworkChannel.SendPacket(new AddMaterialPacket {Pos=atBlock, MaterialId = id, Face=face.Flag});
            ItemBitPouch.PlacePouchAsBlock(player, atBlock, id, face);
            TryClose();
            return true;
        }
     
        private void OnNewScrollbarvalue(float value)
        {
            ElementBounds bounds = Composers["loreList"].GetContainer("journallist").Bounds;
            bounds.fixedY = 0 - value;
            bounds.CalcWorldBounds();
        }

        private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            double top = 30;

            // Arrow Right
            ctx.Save();
            Matrix m = ctx.Matrix;
            m.Translate(GuiElement.scaled(63), GuiElement.scaled(top + 2));
            m.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
            ctx.Matrix = m;
            capi.Gui.Icons.DrawArrowRight(ctx, 2);

            double dx = 0.5;


            ctx.Rectangle(GuiElement.scaled(5), 0, GuiElement.scaled(125 * dx), GuiElement.scaled(100));
            ctx.Clip();
            LinearGradient gradient = new LinearGradient(0, 0, GuiElement.scaled(200), 0);
            gradient.AddColorStop(0, new Color(0, 0.4, 0, 1));
            gradient.AddColorStop(1, new Color(0.2, 0.6, 0.2, 1));
            ctx.SetSource(gradient);
            capi.Gui.Icons.DrawArrowRight(ctx, 0, false, false);
            gradient.Dispose();
            ctx.Restore();
        }
        
        private void OnTitleBarClose()
        {
            TryClose();
        }
}