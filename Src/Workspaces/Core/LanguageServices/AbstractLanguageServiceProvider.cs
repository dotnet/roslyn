using System;
using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Services.LanguageServices
{
    internal class AbstractLanguageServiceProvider : ILanguageServiceProvider
    {
        public string Language { get; private set; }
        private readonly Dictionary<Type, Lazy<ILanguageService, ILanguageServiceMetadata>> languageServiceMap;

        public AbstractLanguageServiceProvider(
            string language,
            IEnumerable<Lazy<ILanguageService, ILanguageServiceMetadata>> languageServices)
        {
            this.Language = language;
            this.languageServiceMap = languageServices.Where(lazy => lazy.Metadata.Language == language)
                                                      .ToDictionary(service => service.Metadata.ServiceType);
        }

        public T GetService<T>() where T : ILanguageService
        {
            var serviceType = typeof(T);
            Lazy<ILanguageService, ILanguageServiceMetadata> service;
            if (!languageServiceMap.TryGetValue(serviceType, out service) ||
                !(service.Value is T))
            {
                return default(T);
            }

            return (T)service.Value;
        }
    }
}
