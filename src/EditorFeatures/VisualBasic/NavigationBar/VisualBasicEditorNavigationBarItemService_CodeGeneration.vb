' Licensed to the .NET Foundation under one or more agreements.
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
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.NavigationBar
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Partial Friend Class VisualBasicEditorNavigationBarItemService
        Private Async Function GenerateCodeForItemAsync(document As Document, generateCodeItem As AbstractGenerateCodeItem, textView As ITextView, cancellationToken As CancellationToken) As Task
            ' We'll compute everything up front before we go mutate state
            Dim text = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim newDocument = Await GetGeneratedDocumentAsync(document, generateCodeItem, _globalOptions, cancellationToken).ConfigureAwait(False)
            Dim generatedTree = Await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim generatedNode = generatedTree.GetAnnotatedNodes(GeneratedSymbolAnnotation).Single().FirstAncestorOrSelf(Of MethodBlockBaseSyntax)
            Dim documentOptions = Await document.GetOptionsAsync(cancellationToken).ConfigureAwait(False)
            Dim indentSize = documentOptions.GetOption(FormattingOptions.IndentationSize)

            Dim navigationPoint = NavigationPointHelpers.GetNavigationPoint(generatedTree.GetText(text.Encoding), indentSize, generatedNode)

            ' switch back to ui thread to actually perform the application and navigation
            Await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken)

            Using transaction = New CaretPreservingEditTransaction(VBEditorResources.Generate_Member, textView, _textUndoHistoryRegistry, _editorOperationsFactoryService)
                newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken)

                Dim solution = newDocument.Project.Solution
                Await NavigateToPositionAsync(
                    solution.Workspace, solution.GetRequiredDocument(navigationPoint.Tree).Id,
                    navigationPoint.Position, navigationPoint.VirtualSpaces, cancellationToken).ConfigureAwait(True)

                transaction.Complete()
            End Using
        End Function

        Public Shared Async Function GetGeneratedDocumentAsync(document As Document, generateCodeItem As RoslynNavigationBarItem, globalOptions As IGlobalOptionService, cancellationToken As CancellationToken) As Task(Of Document)
            Dim syntaxTree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim contextLocation = syntaxTree.GetLocation(New TextSpan(0, 0))

            Dim codeGenerationContext = New CodeGenerationContext(contextLocation, generateMethodBodies:=True)

            Dim newDocument = Await GetGeneratedDocumentCoreAsync(document, generateCodeItem, codeGenerationContext, globalOptions.CreateProvider(), cancellationToken).ConfigureAwait(False)
            If newDocument Is Nothing Then
                Return document
            End If

            Dim simplifierOptions = Await newDocument.GetSimplifierOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(False)
            Dim formattingOptions = Await newDocument.GetSyntaxFormattingOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(False)

            newDocument = Await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, simplifierOptions, cancellationToken).ConfigureAwait(False)

            Dim formatterRules = Formatter.GetDefaultFormattingRules(newDocument)
            If ShouldApplyLineAdjustmentFormattingRule(generateCodeItem) Then
                formatterRules = ImmutableArray.Create(Of AbstractFormattingRule)(LineAdjustmentFormattingRule.Instance).AddRange(formatterRules)
            End If

            Return Await Formatter.FormatAsync(
                newDocument,
                Formatter.Annotation,
                options:=formattingOptions,
                cancellationToken:=cancellationToken,
                rules:=formatterRules).ConfigureAwait(False)
        End Function

        Private Shared Function ShouldApplyLineAdjustmentFormattingRule(generateCodeItem As RoslynNavigationBarItem) As Boolean
            Return generateCodeItem.Kind <> RoslynNavigationBarItemKind.GenerateFinalizer
        End Function

        Private Shared Function GetGeneratedDocumentCoreAsync(
                document As Document,
                generateCodeItem As RoslynNavigationBarItem,
                codeGenerationContext As CodeGenerationContext,
                fallbackOptions As CodeAndImportGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of Document)

            Select Case generateCodeItem.Kind
                Case RoslynNavigationBarItemKind.GenerateDefaultConstructor
                    Return GenerateDefaultConstructorAsync(document, DirectCast(generateCodeItem, GenerateDefaultConstructor), codeGenerationContext, fallbackOptions, cancellationToken)

                Case RoslynNavigationBarItemKind.GenerateEventHandler
                    Return GenerateEventHandlerAsync(document, DirectCast(generateCodeItem, GenerateEventHandler), codeGenerationContext, fallbackOptions, cancellationToken)

                Case RoslynNavigationBarItemKind.GenerateFinalizer
                    Return GenerateFinalizerAsync(document, DirectCast(generateCodeItem, GenerateFinalizer), codeGenerationContext, fallbackOptions, cancellationToken)

                Case RoslynNavigationBarItemKind.GenerateMethod
                    Return GenerateMethodAsync(document, DirectCast(generateCodeItem, GenerateMethod), codeGenerationContext, fallbackOptions, cancellationToken)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(generateCodeItem.Kind)
            End Select
        End Function

        Private Shared Async Function GenerateDefaultConstructorAsync(
                document As Document,
                generateCodeItem As GenerateDefaultConstructor,
                codeGenerationContext As CodeGenerationContext,
                fallbackOptions As CodeAndImportGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of Document)

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

            Return Await CodeGenerator.AddMethodDeclarationAsync(
                New CodeGenerationSolutionContext(
                    document.Project.Solution,
                    codeGenerationContext,
                    fallbackOptions),
                destinationType,
                methodSymbol,
                cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Async Function GenerateEventHandlerAsync(
                document As Document,
                generateCodeItem As GenerateEventHandler,
                codeGenerationContext As CodeGenerationContext,
                fallbackOptions As CodeAndImportGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of Document)

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

            Return Await CodeGenerator.AddMethodDeclarationAsync(
                New CodeGenerationSolutionContext(
                    document.Project.Solution,
                    codeGenerationContext,
                    fallbackOptions),
                destinationType,
                methodSymbol,
                cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Async Function GenerateFinalizerAsync(
                document As Document,
                generateCodeItem As GenerateFinalizer,
                codeGenerationContext As CodeGenerationContext,
                fallbackOptions As CodeAndImportGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of Document)

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

            Return Await CodeGenerator.AddMethodDeclarationAsync(
                New CodeGenerationSolutionContext(
                    document.Project.Solution,
                    codeGenerationContext,
                    fallbackOptions),
                destinationType,
                finalizerMethodSymbol,
                cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Async Function GenerateMethodAsync(
                document As Document,
                generateCodeItem As GenerateMethod,
                codeGenerationContext As CodeGenerationContext,
                fallbackOptions As CodeAndImportGenerationOptionsProvider,
                cancellationToken As CancellationToken) As Task(Of Document)

            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(generateCodeItem.DestinationTypeSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).Symbol, INamedTypeSymbol)
            Dim methodToReplicate = TryCast(generateCodeItem.MethodToReplicateSymbolKey.Resolve(compilation, cancellationToken:=cancellationToken).Symbol, IMethodSymbol)

            If destinationType Is Nothing OrElse methodToReplicate Is Nothing Then
                Return Nothing
            End If

            Dim codeGenerationSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(
                CodeGenerationSymbolFactory.CreateMethodSymbol(
                    methodToReplicate.RemoveInaccessibleAttributesAndAttributesOfTypes(destinationType)))

            Return Await CodeGenerator.AddMethodDeclarationAsync(
                 New CodeGenerationSolutionContext(
                    document.Project.Solution,
                    codeGenerationContext,
                    fallbackOptions),
                destinationType,
                codeGenerationSymbol,
                cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
