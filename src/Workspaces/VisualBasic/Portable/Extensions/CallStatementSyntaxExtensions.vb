' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module CallStatementSyntaxExtensions

        <Extension>
        Public Function CanRemoveCallKeyword(callStatement As CallStatementSyntax, semanticModel As SemanticModel) As Boolean
            Dim nextToken = callStatement.CallKeyword.GetNextToken()
            If (nextToken.IsKindOrHasMatchingText(SyntaxKind.IdentifierToken) OrElse
                nextToken.Parent.IsKind(SyntaxKind.PredefinedType)) AndAlso
               Not SyntaxFacts.GetContextualKeywordKind(nextToken.ToString()) = SyntaxKind.MidKeyword Then
                Return True
            End If

            ' Only keywords starting "invocable" expressions
            If nextToken.IsKindOrHasMatchingText(SyntaxKind.CBoolKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CCharKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CDateKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CDblKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CDecKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CIntKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CLngKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CObjKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CSByteKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CShortKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CSngKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CStrKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CTypeKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CUIntKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CULngKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.CUShortKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.DirectCastKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.GetTypeKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.GetXmlNamespaceKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.MeKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.MyBaseKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.MyClassKeyword) OrElse
               nextToken.IsKindOrHasMatchingText(SyntaxKind.TryCastKeyword) Then

                Return True
            End If

            Return False
        End Function
    End Module
End Namespace
