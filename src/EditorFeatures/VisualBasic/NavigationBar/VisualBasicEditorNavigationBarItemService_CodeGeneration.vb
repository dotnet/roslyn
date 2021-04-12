﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.NavigationBar
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Partial Friend Class VisualBasicEditorNavigationBarItemService
        Private Sub GenerateCodeForItem(document As Document, generateCodeItem As AbstractGenerateCodeItem, textView As ITextView, cancellationToken As CancellationToken)
            ' We'll compute everything up front before we go mutate state
            Dim text = document.GetTextSynchronously(cancellationToken)
            Dim newDocument = GetGeneratedDocumentAsync(document, generateCodeItem, cancellationToken).WaitAndGetResult(cancellationToken)
            Dim generatedTree = newDocument.GetSyntaxRootSynchronously(cancellationToken)
            Dim generatedNode = generatedTree.GetAnnotatedNodes(GeneratedSymbolAnnotation).Single().FirstAncestorOrSelf(Of MethodBlockBaseSyntax)
            Dim documentOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            Dim indentSize = documentOptions.GetOption(FormattingOptions.IndentationSize)
            Dim navigationPoint = NavigationPointHelpers.GetNavigationPoint(generatedTree.GetText(text.Encoding), indentSize, generatedNode)

            Using transaction = New CaretPreservingEditTransaction(VBEditorResources.Generate_Member, textView, _textUndoHistoryRegistry, _editorOperationsFactoryService)
                newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken)

                NavigateToVirtualTreePoint(newDocument.Project.Solution, navigationPoint, cancellationToken)

                transaction.Complete()
            End Using
        End Sub

        Public Shared Async Function GetGeneratedDocumentAsync(document As Document, generateCodeItem As RoslynNavigationBarItem, cancellationToken As CancellationToken) As Task(Of Document)
            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim contextLocation = syntaxTree.GetLocation(New TextSpan(0, 0))
            Dim codeGenerationOptions As New CodeGenerationOptions(contextLocation, generateMethodBodies:=True)

            Dim newDocument = Await GetGeneratedDocumentCoreAsync(document, generateCodeItem, codeGenerationOptions, cancellationToken).ConfigureAwait(False)
            If newDocument Is Nothing Then
                Return document
            End If

            newDocument = Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).WaitAndGetResult(cancellationToken)

            Dim formatterRules = Formatter.GetDefaultFormattingRules(newDocument)
            If ShouldApplyLineAdjustmentFormattingRule(generateCodeItem) Then
                formatterRules = LineAdjustmentFormattingRule.Instance.Concat(formatterRules)
            End If

            Dim documentOptions = Await newDocument.GetOptionsAsync(cancellationToken).ConfigureAwait(False)
            Return Formatter.FormatAsync(newDocument,
                                         Formatter.Annotation,
                                         options:=documentOptions,
                                         cancellationToken:=cancellationToken,
                                         rules:=formatterRules).WaitAndGetResult(cancellationToken)
        End Function

        Private Shared Function ShouldApplyLineAdjustmentFormattingRule(generateCodeItem As RoslynNavigationBarItem) As Boolean
            Return generateCodeItem.Kind <> RoslynNavigationBarItemKind.GenerateFinalizer
        End Function

        Private Shared Function GetGeneratedDocumentCoreAsync(document As Document, generateCodeItem As RoslynNavigationBarItem, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Select Case generateCodeItem.Kind
                Case RoslynNavigationBarItemKind.GenerateDefaultConstructor
                    Return GenerateDefaultConstructorAsync(document, DirectCast(generateCodeItem, GenerateDefaultConstructor), codeGenerationOptions, cancellationToken)

                Case RoslynNavigationBarItemKind.GenerateEventHandler
                    Return GenerateEventHandlerAsync(document, DirectCast(generateCodeItem, GenerateEventHandler), codeGenerationOptions, cancellationToken)

                Case RoslynNavigationBarItemKind.GenerateFinalizer
                    Return GenerateFinalizerAsync(document, DirectCast(generateCodeItem, GenerateFinalizer), codeGenerationOptions, cancellationToken)

                Case RoslynNavigationBarItemKind.GenerateMethod
                    Return GenerateMethodAsync(document, DirectCast(generateCodeItem, GenerateMethod), codeGenerationOptions, cancellationToken)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(generateCodeItem.Kind)
            End Select
        End Function

        Private Shared Async Function GenerateDefaultConstructorAsync(document As Document, generateCodeItem As GenerateDefaultConstructor, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(generateCodeItem.DestinationTypeSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).Symbol, INamedTypeSymbol)

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

        Private Shared Async Function GenerateEventHandlerAsync(document As Document, generateCodeItem As GenerateEventHandler, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim eventSymbol = TryCast(generateCodeItem.EventSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).GetAnySymbol(), IEventSymbol)
            Dim destinationType = TryCast(generateCodeItem.DestinationTypeSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).GetAnySymbol(), INamedTypeSymbol)

            If eventSymbol Is Nothing OrElse destinationType Is Nothing Then
                Return Nothing
            End If

            Dim delegateInvokeMethod = DirectCast(eventSymbol.Type, INamedTypeSymbol).DelegateInvokeMethod

            If delegateInvokeMethod Is Nothing Then
                Return Nothing
            End If

            Dim containerSyntax As ExpressionSyntax
            Dim methodName As String

            If generateCodeItem.ContainerName IsNot Nothing Then
                containerSyntax = SyntaxFactory.IdentifierName(generateCodeItem.ContainerName)
                methodName = generateCodeItem.ContainerName + "_" + eventSymbol.Name
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
                parameters:=delegateInvokeMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(destinationType).Parameters,
                handlesExpressions:=ImmutableArray.Create(Of SyntaxNode)(handlesSyntax))
            methodSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(methodSymbol)

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 methodSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Async Function GenerateFinalizerAsync(document As Document, generateCodeItem As GenerateFinalizer, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(generateCodeItem.DestinationTypeSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).Symbol, INamedTypeSymbol)

            If destinationType Is Nothing Then
                Return Nothing
            End If

            Dim syntaxFactory = document.GetLanguageService(Of SyntaxGenerator)()
            Dim finalizeCall =
                syntaxFactory.ExpressionStatement(
                    syntaxFactory.InvocationExpression(
                        syntaxFactory.MemberAccessExpression(
                            syntaxFactory.BaseExpression(),
                            syntaxFactory.IdentifierName(WellKnownMemberNames.DestructorName))))

            Dim finalizerMethodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes:=Nothing,
                accessibility:=Accessibility.Protected,
                modifiers:=New DeclarationModifiers(isOverride:=True),
                returnType:=compilation.GetSpecialType(SpecialType.System_Void),
                refKind:=RefKind.None,
                explicitInterfaceImplementations:=Nothing,
                name:=WellKnownMemberNames.DestructorName,
                typeParameters:=Nothing,
                parameters:=ImmutableArray(Of IParameterSymbol).Empty,
                statements:=ImmutableArray.Create(finalizeCall))

            finalizerMethodSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(finalizerMethodSymbol)

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 finalizerMethodSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Async Function GenerateMethodAsync(document As Document, generateCodeItem As GenerateMethod, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(generateCodeItem.DestinationTypeSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).Symbol, INamedTypeSymbol)
            Dim methodToReplicate = TryCast(generateCodeItem.MethodToReplicateSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).Symbol, IMethodSymbol)

            If destinationType Is Nothing OrElse methodToReplicate Is Nothing Then
                Return Nothing
            End If

            Dim codeGenerationSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(
                CodeGenerationSymbolFactory.CreateMethodSymbol(
                    methodToReplicate.RemoveInaccessibleAttributesAndAttributesOfTypes(destinationType)))

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 codeGenerationSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
