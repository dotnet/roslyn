' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem

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
            Dim typeSymbolIndexProvider As New NavigationBarSymbolIdIndexProvider(caseSensitive:=False)

            Dim symbolDeclarationService = document.GetLanguageService(Of ISymbolDeclarationService)

            For Each typeAndDeclaration In typesAndDeclarations
                Dim type = typeAndDeclaration.Item1
                Dim position = typeAndDeclaration.Item2.SpanStart
                typeItems.AddRange(CreateItemsForType(type, position, typeSymbolIndexProvider.GetIndexForSymbolId(type.GetSymbolKey(cancellationToken)), semanticModel, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
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
                type As INamedTypeSymbol,
                position As Integer,
                typeSymbolIdIndex As Integer,
                semanticModel As SemanticModel,
                workspaceSupportsDocumentChanges As Boolean,
                symbolDeclarationService As ISymbolDeclarationService,
                cancellationToken As CancellationToken) As ImmutableArray(Of RoslynNavigationBarItem)

            Dim items = ImmutableArray.CreateBuilder(Of RoslynNavigationBarItem)
            If type.TypeKind = TypeKind.Enum Then
                items.Add(CreateItemForEnum(type, typeSymbolIdIndex, semanticModel.SyntaxTree, symbolDeclarationService, cancellationToken))
            Else
                items.Add(CreatePrimaryItemForType(type, typeSymbolIdIndex, semanticModel.SyntaxTree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))

                If type.TypeKind <> TypeKind.Interface Then
                    Dim typeEvents = CreateItemForEvents(
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
                            items.Add(CreateItemForEvents(
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

            Return items.ToImmutable()
        End Function

        Private Shared Function CreateItemForEnum(
                type As INamedTypeSymbol,
                typeSymbolIdIndex As Integer,
                tree As SyntaxTree,
                symbolDeclarationService As ISymbolDeclarationService,
                cancellationToken As CancellationToken) As RoslynNavigationBarItem

            Dim symbolIndexProvider As New NavigationBarSymbolIdIndexProvider(caseSensitive:=False)

            Dim members = Aggregate member In type.GetMembers()
                          Where member.IsShared AndAlso member.Kind = SymbolKind.Field
                          Order By member.Name
                          Select DirectCast(New SymbolItem(
                              member.Name,
                              member.GetGlyph(),
                              GetSpansInDocument(member, tree, symbolDeclarationService, cancellationToken),
                              member.GetSymbolKey(cancellationToken),
                              symbolIndexProvider.GetIndexForSymbolId(member.GetSymbolKey(cancellationToken))), RoslynNavigationBarItem)
                          Into ToImmutableArray()

            Return New SymbolItem(
                type.Name,
                type.GetGlyph(),
                GetSpansInDocument(type, tree, symbolDeclarationService, cancellationToken),
                type.GetSymbolKey(cancellationToken),
                typeSymbolIdIndex,
                members,
                bolded:=True)
        End Function

        Private Function CreatePrimaryItemForType(
                type As INamedTypeSymbol,
                typeSymbolIdIndex As Integer,
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
                childItems.AddRange(CreateItemsForMemberGroup(constructors, tree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
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
                childItems.AddRange(CreateItemsForMemberGroup(finalizeMethods, tree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
            End If

            ' And now, methods and properties
            If type.TypeKind <> TypeKind.Delegate Then
                Dim memberGroups = type.GetMembers().Where(AddressOf IncludeMember) _
                                                    .GroupBy(Function(m) m.Name, CaseInsensitiveComparison.Comparer) _
                                                    .OrderBy(Function(g) g.Key)

                For Each memberGroup In memberGroups
                    If Not CaseInsensitiveComparison.Equals(memberGroup.Key, WellKnownMemberNames.DestructorName) Then
                        childItems.AddRange(CreateItemsForMemberGroup(memberGroup, tree, workspaceSupportsDocumentChanges, symbolDeclarationService, cancellationToken))
                    End If
                Next
            End If

            Dim name = type.ToDisplayString(_typeFormat)

            If type.ContainingType IsNot Nothing Then
                name &= " (" & type.ContainingType.ToDisplayString() & ")"
            End If

            Return New SymbolItem(
                name,
                type.GetGlyph(),
                spans:=GetSpansInDocument(type, tree, symbolDeclarationService, cancellationToken),
                navigationSymbolId:=type.GetSymbolKey(cancellationToken),
                navigationSymbolIndex:=typeSymbolIdIndex,
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

            ' The spans of the left item will encompass all event handler spans
            Dim allMethodSpans As New List(Of TextSpan)

            ' Generate an item for each event
            For Each e In accessibleEvents
                If eventToImplementingMethods.ContainsKey(e) Then
                    Dim methodSpans = GetSpansInDocument(eventToImplementingMethods(e), semanticModel.SyntaxTree, symbolDeclarationService)

                    ' Dev11 arbitrarily will navigate to the last method that implements the event
                    ' if more than one exists
                    Dim navigationSymbolId = eventToImplementingMethods(e).Last.GetSymbolKey(cancellationToken)

                    rightHandMemberItems.Add(
                        New SymbolItem(
                            e.Name,
                            e.GetGlyph(),
                            methodSpans,
                            navigationSymbolId,
                            navigationSymbolIndex:=0,
                            bolded:=True))

                    allMethodSpans.AddRange(methodSpans)
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
                    spans:=allMethodSpans.ToImmutableArray(),
                    childItems:=rightHandMemberItems.ToImmutableArray())
            Else
                Return New ActionlessItem(
                    String.Format(VBFeaturesResources._0_Events, containingType.Name),
                    Glyph.EventPublic,
                    indent:=1,
                    spans:=allMethodSpans.ToImmutableArray(),
                    childItems:=rightHandMemberItems.ToImmutableArray())
            End If
        End Function

        Private Shared Function GetSpansInDocument(symbol As ISymbol, tree As SyntaxTree, symbolDeclarationService As ISymbolDeclarationService, cancellationToken As CancellationToken) As ImmutableArray(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then
                Return ImmutableArray(Of TextSpan).Empty
            End If

            Return GetSpansInDocument(SpecializedCollections.SingletonEnumerable(symbol), tree, symbolDeclarationService)
        End Function

        Private Shared Function GetSpansInDocument(list As IEnumerable(Of ISymbol), tree As SyntaxTree, symbolDeclarationService As ISymbolDeclarationService) As ImmutableArray(Of TextSpan)
            Return list.SelectMany(AddressOf symbolDeclarationService.GetDeclarations) _
                        .Where(Function(r) r.SyntaxTree.Equals(tree)) _
                        .Select(Function(r) r.GetSyntax().FullSpan) _
                        .ToImmutableArray()
        End Function

        Private Function CreateItemsForMemberGroup(members As IEnumerable(Of ISymbol),
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
            Dim symbolIdIndexProvider As New NavigationBarSymbolIdIndexProvider(caseSensitive:=False)

            For Each member In members
                Dim spans = GetSpansInDocument(member, tree, symbolDeclarationService, cancellationToken)

                ' If this is a partial method, we'll care about the implementation part if one
                ' exists
                Dim method = TryCast(member, IMethodSymbol)
                If method IsNot Nothing AndAlso method.PartialImplementationPart IsNot Nothing Then
                    method = method.PartialImplementationPart
                    items.Add(New SymbolItem(
                        method.ToDisplayString(displayFormat),
                        method.GetGlyph(),
                        spans,
                        method.GetSymbolKey(cancellationToken),
                        symbolIdIndexProvider.GetIndexForSymbolId(method.GetSymbolKey(cancellationToken)),
                        bolded:=spans.Count > 0,
                        grayed:=spans.Count = 0))
                ElseIf method IsNot Nothing AndAlso IsUnimplementedPartial(method) Then
                    If workspaceSupportsDocumentChanges Then
                        items.Add(New GenerateMethod(
                        member.ToDisplayString(displayFormat),
                        member.GetGlyph(),
                        member.ContainingType.GetSymbolKey(cancellationToken),
                        member.GetSymbolKey(cancellationToken)))
                    End If
                Else
                    items.Add(New SymbolItem(
                        member.ToDisplayString(displayFormat),
                        member.GetGlyph(),
                        spans,
                        member.GetSymbolKey(cancellationToken),
                        symbolIdIndexProvider.GetIndexForSymbolId(member.GetSymbolKey(cancellationToken)),
                        bolded:=spans.Count > 0,
                        grayed:=spans.Count = 0))
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
