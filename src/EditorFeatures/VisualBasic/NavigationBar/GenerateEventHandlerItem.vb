' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Friend Class GenerateEventHandlerItem
        Inherits AbstractGenerateCodeItem

        Private ReadOnly _containerName As String
        Private ReadOnly _eventSymbolKey As SymbolKey
        Private ReadOnly _destinationTypeSymbolKey As SymbolKey

        Public Sub New(eventName As String, glyph As Glyph, containerName As String, eventSymbolKey As SymbolKey, destinationTypeSymbolKey As SymbolKey)
            MyBase.New(eventName, glyph)

            _containerName = containerName
            _eventSymbolKey = eventSymbolKey
            _destinationTypeSymbolKey = destinationTypeSymbolKey
        End Sub

        Protected Overrides Async Function GetGeneratedDocumentCoreAsync(document As Document, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim eventSymbol = TryCast(_eventSymbolKey.Resolve(compilation).GetAnySymbol(), IEventSymbol)
            Dim destinationType = TryCast(_destinationTypeSymbolKey.Resolve(compilation).GetAnySymbol(), INamedTypeSymbol)

            If eventSymbol Is Nothing OrElse destinationType Is Nothing Then
                Return Nothing
            End If

            Dim delegateInvokeMethod = DirectCast(eventSymbol.Type, INamedTypeSymbol).DelegateInvokeMethod

            If delegateInvokeMethod Is Nothing Then
                Return Nothing
            End If

            Dim containerSyntax As ExpressionSyntax
            Dim methodName As String

            If _containerName IsNot Nothing Then
                containerSyntax = SyntaxFactory.IdentifierName(_containerName)
                methodName = _containerName + "_" + eventSymbol.Name
            Else
                containerSyntax = SyntaxFactory.KeywordEventContainer(SyntaxFactory.Token(SyntaxKind.MeKeyword))
                methodName = destinationType.Name + "_" + eventSymbol.Name
            End If

            Dim handlesSyntax = SyntaxFactory.SimpleMemberAccessExpression(containerSyntax, SyntaxFactory.Token(SyntaxKind.DotToken), eventSymbol.Name.ToIdentifierName())

            Dim methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes:=Nothing,
                accessibility:=Accessibility.Private,
                modifiers:=New DeclarationModifiers(),
                returnType:=delegateInvokeMethod.ReturnType,
                refKind:=delegateInvokeMethod.RefKind,
                explicitInterfaceImplementations:=Nothing,
                name:=methodName,
                typeParameters:=Nothing,
                parameters:=delegateInvokeMethod.Parameters.WithAttributesToBeCopied(destinationType),
                handlesExpressions:=ImmutableArray.Create(Of SyntaxNode)(handlesSyntax))
            methodSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(methodSymbol)

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 methodSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
