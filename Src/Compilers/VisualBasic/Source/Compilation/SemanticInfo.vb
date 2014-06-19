Imports System.Diagnostics
Imports Roslyn.Compilers.Common

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' Summarizes the semantic information about a syntax node. 
    ''' </summary>
    Friend Class SemanticInfo
        Implements ISemanticInfo

        ' should be best guess if there is one, or error type if none.
        Private ReadOnly _type As TypeSymbol

        ''' <summary>
        ''' The type of the expression represented by the syntax node. For expressions that do not
        ''' have a type, null is returned. If the type could not be determined due to an error, than
        ''' an object derived from ErrorTypeSymbol is returned.
        ''' </summary>
        Friend ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        Private ReadOnly _convertedType As TypeSymbol

        ''' <summary>
        ''' The type of the expression after it has undergone an implicit conversion. If the type
        ''' did not undergo an implicit conversion, returns the same as Type.
        ''' </summary>
        Friend ReadOnly Property ConvertedType As TypeSymbol
            Get
                Return _convertedType
            End Get
        End Property

        Private ReadOnly _implicitConversion As Conversion

        ''' <summary>
        ''' If the expression underwent an implicit conversion, return information about that
        ''' conversion. Otherwise, returns an identity conversion.
        ''' </summary>
        Friend ReadOnly Property ImplicitConversion As Conversion
            Get
                Return _implicitConversion
            End Get
        End Property

        ' The symbols resulting from binding, and what kind of binding problem might have resulted.
        Private ReadOnly _symbols As ReadOnlyArray(Of Symbol)
        Private ReadOnly _resultKind As LookupResultKind

        Friend ReadOnly Property AllSymbols As ReadOnlyArray(Of Symbol)
            Get
                Return _symbols
            End Get
        End Property

        Friend ReadOnly Property ResultKind As LookupResultKind
            Get
                Return _resultKind
            End Get
        End Property

        ''' <summary>
        ''' The symbol that was referred to by the syntax node, if any. Returns null if the given
        ''' expression did not bind successfully to a single symbol. If null is returned, it may
        ''' still be that case that we have one or more "best guesses" as to what symbol was
        ''' intended. These best guesses are available via the CandidateSymbols property.
        ''' </summary>
        Friend ReadOnly Property Symbol As Symbol
            Get
                If _resultKind = LookupResultKind.Good AndAlso _symbols.Count > 0 Then
                    Debug.Assert(_symbols.Count = 1)
                    Return _symbols(0)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ''' <summary>
        ''' If the expression did not successfully resolve to a symbol, but there were one or more
        ''' symbols that may have been considered but discarded, this property returns those
        ''' symbols. The reason that the symbols did not successfully resolve to a symbol are
        ''' available in the CandidateReason property. For example, if the symbol was inaccessible,
        ''' ambiguous, or used in the wrong context.
        ''' </summary>
        Friend ReadOnly Property CandidateSymbols As ReadOnlyArray(Of Symbol)
            Get
                If _resultKind <> LookupResultKind.Good AndAlso _symbols.Count > 0 Then
                    Return _symbols
                Else
                    Return ReadOnlyArray(Of Symbol).Empty
                End If
            End Get
        End Property

        '''<summary>
        ''' If the expression did not successfully resolve to a symbol, but there were one or more
        ''' symbols that may have been considered but discarded, this property describes why those
        ''' symbol or symbols were not considered suitable.
        ''' </summary>
        Friend ReadOnly Property CandidateReason As CandidateReason
            Get
                Return If(_resultKind = LookupResultKind.Good, CandidateReason.None, _resultKind.ToCandidateReason())
            End Get
        End Property

        Private ReadOnly _memberGroup As ReadOnlyArray(Of Symbol)

        ''' <summary>
        ''' When getting information for a symbol that resolves to a method or property group, from which a
        ''' method or property is then chosen; the chosen method or property is present in Symbol; all methods in the
        ''' group that was consulted are placed in this property.
        ''' </summary>
        Friend ReadOnly Property MemberGroup As ReadOnlyArray(Of Symbol)
            Get
                Return _memberGroup
            End Get
        End Property

        Private ReadOnly _constantValue As ConstantValue

        ''' <summary>
        ''' Returns true if the expression is a compile-time constant. The value of the constant can
        ''' be obtained with the ConstantValue property.
        ''' </summary>
        Friend ReadOnly Property IsCompileTimeConstant As Boolean
            Get
                Return _constantValue IsNot Nothing AndAlso Not _constantValue.IsBad
            End Get
        End Property

        ''' <summary>
        ''' If IsCompileTimeConstant returns true, then returns the constant value of the field or
        ''' enum member. If IsCompileTimeConstant returns false, then returns null.
        ''' </summary>
        Friend ReadOnly Property ConstantValue As Object
            Get
                Return If(_constantValue Is Nothing, Nothing, _constantValue.Value)
            End Get
        End Property

        Friend Sub New(type As TypeSymbol,
                       conversion As Conversion,
                       convertedType As TypeSymbol,
                       symbols As ReadOnlyArray(Of Symbol),
                       resultKind As LookupResultKind,
                       memberGroup As ReadOnlyArray(Of Symbol),
                       constantValue As ConstantValue)

            ' TODO: C# has GetNonErrorGuess() call here.
            Me._type = GetPossibleGuessForErrorType(type)
            Me._convertedType = GetPossibleGuessForErrorType(convertedType)
            Me._symbols = symbols
            Me._implicitConversion = conversion
            Me._resultKind = resultKind
            If Not symbols.Any() Then
                Me._resultKind = LookupResultKind.Empty
            End If

            Me._memberGroup = memberGroup
            Me._constantValue = constantValue
        End Sub

        Public Shared ReadOnly None As SemanticInfo = New SemanticInfo(Type:=Nothing,
                                                                                       Conversion:=New Conversion(ConversionKind.Identity),
                                                                                       ConvertedType:=Nothing,
                                                                                       symbols:=ReadOnlyArray(Of Symbol).Empty,
                                                                                       ResultKind:=LookupResultKind.Empty,
                                                                                       MemberGroup:=ReadOnlyArray(Of Symbol).Empty,
                                                                                       ConstantValue:=Nothing)

        ''' <summary>
        ''' Guess the non-error type that the given type was intended to represent, or return
        ''' the type itself. If a single, non-ambiguous type is a guess-type inside the type symbol, 
        ''' return that; otherwise return the type itself (even if it is an error type).
        ''' </summary>
        Private Shared Function GetPossibleGuessForErrorType(type As TypeSymbol) As TypeSymbol
            Dim errorSymbol As ErrorTypeSymbol = TryCast(type, ErrorTypeSymbol)
            If errorSymbol Is Nothing Then
                Return type
            End If

            Dim nonErrorGuess = errorSymbol.NonErrorGuessType
            If nonErrorGuess Is Nothing Then
                Return type
            Else
                Return nonErrorGuess
            End If
        End Function
    End Class
End Namespace