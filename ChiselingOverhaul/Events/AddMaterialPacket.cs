using ProtoBuf;
using Vintagestory.API.MathTools;

#nullable disable
namespace ChiselingOverhaul.Events;

[ProtoContract]
public class AddMaterialPacket
{
    [ProtoMember(1)] public BlockPos Pos;
    [ProtoMember(2)] public int MaterialId;
}