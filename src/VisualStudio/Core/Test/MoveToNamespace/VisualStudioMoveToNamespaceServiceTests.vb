' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.MoveToNamespace

    Public Class VisualStudioMoveToNamespaceServiceTests
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMoveToNamespace)>
        Public Sub TestMoveNamespace_History()
            Dim service = New VisualStudioMoveToNamespaceOptionsService(Function(viewModel As MoveToNamespaceDialogViewModel) True)

            Dim namespaces = {"namespaceone", "namespacetwo", "namespaces.two", "namespaces.three"}
            Dim result = service.GetChangeNamespaceOptions(namespaces(0), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)

            AssertEx.Equal({namespaces(0)}, service.History)

            result = service.GetChangeNamespaceOptions(namespaces(1), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.Equal({namespaces(1), namespaces(0)}, service.History)

            result = service.GetChangeNamespaceOptions(namespaces(2), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.Equal({namespaces(2), namespaces(1), namespaces(0)}, service.History)

            result = service.GetChangeNamespaceOptions(namespaces(3), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.Equal({namespaces(3), namespaces(2), namespaces(1)}, service.History)

            result = service.GetChangeNamespaceOptions(namespaces(2), namespaces.AsImmutable(), syntaxFactsService:=VisualBasicSyntaxFacts.Instance)
            AssertEx.Equal({namespaces(2), namespaces(3), namespaces(1)}, service.History)
        End Sub
    End Class
End Namespace
