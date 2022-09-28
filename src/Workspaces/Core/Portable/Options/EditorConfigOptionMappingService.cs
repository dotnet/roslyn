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
    [ExportWorkspaceService(typeof(IEditorConfigOptionMappingService)), Shared]
    internal sealed class EditorConfigOptionMappingService : IEditorConfigOptionMappingService
    {
        private static readonly ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> s_emptyEditorConfigKeysToOptions
            = ImmutableDictionary.Create<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)>(AnalyzerConfigOptions.KeyComparer);

        private ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> _neutralEditorConfigKeysToOptions = s_emptyEditorConfigKeysToOptions;
        private ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> _csharpEditorConfigKeysToOptions = s_emptyEditorConfigKeysToOptions;
        private ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> _visualBasicEditorConfigKeysToOptions = s_emptyEditorConfigKeysToOptions;

        private readonly ImmutableDictionary<string, Lazy<ImmutableHashSet<IOption>>> _serializableOptionsByLanguage;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigOptionMappingService(
            [ImportMany] IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            _serializableOptionsByLanguage = CreateLazySerializableOptionsByLanguage(optionProviders);
        }

        private static ImmutableDictionary<string, Lazy<ImmutableHashSet<IOption>>> CreateLazySerializableOptionsByLanguage(IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, Lazy<ImmutableHashSet<IOption>>>();

            foreach (var (language, lazyProvidersAndMetadata) in optionProviders.ToPerLanguageMap())
            {
                builder.Add(language, new Lazy<ImmutableHashSet<IOption>>(() => ComputeSerializableOptionsFromProviders(lazyProvidersAndMetadata)));
            }

            return builder.ToImmutable();

            // Local functions
            static ImmutableHashSet<IOption> ComputeSerializableOptionsFromProviders(ImmutableArray<Lazy<IOptionProvider, LanguageMetadata>> lazyProvidersAndMetadata)
            {
                var builder = ImmutableHashSet.CreateBuilder<IOption>();

                foreach (var lazyProviderAndMetadata in lazyProvidersAndMetadata)
                {
                    var provider = lazyProviderAndMetadata.Value;
                    if (!IsSolutionOptionProvider(provider))
                    {
                        continue;
                    }

                    builder.AddRange(provider.Options);
                }

                return builder.ToImmutable();
            }
        }

        // We only consider the options defined in the the DefaultAssemblies (Workspaces and Features) as serializable.
        // This is due to the fact that other layers above are VS specific and do not execute in OOP.
        internal static bool IsSolutionOptionProvider(IOptionProvider provider)
            => MefHostServices.IsDefaultAssembly(provider.GetType().Assembly);

        public bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey)
        {
            var temporaryOptions = s_emptyEditorConfigKeysToOptions;
            ref var editorConfigToOptionsStorage = ref temporaryOptions;
            switch (language)
            {
                case LanguageNames.CSharp:
                    editorConfigToOptionsStorage = ref _csharpEditorConfigKeysToOptions!;
                    break;

                case LanguageNames.VisualBasic:
                    editorConfigToOptionsStorage = ref _visualBasicEditorConfigKeysToOptions!;
                    break;

                case null:
                case "":
                    editorConfigToOptionsStorage = ref _neutralEditorConfigKeysToOptions!;
                    break;
            }

            var (option, storage) = ImmutableInterlocked.GetOrAdd(
                ref editorConfigToOptionsStorage,
                key,
                (key, arg) => MapToOptionIgnorePerLanguage(arg.self, key, arg.language),
                (self: this, language));

            if (option is object)
            {
                RoslynDebug.AssertNotNull(storage);
                storageLocation = storage;
                optionKey = option.IsPerLanguage ? new OptionKey(option, language) : new OptionKey(option);
                return true;
            }

            storageLocation = null;
            optionKey = default;
            return false;

            // Local function
            static (IOption? option, IEditorConfigStorageLocation2? storageLocation) MapToOptionIgnorePerLanguage(EditorConfigOptionMappingService service, string key, string? language)
            {
                // Use GetRegisteredSerializableOptions instead of GetRegisteredOptions to avoid loading assemblies for
                // inactive languages.
                foreach (var option in service.GetRegisteredSerializableOptions(ImmutableHashSet.Create(language ?? "")))
                {
                    foreach (var storage in option.StorageLocations)
                    {
                        if (storage is not IEditorConfigStorageLocation2 editorConfigStorage)
                            continue;

                        if (!AnalyzerConfigOptions.KeyComparer.Equals(key, editorConfigStorage.KeyName))
                            continue;

                        return (option, editorConfigStorage);
                    }
                }

                return (null, null);
            }
        }

        public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages)
        {
            if (languages.IsEmpty)
            {
                return ImmutableHashSet<IOption>.Empty;
            }

            var builder = ImmutableHashSet.CreateBuilder<IOption>();

            // "string.Empty" for options from language agnostic option providers.
            builder.AddRange(GetSerializableOptionsForLanguage(string.Empty));

            foreach (var language in languages)
            {
                builder.AddRange(GetSerializableOptionsForLanguage(language));
            }

            return builder.ToImmutable();

            // Local functions.
            ImmutableHashSet<IOption> GetSerializableOptionsForLanguage(string language)
            {
                if (_serializableOptionsByLanguage.TryGetValue(language, out var lazyOptions))
                {
                    return lazyOptions.Value;
                }

                return ImmutableHashSet<IOption>.Empty;
            }
        }
    }
}
