﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Options
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    Public Class VisualBasicFormattingEngineTests
        Inherits VisualBasicFormatterTestBase

        Public Sub New(output As ITestOutputHelper)
            MyBase.New(output)
        End Sub

        Private Shared Function SeparateImportDirectiveGroups() As Dictionary(Of OptionKey, Object)
            Return New Dictionary(Of OptionKey, Object) From {
                {New OptionKey(GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.VisualBasic), True}
            }
        End Function

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SeparateGroups_KeepMultipleLinesBetweenGroups() As Task
            Dim code = "[|
Imports System.A
Imports System.B


Imports MS.A
Imports MS.B
|]"

            Dim expected = "
Imports System.A
Imports System.B


Imports MS.A
Imports MS.B
"

            Await AssertFormatWithBaseIndentAsync(
                expected, code, baseIndentation:=0, options:=SeparateImportDirectiveGroups)
        End Function

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SeparateGroups_DoNotGroupIfNotSorted() As Task
            Dim code = "[|
Imports System.B
Imports System.A
Imports MS.B
Imports MS.A
|]"

            Dim expected = "
Imports System.B
Imports System.A
Imports MS.B
Imports MS.A
"

            Await AssertFormatWithBaseIndentAsync(
                expected, code, baseIndentation:=0, options:=SeparateImportDirectiveGroups)
        End Function

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SeparateGroups_GroupIfSorted() As Task
            Dim code = "[|
Imports System.A
Imports System.B
Imports MS.A
Imports MS.B
|]"

            Dim expected = "
Imports System.A
Imports System.B

Imports MS.A
Imports MS.B
"

            Await AssertFormatWithBaseIndentAsync(
                expected, code, baseIndentation:=0, options:=SeparateImportDirectiveGroups)
        End Function

        <WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
        Public Async Function SeparateGroups_GroupIfSorted_RecognizeSystemNotFirst() As Task
            Dim code = "[|
Imports MS.A
Imports MS.B
Imports System.A
Imports System.B
|]"

            Dim expected = "
Imports MS.A
Imports MS.B

Imports System.A
Imports System.B
"

            Await AssertFormatWithBaseIndentAsync(
                expected, code, baseIndentation:=0, options:=SeparateImportDirectiveGroups)
        End Function
    End Class
End Namespace
