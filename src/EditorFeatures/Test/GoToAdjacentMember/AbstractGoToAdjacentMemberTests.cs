// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToAdjacentMember
{
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
                using (var workspace = await TestWorkspace.CreateAsync(
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
            using (var workspace = await TestWorkspace.CreateAsync(
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
