// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

public sealed class FSharpSourceReferenceItemTests
{
    [Fact]
    public void ReferenceCarriesTheClassifiedLine()
    {
        using var workspace = new AdhocWorkspace(NoCompilationLanguage.CreateHostServices());
        var document = workspace.AddProject("Project", NoCompilationLanguage.Name).AddDocument("Document.fs", "    let x = f 1");
        var sourceSpan = new FSharpDocumentSpan(document, new TextSpan(12, 1));
        ImmutableArray<ClassifiedSpan> classifiedSpans =
        [
            new(ClassificationTypeNames.Keyword, new TextSpan(4, 3)),
            new(ClassificationTypeNames.Text, new TextSpan(7, 5)),
            new(ClassificationTypeNames.MethodName, new TextSpan(12, 1)),
            new(ClassificationTypeNames.Text, new TextSpan(13, 1)),
            new(ClassificationTypeNames.NumericLiteral, new TextSpan(14, 1)),
        ];

        var reference = new FSharpSourceReferenceItem(Definition(sourceSpan), sourceSpan, classifiedSpans, highlightSpan: new TextSpan(8, 1));

        var classified = reference.RoslynSourceReferenceItem.ClassifiedSpans;
        Assert.NotNull(classified);
        Assert.Equal(classifiedSpans, classified.Value.ClassifiedSpans);
        Assert.Equal(new TextSpan(8, 1), classified.Value.HighlightSpan);
    }

    [Fact]
    public void ReferenceWithoutClassificationLeavesItToTheWindow()
    {
        using var workspace = new AdhocWorkspace(NoCompilationLanguage.CreateHostServices());
        var document = workspace.AddProject("Project", NoCompilationLanguage.Name).AddDocument("Document.fs", "f 1");
        var sourceSpan = new FSharpDocumentSpan(document, new TextSpan(0, 1));

        var reference = new FSharpSourceReferenceItem(Definition(sourceSpan), sourceSpan);

        Assert.Null(reference.RoslynSourceReferenceItem.ClassifiedSpans);
    }

    private static FSharpDefinitionItem Definition(FSharpDocumentSpan sourceSpan)
        => FSharpDefinitionItem.Create([], [new TaggedText(TextTags.Text, "f")], sourceSpan);
}
