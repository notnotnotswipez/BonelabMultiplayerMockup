namespace BonelabMultiplayerMockup.Packets.Player
{
    public class PlayerDamagePacket : NetworkPacket
    {
        public override PacketByteBuf CompressData(MessageData messageData)
        {
            PlayerDamageData playerDamageData = (PlayerDamageData)messageData;
            PacketByteBuf packetByteBuf = new PacketByteBuf();
            packetByteBuf.WriteFloat(playerDamageData.damage);
            packetByteBuf.create();

            return packetByteBuf;
        }

        public override void ReadData(PacketByteBuf packetByteBuf, long sender)
        {
            float damage = packetByteBuf.ReadFloat();
            BoneLib.Player.GetRigManager().GetComponentInChildren<Player_Health>().TAKEDAMAGE(damage);
        }
    }

    public class PlayerDamageData : MessageData
    {
        public float damage;
    }
}