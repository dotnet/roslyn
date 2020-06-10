// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
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

            private Document TryGetDocument()
                => DefinitionBucket.DefinitionItem.SourceSpans.FirstOrDefault().Document;

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.DocumentName:
                        return this.TryGetDocument()?.FilePath;
                    case StandardTableKeyNames.ProjectName:
                        return this.TryGetDocument()?.Project.Name;
                    case StandardTableKeyNames.Text:
                        return _message;
                }

                return null;
            }
        }
    }
}
