' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' The kinds of results. Higher results take precedence over lower results (except that Good and Ambiguous have
    ' equal priority, generally).
    Friend Enum LookupResultKind
        Empty
        NotATypeOrNamespace
        NotAnAttributeType
        WrongArity
        NotCreatable
        Inaccessible
        NotReferencable   ' e.g., implemented interface member in wrong interface, accessor that can't be called directly.
        NotAValue
        NotAVariable
        MustNotBeInstance
        MustBeInstance
        NotAnEvent ' appears  only in semantic info (not in regular lookup)
        LateBound ' appears  only in semantic info (not in regular lookup)
        ' Above this point, we continue looking up the binder chain for better possible symbol
        ' Below this point, we stop looking further (see StopFurtherLookup property)
        EmptyAndStopLookup
        WrongArityAndStopLookup
        OverloadResolutionFailure
        NotAWithEventsMember
        Ambiguous
        MemberGroup ' Indicates a set of symbols, and they are totally fine.
        Good

    End Enum

    Friend Module LookupResultKindExtensions
        <Extension()>
        Public Function ToCandidateReason(resultKind As LookupResultKind) As CandidateReason
            Select Case resultKind
                Case LookupResultKind.Empty, LookupResultKind.EmptyAndStopLookup
                    Return CandidateReason.None
                Case LookupResultKind.OverloadResolutionFailure
                    Return CandidateReason.OverloadResolutionFailure
                Case LookupResultKind.NotATypeOrNamespace
                    Return CandidateReason.NotATypeOrNamespace
                Case LookupResultKind.NotAnEvent
                    Return CandidateReason.NotAnEvent
                Case LookupResultKind.LateBound
                    Return CandidateReason.LateBound
                Case LookupResultKind.NotAnAttributeType
                    Return CandidateReason.NotAnAttributeType
                Case LookupResultKind.NotAWithEventsMember
                    Return CandidateReason.NotAWithEventsMember
                Case LookupResultKind.WrongArity, LookupResultKind.WrongArityAndStopLookup
                    Return CandidateReason.WrongArity
                Case LookupResultKind.NotCreatable
                    Return CandidateReason.NotCreatable
                Case LookupResultKind.Inaccessible
                    Return CandidateReason.Inaccessible
                Case LookupResultKind.NotAValue
                    Return CandidateReason.NotAValue
                Case LookupResultKind.NotAVariable
                    Return CandidateReason.NotAVariable
                Case LookupResultKind.NotReferencable
                    Return CandidateReason.NotReferencable
                Case LookupResultKind.MustNotBeInstance, LookupResultKind.MustBeInstance
                    Return CandidateReason.StaticInstanceMismatch
                Case LookupResultKind.Ambiguous
                    Return CandidateReason.Ambiguous
                Case LookupResultKind.MemberGroup
                    Return CandidateReason.MemberGroup

                Case Else
                    ' Should not call this on LookupResultKind.Good or undefined kind
                    Throw ExceptionUtilities.UnexpectedValue(resultKind)
            End Select
        End Function
    End Module

    ''' <summary> 
    ''' Represents a result of lookup operation over a 0 or 1 symbol (as opposed to a scope).
    ''' The typical use is to represent that a particular symbol is good/bad/unavailable.
    '''
    '''For more explanation of Kind, Symbol, Error - see LookupResult.
    ''' </summary> 
    Friend Structure SingleLookupResult
        ' the kind of result.
        Friend ReadOnly Kind As LookupResultKind
        ' the symbol or null.
        Friend ReadOnly Symbol As Symbol
        ' the error of the result, if it is Bag or Inaccessible or WrongArityAndStopLookup
        Friend ReadOnly Diagnostic As DiagnosticInfo

        Friend Sub New(kind As LookupResultKind, symbol As Symbol, diagInfo As DiagnosticInfo)
            Me.Kind = kind
            Me.Symbol = symbol
            Me.Diagnostic = diagInfo
        End Sub

        Public ReadOnly Property HasDiagnostic As Boolean
            Get
                Return Diagnostic IsNot Nothing
            End Get
        End Property

        ' Get a result for a good (viable) symbol with no errors.
        ' Get an empty result.
        Public Shared ReadOnly Empty As New SingleLookupResult(LookupResultKind.Empty, Nothing, Nothing)

        Public Shared Function Good(sym As Symbol) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.Good, sym, Nothing)
        End Function

        ' 2 or more ambiguous symbols.
        Public Shared Function Ambiguous(syms As ImmutableArray(Of Symbol),
                                    generateAmbiguityDiagnostic As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic)) As SingleLookupResult
            Debug.Assert(syms.Length > 1)
            Dim diagInfo As DiagnosticInfo = generateAmbiguityDiagnostic(syms)
            Return New SingleLookupResult(LookupResultKind.Ambiguous, syms.First(), diagInfo)
        End Function

        Public Shared Function WrongArityAndStopLookup(sym As Symbol, err As ERRID) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.WrongArityAndStopLookup, sym, New BadSymbolDiagnostic(sym, err))
        End Function

        Public Shared ReadOnly EmptyAndStopLookup As New SingleLookupResult(LookupResultKind.EmptyAndStopLookup, Nothing, Nothing)

        Public Shared Function WrongArityAndStopLookup(sym As Symbol, diagInfo As DiagnosticInfo) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.WrongArityAndStopLookup, sym, diagInfo)
        End Function

        Public Shared Function WrongArity(sym As Symbol,
                                          diagInfo As DiagnosticInfo) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.WrongArity, sym, diagInfo)
        End Function

        Public Shared Function WrongArity(sym As Symbol,
                                          err As ERRID) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.WrongArity, sym, New BadSymbolDiagnostic(sym, err))
        End Function

        ' Gets a bad result for a symbol that doesn't match static/instance
        Public Shared Function MustNotBeInstance(sym As Symbol, err As ERRID) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.MustNotBeInstance, sym, New BadSymbolDiagnostic(sym, err, Array.Empty(Of Object)))
        End Function

        ' Gets a bad result for a symbol that doesn't match static/instance, with no error message (special case for API-access)
        Public Shared Function MustBeInstance(sym As Symbol) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.MustBeInstance, sym, Nothing)
        End Function

        ' Gets a inaccessible result for a symbol.
        Public Shared Function Inaccessible(sym As Symbol,
                                            diagInfo As DiagnosticInfo) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.Inaccessible, sym, diagInfo)
        End Function

        Friend Shared Function NotAnAttributeType(sym As Symbol, [error] As DiagnosticInfo) As SingleLookupResult
            Return New SingleLookupResult(LookupResultKind.NotAnAttributeType, sym, [error])
        End Function

        ' Should we stop looking further for a better result?
        Public ReadOnly Property StopFurtherLookup As Boolean
            Get
                Return Kind >= LookupResultKind.WrongArityAndStopLookup
            End Get
        End Property

        Public ReadOnly Property IsGoodOrAmbiguous As Boolean
            Get
                Return Kind = LookupResultKind.Good OrElse Kind = LookupResultKind.Ambiguous
            End Get
        End Property

        Public ReadOnly Property IsGood As Boolean
            Get
                Return Kind = LookupResultKind.Good
            End Get
        End Property

        Public ReadOnly Property IsAmbiguous As Boolean
            Get
                Return Kind = LookupResultKind.Ambiguous
            End Get
        End Property
    End Structure

    ''' <summary>
    ''' A LookupResult summarizes the result of a name lookup, and allows combining name lookups
    ''' from different scopes in an easy way.
    ''' 
    ''' A LookupResult can be ONE OF:
    '''    empty - nothing found.
    '''    a non-accessible result - this kind of result means that search continues into further scopes of lower priority for
    '''                      a viable result. An error is attached with the inaccessibility errors. Non-accessible results take priority over
    '''                      non-viable results.
    '''    a non-viable result - a result that means that the search continues into further scopes of lower priority for
    '''                          a viable or non-accessible result. An error is attached with the error that indicates
    '''                          why the result is non-viable.
    '''    a bad symbol that stops further lookup -  this kind of result prevents lookup into further scopes of lower priority.
    '''                      a diagnostic is attached explaining why the symbol is bad.
    '''    ambiguous symbols.- In this case, an AmbiguousSymbolDiagnostic diagnostic has the other symbols. 
    '''    a good symbol, or set of good overloaded symbols - no diagnostic is attached in this case
    ''' 
    ''' Occasionally, good or ambiguous results are referred to as "viable" results.
    ''' 
    ''' Multiple symbols can be represented in a single LookupResult. Multiple symbols are ONLY USED for overloadable
    ''' entities, such as methods or properties, and represent all the symbols that overload resolution needs to consider.
    ''' When ambiguous symbols are encountered, a single representative symbols is returned, with an attached AmbiguousSymbolDiagnostic
    ''' from which all the ambiguous symbols can be retrieved. This implies that Lookup operations that are restricted to namespaces
    ''' and/or types always create a LookupResult with 0 or 1 symbol.
    ''' 
    ''' Note that the class is poolable so its instances can be obtained from a pool via GetInstance.
    ''' Also it is a good idea to call Free on instances after they no longer needed.
    ''' 
    ''' The typical pattern is "caller allocates / caller frees" -
    '''    
    '''    Dim result = LookupResult.GetInstance()
    '''  
    '''    scope.Lookup(result, "goo")
    '''    ... use result ...
    '''         
    '''    result.Clear()
    '''    anotherScope.Lookup(result, "moo")
    '''    ... use result ...
    ''' 
    '''    result.Free()   'result and its content is invalid after this
    ''' </summary>
    Friend Class LookupResult

        ' The kind of result.
        Private _kind As LookupResultKind

        ' The symbol, unless the kind is empty.
        Private ReadOnly _symList As ArrayBuilder(Of Symbol)

        ' The diagnostic. This is always set for NonAccessible and NonViable results. It may be
        ' set for viable results.
        Private _diagInfo As DiagnosticInfo

        ' The pool used to get instances from.
        Private ReadOnly _pool As ObjectPool(Of LookupResult)

        ''''''''''''''''''''''''''''''
        ' Access routines

        Public ReadOnly Property Kind As LookupResultKind
            Get
                Return _kind
            End Get
        End Property

        ' Should we stop looking further for a better result? Stop for kind EmptyAndStopLookup, WrongArityAndStopLookup, Ambiguous, or Good.
        Public ReadOnly Property StopFurtherLookup As Boolean
            Get
                Return _kind >= LookupResultKind.EmptyAndStopLookup
            End Get
        End Property

        ' Does it have a symbol without error.
        Public ReadOnly Property IsGood As Boolean
            Get
                Return _kind = LookupResultKind.Good
            End Get
        End Property

        Public ReadOnly Property IsGoodOrAmbiguous As Boolean
            Get
                Return _kind = LookupResultKind.Good OrElse _kind = LookupResultKind.Ambiguous
            End Get
        End Property

        Public ReadOnly Property IsAmbiguous As Boolean
            Get
                Return _kind = LookupResultKind.Ambiguous
            End Get
        End Property

        Public ReadOnly Property IsWrongArity As Boolean
            Get
                Return _kind = LookupResultKind.WrongArity OrElse _kind = LookupResultKind.WrongArityAndStopLookup
            End Get
        End Property

        Public ReadOnly Property HasDiagnostic As Boolean
            Get
                Return _diagInfo IsNot Nothing
            End Get
        End Property

        Public ReadOnly Property Diagnostic As DiagnosticInfo
            Get
                Return _diagInfo
            End Get
        End Property

        Public ReadOnly Property HasSymbol As Boolean
            Get
                Return _symList.Count > 0
            End Get
        End Property

        Public ReadOnly Property HasSingleSymbol As Boolean
            Get
                Return _symList.Count = 1
            End Get
        End Property

        Public ReadOnly Property Symbols As ArrayBuilder(Of Symbol)
            Get
                Return _symList
            End Get
        End Property

        Public ReadOnly Property SingleSymbol As Symbol
            Get
                Debug.Assert(HasSingleSymbol)
                Return _symList(0)
            End Get
        End Property

        ''''''''''''''''''''''''''''''
        ' Creation routines

        Private Shared ReadOnly s_poolInstance As ObjectPool(Of LookupResult) = CreatePool()

        Private Shared Function CreatePool() As ObjectPool(Of LookupResult)
            Dim pool As ObjectPool(Of LookupResult) = Nothing
            pool = New ObjectPool(Of LookupResult)(Function() New LookupResult(pool), 128)
            Return pool
        End Function

        ' Private constructor. Use shared methods for construction.
        Private Sub New(pool As ObjectPool(Of LookupResult))
            MyClass.New()

            _pool = pool
        End Sub

        ' used by unit tests
        Friend Sub New()
            _kind = LookupResultKind.Empty
            _symList = New ArrayBuilder(Of Symbol)
            _diagInfo = Nothing
        End Sub

        Public Shared Function GetInstance() As LookupResult
            Return s_poolInstance.Allocate()
        End Function

        Public Sub Free()
            Clear()
            If _pool IsNot Nothing Then
                _pool.Free(Me)
            End If
        End Sub

        Public Sub Clear()
            _kind = LookupResultKind.Empty
            _symList.Clear()
            _diagInfo = Nothing
        End Sub

        Public ReadOnly Property IsClear As Boolean
            Get
                Return _kind = LookupResultKind.Empty AndAlso _symList.Count = 0 AndAlso _diagInfo Is Nothing
            End Get
        End Property

        Private Sub SetFrom(kind As LookupResultKind, sym As Symbol, diagInfo As DiagnosticInfo)
            _kind = kind
            _symList.Clear()
            If sym IsNot Nothing Then
                _symList.Add(sym)
            End If
            _diagInfo = diagInfo
        End Sub

        ''' <summary>
        ''' Set current result according to another
        ''' </summary>
        Public Sub SetFrom(other As SingleLookupResult)
            SetFrom(other.Kind, other.Symbol, other.Diagnostic)
        End Sub

        ''' <summary>
        ''' Set current result according to another
        ''' </summary>
        Public Sub SetFrom(other As LookupResult)
            _kind = other._kind
            _symList.Clear()
            _symList.AddRange(other._symList)
            _diagInfo = other._diagInfo
        End Sub

        ''' <summary>
        ''' Set current result according to a given symbol    
        ''' </summary>
        ''' <param name="s"></param>
        ''' <remarks></remarks>
        Public Sub SetFrom(s As Symbol)
            SetFrom(SingleLookupResult.Good(s))
        End Sub

        ' Get a result for set of symbols. 
        ' If the set is empty, an empty result is created.
        ' If the set has one member, a good (viable) symbol is created.
        ' If the set has more than one member, the first member is a good result, but an ambiguity error
        ' is attached with all the ambiguous symbols in it. The supplied delegate is called to generate the
        ' ambiguity error (only if the set has >1 symbol in it).
        Public Sub SetFrom(syms As ImmutableArray(Of Symbol),
                           generateAmbiguityDiagnostic As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic))
            If syms.Length = 0 Then
                ' no symbols provided. return empty result
                Clear()
            ElseIf syms.Length > 1 Then
                ' ambiguous symbols.
                Dim diagInfo As DiagnosticInfo = generateAmbiguityDiagnostic(syms)
                SetFrom(LookupResultKind.Ambiguous, syms(0), diagInfo)
            Else
                ' single good symbol
                SetFrom(SingleLookupResult.Good(syms(0)))
            End If
        End Sub

        ''''''''''''''''''''''''''''''
        ' Combining routines

        ' Merge two results, returning the better one. If they are equally good, the first has
        ' priority. Never produces an ambiguity.
        Public Sub MergePrioritized(other As LookupResult)
            If other.Kind > Me.Kind AndAlso Me.Kind < LookupResultKind.Ambiguous Then
                SetFrom(other)
            End If
        End Sub

        ' Merge two results, returning the better one. If they are equally good, the first has
        ' priority. Never produces an ambiguity.
        Public Sub MergePrioritized(other As SingleLookupResult)
            If other.Kind > Me.Kind AndAlso Me.Kind < LookupResultKind.Ambiguous Then
                SetFrom(other)
            End If
        End Sub

        ' Merge two results, returning the best. If there are
        ' multiple viable results, instead produce an ambiguity between all of them.
        Public Sub MergeAmbiguous(other As LookupResult,
                                  generateAmbiguityDiagnostic As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic))
            If Me.IsGoodOrAmbiguous AndAlso other.IsGoodOrAmbiguous Then
                ' Two viable or ambiguous results. Produce ambiguity.
                Dim ambiguousResults = ArrayBuilder(Of Symbol).GetInstance()

                If TypeOf Me.Diagnostic Is AmbiguousSymbolDiagnostic Then
                    ambiguousResults.AddRange(DirectCast(Me.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols)
                Else
                    ambiguousResults.AddRange(Me.Symbols)
                End If

                If TypeOf other.Diagnostic Is AmbiguousSymbolDiagnostic Then
                    ambiguousResults.AddRange(DirectCast(other.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols)
                Else
                    ambiguousResults.AddRange(other.Symbols)
                End If

                SetFrom(ambiguousResults.ToImmutableAndFree(), generateAmbiguityDiagnostic)
            ElseIf other.Kind > Me.Kind Then
                SetFrom(other)
            Else
                Return
            End If
        End Sub

        ' Merge two results, returning the best. If there are
        ' multiple viable results, instead produce an ambiguity between all of them.
        Public Sub MergeAmbiguous(other As SingleLookupResult,
                                  generateAmbiguityDiagnostic As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic))
            If Me.IsGoodOrAmbiguous AndAlso other.IsGoodOrAmbiguous Then
                ' Two viable results. Produce ambiguity.
                Dim ambiguousResults = ArrayBuilder(Of Symbol).GetInstance()

                If TypeOf Me.Diagnostic Is AmbiguousSymbolDiagnostic Then
                    ambiguousResults.AddRange(DirectCast(Me.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols)
                Else
                    ambiguousResults.AddRange(Me.Symbols)
                End If

                If TypeOf other.Diagnostic Is AmbiguousSymbolDiagnostic Then
                    ambiguousResults.AddRange(DirectCast(other.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols)
                Else
                    ambiguousResults.Add(other.Symbol)
                End If

                SetFrom(ambiguousResults.ToImmutableAndFree(), generateAmbiguityDiagnostic)
            ElseIf other.Kind > Me.Kind Then
                SetFrom(other)
            Else
                Return
            End If
        End Sub

        ' Determine if two symbols can overload each other.
        ' Two symbols that are both methods or both properties can overload each other.
        Public Shared Function CanOverload(sym1 As Symbol, sym2 As Symbol) As Boolean
            Return sym1.Kind = sym2.Kind AndAlso sym1.IsOverloadable AndAlso sym2.IsOverloadable
        End Function

        ' Determine if all property or method symbols in the current result have the "Overloads" modifier.
        ' The "Overloads" modifier is the opposite of the "Shadows" modifier.
        Public Function AllSymbolsHaveOverloads() As Boolean
            For Each sym In Symbols
                Select Case sym.Kind
                    Case SymbolKind.Method
                        If Not DirectCast(sym, MethodSymbol).IsOverloads Then
                            Return False
                        End If
                    Case SymbolKind.Property
                        If Not DirectCast(sym, PropertySymbol).IsOverloads Then
                            Return False
                        End If
                End Select
            Next

            Return True
        End Function


        ' Merge two results, returning the best. If there are
        ' multiple viable results, either produce a result with both symbols if they can overload each other,
        ' or use the current one..
        Public Sub MergeOverloadedOrPrioritizedExtensionMethods(other As SingleLookupResult)
            Debug.Assert(Not Me.IsAmbiguous AndAlso Not other.IsAmbiguous)
            Debug.Assert(other.Symbol.IsReducedExtensionMethod())
            Debug.Assert(Not Me.HasSymbol OrElse Me.Symbols(0).IsReducedExtensionMethod())

            If Me.IsGood AndAlso other.IsGood Then
                _symList.Add(other.Symbol)
            ElseIf other.Kind > Me.Kind Then
                SetFrom(other)
            ElseIf Me.Kind <> LookupResultKind.Inaccessible OrElse Me.Kind > other.Kind Then
                Return
            Else
                _symList.Add(other.Symbol)
            End If
        End Sub

        ' Merge two results, returning the best. If there are
        ' multiple viable results, either produce a result with both symbols if they can overload each other,
        ' or use the current.
        ' If the "checkIfCurrentHasOverloads" is True, then we only overload if every symbol in our current result has "Overloads" modifier; otherwise
        ' we overload regardless of the modifier.
        Public Sub MergeOverloadedOrPrioritized(other As LookupResult, checkIfCurrentHasOverloads As Boolean)
            If Me.IsGoodOrAmbiguous AndAlso other.IsGoodOrAmbiguous Then
                If Me.IsGood AndAlso other.IsGood Then
                    ' Two viable results. Either they can overload each other, or we need to produce an ambiguity.
                    If CanOverload(Me.Symbols(0), other.Symbols(0)) AndAlso (Not checkIfCurrentHasOverloads OrElse AllSymbolsHaveOverloads()) Then
                        _symList.AddRange(other.Symbols)
                    Else
                        ' They don't overload each other. Just hide.
                        Return
                    End If
                Else
                    Debug.Assert(Me.IsAmbiguous OrElse other.IsAmbiguous)
                    ' Stick with the current result.
                    ' Good result from derived class shouldn't be overridden by an ambiguous result from the base class.
                    ' Ambiguous result from derived class shouldn't be overridden by a good result from the base class.
                    Return
                End If
            ElseIf other.Kind > Me.Kind Then
                SetFrom(other)
            ElseIf Me.Kind <> LookupResultKind.Inaccessible OrElse Me.Kind > other.Kind OrElse
                Not CanOverload(Me.Symbols(0), other.Symbols(0)) OrElse
                (checkIfCurrentHasOverloads AndAlso Not AllSymbolsHaveOverloads()) Then
                Return
            Else
                _symList.AddRange(other.Symbols)
            End If
        End Sub

        ''' <summary>
        ''' Merge two results, returning the best. If there are
        ''' multiple viable results, either produce a result with both symbols if they 
        ''' can overload each other, or use the current.
        ''' </summary>
        ''' <param name="other">Other result.</param>
        ''' <param name="checkIfCurrentHasOverloads">
        ''' If the checkIfCurrentHasOverloads is True, then we only overload if every symbol in 
        ''' our current result has "Overloads" modifier; otherwise we overload 
        ''' regardless of the modifier.
        ''' </param>
        ''' <remarks></remarks>
        Public Sub MergeOverloadedOrPrioritized(other As SingleLookupResult, checkIfCurrentHasOverloads As Boolean)
            If Me.IsGoodOrAmbiguous AndAlso other.IsGoodOrAmbiguous Then
                If Me.IsGood AndAlso other.IsGood Then
                    ' Two viable results. Either they can overload each other, or we need to produce an ambiguity.
                    If CanOverload(Me.Symbols(0), other.Symbol) AndAlso (Not checkIfCurrentHasOverloads OrElse AllSymbolsHaveOverloads()) Then
                        _symList.Add(other.Symbol)
                    Else
                        ' They don't overload each other. Just hide.
                        Return
                    End If
                Else
                    Debug.Assert(Me.IsAmbiguous OrElse other.IsAmbiguous)
                    ' Stick with the current result.
                    ' Good result from derived class shouldn't be overridden by an ambiguous result from the base class.
                    ' Ambiguous result from derived class shouldn't be overridden by a good result from the base class.
                    Return
                End If
            ElseIf other.Kind > Me.Kind Then
                SetFrom(other)
            ElseIf Me.Kind <> LookupResultKind.Inaccessible OrElse Me.Kind > other.Kind OrElse
                Not CanOverload(Me.Symbols(0), other.Symbol) OrElse
                (checkIfCurrentHasOverloads AndAlso Not AllSymbolsHaveOverloads()) Then
                Return
            Else
                _symList.Add(other.Symbol)
            End If
        End Sub

        ' Merge two results, returning the best. If there are
        ' multiple viable results, either produce a result with both symbols if they can overload each other,
        ' or produce an ambiguity error otherwise.
        Public Sub MergeMembersOfTheSameType(other As SingleLookupResult, imported As Boolean)
            Debug.Assert(Not Me.HasSymbol OrElse other.Symbol Is Nothing OrElse TypeSymbol.Equals(Me.Symbols(0).ContainingType, other.Symbol.ContainingType, TypeCompareKind.ConsiderEverything))
            Debug.Assert(Not other.IsAmbiguous)

            If Me.IsGoodOrAmbiguous AndAlso other.IsGood Then
                ' Two viable results. Either they can overload each other, or we need to produce an ambiguity.
                MergeOverloadedOrAmbiguousInTheSameType(other, imported)
            ElseIf other.Kind > Me.Kind Then
                SetFrom(other)
            ElseIf Me.Kind <> LookupResultKind.Inaccessible OrElse Me.Kind > other.Kind Then
                Return
            ElseIf Not CanOverload(Me.Symbols(0), other.Symbol) Then
                Debug.Assert(Me.Kind = LookupResultKind.Inaccessible)
                Debug.Assert(Me.Kind = other.Kind)
                If Me.Symbols.All(Function(candidate, otherSymbol) candidate.DeclaredAccessibility < otherSymbol.DeclaredAccessibility, other.Symbol) Then
                    SetFrom(other)
                End If
            Else
                _symList.Add(other.Symbol)
            End If
        End Sub

        Private Sub MergeOverloadedOrAmbiguousInTheSameType(other As SingleLookupResult, imported As Boolean)
            Debug.Assert(Me.IsGoodOrAmbiguous AndAlso other.IsGood)

            If Me.IsGood Then
                If CanOverload(Me.Symbols(0), other.Symbol) Then
                    _symList.Add(other.Symbol)
                    Return
                End If

                If imported Then
                    ' Prefer more accessible symbols. 
                    Dim lost As Integer = 0
                    Dim ambiguous As Integer = 0
                    Dim otherLost As Boolean = False

                    Dim i As Integer
                    For i = 0 To _symList.Count - 1
                        Dim accessibilityCmp As Integer = CompareAccessibilityOfSymbolsConflictingInSameContainer(_symList(i), other.Symbol)

                        If accessibilityCmp = 0 Then
                            ambiguous += 1
                        ElseIf accessibilityCmp < 0 Then
                            lost += 1
                            _symList(i) = Nothing
                        Else
                            otherLost = True
                        End If
                    Next

                    If lost = _symList.Count Then
                        Debug.Assert(Not otherLost AndAlso ambiguous = 0)
                        SetFrom(other)
                        Return
                    End If

                    If otherLost Then
                        Debug.Assert(lost + ambiguous < _symList.Count)

                        ' Remove ambiguous from the result, because they lost as well.
                        If ambiguous > 0 Then
                            lost += ambiguous
                            ambiguous = RemoveAmbiguousSymbols(other.Symbol, ambiguous)
                        End If

                        Debug.Assert(ambiguous = 0)
                    Else
                        Debug.Assert(ambiguous = _symList.Count - lost)
                    End If

                    ' Compact the array of symbols
                    CompactSymbols(lost)
                    Debug.Assert(_symList.Count > 0)

                    If otherLost Then
                        Return
                    End If

                    ' As a special case, we allow conflicting enum members imported from
                    ' metadata If they have the same value, and take the first
                    If _symList.Count = 1 AndAlso ambiguous = 1 AndAlso AreEquivalentEnumConstants(_symList(0), other.Symbol) Then
                        Return
                    End If
                End If

            ElseIf imported Then
                Debug.Assert(Me.IsAmbiguous)
                ' This function guarantees that accessibility of all ambiguous symbols,
                ' even those dropped from the list by MergeAmbiguous is the same.
                ' So, it is sufficient to test Accessibility of the only symbol we have. 
                Dim accessibilityCmp As Integer = CompareAccessibilityOfSymbolsConflictingInSameContainer(_symList(0), other.Symbol)

                If accessibilityCmp < 0 Then
                    SetFrom(other)
                    Return
                ElseIf accessibilityCmp > 0 Then
                    Return
                End If
            End If

#If DEBUG Then
            If imported Then
                For i = 0 To _symList.Count - 1
                    Debug.Assert(_symList(i).DeclaredAccessibility = other.Symbol.DeclaredAccessibility)
                Next
            End If
#End If
            ' We were unable to resolve the ambiguity
            MergeAmbiguous(other, s_ambiguousInTypeError)
        End Sub

        Private Shared Function AreEquivalentEnumConstants(symbol1 As Symbol, symbol2 As Symbol) As Boolean
            Debug.Assert(TypeSymbol.Equals(symbol1.ContainingType, symbol2.ContainingType, TypeCompareKind.ConsiderEverything))
            If symbol1.Kind <> SymbolKind.Field OrElse symbol2.Kind <> SymbolKind.Field OrElse symbol1.ContainingType.TypeKind <> TypeKind.Enum Then
                Return False
            End If
            Dim f1 = DirectCast(symbol1, FieldSymbol)
            Dim f2 = DirectCast(symbol2, FieldSymbol)
            Return f1.ConstantValue IsNot Nothing AndAlso f1.ConstantValue.Equals(f2.ConstantValue)
        End Function

        ' Create a diagnostic for ambiguous names in the same type. This is typically not possible from source, only 
        ' from metadata.
        Private Shared ReadOnly s_ambiguousInTypeError As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic) =
            Function(syms As ImmutableArray(Of Symbol)) As AmbiguousSymbolDiagnostic
                Dim name As String = syms(0).Name
                Dim container As Symbol = syms(0).ContainingSymbol
                Debug.Assert(container IsNot Nothing, "ContainingSymbol property of the first ambiguous symbol cannot be Nothing")
                Dim containerKindText As String = container.GetKindText()
                Return New AmbiguousSymbolDiagnostic(ERRID.ERR_MetadataMembersAmbiguous3, syms, name, containerKindText, container)
            End Function

        Private Sub CompactSymbols(lost As Integer)
            If lost > 0 Then
                Dim i As Integer

                For i = 0 To _symList.Count - 1
                    If _symList(i) Is Nothing Then
                        Exit For
                    End If
                Next

                Debug.Assert(i < _symList.Count)
                Dim left As Integer = _symList.Count - lost

                If left > i Then
                    Dim j As Integer
                    For j = i + 1 To _symList.Count - 1
                        If _symList(j) IsNot Nothing Then
                            _symList(i) = _symList(j)
                            i += 1

                            If i = left Then
                                Exit For
                            End If
                        End If
                    Next
#If DEBUG Then
                    For j = j + 1 To _symList.Count - 1
                        Debug.Assert(_symList(j) Is Nothing)
                    Next
#End If
                End If
                Debug.Assert(i = left)
                _symList.Clip(left)
            End If
        End Sub

        Private Function RemoveAmbiguousSymbols(other As Symbol, ambiguous As Integer) As Integer
            Dim i As Integer

            For i = 0 To _symList.Count - 1
                If _symList(i) IsNot Nothing AndAlso _symList(i).DeclaredAccessibility = other.DeclaredAccessibility Then
                    _symList(i) = Nothing
                    ambiguous -= 1

                    If ambiguous = 0 Then
                        Exit For
                    End If
                End If
            Next

#If DEBUG Then
            For i = i + 1 To _symList.Count - 1
                Debug.Assert(_symList(i) Is Nothing OrElse _symList(i).DeclaredAccessibility <> other.DeclaredAccessibility)
            Next
#End If
            Return ambiguous
        End Function

        ''' <summary>
        ''' Returns: negative value - when first lost, 0 - when neither lost, > 0 - when second lost.
        ''' </summary>
        Public Shared Function CompareAccessibilityOfSymbolsConflictingInSameContainer(
            first As Symbol,
            second As Symbol
        ) As Integer
            If first.DeclaredAccessibility = second.DeclaredAccessibility Then
                Return 0
            End If

            ' Note, Dev10 logic is - if containing assembly has friends, Protected is preferred over Friend, 
            ' otherwise Friend is preferred over Protected.
            ' That logic (the dependency on presence of InternalsVisibleTo attribute) seems unnecessary complicated.
            ' If there are friends, we prefer Protected. If there are no friends, we prefer Friend, but Friend members
            ' are going to be inaccessible (there are no friend assemblies) and will be discarded by the name lookup,
            ' leaving Protected members uncontested and thus available. Effectively, Protected always wins over Friend,
            ' regardless of presence of InternalsVisibleTo attributes. Am I missing something? 
            ' For now implementing simple logic - Protected is better than Friend.
            If first.DeclaredAccessibility < second.DeclaredAccessibility Then
                If first.DeclaredAccessibility = Accessibility.Protected AndAlso
                   second.DeclaredAccessibility = Accessibility.Friend Then
                    Return 1
                Else
                    Return -1
                End If
            Else
                If second.DeclaredAccessibility = Accessibility.Protected AndAlso
                   first.DeclaredAccessibility = Accessibility.Friend Then
                    Return -1
                Else
                    Return 1
                End If
            End If
        End Function

        Public Sub MergeMembersOfTheSameNamespace(other As SingleLookupResult, sourceModule As ModuleSymbol, options As LookupOptions)

            Dim resolution As Integer = ResolveAmbiguityInTheSameNamespace(other, sourceModule, options)

            If resolution > 0 Then
                Return
            ElseIf resolution < 0 Then
                SetFrom(other)
                Return
            End If

            MergeAmbiguous(other, s_ambiguousInNSError)
        End Sub

        Private Enum SymbolLocation
            FromSourceModule
            FromReferencedAssembly
            FromCorLibrary
        End Enum

        Private Shared Function GetSymbolLocation(sym As Symbol, sourceModule As ModuleSymbol, options As LookupOptions) As SymbolLocation
            ' Dev10 pays attention to the fact that the [sym] refers to a namespace
            ' and the namespace has a declaration in source. This needs some special handling for merged namespaces.
            If sym.Kind = SymbolKind.Namespace Then
                Return If(DirectCast(sym, NamespaceSymbol).IsDeclaredInSourceModule(sourceModule),
                    SymbolLocation.FromSourceModule,
                    SymbolLocation.FromReferencedAssembly)
            End If

            If sym.ContainingModule Is sourceModule Then
                Return SymbolLocation.FromSourceModule
            End If

            If sourceModule.DeclaringCompilation.Options.IgnoreCorLibraryDuplicatedTypes Then
                ' Ignore duplicate types from the cor library if necessary.
                ' (Specifically the framework assemblies loaded at runtime in
                ' the EE may contain types also available from mscorlib.dll.)
                Dim containingAssembly = sym.ContainingAssembly
                If containingAssembly Is containingAssembly.CorLibrary Then
                    Return SymbolLocation.FromCorLibrary
                End If
            End If

            Return SymbolLocation.FromReferencedAssembly
        End Function

        ''' <summary>
        ''' Returns: negative value - when current lost, 0 - when neither lost, > 0 - when other lost.
        ''' </summary>
        Private Function ResolveAmbiguityInTheSameNamespace(other As SingleLookupResult, sourceModule As ModuleSymbol, options As LookupOptions) As Integer
            Debug.Assert(Not other.IsAmbiguous)

            ' Symbols in source take priority over symbols in a referenced assembly.
            If other.StopFurtherLookup AndAlso
               Me.StopFurtherLookup AndAlso Me.Symbols.Count > 0 Then

                Dim currentLocation = GetSymbolLocation(other.Symbol, sourceModule, options)
                Dim contenderLocation = GetSymbolLocation(Me.Symbols(0), sourceModule, options)
                Dim diff = currentLocation - contenderLocation
                If diff <> 0 Then
                    Return diff
                End If
            End If

            If other.IsGood Then
                If Me.IsGood Then
                    Debug.Assert(Me.HasSingleSymbol)
                    Debug.Assert(Me.Symbols(0).Kind <> SymbolKind.Namespace OrElse other.Symbol.Kind <> SymbolKind.Namespace) ' namespaces are supposed to be merged
                    Return ResolveAmbiguityInTheSameNamespace(Me.Symbols(0), other.Symbol, sourceModule)

                ElseIf Me.IsAmbiguous Then
                    ' Check to see if all symbols in the ambiguous result are types, which lose to the new symbol.
                    For Each candidate In DirectCast(Me.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols
                        Debug.Assert(candidate.Kind <> SymbolKind.Namespace OrElse other.Symbol.Kind <> SymbolKind.Namespace) ' namespaces are supposed to be merged
                        If candidate.Kind = SymbolKind.Namespace Then
                            Return 0 ' namespace never loses
                        ElseIf ResolveAmbiguityInTheSameNamespace(candidate, other.Symbol, sourceModule) >= 0 Then
                            Return 0
                        End If
                    Next

                    Return -1
                End If
            End If

            Return 0
        End Function

        ''' <summary>
        ''' Returns: negative value - when first lost, 0 - when neither lost, > 0 - when second lost.
        ''' </summary>
        Private Shared Function ResolveAmbiguityInTheSameNamespace(first As Symbol, second As Symbol, sourceModule As ModuleSymbol) As Integer
            ' If both symbols are from the same container, which could happen in metadata,
            ' prefer most accessible.
            If first.ContainingSymbol Is second.ContainingSymbol Then
                If first.ContainingModule Is sourceModule Then
                    Return 0
                End If

                Return CompareAccessibilityOfSymbolsConflictingInSameContainer(first, second)
            Else
                ' This needs special handling because containing namespace of a merged namespace symbol is a merged namespace symbol.
                ' So, condition above will fail.
                Debug.Assert(first.Kind <> SymbolKind.Namespace OrElse second.Kind <> SymbolKind.Namespace) ' namespaces are supposed to be merged

                ' This is a conflict of namespace and non-namespace symbol. Having a non-namespace symbol 
                ' defined in the source module along with namespace symbol leads to all types in the namespace 
                ' being unreachable because of an ambiguity error (Return 0 below). 
                '
                ' Conditions 'first.IsEmbedded = second.IsEmbedded' below make sure that if an embedded 
                ' namespace like Microsoft.VisualBasic conflicts with user defined type Microsoft.VisualBasic, 
                ' namespace will win making possible to access embedded types via direct reference, 
                ' such as Microsoft.VisualBasic.Embedded

                If first.Kind = SymbolKind.Namespace Then
                    If second.ContainingModule Is sourceModule AndAlso first.IsEmbedded = second.IsEmbedded Then
                        Return 0
                    End If

                    Return ResolveAmbiguityBetweenTypeAndMergedNamespaceInTheSameNamespace(DirectCast(first, NamespaceSymbol), second)
                ElseIf second.Kind = SymbolKind.Namespace Then
                    If first.ContainingModule Is sourceModule AndAlso first.IsEmbedded = second.IsEmbedded Then
                        Return 0
                    End If

                    Return (-1) * ResolveAmbiguityBetweenTypeAndMergedNamespaceInTheSameNamespace(DirectCast(second, NamespaceSymbol), first)
                End If

                Return 0
            End If
        End Function

        ''' <summary>
        ''' Returns: negative value - when namespace lost, 0 - when neither lost, > 0 - when type lost.
        ''' </summary>
        Private Shared Function ResolveAmbiguityBetweenTypeAndMergedNamespaceInTheSameNamespace(
            possiblyMergedNamespace As NamespaceSymbol,
            type As Symbol
        ) As Integer
            Debug.Assert(possiblyMergedNamespace.DeclaredAccessibility = Accessibility.Public)
            Debug.Assert(type.Kind = SymbolKind.NamedType)

            If type.DeclaredAccessibility < Accessibility.Public AndAlso
               possiblyMergedNamespace.Extent.Kind <> NamespaceKind.Module Then
                ' Namespace should be preferred over a non-public type in the same declaring container. But since the namespace symbol
                ' we have is a merged one, we need to do extra work here to figure out if this is the case.
                For Each sibling In DirectCast(type.ContainingSymbol, NamespaceSymbol).GetMembers(type.Name)
                    If sibling.Kind = SymbolKind.Namespace Then
                        ' namespace is better
                        Return 1
                    End If
                Next
            End If

            Return 0
        End Function

        ' Create a diagnostic for ambiguous names in a namespace
        Private Shared ReadOnly s_ambiguousInNSError As Func(Of ImmutableArray(Of Symbol), AmbiguousSymbolDiagnostic) =
            Function(syms As ImmutableArray(Of Symbol)) As AmbiguousSymbolDiagnostic
                Dim container As Symbol = syms(0).ContainingSymbol
                If container.Name.Length > 0 Then
                    Dim containers = syms.Select(Function(sym) sym.ContainingSymbol).
                                     GroupBy(Function(c) c.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat), IdentifierComparison.Comparer).
                                     OrderBy(Function(group) group.Key, IdentifierComparison.Comparer).
                                     Select(Function(group) group.First())

                    If containers.Skip(1).Any() Then
                        Return New AmbiguousSymbolDiagnostic(ERRID.ERR_AmbiguousInNamespaces2, syms, syms(0).Name, New FormattedSymbolList(containers))
                    Else
                        Return New AmbiguousSymbolDiagnostic(ERRID.ERR_AmbiguousInNamespace2, syms, syms(0).Name, container)
                    End If
                Else
                    Return New AmbiguousSymbolDiagnostic(ERRID.ERR_AmbiguousInUnnamedNamespace1, syms, syms(0).Name)
                End If
            End Function

        ''' <summary>
        ''' Replace the symbol replaced with a new one, but the kind
        ''' and diagnostics retained from the current result. Typically used when constructing
        ''' a type from a symbols and type arguments.
        ''' </summary>
        Public Sub ReplaceSymbol(newSym As Symbol)
            _symList.Clear()
            _symList.Add(newSym)
        End Sub

        ' Return the lowest non-empty result kind.
        ' However, if one resultKind is Empty and other is Good, we will return LookupResultKind.Empty
        Friend Shared Function WorseResultKind(resultKind1 As LookupResultKind, resultKind2 As LookupResultKind) As LookupResultKind
            If resultKind1 = LookupResultKind.Empty Then
                Return If(resultKind2 = LookupResultKind.Good, LookupResultKind.Empty, resultKind2)
            End If

            If resultKind2 = LookupResultKind.Empty Then
                Return If(resultKind1 = LookupResultKind.Good, LookupResultKind.Empty, resultKind1)
            End If

            If resultKind1 < resultKind2 Then
                Return resultKind1
            Else
                Return resultKind2
            End If
        End Function

    End Class

End Namespace
