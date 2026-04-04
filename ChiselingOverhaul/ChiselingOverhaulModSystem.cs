using ChiselingOverhaul.Events;
using ChiselingOverhaul.GUI;
using ChiselingOverhaul.Item;
using ChiselingOverhaul.Systems.Recipe;
using ChiselingOverhaul.Utils;
using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ChiselingOverhaul;

public class ChiselingOverhaulModSystem : ModSystem
{
    // TODO | Add command to give pouch with contents
    // TODO | Add conversion from ingots and gems to bits (configurable?)
    // TODO | Add sorter table/carpet to manage pouches
    // TODO | Add adding materials to chiseled entities (without losing and item). Needs GUI and removing old method
    // TODO | ItemId: "The unique number of the item, dynamically assigned by the game". Dynamically???? How do I reference sth then?
    // TODO | Check how compatible it is with Better Chiseling
    // TODO | Placing bit pouches
    // TODO | Creative mode has no restriction on bit pouch
    // TODO | Chisel mode: Break into bits (With double check)
    // TODO | Replace material adding.
    // TODO | Why there are server packets for quern GUI opening and closing? Ask about that.
    
    public Harmony harmony;
    public static string ModID;
    public static IClientNetworkChannel ClientNetworkChannel;
    public static IServerNetworkChannel ServerNetworkChannel;

    private GuiDialogPouchMaterialList pouchMaterialList;
    private HudIngameInfo ingameInfo;
    private ChiselingOverhaulEventBus eventBus;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass(Mod.Info.ModID + ".bitpouch", typeof(ItemBitPouch));
        ModID = Mod.Info.ModID;
        api.Network
            .RegisterChannel(Mod.Info.ModID)
            .RegisterMessageType<AddMaterialPacket>()
            .RegisterMessageType<PlaceBEChiselPacket>()
            .RegisterMessageType<SetBlockPacket>();

        if (!Harmony.HasAnyPatches(ModID)) {
            harmony = new Harmony(ModID);
            harmony.PatchAll();
        }

        eventBus = new();
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID);
        base.StartClientSide(api);
        ingameInfo = new HudIngameInfo(api, eventBus);
        pouchMaterialList = new GuiDialogPouchMaterialList(api, eventBus);
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
            .SetMessageHandler<AddMaterialPacket>(OnAddMaterialPacket)
            .SetMessageHandler<PlaceBEChiselPacket>(OnPlaceBEChiselPacket)
            .SetMessageHandler<SetBlockPacket>(OnSetBlockPacket);
        base.StartServerSide(api);


        //var recipe = new ChiselableToBitsRecipe() {Name = new AssetLocation(ModID, "bitpouch_chisel")};
        //var recipe2 = new IngotToBitsRecipe() {Name = new AssetLocation(ModID, "bitpouch_ingot")};

        //recipe.Resolve(api.World, Mod.Info.ModID);
        //recipe2.Resolve(api.World, Mod.Info.ModID);

        //api.RegisterCraftingRecipe(recipe);
        //api.RegisterCraftingRecipe(recipe2);
    }

    public static void OnAddMaterialPacket(IServerPlayer byPlayer, AddMaterialPacket packet)
    {
        ItemBitPouch.PlacePouchAsBlock(byPlayer, packet.Pos, packet.MaterialId, BlockFacing.FromFlag(packet.Face));
    }

    public static void OnPlaceBEChiselPacket(IServerPlayer byPlayer, PlaceBEChiselPacket packet)
    {
        if(!ItemBitPouch.TryPlaceBEChisel(byPlayer.Entity, packet.blockId, packet.atPos, out _))
        {
            throw new Exception("Failed to place BEChisel on the server side!");
        }
    }
    public static void OnSetBlockPacket(IServerPlayer byPlayer, SetBlockPacket packet)
    {
        byPlayer.Entity.Api.World.BlockAccessor.SetBlock(packet.blockId, packet.atPos);
    }

    public void TriggerIngameInfo(object sender, string text, int lines = 1)
    {
        eventBus.TriggerIngameInfo(sender, text, lines);
    }

    public void TriggerPouchMaterialList(BlockPos atBlock, ItemStack pouch, IClientPlayer player, BlockFacing face)
    {
        eventBus.TriggerOpenPouchMaterialList(atBlock, pouch, player, face);
    }

    public override void Dispose() {
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}