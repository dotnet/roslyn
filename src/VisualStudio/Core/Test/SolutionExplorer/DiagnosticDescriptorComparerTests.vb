' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class DiagnosticDescriptorComparerTests
        <Fact>
        Public Sub Id()
            Const description As String = "A description"
            Const messageFormat As String = "A message format"
            Const category As String = "Test"

            Dim x = New DiagnosticDescriptor(
                "TST0000",
                description,
                messageFormat,
                category,
                DiagnosticSeverity.Warning,
                True)

            Dim y = New DiagnosticDescriptor(
                "TST0001",
                description,
                messageFormat,
                category,
                DiagnosticSeverity.Warning,
                True)

            Dim comparer = New LegacyDiagnosticItemSource.DiagnosticDescriptorComparer()

            Dim result = comparer.Compare(x, y)

            Assert.True(result < 0)
        End Sub

        <Fact>
        Public Sub Description()
            Const id As String = "TST0001"
            Const messageFormat As String = "A message format"
            Const category As String = "Test"

            Dim x = New DiagnosticDescriptor(
                id,
                "alpha",
                messageFormat,
                category,
                DiagnosticSeverity.Warning,
                True)

            Dim y = New DiagnosticDescriptor(
                id,
                "beta",
                messageFormat,
                category,
                DiagnosticSeverity.Warning,
                True)

            Dim comparer = New LegacyDiagnosticItemSource.DiagnosticDescriptorComparer()

            Dim result = comparer.Compare(x, y)

            Assert.True(result < 0)
        End Sub

        <Fact>
        Public Sub MessageFormat()
            Const id As String = "TST0001"
            Const description As String = "A description"
            Const category As String = "Test"

            Dim x = New DiagnosticDescriptor(
                id,
                description,
                "alpha",
                category,
                DiagnosticSeverity.Warning,
                True)

            Dim y = New DiagnosticDescriptor(
                id,
                description,
                "beta",
                category,
                DiagnosticSeverity.Warning,
                True)

            Dim comparer = New LegacyDiagnosticItemSource.DiagnosticDescriptorComparer()

            Dim result = comparer.Compare(x, y)

            Assert.True(result < 0)
        End Sub
    End Class
End Namespace

