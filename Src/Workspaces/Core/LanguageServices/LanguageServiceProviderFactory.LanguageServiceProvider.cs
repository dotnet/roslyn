// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal partial class LanguageServiceProviderFactory : ILanguageServiceProviderFactory
    {
        private class LanguageServiceProvider : ILanguageServiceProvider
        {
            private readonly LanguageServiceProviderFactory factory;
            private readonly ImmutableDictionary<string, Func<ILanguageServiceProvider, ILanguageService>> unboundServicesMap;
            private readonly ConcurrentDictionary<Type, ILanguageService> boundServicesMap = new ConcurrentDictionary<Type, ILanguageService>();
            private readonly Func<Type, ILanguageService> constructLanguageService;

            public string Language { get; private set; }

            internal LanguageServiceProvider(
                LanguageServiceProviderFactory factory,
                string language,
                ImmutableDictionary<string, Func<ILanguageServiceProvider, ILanguageService>> unboundServicesMap)
            {
                this.Language = language;
                this.factory = factory;
                this.unboundServicesMap = unboundServicesMap;
                this.constructLanguageService = this.ConstructLanguageService;
            }

            public ILanguageServiceProviderFactory Factory
            {
                get { return this.factory; }
            }

            private ILanguageService ConstructLanguageService(Type type)
            {
                Func<ILanguageServiceProvider, ILanguageService> unboundService;
                if (this.unboundServicesMap.TryGetValue(type.AssemblyQualifiedName, out unboundService))
                {
                    return unboundService(this);
                }
                else
                {
                    return null;
                }
            }

            public T GetService<T>() where T : ILanguageService
            {
                return (T)this.boundServicesMap.GetOrAdd(typeof(T), this.constructLanguageService);
            }

            internal bool HasServices
            {
                get { return this.unboundServicesMap.Count > 0; }
            }
        }
    }
}
