// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class CodeFixService
    {
        private class ProjectCodeFixProvider
            : AbstractProjectExtensionProvider<ProjectCodeFixProvider, CodeFixProvider, ExportCodeFixProviderAttribute>
        {
            protected override ImmutableArray<string> GetLanguages(ExportCodeFixProviderAttribute exportAttribute)
                => [.. exportAttribute.Languages];

            protected override bool TryGetExtensionsFromReference(AnalyzerReference reference, out ImmutableArray<CodeFixProvider> extensions)
            {
                // check whether the analyzer reference knows how to return fixers directly.
                if (reference is ICodeFixProviderFactory codeFixProviderFactory)
                {
                    extensions = codeFixProviderFactory.GetFixers();
                    return true;
                }

                extensions = default;
                return false;
            }
        }
    }
}
