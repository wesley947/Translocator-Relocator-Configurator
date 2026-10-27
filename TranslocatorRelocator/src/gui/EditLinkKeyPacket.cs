using ProtoBuf;

[ProtoContract]
public class EditLinkKeyPacket
{
    [ProtoMember(1)]
    public string LinkKey;

    [ProtoMember(2)]
    public bool IsHome;

    [ProtoMember(3)]
    public bool ReplaceHome;

    [ProtoMember(4)]
    public string ExpectedHomePos;

    [ProtoMember(5)]
    public string[] NetworkNames;
}
