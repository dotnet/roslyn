// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    [UseExportProvider]
    public class ISemanticSnapshotExtensionTests
    {
        [Fact]
        public async Task TryGetSymbolTouchingPositionOnLeadingTrivia()
        {
            using var workspace = TestWorkspace.CreateCSharp(
                @"using System;
                class Program
                {
                    static void Main()
                    {
                        $$#pragma warning disable 612
                        Goo();
                        #pragma warning restore 612
                    }
                }");
            var position = workspace.Documents.Single(d => d.CursorPosition.HasValue).CursorPosition.Value;
            var snapshot = workspace.Documents.Single().TextBuffer.CurrentSnapshot;

            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id);
            Assert.NotNull(document);

            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position);
            Assert.Null(symbol);
        }
    }
}
