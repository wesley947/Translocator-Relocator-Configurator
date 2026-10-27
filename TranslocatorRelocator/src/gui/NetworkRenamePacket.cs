using ProtoBuf;

[ProtoContract]
public class NetworkRenamePacket
{
    [ProtoMember(1)] public string OldKey;
    [ProtoMember(2)] public string NewKey;
    [ProtoMember(3)] public int ExpectedCount;
}