' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Friend Class GenerateDefaultConstructorItem
        Inherits AbstractGenerateCodeItem

        Private ReadOnly _destinationTypeSymbolKey As SymbolKey

        Public Sub New(destinationTypeSymbolKey As SymbolKey)
            MyBase.New(VBEditorResources.New_,
                       Glyph.MethodPublic)

            _destinationTypeSymbolKey = destinationTypeSymbolKey
        End Sub

        Protected Overrides Async Function GetGeneratedDocumentCoreAsync(document As Document, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(_destinationTypeSymbolKey.Resolve(compilation).Symbol, INamedTypeSymbol)

            If destinationType Is Nothing Then
                Return Nothing
            End If

            Dim statements As New ArrayBuilder(Of SyntaxNode)

            If destinationType.IsDesignerGeneratedTypeWithInitializeComponent(compilation) Then
                Dim statement = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("InitializeComponent"), SyntaxFactory.ArgumentList()))
                Dim endOfLineTrivia = SyntaxFactory.EndOfLineTrivia(vbCrLf)

                ' When sticking on the comments, we don't want the ' in the localized string
                ' lest we try localizing the comment character itself
                statement = statement.WithLeadingTrivia(endOfLineTrivia, SyntaxFactory.CommentTrivia("' " & VBEditorResources.This_call_is_required_by_the_designer), endOfLineTrivia)
                statement = statement.WithTrailingTrivia(endOfLineTrivia, endOfLineTrivia, SyntaxFactory.CommentTrivia("' " & VBEditorResources.Add_any_initialization_after_the_InitializeComponent_call), endOfLineTrivia, endOfLineTrivia)
                statements.Add(statement)
            End If

            Dim methodSymbol = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes:=Nothing,
                accessibility:=Accessibility.Public,
                modifiers:=New DeclarationModifiers(),
                typeName:=destinationType.Name,
                parameters:=ImmutableArray(Of IParameterSymbol).Empty,
                statements:=statements.ToImmutableAndFree())
            methodSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(methodSymbol)

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 methodSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
