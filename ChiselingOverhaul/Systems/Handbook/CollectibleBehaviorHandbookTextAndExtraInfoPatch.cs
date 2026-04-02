using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using static ChiselingOverhaul.Utils.HarmonyUtils;

namespace ChiselingOverhaul.Systems.Handbook
{
    [HarmonyPatch]
    internal class CollectibleBehaviorHandbookTextAndExtraInfoPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addToListUniquely")]
        private static bool addToListUniquely(ICoreClientAPI capi, List<ItemStack> list, ItemStack entry)
        {
            if(entry is null) return ShortCircuitVoid();
            return ContinueWithOriginal();
        }
    }
}
