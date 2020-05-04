// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    internal class OptionServiceMock : IOptionService
    {
#pragma warning disable 67
        public event EventHandler<OptionChangedEventArgs> OptionChanged;
#pragma warning restore 67

        // Feel free to add other option storages
        private readonly IDictionary<IOption, object> _optionsByOption;

        public OptionServiceMock(IDictionary<IOption, object> optionsByOption)
        {
            _optionsByOption = optionsByOption;
        }

        public object GetOption(OptionKey optionKey)
        {
            throw new NotImplementedException();
        }

        public T GetOption<T>(Option<T> option)
        {
            return (T)_optionsByOption[option];
        }

        public T GetOption<T>(Option2<T> option)
        {
            return (T)_optionsByOption[option];
        }

        public T GetOption<T>(PerLanguageOption<T> option, string languageName)
        {
            throw new NotImplementedException();
        }

        public T GetOption<T>(PerLanguageOption2<T> option, string languageName)
        {
            throw new NotImplementedException();
        }

        public SerializableOptionSet GetOptions()
        {
            throw new NotImplementedException();
        }

        public SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IOption> GetRegisteredOptions()
        {
            throw new NotImplementedException();
        }

        public bool TryMapEditorConfigKeyToOption(string key, string language, [NotNullWhen(true)] out IEditorConfigStorageLocation2 storageLocation, out OptionKey optionKey)
        {
            throw new NotImplementedException();
        }

        public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages)
        {
            throw new NotImplementedException();
        }

        public void SetOptions(OptionSet optionSet)
        {
            Equals(null, null);
            throw new NotImplementedException();
        }

        public void RegisterDocumentOptionsProvider(IDocumentOptionsProvider documentOptionsProvider)
        {
            throw new NotImplementedException();
        }

        public Task<OptionSet> GetUpdatedOptionSetForDocumentAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void RegisterWorkspace(Workspace workspace)
        {
            throw new NotImplementedException();
        }

        public void UnregisterWorkspace(Workspace workspace)
        {
            throw new NotImplementedException();
        }
    }
}
