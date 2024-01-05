// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal class SnippetFunctionClassName : AbstractSnippetFunction
    {
        protected readonly string FieldName;

        public SnippetFunctionClassName(AbstractSnippetExpansionClient snippetExpansionClient, ITextBuffer subjectBuffer, string fieldName, IThreadingContext threadingContext)
            : base(snippetExpansionClient, subjectBuffer, threadingContext)
        {
            this.FieldName = fieldName;
        }

        protected override async Task<(int ExitCode, string Value, int HasDefaultValue)> GetDefaultValueAsync(CancellationToken cancellationToken)
        {
            var hasDefaultValue = 0;
            var value = string.Empty;
            if (!TryGetDocument(out var document))
            {
                return (VSConstants.E_FAIL, value, hasDefaultValue);
            }

            var surfaceBufferFieldSpan = new VsTextSpan[1];
            if (snippetExpansionClient.ExpansionSession.GetFieldSpan(FieldName, surfaceBufferFieldSpan) != VSConstants.S_OK)
            {
                return (VSConstants.E_FAIL, value, hasDefaultValue);
            }

            if (!snippetExpansionClient.TryGetSubjectBufferSpan(surfaceBufferFieldSpan[0], out var subjectBufferFieldSpan))
            {
                return (VSConstants.E_FAIL, value, hasDefaultValue);
            }

            var snippetFunctionService = document.Project.GetRequiredLanguageService<SnippetFunctionService>();
            value = await snippetFunctionService.GetContainingClassNameAsync(document, subjectBufferFieldSpan.Start.Position, cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(value))
            {
                hasDefaultValue = 1;
            }

            return (VSConstants.S_OK, value, hasDefaultValue);
        }
    }
}
