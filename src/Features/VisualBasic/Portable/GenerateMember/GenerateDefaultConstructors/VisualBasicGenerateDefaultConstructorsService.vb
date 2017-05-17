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

        Protected Overrides Function TryInitializeState(
                document As SemanticDocument, textSpan As TextSpan, cancellationToken As CancellationToken,
                ByRef classOrStructType As INamedTypeSymbol) As Boolean
            cancellationToken.ThrowIfCancellationRequested()

            ' Offer the feature if we're on the header for the class/struct, or if we're on the 
            ' first base-type of a class.

            Dim syntaxFacts = document.Document.GetLanguageService(Of ISyntaxFactsService)()
            If syntaxFacts.IsOnTypeHeader(document.Root, textSpan.Start) Then
                classOrStructType = AbstractGenerateFromMembersCodeRefactoringProvider.GetEnclosingNamedType(
                    document.SemanticModel, document.Root, textSpan.Start, cancellationToken)
                Return classOrStructType IsNot Nothing
            End If

            Dim token = document.Root.FindToken(textSpan.Start)

            Dim type = token.GetAncestor(Of TypeSyntax)()
            If type IsNot Nothing AndAlso type.IsParentKind(SyntaxKind.InheritsStatement) Then
                Dim baseList = DirectCast(type.Parent, InheritsStatementSyntax)

                If baseList.Types.Count > 0 AndAlso
                   baseList.Types(0) Is type AndAlso
                   baseList.IsParentKind(SyntaxKind.ClassBlock) Then

                    classOrStructType = TryCast(document.SemanticModel.GetDeclaredSymbol(baseList.Parent, cancellationToken), INamedTypeSymbol)
                    Return classOrStructType IsNot Nothing
                End If
            End If

            classOrStructType = Nothing
            Return False
        End Function
    End Class
End Namespace