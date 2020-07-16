// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Completion
{
    public abstract partial class CompletionServiceWithProviders
    {
        private class ProjectCompletionProvider
            : AbstractProjectExtensionProvider<CompletionProvider, ExportCompletionProviderAttribute>
        {
            public ProjectCompletionProvider(AnalyzerReference reference)
                : base(reference)
            {
            }

            protected override bool SupportsLanguage(ExportCompletionProviderAttribute exportAttribute, string language)
            {
                return exportAttribute.Language == null
                    || exportAttribute.Language.Length == 0
                    || exportAttribute.Language.Contains(language);
            }

            protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CompletionProvider> extensions)
            {
                // check whether the analyzer reference knows how to return completion providers directly.
                if (reference is ICompletionProviderFactory completionProviderFactory)
                {
                    extensions = completionProviderFactory.GetCompletionProviders();
                    return true;
                }

                extensions = default;
                return false;
            }
        }
    }
}
