namespace BonelabMultiplayerMockup.Messages
{
    public enum NetworkMessageType : byte
    {
        PlayerUpdateMessage = 0,
        ShortIdUpdateMessage = 1,
        TransformUpdateMessage = 2,
        InitializeSyncMessage = 3,
        OwnerChangeMessage = 4,
        DisconnectMessage = 5,
        RequestIdsMessage = 6,
        IdCatchupMessage = 7,
        SpawnGunMessage = 8,
        AvatarChangeMessage = 9,
        GunStateMessage = 10,
        MagInsertMessage = 11,
        GroupDestroyMessage = 12,
        AvatarQuestionMessage = 13
    }
}