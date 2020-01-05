' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateFromMembers
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateMember.GenerateDefaultConstructors
    <ExportLanguageService(GetType(IGenerateDefaultConstructorsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateDefaultConstructorsService
        Inherits AbstractGenerateDefaultConstructorsService(Of VisualBasicGenerateDefaultConstructorsService)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function TryInitializeState(
                semanticDocument As SemanticDocument, textSpan As TextSpan, cancellationToken As CancellationToken,
                ByRef classType As INamedTypeSymbol) As Boolean
            cancellationToken.ThrowIfCancellationRequested()

            ' Offer the feature if we're on the header for the class/struct, or if we're on the 
            ' first base-type of a class.

            Dim syntaxFacts = semanticDocument.Document.GetLanguageService(Of ISyntaxFactsService)()
            Dim typeDecl As SyntaxNode = Nothing
            If syntaxFacts.IsOnTypeHeader(semanticDocument.Root, textSpan.Start, typeDecl) Then
                classType = TryCast(semanticDocument.SemanticModel.GetDeclaredSymbol(typeDecl), INamedTypeSymbol)
                Return classType IsNot Nothing AndAlso classType.TypeKind = TypeKind.Class
            End If

            Dim token = semanticDocument.Root.FindToken(textSpan.Start)

            Dim type = token.GetAncestor(Of TypeSyntax)()
            If type IsNot Nothing AndAlso type.IsParentKind(SyntaxKind.InheritsStatement) Then
                Dim baseList = DirectCast(type.Parent, InheritsStatementSyntax)

                If baseList.Types.Count > 0 AndAlso
                   baseList.Types(0) Is type AndAlso
                   baseList.IsParentKind(SyntaxKind.ClassBlock) Then

                    classType = TryCast(semanticDocument.SemanticModel.GetDeclaredSymbol(baseList.Parent, cancellationToken), INamedTypeSymbol)
                    Return classType IsNot Nothing
                End If
            End If

            classType = Nothing
            Return False
        End Function
    End Class
End Namespace
