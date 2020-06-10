' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.MoveToNamespace

    Public Class VisualStudioMoveToNamespaceServiceTests
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)>
        Public Sub TestMoveNamespace_History()
            Dim history(2) As String
            Dim service = New VisualStudioMoveToNamespaceOptionsService(history, Function(viewModel As MoveToNamespaceDialogViewModel) True)

            Dim namespaces = {"namespaceone", "namespacetwo", "namespaces.two", "namespaces.three"}
            Dim result = service.GetChangeNamespaceOptions(namespaces(0), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)

            AssertEx.SetEqual({namespaces(0), Nothing, Nothing}, history)

            result = service.GetChangeNamespaceOptions(namespaces(1), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.SetEqual({namespaces(1), namespaces(0), Nothing}, history)

            result = service.GetChangeNamespaceOptions(namespaces(2), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.SetEqual({namespaces(2), namespaces(1), namespaces(0)}, history)

            result = service.GetChangeNamespaceOptions(namespaces(3), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.SetEqual({namespaces(3), namespaces(2), namespaces(1)}, history)

            result = service.GetChangeNamespaceOptions(namespaces(2), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.SetEqual({namespaces(2), namespaces(3), namespaces(1)}, history)
        End Sub
    End Class
End Namespace
