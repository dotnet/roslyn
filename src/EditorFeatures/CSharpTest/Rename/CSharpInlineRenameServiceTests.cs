// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Rename;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Rename)]
public sealed class CSharpInlineRenameServiceTests
{
    private sealed class ContextDictionaryComparer : IEqualityComparer<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>?>
    {
        public static ContextDictionaryComparer Instance = new();

        public bool Equals(ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>? x, ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>? y)
        {
            if (x == y)
                return true;

            if (x is null || y is null)
                return false;

            if (x.Count != y.Count)
                return false;

            foreach (var (elementFromX, elementFromY) in x.Zip(y, (elementFromX, elementFromY) => (elementFromX, elementFromY)))
            {
                var (keyFromX, valueFromX) = elementFromX;
                var (keyFromY, valueFromY) = elementFromY;

                if (keyFromX != keyFromY || !valueFromX.SequenceEqual(valueFromY))
                    return false;
            }

            return true;
        }

        public int GetHashCode(ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>? obj)
            => EqualityComparer<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>?>.Default.GetHashCode(obj);
    }

    private static async Task VerifyGetRenameContextAsync(
        string markup, string expectedContextJson, SymbolRenameOptions options, CancellationToken cancellationToken)
    {
        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);
        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, cancellationToken).ConfigureAwait(false);
        var inlineRenameLocationSet = await inlineRenameInfo.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
        var context = await inlineRenameService.GetRenameContextAsync(inlineRenameInfo, inlineRenameLocationSet, cancellationToken).ConfigureAwait(false);
        var serializationOptions = new JsonSerializerOptions
        {
            IncludeFields = true,
        };
        var expectedContext = JsonSerializer.Deserialize<ImmutableDictionary<string, ImmutableArray<(string, string)>>>(expectedContextJson, serializationOptions);
        AssertEx.AreEqual<ImmutableDictionary<string, ImmutableArray<(string filePath, string content)>>?>(expectedContext, context, comparer: ContextDictionaryComparer.Instance);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/74545")]
    public async Task VerifyContextReachEndOfFile()
    {
        var escapedPath = Path.Combine(TestWorkspace.RootDirectory, "test1.cs").Replace("\\", "\\\\");

        await VerifyGetRenameContextAsync(
            """
            public class Sampl$$eClass()
            {
            }
            """,
            $$"""
            {
                "definition": [{"Item1":"{{escapedPath}}", "Item2":"public class SampleClass()\r\n{\r\n}"}]
            }
            """,
            new SymbolRenameOptions(),
            CancellationToken.None);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/883")]
    public async Task VerifyAnonymousTypeMemberRenameIsAllowed()
    {
        var markup = """
            using System;
            
            class Program
            {
                static void Main(string[] args)
                {
                    var x = new { Pr$$op = 3 };
                    Console.WriteLine(x.Prop);
                }
            }
            """;

        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId);
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(false);

        // Verify that rename is allowed (not error)
        Assert.True(inlineRenameInfo.CanRename, "Anonymous type member should be renameable");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task VerifyNestedTypeFileRenameAllowed_SimpleName()
    {
        var markup = """
            partial class Outer
            {
                class In$$ner { }
            }
            """;

        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId).WithName("Inner.cs");
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(false);

        // Verify that file rename is allowed for simple name matching
        Assert.Equal(InlineRenameFileRenameInfo.Allowed, inlineRenameInfo.GetFileRenameInfo());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task VerifyNestedTypeFileRenameAllowed_DottedName()
    {
        var markup = """
            partial class Outer
            {
                class In$$ner { }
            }
            """;

        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId).WithName("Outer.Inner.cs");
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(false);

        // Verify that file rename is allowed for nested type naming convention
        Assert.Equal(InlineRenameFileRenameInfo.Allowed, inlineRenameInfo.GetFileRenameInfo());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task VerifyNestedTypeFileRenameAllowed_RenameOuterType()
    {
        var markup = """
            partial class Out$$er
            {
                class Inner { }
            }
            """;

        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId).WithName("Outer.Inner.cs");
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(false);

        // Verify that file rename is allowed when renaming outer type
        Assert.Equal(InlineRenameFileRenameInfo.Allowed, inlineRenameInfo.GetFileRenameInfo());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task VerifyNestedTypeFileRenameNotAllowed_WrongFileName()
    {
        var markup = """
            partial class Outer
            {
                class In$$ner { }
            }
            """;

        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId).WithName("SomeOtherName.cs");
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(false);

        // Verify that file rename is NOT allowed when file name doesn't match
        Assert.Equal(InlineRenameFileRenameInfo.TypeDoesNotMatchFileName, inlineRenameInfo.GetFileRenameInfo());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task VerifyDeeplyNestedTypeFileRenameAllowed()
    {
        var markup = """
            partial class A
            {
                class B
                {
                    class C$$
                    {
                    }
                }
            }
            """;

        using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        var documentId = workspace.Documents.Single().Id;
        var document = workspace.CurrentSolution.GetRequiredDocument(documentId).WithName("A.B.C.cs");
        var inlineRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
        MarkupTestFile.GetPosition(markup, out _, out int cursorPosition);
        var inlineRenameInfo = await inlineRenameService.GetRenameInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(false);

        // Verify that file rename is allowed for deeply nested types
        Assert.Equal(InlineRenameFileRenameInfo.Allowed, inlineRenameInfo.GetFileRenameInfo());
    }
}
