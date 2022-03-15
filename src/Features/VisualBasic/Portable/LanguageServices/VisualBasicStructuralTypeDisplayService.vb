' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices
    <Export, [Shared]>
    <ExportLanguageService(GetType(IStructuralTypeDisplayService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicStructuralTypeDisplayService
        Inherits AbstractStructuralTypeDisplayService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFactsService As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetNormalAnonymousTypeParts(
                anonymousType As INamedTypeSymbol,
                semanticModel As SemanticModel,
                position As Integer) As ImmutableArray(Of SymbolDisplayPart)
            Dim members = ArrayBuilder(Of SymbolDisplayPart).GetInstance()

            members.Add(Keyword(SyntaxFacts.GetText(SyntaxKind.NewKeyword)))
            members.AddRange(Space())
            members.Add(Keyword(SyntaxFacts.GetText(SyntaxKind.WithKeyword)))
            members.AddRange(Space())
            members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.OpenBraceToken)))

            Dim first = True
            For Each [property] In anonymousType.GetValidAnonymousTypeProperties()
                If Not first Then
                    members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CommaToken)))
                End If

                first = False

                If [property].IsReadOnly Then
                    members.AddRange(Space())
                    members.Add(Keyword(SyntaxFacts.GetText(SyntaxKind.KeyKeyword)))
                End If

                members.AddRange(Space())
                members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.DotToken)))
                members.Add(New SymbolDisplayPart(SymbolDisplayPartKind.PropertyName, [property], [property].Name))
                members.AddRange(Space())
                members.Add(Keyword(SyntaxFacts.GetText(SyntaxKind.AsKeyword)))
                members.AddRange(Space())
                members.AddRange([property].Type.ToMinimalDisplayParts(semanticModel, position, s_minimalWithoutExpandedTuples).Select(Function(p) p.MassageErrorTypeNames("?")))
            Next

            members.AddRange(Space())
            members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CloseBraceToken)))

            Return members.ToImmutableAndFree()
        End Function
    End Class
End Namespace
