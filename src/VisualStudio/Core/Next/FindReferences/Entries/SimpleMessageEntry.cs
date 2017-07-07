// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                // We only create 'message entries' when we found no references to this definition.
                // In that case, suppress showing the count next to the definition as it will just
                // be strange.  i.e. the count will show '(1)' (i.e. us) even though we're indicating
                // that there really were zero references.
                DefinitionBucket.ShowCount = false;
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
            {
                return DefinitionBucket.DefinitionItem.SourceSpans.FirstOrDefault().Document;
            }

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
