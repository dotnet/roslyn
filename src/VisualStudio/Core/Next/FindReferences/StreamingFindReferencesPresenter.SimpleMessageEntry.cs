// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class SimpleMessageEntry : Entry
        {
            private readonly string _message;

            private SimpleMessageEntry(
                RoslynDefinitionBucket definitionBucket,
                string message)
                : base(definitionBucket)
            {
                _message = message;
            }

            public static Task<Entry> CreateAsync(
                RoslynDefinitionBucket definitionBucket,
                string message)
            {
                var referenceEntry = new SimpleMessageEntry(definitionBucket, message);
                return Task.FromResult<Entry>(referenceEntry);
            }

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                case StandardTableKeyNames.Text:
                    return _message; 
                }

                return null;
            }
        }
    }
}
