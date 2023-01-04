' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.GenerateDefaultConstructors
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateDefaultConstructors
    <ExportLanguageService(GetType(IGenerateDefaultConstructorsService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicGenerateDefaultConstructorsService
        Inherits AbstractGenerateDefaultConstructorsService(Of VisualBasicGenerateDefaultConstructorsService)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function TryInitializeState(
                semanticDocument As SemanticDocument, textSpan As TextSpan, cancellationToken As CancellationToken,
                ByRef classType As INamedTypeSymbol) As Boolean
            cancellationToken.ThrowIfCancellationRequested()

            ' Offer the feature if we're on the header for the class/struct, or if we're on the 
            ' first base-type of a class.

            Dim headerFacts = semanticDocument.Document.GetLanguageService(Of IHeaderFactsService)()
            Dim typeDecl As SyntaxNode = Nothing
            If headerFacts.IsOnTypeHeader(semanticDocument.Root, textSpan.Start, typeDecl) Then
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
