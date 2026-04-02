using Cairo.Freetype;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ChiselingOverhaul.Utils
{
    public class ChiselingOverhaulEventBus
    {
        public event Action<object, string, int> ingameInfo;
        public event Action<BlockPos, ItemStack, IClientPlayer, BlockFacing> pouchMaterialList;

        public void TriggerIngameInfo(object sender, string text, int lines)
        {
            ingameInfo?.Invoke(sender, text, lines);
        }

        public void TriggerOpenPouchMaterialList(BlockPos atBlock, ItemStack pouch, IClientPlayer player, BlockFacing face)
        {
            pouchMaterialList?.Invoke(atBlock, pouch, player, face);
        }
    }
}
