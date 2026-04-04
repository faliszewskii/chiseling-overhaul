using ProtoBuf;
using Vintagestory.API.MathTools;

#nullable disable
namespace ChiselingOverhaul.Events;

[ProtoContract]
public class SetBlockPacket
{
    [ProtoMember(1)] public BlockPos atPos;
    [ProtoMember(2)] public int blockId;
}