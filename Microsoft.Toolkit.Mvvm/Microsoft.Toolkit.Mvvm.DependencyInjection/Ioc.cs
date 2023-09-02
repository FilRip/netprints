using System;
using System.Threading;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.DependencyInjection
{
    public sealed class Ioc : IServiceProvider
    {
        private volatile IServiceProvider? serviceProvider;

        public static Ioc Default { get; } = new Ioc();


        public object? GetService(Type serviceType)
        {
            IServiceProvider? obj = serviceProvider;
            if (obj == null)
            {
                ThrowInvalidOperationExceptionForMissingInitialization();
            }
            return obj!.GetService(serviceType);
        }

        public T? GetService<T>() where T : class
        {
            IServiceProvider? obj = serviceProvider;
            if (obj == null)
            {
                ThrowInvalidOperationExceptionForMissingInitialization();
            }
            return (T?)obj!.GetService(typeof(T));
        }

        public T? GetRequiredService<T>() where T : class
        {
            IServiceProvider? obj = serviceProvider;
            if (obj == null)
            {
                ThrowInvalidOperationExceptionForMissingInitialization();
            }
            T? obj2 = (T?)obj!.GetService(typeof(T));
            if (obj2 == null)
            {
                ThrowInvalidOperationExceptionForUnregisteredType();
            }
            return obj2;
        }

        public void ConfigureServices(IServiceProvider serviceProvider)
        {
            if (Interlocked.CompareExchange(ref this.serviceProvider, serviceProvider, null) != null)
            {
                ThrowInvalidOperationExceptionForRepeatedConfiguration();
            }
        }

        private static void ThrowInvalidOperationExceptionForMissingInitialization()
        {
            throw new InvalidOperationException("The service provider has not been configured yet");
        }

        private static void ThrowInvalidOperationExceptionForUnregisteredType()
        {
            throw new InvalidOperationException("The requested service type was not registered");
        }

        private static void ThrowInvalidOperationExceptionForRepeatedConfiguration()
        {
            throw new InvalidOperationException("The default service provider has already been configured");
        }
    }
}
