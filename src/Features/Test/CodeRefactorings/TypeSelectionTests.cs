// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.CSharp.ConvertToRecord;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeRefactorings;

[UseExportProvider]
public sealed class TypeSelectionTests
{
    public static TheoryData<string, bool> CSharpSelections => new()
    {
        // The enclosing node is a member rather than a type: the new guard does not apply.
        { "class C { [|public int P { get; set; }|] public string Q { get; set; } }", false },

        // The selection starts at the declaration's first token: no header-position check is needed.
        { "[|class C|] { public int P { get; set; } public string Q { get; set; } }", true },
        { "[|class C { public int P { get; set; } public string Q { get; set; } }|]", true },

        // Starting later in this type's header is allowed, including when the selection ends in the body.
        { "class [|C|] { public int P { get; set; } public string Q { get; set; } }", true },
        { "class [|C { public int P { get; set; }|] public string Q { get; set; } }", true },
        { "interface I { } class C [|: I { public int P { get; set; }|] public string Q { get; set; } }", true },

        // Starting in the body fails the header-position check.
        { "class C { [|public int P { get; set; } public string Q { get; set; }|] }", false },
        { "class C [|{ public int P { get; set; } public string Q { get; set; } }|]", false },
        { "interface I { } class C : I [|{ public int P { get; set; } public string Q { get; set; } }|]", false },

        // A nested type's header must not make the enclosing type relevant.
        { "class C { public int P { get; set; } [|class A { } class B { }|] }", false },
        { "class Outer { class [|C { public int P { get; set; }|] public string Q { get; set; } } }", true },

        // Attribute lists are excluded from the header's start. All of these include the header.
        { "[|[System.Obsolete] [System.Serializable] class C|] { public int P { get; set; } public string Q { get; set; } }", true },
        { "[System.Obsolete] [|[System.Serializable] class C|] { public int P { get; set; } public string Q { get; set; } }", true },
        { "[System.Obsolete][|[System.Serializable]class C|] { public int P { get; set; } public string Q { get; set; } }", true },
        { "[System.Obsolete] [System.Serializable] [|class C|] { public int P { get; set; } public string Q { get; set; } }", true },
        { "[System.Obsolete] [System.Serializable] class [|C { public int P { get; set; }|] public string Q { get; set; } }", true },

        // Selecting attributes alone must not offer a type action, even when FindNode returns the type.
        { "[|[System.Obsolete] [System.Serializable]|] class C { public int P { get; set; } public string Q { get; set; } }", false },
        { "[|[System.Obsolete][System.Serializable]|]class C { public int P { get; set; } public string Q { get; set; } }", false },
        { "[System.Obsolete] [|[System.Serializable]|] class C { public int P { get; set; } public string Q { get; set; } }", false },
        { "[System.Obsolete] [System.Serializable] class C { [|public int P { get; set; } public string Q { get; set; }|] }", false },

        // A caret takes the existing location path; selecting the same closing brace takes the selection path.
        { "class C { public int P { get; set; } public string Q { get; set; } [||]}", true },
        { "class C { public int P { get; set; } public string Q { get; set; } [|}|]", false },
        { "class C { int [||]i; public string Q { get; set; } }", false },
    };

    public static TheoryData<string, bool> VisualBasicSelections => new()
    {
        { "Class C\n[|Dim i As Integer|]\nDim s As String\nEnd Class", false },

        // Selecting just the statement does not select the TypeBlockSyntax used by this provider.
        { "[|Class C|]\nDim i As Integer\nDim s As String\nEnd Class", false },
        { "[|Class C\nDim i As Integer\nDim s As String\nEnd Class|]", true },
        { "Class [|C|]\nDim i As Integer\nDim s As String\nEnd Class", false },
        { "Class [|C\nDim i As Integer|]\nDim s As String\nEnd Class", true },
        { "Class C\n[|Dim i As Integer\nDim s As String|]\nEnd Class", false },
        { "Class C\n[|Class A\nEnd Class\nClass B\nEnd Class|]\nEnd Class", false },
        { "Class Outer\nClass [|C\nDim i As Integer|]\nDim s As String\nEnd Class\nEnd Class", true },
        { "[|<System.Obsolete> <System.Serializable> Class C\nDim i As Integer|]\nDim s As String\nEnd Class", true },
        { "<System.Obsolete> [|<System.Serializable> Class C\nDim i As Integer|]\nDim s As String\nEnd Class", true },
        { "<System.Obsolete> <System.Serializable> [|Class C\nDim i As Integer|]\nDim s As String\nEnd Class", true },
        { "[|<System.Obsolete> <System.Serializable>|] Class C\nDim i As Integer\nDim s As String\nEnd Class", false },
        { "<System.Obsolete> [|<System.Serializable>|] Class C\nDim i As Integer\nDim s As String\nEnd Class", false },
        { "<System.Obsolete> <System.Serializable> Class C\n[|Dim i As Integer\nDim s As String|]\nEnd Class", false },
        { "Class [||]C\nDim i As Integer\nDim s As String\nEnd Class", true },
        { "Class C\nDim i As Integer\nDim s As String\n[|End Class|]", false },
        { "Class C\nDim [||]i As Integer\nDim s As String\nEnd Class", false },
    };

    [Theory]
    [MemberData(nameof(CSharpSelections))]
    public Task CSharpMoveType(string markup, bool expected)
        => VerifyRefactoringAsync<MoveTypeCodeRefactoringProvider>(LanguageNames.CSharp, markup, expected);

    [Theory]
    [MemberData(nameof(CSharpSelections))]
    public Task CSharpAddDebuggerDisplay(string markup, bool expected)
        => VerifyRefactoringAsync<CSharpAddDebuggerDisplayCodeRefactoringProvider>(LanguageNames.CSharp, markup, expected);

    [Theory]
    [MemberData(nameof(CSharpSelections))]
    public Task CSharpConvertToRecord(string markup, bool expected)
        => VerifyRefactoringAsync<CSharpConvertToRecordRefactoringProvider>(LanguageNames.CSharp, markup, expected);

    [Theory]
    [MemberData(nameof(VisualBasicSelections))]
    public Task VisualBasicAddDebuggerDisplay(string markup, bool expected)
        => VerifyRefactoringAsync<VisualBasicAddDebuggerDisplayCodeRefactoringProvider>(LanguageNames.VisualBasic, markup, expected);

    [Theory]
    [InlineData("[|Class C|]\nDim i As Integer\nEnd Class", true)]
    [InlineData("Class [|C|]\nDim i As Integer\nEnd Class", true)]
    [InlineData("[|<System.Obsolete> <System.Serializable> Class C|]\nDim i As Integer\nEnd Class", true)]
    [InlineData("<System.Obsolete> [|<System.Serializable> Class C|]\nDim i As Integer\nEnd Class", true)]
    [InlineData("<System.Obsolete>[|<System.Serializable>Class C|]\nDim i As Integer\nEnd Class", true)]
    [InlineData("<System.Obsolete> <System.Serializable> [|Class C|]\nDim i As Integer\nEnd Class", true)]
    [InlineData("[|<System.Obsolete> <System.Serializable>|] Class C\nDim i As Integer\nEnd Class", false)]
    [InlineData("[|<System.Obsolete><System.Serializable>|]Class C\nDim i As Integer\nEnd Class", false)]
    [InlineData("<System.Obsolete> [|<System.Serializable>|] Class C\nDim i As Integer\nEnd Class", false)]
    [InlineData("Class C\n[|Dim i As Integer\nDim s As String|]\nEnd Class", false)]
    [InlineData("Class C\n[|Class A\nEnd Class\nClass B\nEnd Class|]\nEnd Class", false)]
    [InlineData("Class [||]C\nDim i As Integer\nEnd Class", true)]
    [InlineData("Class C\nDim [||]i As Integer\nEnd Class", false)]
    public Task VisualBasicMoveType(string markup, bool expected)
        => VerifyRefactoringAsync<MoveTypeCodeRefactoringProvider>(LanguageNames.VisualBasic, markup, expected);

    [Theory]
    [InlineData("class C { public override string [|ToString|]() => \"\"; }", true)]
    [InlineData("class C { public string [|M|]() => \"\"; }", false)]
    public Task CSharpAddDebuggerDisplayOnSelectedMethod(string markup, bool expected)
        => VerifyRefactoringAsync<CSharpAddDebuggerDisplayCodeRefactoringProvider>(LanguageNames.CSharp, markup, expected);

    [Theory]
    [InlineData("Class C\nPublic Overrides Function [|ToString|]() As String\nReturn \"\"\nEnd Function\nEnd Class", true)]
    [InlineData("Class C\nPublic Function [|M|]() As String\nReturn \"\"\nEnd Function\nEnd Class", false)]
    public Task VisualBasicAddDebuggerDisplayOnSelectedMethod(string markup, bool expected)
        => VerifyRefactoringAsync<VisualBasicAddDebuggerDisplayCodeRefactoringProvider>(LanguageNames.VisualBasic, markup, expected);

    private static async Task VerifyRefactoringAsync<TProvider>(string language, string markup, bool expected)
        where TProvider : CodeRefactoringProvider
    {
        using var workspace = language == LanguageNames.CSharp
            ? TestWorkspace.CreateCSharp(markup)
            : TestWorkspace.CreateVisualBasic(markup);
        var hostDocument = workspace.Documents.Single();
        var document = workspace.CurrentSolution.GetRequiredDocument(hostDocument.Id);
        var span = hostDocument.CursorPosition is int position
            ? new TextSpan(position, 0)
            : hostDocument.SelectedSpans.Single();
        var provider = workspace.ExportProvider.GetExportedValues<CodeRefactoringProvider>().OfType<TProvider>().Single();
        var actions = new List<CodeAction>();

        await provider.ComputeRefactoringsAsync(new CodeRefactoringContext(document, span, actions.Add, CancellationToken.None));

        Assert.Equal(expected, actions.Count > 0);
    }
}
