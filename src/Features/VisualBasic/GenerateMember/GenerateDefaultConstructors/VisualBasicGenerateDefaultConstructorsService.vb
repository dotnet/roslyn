' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateDefaultConstructors
    <ExportLanguageService(GetType(IGenerateDefaultConstructorsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateDefaultConstructorsService
        Inherits AbstractGenerateDefaultConstructorsService(Of VisualBasicGenerateDefaultConstructorsService)

        Protected Overrides Function TryInitializeState(
                document As SemanticDocument, textSpan As TextSpan, cancellationToken As CancellationToken,
                ByRef baseTypeNode As SyntaxNode, ByRef classType As INamedTypeSymbol) As Boolean
            If cancellationToken.IsCancellationRequested Then
                Return False
            End If

            Dim token = DirectCast(document.Root, SyntaxNode).FindToken(textSpan.Start)

            Dim type = token.GetAncestor(Of TypeSyntax)()
            If type IsNot Nothing AndAlso type.IsParentKind(SyntaxKind.InheritsStatement) Then
                Dim baseList = DirectCast(type.Parent, InheritsStatementSyntax)
                If baseList.Types.Count > 0 AndAlso
                   baseList.Types(0) Is type AndAlso
                   baseList.IsParentKind(SyntaxKind.ClassBlock) Then
                    classType = TryCast(document.SemanticModel.GetDeclaredSymbol(baseList.Parent, cancellationToken), INamedTypeSymbol)
                    baseTypeNode = type
                    Return classType IsNot Nothing
                End If
            End If

            baseTypeNode = Nothing
            classType = Nothing
            Return False
        End Function
    End Class
End Namespace
