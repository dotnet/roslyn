// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [ExportWorkspaceServiceFactory(typeof(IOptionService)), Shared]
    internal class OptionServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OptionServiceFactory(IGlobalOptionService globalOptionService)
            => _globalOptionService = globalOptionService;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new OptionService(_globalOptionService);

        internal sealed class OptionService : IOptionService
        {
            private readonly IGlobalOptionService _globalOptionService;

            public OptionService(IGlobalOptionService globalOptionService)
            {
                _globalOptionService = globalOptionService;
            }

            // Simple forwarding functions.
            public SerializableOptionSet GetOptions() => GetSerializableOptionsSnapshot(ImmutableHashSet<string>.Empty);
            public SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages) => _globalOptionService.GetSerializableOptionsSnapshot(languages, this);
            public object? GetOption(OptionKey optionKey) => _globalOptionService.GetOption(optionKey);
            public T? GetOption<T>(Option<T> option) => _globalOptionService.GetOption(option);
            public T? GetOption<T>(Option2<T> option) => _globalOptionService.GetOption(option);
            public T? GetOption<T>(PerLanguageOption<T> option, string? languageName) => _globalOptionService.GetOption(option, languageName);
            public T? GetOption<T>(PerLanguageOption2<T> option, string? languageName) => _globalOptionService.GetOption(option, languageName);
            public IEnumerable<IOption> GetRegisteredOptions() => _globalOptionService.GetRegisteredOptions();
            public bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey) => _globalOptionService.TryMapEditorConfigKeyToOption(key, language, out storageLocation, out optionKey);
            public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages) => _globalOptionService.GetRegisteredSerializableOptions(languages);
            public void SetOptions(OptionSet optionSet) => _globalOptionService.SetOptions(optionSet);
            public void RegisterWorkspace(Workspace workspace) => _globalOptionService.RegisterWorkspace(workspace);
            public void UnregisterWorkspace(Workspace workspace) => _globalOptionService.UnregisterWorkspace(workspace);
        }
    }
}
