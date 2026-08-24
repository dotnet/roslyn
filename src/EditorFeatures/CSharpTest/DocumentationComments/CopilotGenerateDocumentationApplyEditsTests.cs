// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class CopilotGenerateDocumentationApplyEditsTests
{
    [WpfFact]
    public void ApplyEdits_TranslatesSpansToCurrentSnapshotAndInsertsText()
    {
        const string code = """
            class C
            {
                /// <summary></summary>
                void M() { }
            }
            """;
        using var workspace = EditorTestWorkspace.CreateCSharp(code);
        var buffer = workspace.Documents.Single().GetTextBuffer();

        // Capture the snapshot the edit spans are relative to, as the InlinePrompt accept path does when the
        // chip is shown.
        var snapshot = buffer.CurrentSnapshot;
        var insertionPoint = snapshot.GetText().IndexOf("<summary>", StringComparison.Ordinal) + "<summary>".Length;

        var edits = ImmutableArray.Create(
            new DocumentationCommentEdit(new TextSpan(insertionPoint, 0), "A short summary."));

        // Mutate the buffer AFTER capturing the snapshot so ApplyEdits must translate the edit span across
        // versions before applying it.
        buffer.Insert(0, "// prefix\r\n");

        CopilotGenerateDocumentationCommentManager.ApplyEdits(buffer, snapshot, edits);

        var result = buffer.CurrentSnapshot.GetText();
        Assert.StartsWith("// prefix", result);
        Assert.Contains("<summary>A short summary.</summary>", result);
    }

    [WpfFact]
    public void ApplyEdits_AppliesMultipleEditsInOneTransaction()
    {
        const string code = """
            class C
            {
                /// <summary></summary>
                /// <returns></returns>
                int M() => 0;
            }
            """;
        using var workspace = EditorTestWorkspace.CreateCSharp(code);
        var buffer = workspace.Documents.Single().GetTextBuffer();

        var snapshot = buffer.CurrentSnapshot;
        var text = snapshot.GetText();
        var summaryPoint = text.IndexOf("<summary>", StringComparison.Ordinal) + "<summary>".Length;
        var returnsPoint = text.IndexOf("<returns>", StringComparison.Ordinal) + "<returns>".Length;

        var edits = ImmutableArray.Create(
            new DocumentationCommentEdit(new TextSpan(summaryPoint, 0), "Returns zero."),
            new DocumentationCommentEdit(new TextSpan(returnsPoint, 0), "always zero"));

        CopilotGenerateDocumentationCommentManager.ApplyEdits(buffer, snapshot, edits);

        var result = buffer.CurrentSnapshot.GetText();
        Assert.Contains("<summary>Returns zero.</summary>", result);
        Assert.Contains("<returns>always zero</returns>", result);
    }
}
