' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LanguageServices

    <Export, [Shared]>
    <ExportLanguageService(GetType(IAnonymousTypeDisplayService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicAnonymousTypeDisplayService
        Inherits AbstractAnonymousTypeDisplayService

        Private Shared ReadOnly s_anonymousDelegateFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=
                SymbolDisplayMemberOptions.IncludeParameters Or
                SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:=
                SymbolDisplayParameterOptions.IncludeName Or
                SymbolDisplayParameterOptions.IncludeType Or
                SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                SymbolDisplayParameterOptions.IncludeDefaultValue,
            miscellaneousOptions:=
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            kindOptions:=SymbolDisplayKindOptions.IncludeNamespaceKeyword Or SymbolDisplayKindOptions.IncludeTypeKeyword Or SymbolDisplayKindOptions.IncludeMemberKeyword)

        Public Overrides Function GetAnonymousTypeParts(anonymousType As INamedTypeSymbol, semanticModel As SemanticModel, position As Integer, displayService As ISymbolDisplayService) As IEnumerable(Of SymbolDisplayPart)
            If anonymousType.IsAnonymousDelegateType() Then
                Return GetDelegateAnonymousType(anonymousType, semanticModel, position, displayService)
            Else
                Return GetNormalAnonymousType(anonymousType, semanticModel, position, displayService)
            End If
        End Function

        Private Function GetDelegateAnonymousType(anonymousType As INamedTypeSymbol,
                                                  semanticModel As SemanticModel,
                                                  position As Integer,
                                                  displayService As ISymbolDisplayService) As IList(Of SymbolDisplayPart)
            Dim method = anonymousType.DelegateInvokeMethod

            Dim members = New List(Of SymbolDisplayPart)()
            members.Add(Punctuation("<"))
            members.AddRange(MassageDelegateParts(
                method,
                displayService.ToMinimalDisplayParts(semanticModel, position, method, s_anonymousDelegateFormat)))
            members.Add(Punctuation(">"))

            Return members
        End Function

        Private Function MassageDelegateParts(delegateInvoke As IMethodSymbol,
                                              parts As IEnumerable(Of SymbolDisplayPart)) As IEnumerable(Of SymbolDisplayPart)
            ' So ugly.  We remove the 'Invoke' name that was added by the symbol display service.
            Dim result = New List(Of SymbolDisplayPart)
            For Each part In parts
                If part.Symbol Is delegateInvoke Then
                    Continue For
                End If

                result.Add(part)
            Next

            If result.Count >= 2 AndAlso result(1).Kind = SymbolDisplayPartKind.Space Then
                result.RemoveAt(1)
            End If

            Return result
        End Function

        Private Function GetNormalAnonymousType(anonymousType As INamedTypeSymbol,
                                                semanticModel As SemanticModel,
                                                position As Integer,
                                                displayService As ISymbolDisplayService) As IList(Of SymbolDisplayPart)
            Dim members = New List(Of SymbolDisplayPart)()

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
                members.AddRange(displayService.ToMinimalDisplayParts(semanticModel, position, [property].Type).Select(Function(p) p.MassageErrorTypeNames("?")))
            Next

            members.AddRange(Space())
            members.Add(Punctuation(SyntaxFacts.GetText(SyntaxKind.CloseBraceToken)))

            Return members
        End Function
    End Class

End Namespace
