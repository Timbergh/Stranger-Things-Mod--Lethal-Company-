using GameNetcodeStuff;

namespace StrangerThingsMod
{
    public static class CarriedPlayerManager
    {
        public static PlayerControllerB CarriedPlayer { get; private set; } = null;

        public static bool IsPlayerCarried(PlayerControllerB carriedPlayer)
        {
            return CarriedPlayer == carriedPlayer;
        }

        public static void SetCarriedPlayer(PlayerControllerB carriedPlayer)
        {
            CarriedPlayer = carriedPlayer;
        }

        public static void ClearCarriedPlayer()
        {
            CarriedPlayer = null;
        }
    }

}