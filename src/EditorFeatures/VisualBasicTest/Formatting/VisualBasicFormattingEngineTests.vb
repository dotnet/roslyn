' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    Public Class VisualBasicFormattingEngineTests
        Inherits VisualBasicFormatterTestBase

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SeparateGroups_KeepMultipleLinesBetweenGroups()
            Dim code = "$$
Imports System.A
Imports System.B


Imports MS.A
Imports MS.B
"

            Dim expected = "$$
Imports System.A
Imports System.B


Imports MS.A
Imports MS.B
"

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, True))
        End Sub

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SeparateGroups_DoNotGroupIfNotSorted()
            Dim code = "$$
Imports System.B
Imports System.A
Imports MS.B
Imports MS.A
"

            Dim expected = "$$
Imports System.B
Imports System.A
Imports MS.B
Imports MS.A
"

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, True))
        End Sub

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SeparateGroups_GroupIfSorted()
            Dim code = "$$
Imports System.A
Imports System.B
Imports MS.A
Imports MS.B
"

            Dim expected = "$$
Imports System.A
Imports System.B

Imports MS.A
Imports MS.B
"

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, True))
        End Sub

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Sub SeparateGroups_GroupIfSorted_RecognizeSystemNotFirst()
            Dim code = "$$
Imports MS.A
Imports MS.B
Imports System.A
Imports System.B
"

            Dim expected = "$$
Imports MS.A
Imports MS.B

Imports System.A
Imports System.B
"

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, True))
        End Sub
    End Class
End Namespace
