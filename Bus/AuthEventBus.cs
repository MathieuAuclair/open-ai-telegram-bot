
namespace OlegBot.Bus
{
    public static class AuthEventBus
    {
        public static event Action<string, string> OnWalletLinked;

        public static void Notify(string requestId, string token) =>
            OnWalletLinked?.Invoke(requestId, token);
    }
}