' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class DiagnosticDescriptorComparerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim comparer = New DiagnosticItemSource.DiagnosticDescriptorComparer()

            Dim result = comparer.Compare(x, y)

            Assert.True(result < 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim comparer = New DiagnosticItemSource.DiagnosticDescriptorComparer()

            Dim result = comparer.Compare(x, y)

            Assert.True(result < 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim comparer = New DiagnosticItemSource.DiagnosticDescriptorComparer()

            Dim result = comparer.Compare(x, y)

            Assert.True(result < 0)
        End Sub
    End Class
End Namespace

