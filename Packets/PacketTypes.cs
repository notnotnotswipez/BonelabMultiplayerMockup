namespace BonelabMultiplayerMockup.Packets
{
    public enum NetworkMessageType : byte
    {
        PlayerUpdatePacket = 0,
        ShortIdUpdatePacket = 1,
        TransformUpdatePacket = 2,
        InitializeSyncPacket = 3,
        OwnerChangePacket = 4,
        DisconnectPacket = 5,
        RequestIdsPacket = 6,
        IdCatchupPacket = 7,
        SpawnGunPacket = 8,
        AvatarChangePacket = 9,
        GunStatePacket = 10,
        MagInsertPacket = 11,
        GroupDestroyPacket = 12,
        AvatarQuestionPacket = 13,
        SyncResetPacket = 14,
        PlayerColliderPacket = 15,
        NpcDeathPacket = 16,
        LevelResponsePacket = 17,
        SceneChangePacket = 18,
        SimpleGripEventPacket = 19
    }
}