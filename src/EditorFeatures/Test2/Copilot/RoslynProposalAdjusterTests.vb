' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Copilot
Imports Microsoft.CodeAnalysis.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Copilot
    <UseExportProvider>
    Public NotInheritable Class RoslynProposalAdjusterTests
        Private Shared ReadOnly s_composition As TestComposition = FeaturesTestCompositions.Features
        'Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures

        Private Shared Async Function Test(code As String, expected As String, language As String) As Task
            Using workspace = If(language Is LanguageNames.CSharp,
                    EditorTestWorkspace.CreateCSharp(code, composition:=s_composition),
                    EditorTestWorkspace.CreateVisualBasic(code, composition:=s_composition))
                Dim documentId = workspace.Documents.First().Id
                Dim proposalSpans = workspace.Documents.First().SelectedSpans

                Dim sourceText = Await workspace.CurrentSolution.GetDocument(documentId).GetTextAsync()

                ' Get the original document without the proposal edit in it.
                Dim originalDocument = workspace.CurrentSolution.GetDocument(documentId).WithText(
                    sourceText.WithChanges(proposalSpans.Select(Function(s) New TextChange(s, newText:=""))))

                Dim changes = New List(Of TextChange)()
                Dim delta = 0
                For Each selectionSpan In proposalSpans
                    changes.Add(New TextChange(
                        New TextSpan(selectionSpan.Start + delta, 0), newText:=sourceText.ToString(selectionSpan)))

                    delta -= selectionSpan.Length
                Next

                Dim service = workspace.Services.GetRequiredService(Of ICopilotProposalAdjusterService)
                Dim adjustedChanges = Await service.TryAdjustProposalAsync(
                    originalDocument, CopilotUtilities.TryNormalizeCopilotTextChanges(changes), CancellationToken.None)

                Dim originalDocumentText = Await originalDocument.GetTextAsync()
                Dim adjustedDocumentText = originalDocumentText.WithChanges(adjustedChanges)

                AssertEx.Equal(expected, adjustedDocumentText.ToString())
            End Using
        End Function

        Private Shared Async Function TestCSharp(code As String, expected As String) As Task
            Await Test(code, expected, LanguageNames.CSharp)
        End Function

        <WpfFact>
        Public Async Function TestCSharp1() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_ExistingUsingAfter() As Task
            Await TestCSharp("
using Test;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
    }
}", "
using System;
using Test;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_ExistingUsingBefore() As Task
            Await TestCSharp("
using System;

class C
{
    void M()
    {
        [|Task.Yield();|]
    }
}", "
using System;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Task.Yield();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_PartiallyWritten() As Task
            Await TestCSharp("
class C
{
    void M()
    {
        Con[|sole.WriteLine(1);|]
    }
}", "
using System;

class C
{
    void M()
    {
        Console.WriteLine(1);
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AddMultiple_Different() As Task
            Await TestCSharp("
using System.Collections.Generic;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
        if (true) { }
        [|Task.Yield();|]
    }
}", "
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true) { }
        Task.Yield();
    }
}")
        End Function

        <WpfFact>
        Public Async Function TestCSharp_AddMultiple_Same() As Task
            Await TestCSharp("
using System.Collections.Generic;

class C
{
    void M()
    {
        [|Console.WriteLine(1);|]
        if (true) { }
        [|Console.WriteLine();|]
    }
}", "
using System;
using System.Collections.Generic;

class C
{
    void M()
    {
        Console.WriteLine(1);
        if (true) { }
        Console.WriteLine();
    }
}")
        End Function
    End Class
End Namespace
