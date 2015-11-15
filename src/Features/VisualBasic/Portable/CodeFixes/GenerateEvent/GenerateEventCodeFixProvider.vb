' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateEvent
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateEvent), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateEnumMember)>
    Partial Friend Class GenerateEventCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30401 As String = "BC30401" ' error BC30401: 'foo' cannot implement 'E' because there is no matching event on interface 'MyInterface'.
        Friend Const BC30590 As String = "BC30590" ' error BC30590: Event 'MyEvent' cannot be found.
        Friend Const BC30456 As String = "BC30456" ' error BC30456: 'x' is not a member of 'y'.
        Friend Const BC30451 As String = "BC30451" ' error BC30451: 'x' is not declared, it may be inaccessible due to its protection level.

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30401, BC30590, BC30456, BC30451)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(context.Span.Start)
            If Not token.Span.IntersectsWith(context.Span) Then
                Return
            End If

            Dim result As CodeAction = Nothing
            For Each node In token.GetAncestors(Of SyntaxNode).Where(Function(c) c.Span.IntersectsWith(context.Span) AndAlso IsCandidate(c))
                Dim qualifiedName = TryCast(node, QualifiedNameSyntax)
                If qualifiedName IsNot Nothing Then
                    result = Await GenerateEventFromImplementsAsync(context.Document, qualifiedName, context.CancellationToken).ConfigureAwait(False)
                End If

                Dim handlesClauseItem = TryCast(node, HandlesClauseItemSyntax)
                If handlesClauseItem IsNot Nothing Then
                    result = Await GenerateEventFromHandlesAsync(context.Document, handlesClauseItem, context.CancellationToken).ConfigureAwait(False)
                End If

                Dim handlerStatement = TryCast(node, AddRemoveHandlerStatementSyntax)
                If handlerStatement IsNot Nothing Then
                    result = Await GenerateEventFromAddRemoveHandlerAsync(context.Document, handlerStatement, context.CancellationToken).ConfigureAwait(False)
                End If

                If result IsNot Nothing Then
                    context.RegisterCodeFix(result, context.Diagnostics)
                    Return
                End If
            Next
        End Function

        Private Async Function GenerateEventFromAddRemoveHandlerAsync(document As Document, handlerStatement As AddRemoveHandlerStatementSyntax, cancellationToken As CancellationToken) As Task(Of CodeAction)
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim handlerExpression = GetHandlerExpression(handlerStatement)

            Dim delegateSymbol As IMethodSymbol = Nothing
            If Not TryGetDelegateSymbol(handlerExpression, semanticModel, delegateSymbol, cancellationToken) Then
                Return Nothing
            End If

            Dim eventExpression = handlerStatement.EventExpression
            Dim eventSymbol = semanticModel.GetSymbolInfo(eventExpression, cancellationToken).GetAnySymbol()
            If eventSymbol IsNot Nothing Then
                Return Nothing
            End If

            Dim containingSymbol = semanticModel.GetEnclosingNamedType(handlerStatement.SpanStart, cancellationToken)
            If containingSymbol Is Nothing Then
                Return Nothing
            End If

            Dim targetType As INamedTypeSymbol = Nothing
            Dim actualEventName As String = Nothing
            If Not TryGetNameAndTargetType(eventExpression, containingSymbol, semanticModel, targetType, actualEventName, cancellationToken) Then
                Return Nothing
            End If

            If Not ResolveTargetType(targetType, semanticModel) Then
                Return Nothing
            End If

            ' Target type may be in other project so we need to find its source definition
            Dim sourceDefinition = Await SymbolFinder.FindSourceDefinitionAsync(targetType, document.Project.Solution, cancellationToken).ConfigureAwait(False)

            targetType = TryCast(sourceDefinition, INamedTypeSymbol)

            If targetType Is Nothing Then
                Return Nothing
            End If

            Return Await GenerateCodeAction(document, semanticModel, delegateSymbol, actualEventName, targetType, cancellationToken).ConfigureAwait(False)
        End Function

        Private Shared Async Function GenerateCodeAction(document As Document, semanticModel As SemanticModel, delegateSymbol As IMethodSymbol, actualEventName As String, targetType As INamedTypeSymbol, cancellationToken As CancellationToken) As Task(Of CodeAction)
            Dim codeGenService = document.Project.Solution.Workspace.Services.GetLanguageServices(targetType.Language).GetService(Of ICodeGenerationService)
            Dim semanticFactService = document.Project.Solution.Workspace.Services.GetLanguageServices(targetType.Language).GetService(Of ISemanticFactsService)
            Dim syntaxFactService = document.Project.Solution.Workspace.Services.GetLanguageServices(targetType.Language).GetService(Of ISyntaxFactsService)

            Dim eventHandlerName As String = actualEventName + "Handler"
            Dim existingSymbols = Await SymbolFinder.FindSourceDeclarationsAsync(
                document.Project.Solution, eventHandlerName, Not syntaxFactService.IsCaseSensitive, SymbolFilter.Type, cancellationToken).ConfigureAwait(False)

            If existingSymbols.Any(Function(existingSymbol) existingSymbol IsNot Nothing _
                                                   AndAlso existingSymbol.ContainingNamespace Is targetType.ContainingNamespace) Then
                ' There already exists a delegate that matches the event handler name
                Return Nothing
            End If

            If semanticFactService.SupportsParameterizedEvents Then

                ' We also need to generate the delegate type
                Dim delegateType = CodeGenerationSymbolFactory.CreateDelegateTypeSymbol(Nothing, Accessibility.Public, Nothing,
                                                                                        semanticModel.Compilation.GetSpecialType(SpecialType.System_Void),
                                                                                        name:=eventHandlerName,
                                                                                        parameters:=delegateSymbol.GetParameters())

                Dim generatedEvent = CodeGenerationSymbolFactory.CreateEventSymbol(attributes:=SpecializedCollections.EmptyList(Of AttributeData)(),
                                                                                    accessibility:=Accessibility.Public, modifiers:=Nothing,
                                                                                    explicitInterfaceSymbol:=Nothing,
                                                                                    type:=delegateType, name:=actualEventName,
                                                                                    parameterList:=TryCast(delegateSymbol, IMethodSymbol).Parameters)

                Return New GenerateEventCodeAction(document.Project.Solution, targetType, generatedEvent, delegateType, codeGenService, CodeGenerationOptions.Default)
            Else
                Dim generatedEvent = CodeGenerationSymbolFactory.CreateEventSymbol(attributes:=SpecializedCollections.EmptyList(Of AttributeData)(),
                                                accessibility:=Accessibility.Public, modifiers:=Nothing,
                                                explicitInterfaceSymbol:=Nothing,
                                                type:=Nothing, name:=actualEventName,
                                                parameterList:=delegateSymbol.GetParameters())

                Return New GenerateEventCodeAction(document.Project.Solution, TryCast(targetType, INamedTypeSymbol), generatedEvent, Nothing, codeGenService, CodeGenerationOptions.Default)
            End If
        End Function

        Private Function GetHandlerExpression(handlerStatement As AddRemoveHandlerStatementSyntax) As ExpressionSyntax
            Dim unaryExpression = TryCast(handlerStatement.DelegateExpression.DescendantNodesAndSelf().Where(Function(n) n.IsKind(SyntaxKind.AddressOfExpression)).FirstOrDefault, UnaryExpressionSyntax)
            If unaryExpression Is Nothing Then
                Return handlerStatement.DelegateExpression
            Else
                Return unaryExpression.Operand
            End If
        End Function

        Private Function TryGetDelegateSymbol(handlerExpression As ExpressionSyntax, semanticModel As SemanticModel, ByRef delegateSymbol As IMethodSymbol, cancellationToken As CancellationToken) As Boolean
            delegateSymbol = TryCast(semanticModel.GetSymbolInfo(handlerExpression, cancellationToken).GetAnySymbol(), IMethodSymbol)
            If delegateSymbol Is Nothing Then
                Dim typeSymbol = TryCast(semanticModel.GetTypeInfo(handlerExpression, cancellationToken).Type, INamedTypeSymbol)
                If typeSymbol IsNot Nothing AndAlso typeSymbol.DelegateInvokeMethod IsNot Nothing Then
                    delegateSymbol = typeSymbol.DelegateInvokeMethod
                Else
                    Return False
                End If
            End If

            If delegateSymbol.Arity <> 0 AndAlso delegateSymbol.TypeArguments.Any(Function(n) n.TypeKind = TypeKind.TypeParameter) Then
                Return False
            End If

            Return True
        End Function

        Private Function ResolveTargetType(ByRef targetType As INamedTypeSymbol, semanticModel As SemanticModel) As Boolean
            If targetType Is Nothing OrElse
                Not (targetType.TypeKind = TypeKind.Class OrElse targetType.TypeKind = TypeKind.Interface) OrElse
                targetType.IsAnonymousType Then
                Return False
            End If

            targetType = DirectCast(targetType.GetSymbolKey().Resolve(semanticModel.Compilation).Symbol, INamedTypeSymbol)

            If targetType Is Nothing Then
                Return False
            End If

            Return True
        End Function

        Private Function TryGetNameAndTargetType(eventExpression As ExpressionSyntax, containingSymbol As INamedTypeSymbol, semanticModel As SemanticModel, ByRef targetType As INamedTypeSymbol, ByRef actualEventName As String, cancellationToken As CancellationToken) As Boolean

            Dim eventType As INamedTypeSymbol = Nothing
            If TypeOf eventExpression Is IdentifierNameSyntax Then
                actualEventName = CType(eventExpression, IdentifierNameSyntax).Identifier.ValueText
            ElseIf TypeOf eventExpression Is MemberAccessExpressionSyntax Then
                Dim memberAccess = CType(eventExpression, MemberAccessExpressionSyntax)
                Dim qualifier As ExpressionSyntax = Nothing
                Dim arity As Integer
                memberAccess.DecomposeName(qualifier, actualEventName, arity)
                eventType = TryCast(semanticModel.GetTypeInfo(qualifier, cancellationToken).Type, INamedTypeSymbol)
            Else
                Return False
            End If

            If eventExpression.DescendantTokens().Where(Function(n) n.IsKind(SyntaxKind.MeKeyword, SyntaxKind.MyClassKeyword)).Any Then
                targetType = containingSymbol
                Return True
            ElseIf eventExpression.DescendantTokens().Where(Function(n) n.IsKind(SyntaxKind.MyBaseKeyword)).Any Then
                targetType = containingSymbol.BaseType
                Return True
            ElseIf TypeOf eventExpression Is IdentifierNameSyntax Then
                targetType = containingSymbol
                Return True
            ElseIf TypeOf eventExpression Is MemberAccessExpressionSyntax Then
                If eventType IsNot Nothing Then
                    targetType = eventType
                    Return True
                End If
            End If

            Return False
        End Function

        Private Shared Function IsCandidate(node As SyntaxNode) As Boolean
            Return TypeOf node Is HandlesClauseItemSyntax OrElse TypeOf node Is QualifiedNameSyntax OrElse TypeOf node Is AddRemoveHandlerStatementSyntax
        End Function

        Private Async Function GenerateEventFromImplementsAsync(document As Document, node As QualifiedNameSyntax, cancellationToken As CancellationToken) As Task(Of CodeAction)
            If node.Right.IsMissing Then
                Return Nothing
            End If

            ' We must be trying to implement an event
            If node.IsParentKind(SyntaxKind.ImplementsClause) AndAlso node.Parent.IsParentKind(SyntaxKind.EventStatement) Then
                ' Does this name already bind?
                Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                Dim nameToGenerate = semanticModel.GetSymbolInfo(node).Symbol

                If nameToGenerate IsNot Nothing Then
                    Return Nothing
                End If

                Dim targetType = TryCast(Await SymbolFinder.FindSourceDefinitionAsync(semanticModel.GetSymbolInfo(node.Left).Symbol, document.Project.Solution, cancellationToken).ConfigureAwait(False), INamedTypeSymbol)
                If targetType Is Nothing OrElse (targetType.TypeKind <> TypeKind.Interface AndAlso targetType.TypeKind <> TypeKind.Class) Then
                    Return Nothing
                End If

                Dim boundEvent = TryCast(semanticModel.GetDeclaredSymbol(node.Parent.Parent, cancellationToken), IEventSymbol)
                If boundEvent Is Nothing Then
                    Return Nothing
                End If

                Dim codeGenService = document.Project.Solution.Workspace.Services.GetLanguageServices(targetType.Language).GetService(Of ICodeGenerationService)

                Dim actualEventName = node.Right.Identifier.ValueText

                Dim semanticFactService = document.Project.Solution.Workspace.Services.GetLanguageServices(targetType.Language).GetService(Of ISemanticFactsService)

                If semanticFactService.SupportsParameterizedEvents Then
                    ' If we support parameterized events (C#) and it's an event declaration with a parameter list
                    ' (not a type), we need to generate a delegate type in the C# file.
                    Dim eventSyntax = node.GetAncestor(Of EventStatementSyntax)()

                    If eventSyntax.ParameterList IsNot Nothing Then
                        Dim eventType = TryCast(boundEvent.Type, INamedTypeSymbol)
                        If eventType Is Nothing Then
                            Return Nothing
                        End If

                        Dim returnType = If(eventType.DelegateInvokeMethod IsNot Nothing,
                                            eventType.DelegateInvokeMethod.ReturnType,
                                            semanticModel.Compilation.GetSpecialType(SpecialType.System_Void))

                        Dim parameters As IList(Of IParameterSymbol) = If(eventType.DelegateInvokeMethod IsNot Nothing,
                                            eventType.DelegateInvokeMethod.Parameters,
                                            SpecializedCollections.EmptyList(Of IParameterSymbol)())

                        Dim eventHandlerType = CodeGenerationSymbolFactory.CreateDelegateTypeSymbol(eventType.GetAttributes(),
                                                        eventType.DeclaredAccessibility,
                                                        Nothing,
                                                        returnType,
                                                        actualEventName + "EventHandler",
                                                        eventType.TypeParameters,
                                                        parameters)

                        Dim generatedEvent = CodeGenerationSymbolFactory.CreateEventSymbol(boundEvent.GetAttributes(), boundEvent.DeclaredAccessibility,
                                                                       modifiers:=Nothing, type:=eventHandlerType, explicitInterfaceSymbol:=Nothing,
                                                                       name:=actualEventName,
                                                                       parameterList:=boundEvent.GetParameters())
                        Return New GenerateEventCodeAction(document.Project.Solution, targetType, generatedEvent, eventHandlerType, codeGenService, New CodeGenerationOptions())
                    End If
                End If

                ' C# with no delegate type or VB
                Dim generatedMember = CodeGenerationSymbolFactory.CreateEventSymbol(boundEvent, name:=actualEventName)
                Return New GenerateEventCodeAction(document.Project.Solution, targetType, generatedMember, Nothing, codeGenService, New CodeGenerationOptions())
            End If

            Return Nothing
        End Function

        Private Async Function GenerateEventFromHandlesAsync(document As Document, handlesClauseItem As HandlesClauseItemSyntax, cancellationToken As CancellationToken) As Task(Of CodeAction)
            If handlesClauseItem.IsMissing OrElse handlesClauseItem.EventContainer.IsMissing OrElse handlesClauseItem.EventMember.IsMissing Then
                Return Nothing
            End If

            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            ' Does this handlesClauseItem actually bind?
            Dim symbol = semanticModel.GetSymbolInfo(handlesClauseItem, cancellationToken).Symbol
            If symbol IsNot Nothing Then
                Return Nothing
            End If

            Dim targetType As INamedTypeSymbol = Nothing


            Dim keywordEventContainer = TryCast(handlesClauseItem.EventContainer, KeywordEventContainerSyntax)
            If keywordEventContainer IsNot Nothing Then
                ' Me/MyClass/MyBase
                Dim containingSymbol = semanticModel.GetEnclosingNamedType(handlesClauseItem.SpanStart, cancellationToken)

                If containingSymbol Is Nothing Then
                    Return Nothing
                End If

                If keywordEventContainer.Keyword.IsKind(SyntaxKind.MeKeyword, SyntaxKind.MyClassKeyword) Then
                    targetType = containingSymbol
                ElseIf keywordEventContainer.Keyword.IsKind(SyntaxKind.MyBaseKeyword) Then
                    targetType = containingSymbol.BaseType
                End If
            Else
                ' Withevents property. We'll generate into its type.
                Dim withEventsProperty = TryCast(semanticModel.GetSymbolInfo(handlesClauseItem.EventContainer, cancellationToken).Symbol, IPropertySymbol)
                If withEventsProperty Is Nothing OrElse Not withEventsProperty.IsWithEvents Then
                    Return Nothing
                End If

                targetType = TryCast(Await SymbolFinder.FindSourceDefinitionAsync(withEventsProperty.Type, document.Project.Solution, cancellationToken).ConfigureAwait(False), INamedTypeSymbol)

            End If

            targetType = TryCast(Await SymbolFinder.FindSourceDefinitionAsync(targetType, document.Project.Solution, cancellationToken).ConfigureAwait(False), INamedTypeSymbol)
            If targetType Is Nothing OrElse
                Not (targetType.TypeKind = TypeKind.Class OrElse targetType.TypeKind = TypeKind.Interface) OrElse
                targetType.IsAnonymousType Then
                Return Nothing
            End If

            ' Our target type may be from a CSharp file, in which case we should resolve it to our VB compilation.
            Dim originalTargetType = targetType
            targetType = DirectCast(targetType.GetSymbolKey().Resolve(semanticModel.Compilation).Symbol, INamedTypeSymbol)

            If targetType Is Nothing Then
                Return Nothing
            End If

            If semanticModel.LookupSymbols(handlesClauseItem.SpanStart, container:=targetType, name:=handlesClauseItem.EventMember.Identifier.ValueText).
                Any(Function(x) x.MatchesKind(SymbolKind.Event) AndAlso x.Name = handlesClauseItem.EventMember.Identifier.ValueText) Then

                Return Nothing
            End If

            If targetType.GetMembers(handlesClauseItem.EventMember.Identifier.ValueText).Any() Then
                Return Nothing
            End If

            Dim codeGenService = document.Project.Solution.Workspace.Services.GetLanguageServices(originalTargetType.Language).GetService(Of ICodeGenerationService)

            ' Let's bind the method declaration so we can get its parameters.
            Dim boundMethod = semanticModel.GetDeclaredSymbol(handlesClauseItem.GetAncestor(Of MethodStatementSyntax)(), cancellationToken)
            If boundMethod Is Nothing Then
                Return Nothing
            End If

            Dim actualEventName = handlesClauseItem.EventMember.Identifier.ValueText

            Dim semanticFactService = document.Project.Solution.Workspace.Services.GetLanguageServices(originalTargetType.Language).GetService(Of ISemanticFactsService)

            If semanticFactService.SupportsParameterizedEvents Then
                ' We need to generate the delegate, too.
                Dim delegateType = CodeGenerationSymbolFactory.CreateDelegateTypeSymbol(Nothing, Accessibility.Public, Nothing, semanticModel.Compilation.GetSpecialType(SpecialType.System_Void),
                                                                    name:=actualEventName + "Handler",
                                                                    parameters:=boundMethod.GetParameters())

                Dim generatedEvent = CodeGenerationSymbolFactory.CreateEventSymbol(attributes:=SpecializedCollections.EmptyList(Of AttributeData)(),
                                                            accessibility:=Accessibility.Public, modifiers:=Nothing,
                                                            explicitInterfaceSymbol:=Nothing,
                                                            type:=delegateType, name:=actualEventName,
                                                            parameterList:=TryCast(boundMethod, IMethodSymbol).Parameters)

                Return New GenerateEventCodeAction(document.Project.Solution, originalTargetType, generatedEvent, delegateType, codeGenService, New CodeGenerationOptions())
            Else
                Dim generatedEvent = CodeGenerationSymbolFactory.CreateEventSymbol(attributes:=SpecializedCollections.EmptyList(Of AttributeData)(),
                                                accessibility:=Accessibility.Public, modifiers:=Nothing,
                                                explicitInterfaceSymbol:=Nothing,
                                                type:=Nothing, name:=actualEventName,
                                                parameterList:=boundMethod.GetParameters())

                Return New GenerateEventCodeAction(document.Project.Solution, TryCast(originalTargetType, INamedTypeSymbol), generatedEvent, Nothing, codeGenService, CodeGenerationOptions.Default)
            End If
        End Function
    End Class
End Namespace
