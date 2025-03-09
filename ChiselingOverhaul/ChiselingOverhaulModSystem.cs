using ChiselingOverhaul.Events;
using ChiselingOverhaul.Item;
using HarmonyLib;
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
    
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass(Mod.Info.ModID + ".bitpouch", typeof(ItemBitPouch));
        ModID = Mod.Info.ModID;
        api.Network
            .RegisterChannel(Mod.Info.ModID)
            .RegisterMessageType<AddMaterialPacket>();
        
        if (!Harmony.HasAnyPatches(ModID)) {
            harmony = new Harmony(ModID);
            harmony.PatchAll();
        }
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID);
        base.StartClientSide(api);
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
            .SetMessageHandler<AddMaterialPacket>(OnAddMaterialPacket);
        base.StartServerSide(api);
    }

    public static void OnAddMaterialPacket(IServerPlayer byPlayer, AddMaterialPacket packet)
    {
        var block = byPlayer.Entity.Api.World.GetBlock(packet.MaterialId);
        var chiselEntity = byPlayer.Entity.Api.World.BlockAccessor.GetBlockEntity(packet.Pos) as BlockEntityChisel;
        chiselEntity.AddMaterial(block, out _);
        chiselEntity?.MarkDirty();
    }
    
    public override void Dispose() {
        harmony?.UnpatchAll(Mod.Info.ModID);
    }
}