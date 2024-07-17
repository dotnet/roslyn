' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
    ''' <summary>
    ''' Tuples can be represented using tuple syntax and be given
    ''' names. However, the underlying representation for tuples unifies
    ''' to a single underlying tuple type, System.ValueTuple. Since the
    ''' names aren't part of the underlying tuple type they have to be
    ''' recorded somewhere else.
    ''' 
    ''' Roslyn records tuple names in an attribute: the
    ''' TupleElementNamesAttribute. The attribute contains a single string
    ''' array which records the names of the tuple elements in a pre-order
    ''' depth-first traversal. If the type contains nested parameters,
    ''' they are also recorded in a pre-order depth-first traversal.
    ''' <see cref="DecodeTupleTypesIfApplicable(TypeSymbol, EntityHandle, PEModuleSymbol)"/>
    ''' can be used to extract tuple names and types from metadata and create
    ''' a <see cref="TupleTypeSymbol"/> with attached names.
    ''' 
    ''' <example>
    ''' For instance, a method returning a tuple
    ''' 
    ''' <code>
    '''     Function M() As (x As Integer, y As Integer)
    ''' </code>
    '''
    ''' will be encoded using an attribute on the return type as follows
    ''' 
    ''' <code>
    '''     &lt; return: TupleElementNamesAttribute({ "x", "y" }) >
    '''     Function M() As System.ValueTuple(Of Integer, Integer)
    ''' </code>
    ''' </example>
    ''' 
    ''' <example>
    ''' For nested type parameters, we expand the tuple names in a pre-order
    ''' traversal:
    ''' 
    ''' <code>
    '''     Class C 
    '''          Inherits BaseType(Of (e3 As (e1 As Integer, e2 As Integer), e4 As Integer))
    ''' </code>
    '''
    ''' becomes
    ''' 
    ''' <code>
    '''     &lt; TupleElementNamesAttribute({ "e3", "e4", "e1", "e2" }) >
    '''     Class C 
    '''         Inherits BaseType(of System.ValueTuple(of System.ValueTuple(Of Integer, Integer), Integer)
    ''' </code>
    ''' </example>
    ''' </summary>
    Friend Structure TupleTypeDecoder
        Private ReadOnly _elementNames As ImmutableArray(Of String)
        ' Keep track of how many names we've "used" during decoding. Starts at
        ' the back of the array and moves forward.
        Private _namesIndex As Integer

        Private _foundUsableErrorType As Boolean
        Private _decodingFailed As Boolean

        Private Sub New(elementNames As ImmutableArray(Of String))
            _elementNames = elementNames
            _namesIndex = If(elementNames.IsDefault, 0, elementNames.Length)
            _foundUsableErrorType = False
            _decodingFailed = False
        End Sub

        Public Shared Function DecodeTupleTypesIfApplicable(
            metadataType As TypeSymbol,
            targetSymbolToken As EntityHandle,
            containingModule As PEModuleSymbol) As TypeSymbol

            Dim elementNames As ImmutableArray(Of String) = Nothing
            Dim hasTupleElementNamesAttribute = containingModule.Module.HasTupleElementNamesAttribute(targetSymbolToken, elementNames)

            ' If we have the TupleElementNamesAttribute, but no names, that's
            ' bad metadata
            If hasTupleElementNamesAttribute AndAlso elementNames.IsDefaultOrEmpty Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Return DecodeTupleTypesInternal(metadataType, elementNames, hasTupleElementNamesAttribute)
        End Function

        Public Shared Function DecodeTupleTypesIfApplicable(
             metadataType As TypeSymbol,
             elementNames As ImmutableArray(Of String)) As TypeSymbol

            Return DecodeTupleTypesInternal(metadataType, elementNames, hasTupleElementNamesAttribute:=Not elementNames.IsDefaultOrEmpty)
        End Function

        Private Shared Function DecodeTupleTypesInternal(metadataType As TypeSymbol,
                                                         elementNames As ImmutableArray(Of String),
                                                         hasTupleElementNamesAttribute As Boolean) As TypeSymbol

            Debug.Assert(metadataType IsNot Nothing)

            Dim decoder = New TupleTypeDecoder(elementNames)

            Dim decoded = decoder.DecodeType(metadataType)

            If Not decoder._decodingFailed Then
                If Not hasTupleElementNamesAttribute OrElse decoder._namesIndex = 0 Then
                    Return decoded
                End If
            End If

            ' If not all of the names have been used, the metadata is bad

            If decoder._foundUsableErrorType Then
                Return metadataType
            End If

            ' Bad metadata
            Return New UnsupportedMetadataTypeSymbol()
        End Function

        Private Function DecodeType(type As TypeSymbol) As TypeSymbol
            Select Case type.Kind
                Case SymbolKind.ErrorType
                    _foundUsableErrorType = True
                    Return type

                Case SymbolKind.DynamicType,
                    SymbolKind.TypeParameter,
                    SymbolKind.PointerType

                    Return type

                Case SymbolKind.NamedType
                    ' We may have a tuple type from a substituted type symbol,
                    ' but it will be missing names from metadata, so we'll
                    ' need to re-create the type.
                    '
                    ' Consider the declaration
                    '
                    '      class C : Inherits BaseType(of (x As Integer, y As Integer))
                    '
                    ' The process for decoding tuples in looks at the BaseType, calls
                    ' DecodeOrThrow, then passes the decoded type to the TupleTypeDecoder.
                    ' However, DecodeOrThrow uses the AbstractTypeMap to construct a
                    ' SubstitutedTypeSymbol, which eagerly converts tuple-compatible
                    ' types to TupleTypeSymbols. Thus, by the time we get to the Decoder
                    ' all metadata instances of System.ValueTuple will have been
                    '  replaced with TupleTypeSymbols without names.
                    ' 
                    ' Rather than fixing up after-the-fact it's possible that we could
                    ' flow up a SubstituteWith/Without tuple unification to the top level
                    ' of the type map and change DecodeOrThrow to call into the substitution
                    ' without unification instead.
                    Return If(type.IsTupleType,
                             DecodeNamedType(type.TupleUnderlyingType),
                             DecodeNamedType(DirectCast(type, NamedTypeSymbol)))

                Case SymbolKind.ArrayType
                    Return DecodeArrayType(DirectCast(type, ArrayTypeSymbol))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
            End Select
        End Function

        Private Function DecodeNamedType(type As NamedTypeSymbol) As NamedTypeSymbol
            ' First decode the type arguments
            Dim typeArgs = type.TypeArgumentsNoUseSiteDiagnostics
            Dim decodedArgs = DecodeTypeArguments(typeArgs)

            Dim decodedType = type

            ' Now check the container
            Dim containingType = type.ContainingType
            Dim decodedContainingType As NamedTypeSymbol = Nothing

            If containingType IsNot Nothing AndAlso containingType.IsGenericType Then
                decodedContainingType = DecodeNamedType(containingType)
                Debug.Assert(decodedContainingType.IsGenericType)
            Else
                decodedContainingType = containingType
            End If

            ' Replace the type if necessary
            Dim containerChanged = decodedContainingType IsNot containingType
            Dim typeArgsChanged = typeArgs <> decodedArgs
            If typeArgsChanged OrElse containerChanged Then
                Dim newTypeArgs = If(type.HasTypeArgumentsCustomModifiers,
                                     decodedArgs.SelectAsArray(map:=Function(t, i, m) New TypeWithModifiers(t, m.GetTypeArgumentCustomModifiers(i)), arg:=type),
                                     decodedArgs.SelectAsArray(Function(t) New TypeWithModifiers(t, Nothing)))

                If containerChanged Then
                    decodedType = decodedType.OriginalDefinition.AsMember(decodedContainingType)
                    ' If the type is nested, e.g. Outer(of T).Inner(of V), then Inner is definitely
                    ' not a tuple, since we know all tuple-compatible types (System.ValueTuple)
                    ' are not nested types. Thus, it is safe to return without checking if
                    ' Inner is a tuple.
                    Return If(decodedType.TypeParameters.IsEmpty,
                                decodedType,
                                Construct(decodedType, newTypeArgs))
                End If

                decodedType = Construct(type, newTypeArgs)
            End If

            ' Now decode into a tuple, if it is one
            Dim tupleCardinality As Integer
            If decodedType.IsTupleCompatible(tupleCardinality) Then
                Dim elementNames = EatElementNamesIfAvailable(tupleCardinality)

                Debug.Assert(elementNames.IsDefault OrElse elementNames.Length = tupleCardinality)

                decodedType = TupleTypeSymbol.Create(decodedType, elementNames)
            End If

            Return decodedType
        End Function

        Private Shared Function Construct(type As NamedTypeSymbol, newTypeArgs As ImmutableArray(Of TypeWithModifiers)) As NamedTypeSymbol
            Dim definition = type.OriginalDefinition

            Dim parentSubst = type.ConstructedFrom.ContainingType?.TypeSubstitution
            Dim subst As TypeSubstitution
            If parentSubst IsNot Nothing Then
                subst = TypeSubstitution.Create(parentSubst, definition, newTypeArgs, False)
            Else
                subst = TypeSubstitution.Create(definition, definition.TypeParameters, newTypeArgs, False)
            End If

            Return definition.Construct(subst)
        End Function

        Private Function DecodeTypeArguments(typeArgs As ImmutableArray(Of TypeSymbol)) As ImmutableArray(Of TypeSymbol)
            If typeArgs.IsEmpty Then
                Return typeArgs
            End If

            Dim decodedArgs = ArrayBuilder(Of TypeSymbol).GetInstance(typeArgs.Length)
            Dim anyDecoded = False
            ' Visit the type arguments in reverse
            For i As Integer = typeArgs.Length - 1 To 0 Step -1
                Dim typeArg = typeArgs(i)
                Dim decoded = DecodeType(typeArg)
                anyDecoded = anyDecoded Or decoded IsNot typeArg
                decodedArgs.Add(decoded)
            Next

            If Not anyDecoded Then
                decodedArgs.Free()
                Return typeArgs
            End If

            decodedArgs.ReverseContents()
            Return decodedArgs.ToImmutableAndFree()
        End Function

        Private Function DecodeArrayType(type As ArrayTypeSymbol) As ArrayTypeSymbol
            Dim decodedElementType = DecodeType(type.ElementType)
            Return If(decodedElementType Is type.ElementType, type, type.WithElementType(decodedElementType))
        End Function

        Private Function EatElementNamesIfAvailable(numberOfElements As Integer) As ImmutableArray(Of String)
            Debug.Assert(numberOfElements > 0)

            ' If we don't have any element names there's nothing to eat
            If _elementNames.IsDefault Then
                Return _elementNames
            End If

            ' We've gone past the end of the names -- bad metadata
            If numberOfElements > _namesIndex Then
                ' We'll want to continue decoding without consuming more names to see if there are any error types
                _namesIndex = 0
                _decodingFailed = True
                Return Nothing
            End If

            ' Check to see if all the elements are null
            Dim start = _namesIndex - numberOfElements
            Dim allNull = True
            _namesIndex = start

            For i As Integer = 0 To numberOfElements - 1
                If _elementNames(start + i) IsNot Nothing Then
                    allNull = False
                    Exit For
                End If
            Next

            If allNull Then
                Return Nothing
            End If

            Dim builder = ArrayBuilder(Of String).GetInstance(numberOfElements)

            For i As Integer = 0 To numberOfElements - 1
                builder.Add(_elementNames(start + i))
            Next

            Return builder.ToImmutableAndFree()
        End Function
    End Structure
End Namespace
