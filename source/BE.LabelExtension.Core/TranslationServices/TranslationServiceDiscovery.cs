using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BE.LabelExtension.Core.TranslationServices
{
    /// <summary>
    /// Finds the translation services in assemblies and creates them, so that a further service
    /// is a class of its own and no existing class changes (NFA03, TC38).
    /// </summary>
    /// <remarks>
    /// A service is a class that implements <see cref="ITranslationService"/>, is not abstract
    /// and has a public constructor of one of two kinds: one whose first parameter takes the
    /// function that reads the API key and whose further parameters are optional, or one
    /// without parameters. Other classes are left out.
    /// </remarks>
    public static class TranslationServiceDiscovery
    {
        /// <summary>
        /// Creates one instance of every service in the assemblies. DeepL comes first, because
        /// the settings choose it by default; the others follow by name. Of two services with
        /// the same name, the first one found counts.
        /// </summary>
        /// <param name="assemblies">The assemblies to look in.</param>
        /// <param name="apiKey">Reads the API key; called by the service for every translation.</param>
        /// <returns>The services.</returns>
        public static IReadOnlyList<ITranslationService> Create(IEnumerable<Assembly> assemblies, Func<string?> apiKey)
        {
            var services = new List<ITranslationService>();
            IEnumerable<Type> types = assemblies.Distinct().SelectMany(LoadableTypes).Where(IsService).OrderBy(t => t.FullName, StringComparer.Ordinal);
            foreach (Type type in types)
            {
                if (TryCreate(type, apiKey) is ITranslationService service)
                {
                    if (services.Any(s => string.Equals(s.Name, service.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        (service as IDisposable)?.Dispose();
                    }
                    else
                    {
                        services.Add(service);
                    }
                }
            }

            return services
                .OrderBy(s => string.Equals(s.Name, DeepLTranslationService.ServiceName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsService(Type type)
            => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(ITranslationService).IsAssignableFrom(type);

        // An assembly whose dependencies are not all at hand still gives the types it can load.
        private static IEnumerable<Type> LoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(t => t != null)!;
            }
        }

        private static ITranslationService? TryCreate(Type type, Func<string?> apiKey)
        {
            ConstructorInfo[] constructors = type.GetConstructors();
            ConstructorInfo? withKey = constructors.FirstOrDefault(c =>
            {
                ParameterInfo[] parameters = c.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(Func<string>) && parameters.Skip(1).All(p => p.IsOptional);
            });
            if (withKey != null)
            {
                object?[] arguments = withKey.GetParameters().Select((p, i) => i == 0 ? apiKey : DefaultOf(p)).ToArray();
                return (ITranslationService)withKey.Invoke(arguments);
            }

            ConstructorInfo? withoutParameters = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            return withoutParameters != null ? (ITranslationService)withoutParameters.Invoke(null) : null;
        }

        private static object? DefaultOf(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue;
            }

            return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
        }
    }
}
