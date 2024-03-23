using System;
using Microsoft.Extensions.DependencyInjection;

namespace ListenerAPI.Factories
{
  public static class AbstractFactoryExtension
  {
    public static void AddSingletonFactory<TInterface, TImplementation>(this IServiceCollection services)
      where TInterface : class
      where TImplementation : class, TInterface
    {
      services.AddSingleton<TInterface, TImplementation>();
      services.AddSingleton<Func<TInterface>>(x => () => x.GetService<TInterface>()!); // Factory to generate TInterface SINGLETON instances per calls, if needed
      services.AddSingleton<IAbstractFactory<TInterface>, AbstractFactory<TInterface>>();
    }

    public static void AddTransientFactory<TInterface, TImplementation>(this IServiceCollection services)
      where TInterface : class
      where TImplementation : class, TInterface
    {
      services.AddTransient<TInterface, TImplementation>();
      services.AddSingleton<Func<TInterface>>(x => () => x.GetService<TInterface>()!); // Factory to generate TInterface TRANSIENT instances per calls, if needed
      services.AddSingleton<IAbstractFactory<TInterface>, AbstractFactory<TInterface>>();
    }
  }
}
