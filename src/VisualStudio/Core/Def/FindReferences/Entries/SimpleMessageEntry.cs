// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        private sealed class SimpleMessageEntry : Entry, ISupportsNavigation
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
                    StandardTableKeyNames.ProjectName => ServicesVSResources.Not_applicable,
                    StandardTableKeyNames.Text => _message,
                    _ => null,
                };
            }

            public bool CanNavigateTo()
                => _navigationBucket != null && _navigationBucket.CanNavigateTo();

            public Task NavigateToAsync(NavigationOptions options, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(CanNavigateTo());
                Contract.ThrowIfNull(_navigationBucket);
                return _navigationBucket.NavigateToAsync(options, cancellationToken);
            }
        }
    }
}
