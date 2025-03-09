using System.Collections.Generic;
using Cairo;
using ChiselingOverhaul.Events;
using ChiselingOverhaul.Item;
using ChiselingOverhaul.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace ChiselingOverhaul.GUI;

public class GuiDialogPouchAddMaterial : GuiDialogGeneric
{
    long lastRedrawMs;

    private BlockEntityChisel chiselEntity;
    private ItemStack pouch;
    private ICoreClientAPI capi;
    
    public GuiDialogPouchAddMaterial(ItemStack pouch, BlockEntityChisel chiselEntity, ICoreClientAPI capi) :
        base(Lang.Get(ChiselingOverhaulModSystem.ModID + ":dialog-title-pouch-add-material"), capi)
    {
        this.chiselEntity = chiselEntity;
        this.pouch = pouch;
        this.capi = capi;
        SetupDialog();   
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
                .AddDialogTitleBar(Lang.Get(ChiselingOverhaulModSystem.ModID + ":dialog-title-pouch-add-material"), OnTitleBarClose)
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
            var block = capi.World.GetBlock(id);
            chiselEntity.AddMaterial(block, out _);
            ChiselingOverhaulModSystem.ClientNetworkChannel.SendPacket(new AddMaterialPacket {Pos=chiselEntity.Pos, MaterialId = id});
            chiselEntity.MarkDirty();
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