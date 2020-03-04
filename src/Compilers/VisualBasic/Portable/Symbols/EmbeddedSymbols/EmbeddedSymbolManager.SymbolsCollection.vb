' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class EmbeddedSymbolManager
        Friend ReadOnly IsReferencedPredicate As Func(Of Symbol, Boolean) = Function(t) Not t.IsEmbedded OrElse Me.IsSymbolReferenced(t)

        Private ReadOnly _embedded As EmbeddedSymbolKind

        ''' <summary> Automatically embedded symbols (types, methods and fields) used in the current compilation </summary>
        Private ReadOnly _symbols As ConcurrentDictionary(Of Symbol, Boolean)

        ''' <summary>
        ''' Non-0 indicates that the collection of referenced symbols is sealed
        ''' and so no new symbols are supposed to be added.
        ''' </summary>
        Private _sealed As Integer = 0

        ''' <summary>
        ''' True if StandardModuleAttribute was used in the current compilation
        ''' </summary>
        Private _standardModuleAttributeReferenced As Boolean = False

        Public Sub New(embedded As EmbeddedSymbolKind)
            ' Update assert if additional embedded kinds are expected.
            Debug.Assert((embedded And Not EmbeddedSymbolKind.All) = 0)

            _embedded = embedded
            If (embedded And EmbeddedSymbolKind.All) <> 0 Then
                ' If any bits are set, EmbeddedAttribute should be set.
                Debug.Assert((embedded And EmbeddedSymbolKind.EmbeddedAttribute) <> 0)
                _symbols = New ConcurrentDictionary(Of Symbol, Boolean)(ReferenceEqualityComparer.Instance)
            End If
        End Sub

        Public ReadOnly Property Embedded As EmbeddedSymbolKind
            Get
                Return _embedded
            End Get
        End Property

        ''' <summary>
        ''' Marks StandardModuleAttributeReference type as being references in the 
        ''' current compilation. This method is to be used when a new type symbol for a 
        ''' module is being created; we cannot pass the actual StandardModuleAttribute 
        ''' type symbol because the symbol table is being constructed and calling 
        ''' Compilation.GetWellKnownType(...) will cause infinite recursion. It does
        ''' not seem reasonable to special case this in symbol creation, so we just 
        ''' mark StandardModuleAttribute attribute as referenced and then add 
        ''' the actual symbol when MarkAllDeferredSymbols(...) is called.
        ''' </summary>
        Public Sub RegisterModuleDeclaration()
            If (_embedded And EmbeddedSymbolKind.VbCore) <> 0 Then
                _standardModuleAttributeReferenced = True
            End If
        End Sub

#If DEBUG Then
        Private _markAllDeferredSymbolsAsReferencedIsCalled As Integer = ThreeState.Unknown
#End If

        ''' <summary>
        ''' Mark all deferred types as referenced
        ''' </summary>
        Public Sub MarkAllDeferredSymbolsAsReferenced(compilation As VisualBasicCompilation)
            If Me._standardModuleAttributeReferenced Then
                MarkSymbolAsReferenced(
                    compilation.GetWellKnownType(
                        WellKnownType.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute))
            End If

#If DEBUG Then
            Interlocked.CompareExchange(_markAllDeferredSymbolsAsReferencedIsCalled,
                                        ThreeState.True, ThreeState.Unknown)
#End If
        End Sub

        <Conditional("DEBUG")>
        Friend Sub AssertMarkAllDeferredSymbolsAsReferencedIsCalled()
#If DEBUG Then
            Debug.Assert(Me._markAllDeferredSymbolsAsReferencedIsCalled = ThreeState.True)
#End If
        End Sub

        ''' <summary>
        ''' Returns True if any embedded symbols are referenced.
        ''' 
        ''' WARNING: the referenced symbols collection may not be sealed yet!!!
        ''' </summary>
        Public ReadOnly Property IsAnySymbolReferenced As Boolean
            Get
                Me.AssertMarkAllDeferredSymbolsAsReferencedIsCalled()
                Return (_symbols IsNot Nothing) AndAlso Not _symbols.IsEmpty
            End Get
        End Property

        ''' <summary>
        ''' Makes a snapshot of the current set of referenced symbols filtered by, 
        ''' the set of symbols provided; may be called before the referenced symbol 
        ''' collection is sealed.
        ''' </summary>
        Friend Sub GetCurrentReferencedSymbolsSnapshot(builder As ArrayBuilder(Of Symbol), filter As ConcurrentSet(Of Symbol))
            Debug.Assert(builder IsNot Nothing)
            Debug.Assert(builder.Count = 0)
            Debug.Assert(filter IsNot Nothing)

            For Each pair In _symbols.ToArray()
                If Not filter.Contains(pair.Key) Then
                    builder.Add(pair.Key)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Checks if the embedded symbol provided is in the collection and adds it 
        ''' into collection if not.
        ''' 
        ''' See description of AddReferencedSymbolWithDependents for more details of how 
        ''' it actually works.
        ''' </summary>
        Public Sub MarkSymbolAsReferenced(symbol As Symbol, allSymbols As ConcurrentSet(Of Symbol))
#If Not Debug Then
            ' In RELEASE don't add anything if the collection is sealed
            If _sealed <> 0 Then
                Return
            End If
#End If

            Debug.Assert(symbol.IsDefinition)
            Debug.Assert(symbol.IsEmbedded)
            AddReferencedSymbolWithDependents(symbol, allSymbols)
        End Sub

        Public Sub MarkSymbolAsReferenced(symbol As Symbol)
            MarkSymbolAsReferenced(symbol, New ConcurrentSet(Of Symbol)(ReferenceEqualityComparer.Instance))
        End Sub

        ''' <summary>
        ''' Returns True if the embedded symbol is known to be referenced in the current compilation.
        ''' </summary>
        Public Function IsSymbolReferenced(symbol As Symbol) As Boolean
            Debug.Assert(symbol.IsEmbedded)
            Me.AssertMarkAllDeferredSymbolsAsReferencedIsCalled()
            Return _symbols.TryGetValue(symbol, Nothing)
        End Function

        ''' <summary>
        ''' Seals the collection of referenced symbols, all *new* symbols passed 
        ''' to SpawnSymbolCollection(...) will cause assert and be ignored.
        ''' </summary>
        Public Sub SealCollection()
            Interlocked.CompareExchange(_sealed, 1, 0)
        End Sub

#Region "Add referenced symbol implementation"

        ''' <summary>
        ''' Checks if the embedded symbol provided is present in the 'allSymbols' and if not 
        ''' adds it into 'allSymbols' as well as to the collection of referenced symbols 
        ''' managed by this manager. Also adds all the 'dependent' symbols, i.e. symbols 
        ''' which must also be marked as referenced if 'symbol' is referenced.
        ''' 
        ''' NOTE that when a new embedded symbol is being added to the collection of referenced 
        ''' symbols it should be added along with all the 'dependent' symbols. For example, if 
        ''' we add a method symbol (T1.M1) we should ensure the containing type symbol (T1) is 
        ''' added too, as well as its constructor (T1..ctor) and maybe attribute(s) (Attr1) set 
        ''' on T1 and their constructors/fields (Attr1..ctor), etc...
        ''' 
        ''' All dependent symbols must be added in the current thread not depending on 
        ''' the other concurrent threads and avoiding possible race. Thus, let's suppose we have
        ''' the following dependencies:
        ''' 
        '''          T1.M1 -> { T1,  T1..ctor, Attr1, Attr1..ctor, ... }
        ''' 
        ''' we cannot just check if T1.M1 exists in the collection of referenced symbols and not 
        ''' add dependent symbols if it does; the reason is that T1.M1 may be added by a concurrent 
        ''' thread, but its dependencies may not be added by that thread yet. So we need to 
        ''' calculate all dependencies and try add all the symbols together.
        ''' 
        ''' On the other hand it should be avoided that the method *always* goes through all
        ''' the dependencies for each symbol even though it may be definitely known that the symbol
        ''' is added in one of the previous operations by *the same thread*. To serve this purpose 
        ''' the method uses 'allSymbols' collection to actually check whether or not the symbol 
        ''' is added to the collection. This makes possible to reuse the same collection in several 
        ''' consequent calls to AddReferencedSymbolWithDependents from the same thread; for example 
        ''' in case one thread consequently adds lots of symbols, the thread may use the same 
        ''' 'allSymbols' instance for efficient symbol filtering.
        ''' </summary>
        Private Sub AddReferencedSymbolWithDependents(symbol As Symbol, allSymbols As ConcurrentSet(Of Symbol))
            If Not symbol.IsEmbedded Then
                Return
            End If

            Debug.Assert(symbol.IsDefinition)

            If allSymbols.Contains(symbol) Then
                Return ' was added in this thread before
            End If

            Select Case symbol.Kind

                Case SymbolKind.Field

                    ' add the symbol itself
                    AddReferencedSymbolRaw(symbol, allSymbols)

                    ' add the containing type
                    AddReferencedSymbolWithDependents(symbol.ContainingType, allSymbols)

                Case SymbolKind.Method

                    ' add the symbol itself
                    AddReferencedSymbolRaw(symbol, allSymbols)

                    ' add the containing type
                    AddReferencedSymbolWithDependents(symbol.ContainingType, allSymbols)

                    ' if the method is an accessor
                    Dim methKind As MethodKind = DirectCast(symbol, MethodSymbol).MethodKind
                    Select Case methKind
                        Case MethodKind.PropertyGet, MethodKind.PropertySet
                            ' add associated property, note that adding any accessor will cause 
                            ' adding the property as well as the other accessor if any
                            AddReferencedSymbolWithDependents(DirectCast(symbol, MethodSymbol).AssociatedSymbol, allSymbols)

                        Case MethodKind.Ordinary,
                             MethodKind.Constructor,
                             MethodKind.SharedConstructor
                            ' OK

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(methKind)
                    End Select

                Case SymbolKind.Property

                    ' add the symbol itself
                    AddReferencedSymbolRaw(symbol, allSymbols)

                    ' add the containing type
                    AddReferencedSymbolWithDependents(symbol.ContainingType, allSymbols)

                    ' add accessors
                    Dim [property] = DirectCast(symbol, PropertySymbol)
                    If [property].GetMethod IsNot Nothing Then
                        AddReferencedSymbolWithDependents([property].GetMethod, allSymbols)
                    End If
                    If [property].SetMethod IsNot Nothing Then
                        AddReferencedSymbolWithDependents([property].SetMethod, allSymbols)
                    End If

                Case SymbolKind.NamedType

                    ValidateType(DirectCast(symbol, NamedTypeSymbol))

                    ' add the symbol itself
                    AddReferencedSymbolRaw(symbol, allSymbols)

                    ' add SOME type members
                    For Each member In DirectCast(symbol, NamedTypeSymbol).GetMembers()

                        Select Case member.Kind

                            Case SymbolKind.Field
                                ' Always add non-const fields 
                                If Not DirectCast(member, FieldSymbol).IsConst Then
                                    AddReferencedSymbolRaw(member, allSymbols)

                                    ' fields of embedded types are not supported
                                    Debug.Assert(Not DirectCast(member, FieldSymbol).Type.IsEmbedded)
                                End If

                            Case SymbolKind.Method
                                Select Case DirectCast(member, MethodSymbol).MethodKind
                                    Case MethodKind.SharedConstructor,
                                         MethodKind.Constructor
                                        ' Add constructors
                                        AddReferencedSymbolRaw(member, allSymbols)
                                End Select

                                ' Don't add regular methods, all of them should be added on-demand
                                ' All other method kinds should not get here, it is asserted in ValidateType(...)
                        End Select
                    Next

                    If symbol.ContainingType IsNot Nothing Then
                        AddReferencedSymbolWithDependents(symbol.ContainingType, allSymbols)
                    End If

            End Select
        End Sub

        Private Sub AddReferencedSymbolRaw(symbol As Symbol, allSymbols As ConcurrentSet(Of Symbol))
            Debug.Assert(symbol.Kind = SymbolKind.NamedType OrElse
                         symbol.Kind = SymbolKind.Property OrElse
                         symbol.Kind = SymbolKind.Method OrElse
                         symbol.Kind = SymbolKind.Field)

            If allSymbols.Add(symbol) Then

                If _sealed <> 0 Then
                    ' Collection is sealed 
                    Debug.Assert(_symbols.ContainsKey(symbol))
                Else
                    _symbols.TryAdd(symbol, True)
                    ' NOTE: there is still a chance that a new element is added to a sealed collection
                End If

                ' add symbol's attributes
                For Each attribute In symbol.GetAttributes()
                    AddReferencedSymbolWithDependents(attribute.AttributeClass, allSymbols)
                Next
            End If
        End Sub

#End Region

        <Conditional("DEBUG")>
        Private Shared Sub ValidateType(type As NamedTypeSymbol)
            Debug.Assert(type.TypeKind = TypeKind.Module OrElse type.TypeKind = TypeKind.Class AndAlso type.IsNotInheritable)

            For Each member In type.GetMembers()

                Select Case member.Kind

                    Case SymbolKind.Field
                        ValidateField(DirectCast(member, FieldSymbol))

                    Case SymbolKind.Method
                        ValidateMethod(DirectCast(member, MethodSymbol))

                    Case SymbolKind.NamedType
                        ' Nested types are OK

                    Case SymbolKind.Property
                        ' Properties are OK if the accessors are OK, and accessors will be
                        ' checked separately since those will also appear in GetMembers().

                    Case Else
                        ' No other symbol kinds are allowed
                        Throw ExceptionUtilities.UnexpectedValue(member.Kind)

                End Select

            Next
        End Sub

        <Conditional("DEBUG")>
        Private Shared Sub ValidateField(field As FieldSymbol)
            ' Fields are OK (initializers are checked in method compiler)
            Dim type = field.Type
            Debug.Assert(Not type.IsEmbedded OrElse type.IsTypeParameter)
        End Sub

        <Conditional("DEBUG")>
        Friend Shared Sub ValidateMethod(method As MethodSymbol)
            ' Constructors, regular methods, and property accessors are OK
            Dim kind = method.MethodKind
            Debug.Assert(kind = MethodKind.Constructor OrElse
                         kind = MethodKind.SharedConstructor OrElse
                         kind = MethodKind.Ordinary OrElse
                         kind = MethodKind.PropertyGet OrElse
                         kind = MethodKind.PropertySet)
            Debug.Assert(Not method.IsOverridable)
            Debug.Assert(method.ExplicitInterfaceImplementations.IsEmpty)

            Debug.Assert(Not method.ReturnType.IsEmbedded OrElse method.ReturnType.IsTypeParameter)
            Debug.Assert(method.GetReturnTypeAttributes().IsEmpty)

            For Each parameter In method.Parameters
                Debug.Assert(Not parameter.Type.IsEmbedded OrElse parameter.Type.IsTypeParameter)
                Debug.Assert(parameter.GetAttributes().IsEmpty)
            Next
        End Sub

    End Class

End Namespace
