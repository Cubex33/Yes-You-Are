using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    static Dictionary<Type, object> Services = new();

    public static void Register<T>(T service) where T : class
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        Services[typeof(T)] = service;
    }

    public static void Unregister<T>() where T : class
    {
        Services.Remove(typeof(T));
    }

    public static T Resolve<T>() where T : class
    {
        if (Services.TryGetValue(typeof(T), out var service)) return service as T;
        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public static bool TryResolve<T>(out T service) where T : class
    {
        if (Services.TryGetValue(typeof(T), out var instance) && instance is T typed)
        {
            service = typed;
            return true;
        }
        service = null;
        return false;
    }
}