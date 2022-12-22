// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IEditorConfigOptionMapping)), Shared]
    internal sealed class EditorConfigOptionMapping : IEditorConfigOptionMapping
    {
        [ExportWorkspaceService(typeof(IEditorConfigOptionMappingService)), Shared]
        internal sealed class WorkspaceService : IEditorConfigOptionMappingService
        {
            public IEditorConfigOptionMapping Mapping { get; }

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public WorkspaceService(IEditorConfigOptionMapping mapping)
                => Mapping = mapping;
        }

        private readonly ImmutableDictionary<string, Lazy<ImmutableDictionary<string, IOption2>>> _optionsByLanguage;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigOptionMapping(
            [ImportMany] IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            _optionsByLanguage = CreateLazyOptionsByLanguage(optionProviders);
        }

        private static ImmutableDictionary<string, Lazy<ImmutableDictionary<string, IOption2>>> CreateLazyOptionsByLanguage(IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, Lazy<ImmutableDictionary<string, IOption2>>>();

            foreach (var (language, lazyProvidersAndMetadata) in optionProviders.ToPerLanguageMap())
            {
                builder.Add(language, new Lazy<ImmutableDictionary<string, IOption2>>(() => GetOptionsFromProviders(lazyProvidersAndMetadata)));
            }

            return builder.ToImmutable();

            // Local functions
            static ImmutableDictionary<string, IOption2> GetOptionsFromProviders(ImmutableArray<Lazy<IOptionProvider, LanguageMetadata>> lazyProvidersAndMetadata)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, IOption2>();

                foreach (var lazyProviderAndMetadata in lazyProvidersAndMetadata)
                {
                    var provider = lazyProviderAndMetadata.Value;
                    foreach (var option in provider.Options)
                    {
                        builder.Add(option.OptionDefinition.ConfigName, option);
                    }
                }

                return builder.ToImmutable();
            }
        }

        public bool TryMapEditorConfigKeyToOption(string key, [NotNullWhen(true)] out IOption2? option)
        {
            var language =
                key.StartsWith(OptionDefinition.CSharpConfigNamePrefix, StringComparison.OrdinalIgnoreCase) ? LanguageNames.CSharp :
                key.StartsWith(OptionDefinition.VisualBasicConfigNamePrefix, StringComparison.OrdinalIgnoreCase) ? LanguageNames.VisualBasic :
                "";

            return _optionsByLanguage[language].Value.TryGetValue(key, out option);
        }
    }
}
