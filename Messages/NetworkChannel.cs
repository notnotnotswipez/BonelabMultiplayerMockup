namespace BonelabMultiplayerMockup.Messages
{
    public enum NetworkChannel : byte
    {
        Reliable = 0,
        Unreliable = 1,
        Attack = 2,
        Object = 3,
        Transaction = 4
    }
}