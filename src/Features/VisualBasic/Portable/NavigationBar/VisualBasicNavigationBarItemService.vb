' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.NavigationBar
    <ExportLanguageService(GetType(INavigationBarItemService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicNavigationBarItemService
        Inherits AbstractNavigationBarItemService

        Private ReadOnly _typeFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance)

        Private ReadOnly _memberFormat As SymbolDisplayFormat = New SymbolDisplayFormat(
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Async Function GetItemsInCurrentProcessAsync(
                document As Document,
                workspaceSupportsDocumentChanges As Boolean,
                cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of RoslynNavigationBarItem))
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Contract.ThrowIfNull(semanticModel)

            Dim typesAndDeclarations = GetTypesAndDeclarationsInFile(semanticModel, cancellationToken)

            Dim typeItems = ImmutableArray.CreateBuilder(Of RoslynNavigationBarItem)
            Dim symbolDeclarationService = document.GetLanguageService(Of ISymbolDeclarationService)

            For Each typeAndDeclaration In typesAndDeclarations
                Dim type = typeAndDeclaration.Item1
                Dim position = typeAndDeclaration.Item2.SpanStart
                typeItems.AddRange(CreateItemsForType(
                    document.Project.Solution, type, position, semanticModel, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
            Next

            Return typeItems.ToImmutable()
        End Function

        Private Shared Function GetTypesAndDeclarationsInFile(semanticModel As SemanticModel, cancellationToken As CancellationToken) As IEnumerable(Of Tuple(Of INamedTypeSymbol, SyntaxNode))
            Try
                Dim typesAndDeclarations As New Dictionary(Of INamedTypeSymbol, SyntaxNode)
                Dim nodesToVisit As New Stack(Of SyntaxNode)

                nodesToVisit.Push(DirectCast(semanticModel.SyntaxTree.GetRoot(cancellationToken), SyntaxNode))

                Do Until nodesToVisit.IsEmpty
                    If cancellationToken.IsCancellationRequested Then
                        Return SpecializedCollections.EmptyEnumerable(Of Tuple(Of INamedTypeSymbol, SyntaxNode))()
                    End If

                    Dim node = nodesToVisit.Pop()
                    Dim type = TryCast(semanticModel.GetDeclaredSymbol(node, cancellationToken), INamedTypeSymbol)

                    If type IsNot Nothing Then
                        typesAndDeclarations(type) = node
                    End If

                    If TypeOf node Is MethodBlockBaseSyntax OrElse
                        TypeOf node Is PropertyBlockSyntax OrElse
                        TypeOf node Is EventBlockSyntax OrElse
                        TypeOf node Is FieldDeclarationSyntax OrElse
                        TypeOf node Is ExecutableStatementSyntax OrElse
                        TypeOf node Is ExpressionSyntax Then
                        ' quick bail out to prevent us from creating every nodes exist in current file
                        Continue Do
                    End If

                    For Each child In node.ChildNodes()
                        nodesToVisit.Push(child)
                    Next
                Loop

                Return typesAndDeclarations.Select(Function(kvp) Tuple.Create(kvp.Key, kvp.Value)).OrderBy(Function(t) t.Item1.Name)
            Catch ex As Exception When FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken)
                Throw ExceptionUtilities.Unreachable
            End Try
        End Function

        Private Function CreateItemsForType(
                solution As Solution,
                type As INamedTypeSymbol,
                position As Integer,
                semanticModel As SemanticModel,
        workspaceSupportsDocumentChanges As Boolean,
                symbolDeclarationService As ISymbolDeclarationService,
                cancellationToken As CancellationToken) As ImmutableArray(Of RoslynNavigationBarItem)

            Dim items = ArrayBuilder(Of RoslynNavigationBarItem).GetInstance()
            If type.TypeKind = TypeKind.Enum Then
                items.AddIfNotNull(CreateItemForEnum(solution, type, semanticModel.SyntaxTree, symbolDeclarationService))
            Else
                items.AddIfNotNull(CreatePrimaryItemForType(solution, type, semanticModel.SyntaxTree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))

                If type.TypeKind <> TypeKind.Interface Then
                    Dim typeEvents = CreateItemForEvents(
                        solution,
                        type,
                        position,
                        type,
                        eventContainer:=Nothing,
                        semanticModel:=semanticModel,
                        workspaceSupportsDocumentChanges:=workspaceSupportsDocumentChanges,
                        symbolDeclarationService:=symbolDeclarationService,
                        cancellationToken)

                    ' Add the (<ClassName> Events) item only if it actually has things within it
                    If typeEvents.ChildItems.Count > 0 Then
                        items.Add(typeEvents)
                    End If

                    For Each member In type.GetMembers().OrderBy(Function(m) m.Name)
                        ' If this is a WithEvents property, then we should also add items for it
                        Dim propertySymbol = TryCast(member, IPropertySymbol)
                        If propertySymbol IsNot Nothing AndAlso propertySymbol.IsWithEvents Then
                            items.AddIfNotNull(CreateItemForEvents(
                                solution,
                                type,
                                position,
                                propertySymbol.Type,
                                propertySymbol,
                                semanticModel,
                                workspaceSupportsDocumentChanges,
                                symbolDeclarationService,
                                cancellationToken))
                        End If
                    Next
                End If
            End If

            Return items.ToImmutableAndFree()
        End Function

        Private Shared Function CreateItemForEnum(
                solution As Solution,
                type As INamedTypeSymbol,
                tree As SyntaxTree,
                symbolDeclarationService As ISymbolDeclarationService) As RoslynNavigationBarItem

            Dim members = From member In type.GetMembers()
                          Where member.IsShared AndAlso member.Kind = Global.Microsoft.CodeAnalysis.SymbolKind.Field
                          Order By member.Name
                          Select CreateSymbolItem(solution, member, tree, symbolDeclarationService)

            Dim location = GetSymbolLocation(solution, type, tree, symbolDeclarationService)
            If location Is Nothing Then
                Return Nothing
            End If

            Return New SymbolItem(
                type.Name,
                type.Name,
                type.GetGlyph(),
                type.IsObsolete,
                location.Value,
                ImmutableArray(Of RoslynNavigationBarItem).CastUp(members.WhereNotNull().ToImmutableArray()),
                bolded:=True)
        End Function

        Private Shared Function CreateSymbolItem(
                solution As Solution,
                member As ISymbol,
                tree As SyntaxTree,
                symbolDeclarationService As ISymbolDeclarationService) As SymbolItem

            Dim location = GetSymbolLocation(solution, member, tree, symbolDeclarationService)
            If location Is Nothing Then
                Return Nothing
            End If

            Return New SymbolItem(
                member.Name,
                member.Name,
                member.GetGlyph(),
                member.IsObsolete,
                location.Value)
        End Function

        Private Function CreatePrimaryItemForType(
                solution As Solution,
                type As INamedTypeSymbol,
                tree As SyntaxTree,
                workspaceSupportsDocumentChanges As Boolean,
                symbolDeclarationService As ISymbolDeclarationService,
                cancellationToken As CancellationToken) As RoslynNavigationBarItem

            Dim childItems As New List(Of RoslynNavigationBarItem)

            ' First, we always list the constructors
            Dim constructors = type.Constructors
            If constructors.All(Function(c) c.IsImplicitlyDeclared) Then

                ' Offer to generate the constructor only if it's legal
                If workspaceSupportsDocumentChanges AndAlso type.TypeKind = TypeKind.Class Then
                    childItems.Add(New GenerateDefaultConstructor("New", type.GetSymbolKey(cancellationToken)))
                End If
            Else
                childItems.AddRange(CreateItemsForMemberGroup(solution, constructors, tree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
            End If

            ' Get any of the methods named "Finalize" in this class, and list them first. The legacy
            ' behavior that we will consider a method a finalizer even if it is shadowing the real
            ' Finalize method instead of overriding it, so this code is actually correct!
            Dim finalizeMethods = type.GetMembers(WellKnownMemberNames.DestructorName)

            If Not finalizeMethods.Any() Then
                If workspaceSupportsDocumentChanges AndAlso type.TypeKind = TypeKind.Class Then
                    childItems.Add(New GenerateFinalizer(WellKnownMemberNames.DestructorName, type.GetSymbolKey(cancellationToken)))
                End If
            Else
                childItems.AddRange(CreateItemsForMemberGroup(solution, finalizeMethods, tree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
            End If

            ' And now, methods and properties
            If type.TypeKind <> TypeKind.Delegate Then
                Dim memberGroups = type.GetMembers().Where(AddressOf IncludeMember) _
                                                    .GroupBy(Function(m) m.Name, CaseInsensitiveComparison.Comparer) _
                                                    .OrderBy(Function(g) g.Key)

                For Each memberGroup In memberGroups
                    If Not CaseInsensitiveComparison.Equals(memberGroup.Key, WellKnownMemberNames.DestructorName) Then
                        childItems.AddRange(CreateItemsForMemberGroup(solution, memberGroup, tree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
                    End If
                Next
            End If

            Dim name = type.ToDisplayString(_typeFormat)

            If type.ContainingType IsNot Nothing Then
                name &= " (" & type.ContainingType.ToDisplayString() & ")"
            End If

            Dim location = GetSymbolLocation(solution, type, tree, symbolDeclarationService)
            If location Is Nothing Then
                Return Nothing
            End If

            Return New SymbolItem(
                type.Name,
                name,
                type.GetGlyph(),
                type.IsObsolete,
                location.Value,
                childItems:=childItems.ToImmutableArray(),
                bolded:=True)
        End Function

        Private Shared Function IncludeMember(symbol As ISymbol) As Boolean
            If symbol.Kind = SymbolKind.Method Then
                Dim method = DirectCast(symbol, IMethodSymbol)

                If method.HandledEvents.Any() Then
                    Return False
                End If

                Return method.MethodKind = MethodKind.Ordinary OrElse
                       method.MethodKind = MethodKind.UserDefinedOperator OrElse
                       method.MethodKind = MethodKind.Conversion
            End If

            If symbol.Kind = SymbolKind.Property Then
                Dim p = DirectCast(symbol, IPropertySymbol)
                Return Not p.IsWithEvents
            End If

            If symbol.Kind = SymbolKind.Event Then
                Return True
            End If

            If symbol.Kind = SymbolKind.Field AndAlso Not symbol.IsImplicitlyDeclared Then
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Creates the left-hand entry and right-hand entries for a list of events.
        ''' </summary>
        ''' <param name="containingType">The type that contains the methods attached to the events.
        ''' For items that will generate new methods, they will be generated into this
        ''' class.</param>
        ''' <param name="eventType">The type to list the events of. This is either equal to
        ''' containingType if it's listing the event handlers for the base types, or else it's the
        ''' type of the eventContainer.</param>
        ''' <param name="eventContainer">If this is an entry for a WithEvents member, the WithEvents
        ''' property itself.</param>
        Private Shared Function CreateItemForEvents(
                solution As Solution,
                containingType As INamedTypeSymbol,
                position As Integer,
                eventType As ITypeSymbol,
                eventContainer As IPropertySymbol,
                semanticModel As SemanticModel,
                workspaceSupportsDocumentChanges As Boolean,
                symbolDeclarationService As ISymbolDeclarationService,
                cancellationToken As CancellationToken) As RoslynNavigationBarItem

            Dim rightHandMemberItems As New List(Of RoslynNavigationBarItem)

            Dim accessibleEvents = semanticModel.LookupSymbols(position, eventType).OfType(Of IEventSymbol).OrderBy(Function(e) e.Name)

            Dim methodsImplementingEvents = containingType.GetMembers().OfType(Of IMethodSymbol) _
                                                          .Where(Function(m) m.HandledEvents.Any(Function(he) Object.Equals(he.EventContainer, eventContainer)))

            Dim eventToImplementingMethods As New Dictionary(Of IEventSymbol, List(Of IMethodSymbol))

            For Each method In methodsImplementingEvents
                For Each handledEvent In method.HandledEvents
                    Dim list As List(Of IMethodSymbol) = Nothing

                    If Not eventToImplementingMethods.TryGetValue(handledEvent.EventSymbol, list) Then
                        list = New List(Of IMethodSymbol)
                        eventToImplementingMethods.Add(handledEvent.EventSymbol, list)
                    End If

                    list.Add(method)
                Next
            Next

            ' Generate an item for each event
            For Each e In accessibleEvents
                Dim methods As List(Of IMethodSymbol) = Nothing
                If eventToImplementingMethods.TryGetValue(e, methods) Then
                    Dim methodLocation = GetSymbolLocation(solution, methods.First(), semanticModel.SyntaxTree, symbolDeclarationService)
                    If methodLocation IsNot Nothing Then
                        rightHandMemberItems.Add(New SymbolItem(
                            e.Name,
                            e.Name,
                            e.GetGlyph(),
                            e.IsObsolete,
                            methodLocation.Value,
                            bolded:=True))
                    End If
                Else
                    If workspaceSupportsDocumentChanges AndAlso
                       e.Type IsNot Nothing AndAlso
                       e.Type.IsDelegateType() AndAlso
                       DirectCast(e.Type, INamedTypeSymbol).DelegateInvokeMethod IsNot Nothing Then

                        Dim eventContainerName = eventContainer?.Name

                        rightHandMemberItems.Add(
                            New GenerateEventHandler(
                                e.Name,
                                e.GetGlyph(),
                                eventContainerName,
                                e.GetSymbolKey(cancellationToken),
                                containingType.GetSymbolKey(cancellationToken)))
                    End If
                End If
            Next

            If eventContainer IsNot Nothing Then
                Return New ActionlessItem(
                    eventContainer.Name,
                    eventContainer.GetGlyph(),
                    indent:=1,
                    childItems:=rightHandMemberItems.ToImmutableArray())
            Else
                Return New ActionlessItem(
                    String.Format(VBFeaturesResources._0_Events, containingType.Name),
                    Glyph.EventPublic,
                    indent:=1,
                    childItems:=rightHandMemberItems.ToImmutableArray())
            End If
        End Function

        Private Function CreateItemsForMemberGroup(
                solution As Solution,
                members As IEnumerable(Of ISymbol),
                tree As SyntaxTree,
                workspaceSupportsDocumentChanges As Boolean,
                symbolDeclarationService As ISymbolDeclarationService,
                cancellationToken As CancellationToken) As IEnumerable(Of RoslynNavigationBarItem)
            Dim firstMember = members.First()

            ' If there is exactly one member that has no type arguments, we will skip showing the
            ' parameters for it to maintain behavior with Dev11
            Dim displayFormat = If(members.Count() = 1 AndAlso firstMember.GetArity() = 0, New SymbolDisplayFormat(), _memberFormat)

            ' If we're doing operators, we want to include the keyword
            If firstMember.IsUserDefinedOperator OrElse firstMember.IsConversion Then
                displayFormat = displayFormat.WithKindOptions(displayFormat.KindOptions Or SymbolDisplayKindOptions.IncludeMemberKeyword)
            End If

            Dim items As New List(Of RoslynNavigationBarItem)
            For Each member In members
                ' If this is a partial method, we'll care about the implementation part if one
                ' exists
                Dim method = TryCast(member, IMethodSymbol)
                If method IsNot Nothing AndAlso method.PartialImplementationPart IsNot Nothing Then
                    method = method.PartialImplementationPart

                    Dim location = GetSymbolLocation(solution, method, tree, symbolDeclarationService)
                    If location IsNot Nothing Then
                        items.Add(New SymbolItem(
                            method.Name,
                            method.ToDisplayString(displayFormat),
                            method.GetGlyph(),
                            method.IsObsolete,
                            location.Value,
                            bolded:=location.Value.InDocumentInfo IsNot Nothing))
                    End If
                ElseIf method IsNot Nothing AndAlso IsUnimplementedPartial(method) Then
                    If workspaceSupportsDocumentChanges Then
                        items.Add(New GenerateMethod(
                            member.ToDisplayString(displayFormat),
                            member.GetGlyph(),
                            member.ContainingType.GetSymbolKey(cancellationToken),
                            member.GetSymbolKey(cancellationToken)))
                    End If
                Else
                    Dim location = GetSymbolLocation(solution, member, tree, symbolDeclarationService)
                    If location IsNot Nothing Then
                        items.Add(New SymbolItem(
                            member.Name,
                            member.ToDisplayString(displayFormat),
                            member.GetGlyph(),
                            member.IsObsolete,
                            location.Value,
                            bolded:=location.Value.InDocumentInfo IsNot Nothing))
                    End If
                End If
            Next

            Return items.OrderBy(Function(i) i.Text)
        End Function

        Private Shared Function IsUnimplementedPartial(method As IMethodSymbol) As Boolean
            If method.PartialImplementationPart IsNot Nothing Then
                Return False
            End If

            Return method.DeclaringSyntaxReferences.Select(Function(r) r.GetSyntax()).OfType(Of MethodStatementSyntax)().Any(Function(m) m.Modifiers.Any(Function(t) t.Kind = SyntaxKind.PartialKeyword))
        End Function
    End Class
End Namespace
