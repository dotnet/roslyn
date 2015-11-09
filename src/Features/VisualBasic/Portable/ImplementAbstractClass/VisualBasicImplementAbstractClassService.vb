' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass
    <ExportLanguageService(GetType(IImplementAbstractClassService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicImplementAbstractClassService
        Inherits AbstractImplementAbstractClassService

        Protected Overrides Function TryInitializeState(
                document As Document, model As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken,
                ByRef classType As INamedTypeSymbol, ByRef abstractClassType As INamedTypeSymbol) As Boolean
            If cancellationToken.IsCancellationRequested Then
                Return False
            End If

            Dim baseClassNode = TryCast(node, TypeSyntax)
            If baseClassNode.IsParentKind(SyntaxKind.InheritsStatement) Then
                If baseClassNode.Parent.IsParentKind(SyntaxKind.ClassBlock) Then
                    abstractClassType = TryCast(model.GetTypeInfo(baseClassNode, cancellationToken).Type, INamedTypeSymbol)
                    cancellationToken.ThrowIfCancellationRequested()

                    If abstractClassType.IsAbstractClass() Then
                        Dim classDecl = TryCast(baseClassNode.Parent.Parent, ClassBlockSyntax)
                        classType = model.GetDeclaredSymbol(classDecl.BlockStatement, cancellationToken)
                        cancellationToken.ThrowIfCancellationRequested()

                        Return abstractClassType IsNot Nothing AndAlso classType IsNot Nothing
                    End If
                End If
            End If

            classType = Nothing
            abstractClassType = Nothing
            Return False
        End Function
    End Class
End Namespace
