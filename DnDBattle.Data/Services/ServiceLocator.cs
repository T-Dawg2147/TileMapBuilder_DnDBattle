namespace DnDBattle.Data.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<TInterface>(TInterface imp)
            where TInterface : class
        {
            _services[typeof(TInterface)] = imp
                ?? throw new ArgumentNullException(nameof(imp));
        }

        public static TInterface Get<TInterface>() where TInterface : class
        {
            if (_services.TryGetValue(typeof(TInterface), out var service))
                return (TInterface)service;

            throw new InvalidOperationException(
                $"Service {typeof(TInterface).Name} is not registered. " +
                $"Call ServiceLocator.Register<{typeof(TInterface).Name}>() at startup.");
        }
    }
}
