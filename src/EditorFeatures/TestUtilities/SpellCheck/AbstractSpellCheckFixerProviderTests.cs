// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.SpellCheck;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.SpellChecker;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck
{
    public abstract class AbstractSpellCheckFixerProviderTests
    {
        protected abstract EditorTestWorkspace CreateWorkspace(string content);

#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete

        protected Task TestSuccessAsync(string content, string expected)
            => TestAsync(content, expected, expectFailure: false);

        protected Task TestFailureAsync(string content, string expected)
            => TestAsync(content, expected, expectFailure: true);

        private async Task TestAsync(string content, string expected, bool expectFailure)
        {
            using var workspace = CreateWorkspace(content);

            var threadingContext = workspace.ExportProvider.GetExportedValue<IThreadingContext>();

            var document = workspace.Documents.Single();
            var service = (RoslynSpellCheckFixerProvider)workspace.ExportProvider.GetExportedValue<ISpellCheckFixerProvider>();

            var buffer = document.GetTextBuffer();
            var (replacement, span) = document.AnnotatedSpans.Single();
            var result = await service.GetTestAccessor().TryRenameAsync(buffer.CurrentSnapshot.GetSpan(span.Single().ToSpan()), replacement, CancellationToken.None);

            if (expectFailure)
            {
                Assert.NotNull(result);
            }
            else
            {
                Assert.Null(result);
            }

            AssertEx.Equal(expected, buffer.CurrentSnapshot.GetText());
        }
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
