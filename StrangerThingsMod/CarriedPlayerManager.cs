namespace StrangerThingsMod
{
    public static class CarriedPlayerManager
    {
        public static ulong CarriedPlayerId { get; private set; } = ulong.MaxValue;

        public static bool IsPlayerCarried(ulong playerId)
        {
            return CarriedPlayerId == playerId;
        }

        public static void SetCarriedPlayer(ulong playerId)
        {
            CarriedPlayerId = playerId;
        }

        public static void ClearCarriedPlayer()
        {
            CarriedPlayerId = ulong.MaxValue;
        }
    }

}