// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    internal sealed class MockCodeCleanupProvider : ICodeCleanupProvider
    {
        public IEnumerable<TextSpan> ExpectedResult = null!;

        public Func<MockCodeCleanupProvider, Document, ImmutableArray<TextSpan>, SyntaxFormattingOptions, CancellationToken, Task<Document>>? CleanupDocumentAsyncImpl { get; set; }
        public Func<SyntaxNode, ImmutableArray<TextSpan>, SyntaxFormattingOptions, SolutionServices, SyntaxNode>? CleanupNodeImpl { get; set; }

        public string Name => nameof(MockCodeCleanupProvider);

        public Task<Document> CleanupAsync(Document document, ImmutableArray<TextSpan> spans, CodeCleanupOptions options, CancellationToken cancellationToken)
            => (CleanupDocumentAsyncImpl ?? throw new NotImplementedException()).Invoke(this, document, spans, options.FormattingOptions, cancellationToken);

        public Task<SyntaxNode> CleanupAsync(SyntaxNode root, ImmutableArray<TextSpan> spans, SyntaxFormattingOptions options, SolutionServices services, CancellationToken cancellationToken)
            => Task.FromResult((CleanupNodeImpl ?? throw new NotImplementedException()).Invoke(root, spans, options, services));
    }
}
