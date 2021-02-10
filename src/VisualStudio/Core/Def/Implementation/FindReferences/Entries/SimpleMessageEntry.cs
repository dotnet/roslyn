// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private class SimpleMessageEntry : Entry, ISupportsNavigation
        {
            private readonly RoslynDefinitionBucket? _navigationBucket;
            private readonly string _message;

            private SimpleMessageEntry(
                RoslynDefinitionBucket definitionBucket,
                RoslynDefinitionBucket? navigationBucket,
                string message)
                : base(definitionBucket)
            {
                _navigationBucket = navigationBucket;
                _message = message;
            }

            public static Task<Entry> CreateAsync(
                RoslynDefinitionBucket definitionBucket,
                RoslynDefinitionBucket? navigationBucket,
                string message)
            {
                var referenceEntry = new SimpleMessageEntry(definitionBucket, navigationBucket, message);
                return Task.FromResult<Entry>(referenceEntry);
            }

            protected override object? GetValueWorker(string keyName)
            {
                return keyName switch
                {
                    StandardTableKeyNames.ProjectName => "Not applicable",
                    StandardTableKeyNames.Text => _message,
                    _ => null,
                };
            }

            public bool TryNavigateTo(bool isPreview, CancellationToken cancellationToken)
                => _navigationBucket != null && _navigationBucket.TryNavigateTo(isPreview, cancellationToken);
        }
    }
}
