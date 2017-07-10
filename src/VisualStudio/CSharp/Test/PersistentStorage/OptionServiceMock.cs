// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        public T GetOption<T>(PerLanguageOption<T> option, string languageName)
        {
            throw new NotImplementedException();
        }

        public OptionSet GetOptions()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IOption> GetRegisteredOptions()
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
    }
}
