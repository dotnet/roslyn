// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeRefactorings;

[UseExportProvider]
public sealed class MoveTypeTests
{
    [Theory]
    [InlineData(LanguageNames.CSharp, "class C { [|int i; string s;|] }")]
    [InlineData(LanguageNames.CSharp, "class C { int [|i; string s|]; }")]
    [InlineData(LanguageNames.CSharp, "class C { [|int P { get; set; } string Q { get; set; }|] }")]
    [InlineData(LanguageNames.CSharp, "class C { [|void M() { } void N() { }|] }")]
    [InlineData(LanguageNames.CSharp, "class C { [|class A { } class B { }|] }")]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Dim i As Integer\nDim s As String|]\nEnd Class")]
    [InlineData(LanguageNames.VisualBasic, "Class C\nDim [|i As Integer\nDim s|] As String\nEnd Class")]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Property P As Integer\nProperty Q As String|]\nEnd Class")]
    [InlineData(LanguageNames.VisualBasic, "Class C\n[|Sub M()\nEnd Sub\nSub N()\nEnd Sub|]\nEnd Class")]
    public async Task NotOfferedOnSelectedMembers(string language, string markup)
    {
        using var workspace = language == LanguageNames.CSharp
            ? TestWorkspace.CreateCSharp(markup)
            : TestWorkspace.CreateVisualBasic(markup);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var span = workspace.Documents.Single().SelectedSpans.Single();
        var service = document.GetRequiredLanguageService<IMoveTypeService>();

        Assert.Empty(await service.GetRefactoringAsync(document, span, CancellationToken.None));
    }

    [Theory]
    [InlineData(LanguageNames.CSharp, "[|class C|] { int i; string s; }")]
    [InlineData(LanguageNames.CSharp, "[|[System.Obsolete] class C|] { int i; string s; }")]
    [InlineData(LanguageNames.CSharp, "[|class C { int i; string s; }|]")]
    [InlineData(LanguageNames.CSharp, "class Outer { [|class C { int i; string s; }|] }")]
    [InlineData(LanguageNames.VisualBasic, "[|Class C|]\nDim i As Integer\nDim s As String\nEnd Class")]
    [InlineData(LanguageNames.VisualBasic, "[|<System.Obsolete> Class C|]\nDim i As Integer\nDim s As String\nEnd Class")]
    public async Task OfferedOnSelectedType(string language, string markup)
    {
        using var workspace = language == LanguageNames.CSharp
            ? TestWorkspace.CreateCSharp(markup)
            : TestWorkspace.CreateVisualBasic(markup);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var span = workspace.Documents.Single().SelectedSpans.Single();
        var service = document.GetRequiredLanguageService<IMoveTypeService>();

        Assert.NotEmpty(await service.GetRefactoringAsync(document, span, CancellationToken.None));
    }
}
