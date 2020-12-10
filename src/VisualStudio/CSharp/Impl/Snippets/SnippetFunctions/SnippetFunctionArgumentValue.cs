// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions
{
    internal sealed class SnippetFunctionArgumentValue : AbstractSnippetFunctionArgumentValue
    {
        public SnippetFunctionArgumentValue(SnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string fieldName, string parameter)
            : base(snippetExpansionClient, subjectBuffer, fieldName, parameter)
        {
        }

        protected override string? FallbackDefaultLiteral => "default";
    }
}
