using ChiselingOverhaul.Utils;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace ChiselingOverhaul.GUI
{

    // Close copy of HudIngameError
    public class HudIngameInfo: HudElement
    {
        private long infoTextActiveMs;

        private double x;

        private double y;

        private GuiElementHoverText elem;

        public override double InputOrder => 1.0;

        public override string ToggleKeyCombinationCode => null;

        public override bool Focusable => false;

        public HudIngameInfo(ICoreClientAPI capi, ChiselingOverhaulEventBus eventBus)
            : base(capi)
        {
            eventBus.ingameInfo += Event_InGameInfo;
            capi.Event.RegisterGameTickListener(OnGameTick, 20);
        }

        private void Event_InGameInfo(object sender, string text, int lines)
        {
            ComposeGui(lines);
            if (elem != null)
            {
                infoTextActiveMs = capi.InWorldEllapsedMilliseconds;
                elem.SetNewText(text);
                elem.SetVisible(on: true);
                x = elem.Bounds.absFixedX;
                y = elem.Bounds.absFixedX;
            }
        }

        private void OnGameTick(float dt)
        {
            if (infoTextActiveMs != 0L)
            {
                if (capi.InWorldEllapsedMilliseconds - infoTextActiveMs > 100)
                {
                    infoTextActiveMs = 0L;
                    elem.SetVisible(on: false);
                }
                else
                {
                    Composers["ingameinfo"].Bounds.absFixedX = x;
                    Composers["ingameinfo"].Bounds.absFixedY = y;
                }
            }
        }

        public void ComposeGui(int lines)
        {
            ElementBounds dialogBounds = new ElementBounds
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = 600.0,
                fixedHeight = 5.0
            };
            ElementBounds iteminfoBounds = ElementBounds.Fixed(0.0, -155.0 - 20*lines, 600.0, 30.0);
            ClearComposers();
            CairoFont font = CairoFont.WhiteSmallText().WithColor(GuiStyle.DialogDefaultTextColor).WithStroke(new double[4] { 0.0, 0.0, 0.0, 1.0 }, 2.0)
                .WithOrientation(EnumTextOrientation.Center);
            Composers["ingameinfo"] = capi.Gui.CreateCompo("ingameinfo", dialogBounds.FlatCopy()).BeginChildElements(dialogBounds).AddTranspHoverText("", font, 600, iteminfoBounds, "infotext")
                .EndChildElements()
                .Compose();
            elem = Composers["ingameinfo"].GetHoverText("infotext");
            elem.ZPosition = 100f;
            elem.SetFollowMouse(on: false);
            elem.SetAutoWidth(on: false);
            elem.SetAutoDisplay(on: false);
            elem.fillBounds = true;
            TryOpen();
        }

        public override bool TryClose()
        {
            return false;
        }

        public override bool ShouldReceiveKeyboardEvents()
        {
            return true;
        }

        public override void OnRenderGUI(float deltaTime)
        {
            if (infoTextActiveMs > 0)
            {
                base.OnRenderGUI(deltaTime);
            }
        }

        protected override void OnFocusChanged(bool on)
        {
        }
    }
}