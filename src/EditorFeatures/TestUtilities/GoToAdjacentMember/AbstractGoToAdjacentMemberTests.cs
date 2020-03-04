﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToAdjacentMember
{
    [UseExportProvider]
    public abstract class AbstractGoToAdjacentMemberTests
    {
        protected abstract string LanguageName { get; }
        protected abstract ParseOptions DefaultParseOptions { get; }

        protected async Task AssertNavigatedAsync(string code, bool next, SourceCodeKind? sourceCodeKind = null)
        {
            var kinds = sourceCodeKind != null
                ? SpecializedCollections.SingletonEnumerable(sourceCodeKind.Value)
                : new[] { SourceCodeKind.Regular, SourceCodeKind.Script };

            foreach (var kind in kinds)
            {
                using (var workspace = TestWorkspace.Create(
                    LanguageName,
                    compilationOptions: null,
                    parseOptions: DefaultParseOptions.WithKind(kind),
                    content: code))
                {
                    var hostDocument = workspace.DocumentWithCursor;
                    var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                    Assert.Empty((await document.GetSyntaxTreeAsync()).GetDiagnostics());
                    var targetPosition = await GoToAdjacentMemberCommandHandler.GetTargetPositionAsync(
                        document,
                        hostDocument.CursorPosition.Value,
                        next,
                        CancellationToken.None);

                    Assert.NotNull(targetPosition);
                    Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value);
                }
            }
        }

        protected async Task<int?> GetTargetPositionAsync(string code, bool next)
        {
            using (var workspace = TestWorkspace.Create(
                LanguageName,
                compilationOptions: null,
                parseOptions: DefaultParseOptions,
                content: code))
            {
                var hostDocument = workspace.DocumentWithCursor;
                var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
                Assert.Empty((await document.GetSyntaxTreeAsync()).GetDiagnostics());

                return await GoToAdjacentMemberCommandHandler.GetTargetPositionAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    next,
                    CancellationToken.None);
            }
        }
    }
}
