' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' An ArrayTypeSymbol represents an array type, such as Integer() or Object(,).
    ''' </summary>
    Friend MustInherit Class ArrayTypeSymbol
        Inherits TypeSymbol
        Implements IArrayTypeSymbol

        ''' <summary>
        ''' Create a new ArrayTypeSymbol.
        ''' </summary>
        Friend Shared Function CreateVBArray(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), rank As Integer, compilation As VisualBasicCompilation) As ArrayTypeSymbol
            Return CreateVBArray(elementType, customModifiers, rank, compilation.Assembly)
        End Function

        ''' <summary>
        ''' Create a new ArrayTypeSymbol.
        ''' </summary>
        Friend Shared Function CreateVBArray(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), rank As Integer, declaringAssembly As AssemblySymbol) As ArrayTypeSymbol
            If rank = 1 Then
                Return CreateSZArray(elementType, customModifiers, declaringAssembly)
            End If

            Return CreateMDArray(elementType, customModifiers, rank, Nothing, Nothing, declaringAssembly)
        End Function

        Friend Shared Function CreateMDArray(
            elementType As TypeSymbol,
            customModifiers As ImmutableArray(Of CustomModifier),
            rank As Integer,
            sizes As ImmutableArray(Of Integer),
            lowerBounds As ImmutableArray(Of Integer),
            declaringAssembly As AssemblySymbol
        ) As ArrayTypeSymbol
            Dim systemArray = declaringAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Array)

            ' Optimize for most common case - no sizes and all dimensions are zero lower bound.
            If sizes.IsDefaultOrEmpty AndAlso lowerBounds.IsDefault Then
                Return New MDArray(elementType,
                               customModifiers,
                               rank,
                               systemArray)
            End If

            Return New MDArrayWithSizesAndBounds(elementType,
                                                 customModifiers,
                                                 rank,
                                                 sizes,
                                                 lowerBounds,
                                                 systemArray)
        End Function

        Friend Shared Function CreateSZArray(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), compilation As VisualBasicCompilation) As ArrayTypeSymbol
            Return CreateSZArray(elementType, customModifiers, compilation.Assembly)
        End Function

        Friend Shared Function CreateSZArray(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), declaringAssembly As AssemblySymbol) As ArrayTypeSymbol
            Return New SZArray(elementType,
                               customModifiers,
                               declaringAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Array),
                               GetSZArrayInterfaces(elementType, declaringAssembly))
        End Function

        Private Shared Function GetSZArrayInterfaces(elementType As TypeSymbol, declaringAssembly As AssemblySymbol) As ImmutableArray(Of NamedTypeSymbol)
            ' There are cases where the platform does contain the interfaces.
            ' So it is fine not to have them listed under the type
            Dim iListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IList_T)
            Dim iReadOnlyListOfT = declaringAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IReadOnlyList_T)

            If iListOfT.IsErrorType() Then
                If Not iReadOnlyListOfT.IsErrorType() Then
                    Return ImmutableArray.Create(iReadOnlyListOfT.Construct(elementType))
                End If
            ElseIf iReadOnlyListOfT.IsErrorType() Then
                Return ImmutableArray.Create(iListOfT.Construct(elementType))
            Else
                Return ImmutableArray.Create(iListOfT.Construct(elementType), iReadOnlyListOfT.Construct(elementType))
            End If

            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ''' <summary>
        ''' Returns the list of custom modifiers, if any, associated with the array.
        ''' </summary>
        Public MustOverride ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Returns the number of dimensions of this array. A regular single-dimensional array
        ''' has rank 1, a two-dimensional array has rank 2, etc.
        ''' </summary>
        Public MustOverride ReadOnly Property Rank As Integer

        ''' <summary>
        ''' Is this zero-based one-dimensional array, i.e. SZArray in CLR terms.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsSZArray As Boolean

        Friend Function HasSameShapeAs(other As ArrayTypeSymbol) As Boolean
            Return Rank = other.Rank AndAlso IsSZArray = other.IsSZArray
        End Function

        ''' <summary>
        ''' Specified sizes for dimensions, by position. The length can be less than <see cref="Rank"/>,
        ''' meaning that some trailing dimensions don't have the size specified.
        ''' The most common case is none of the dimensions have the size specified - an empty array is returned.
        ''' </summary>
        Friend Overridable ReadOnly Property Sizes As ImmutableArray(Of Integer)
            Get
                Return ImmutableArray(Of Integer).Empty
            End Get
        End Property

        ''' <summary>
        ''' Specified lower bounds for dimensions, by position. The length can be less than <see cref="Rank"/>,
        ''' meaning that some trailing dimensions don't have the lower bound specified.
        ''' The most common case is all dimensions are zero bound - a null array is returned in this case.
        ''' </summary>
        Friend Overridable ReadOnly Property LowerBounds As ImmutableArray(Of Integer)
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Note, <see cref="Rank"/> equality should be checked separately!!!
        ''' </summary>
        Friend Function HasSameSizesAndLowerBoundsAs(other As ArrayTypeSymbol) As Boolean
            If Me.Sizes.SequenceEqual(other.Sizes) Then
                Dim thisLowerBounds = Me.LowerBounds

                If thisLowerBounds.IsDefault Then
                    Return other.LowerBounds.IsDefault
                End If

                Dim otherLowerBounds = other.LowerBounds

                Return Not otherLowerBounds.IsDefault AndAlso thisLowerBounds.SequenceEqual(otherLowerBounds)
            End If

            Return False
        End Function

        ''' <summary>
        ''' Normally VB arrays have default sizes and lower bounds - sizes are not specified and all dimensions are zero bound.
        ''' This property should return false for any deviations.
        ''' </summary>
        Friend MustOverride ReadOnly Property HasDefaultSizesAndLowerBounds As Boolean

        ''' <summary>
        ''' Returns the type of the elements that are stored in this array.
        ''' </summary>
        Public MustOverride ReadOnly Property ElementType As TypeSymbol

        Friend MustOverride Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol

        ''' <summary>
        ''' Returns true if this type is known to be a reference type. It is never the case
        ''' that <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained
        ''' type parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        ''' </summary>
        ''' <returns>True</returns>
        Public Overrides ReadOnly Property IsReferenceType As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this type is known to be a value type. It is never the case
        ''' that <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained
        ''' type parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        ''' </summary>
        ''' <returns>False</returns>
        Public Overrides ReadOnly Property IsValueType As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Get all the members of this symbol.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that have a particular name.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        ''' no members with this name, returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that are types.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that are types that have a particular name, and any arity.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol with the given name. 
        ''' If this symbol has no type members with this name,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ''' <summary>
        ''' Get all the members of this symbol that are types that have a particular name and arity.
        ''' </summary>
        ''' <returns>An ImmutableArray containing all the types that are members of this symbol with the given name and arity.
        ''' If this symbol has no type members with this name and arity,
        ''' returns an empty ImmutableArray. Never returns Nothing.</returns>
        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ''' <summary>
        ''' Returns <see cref="SymbolKind"/> of the symbol.
        ''' </summary>
        ''' <returns><see cref="SymbolKind.ArrayType"/></returns>
        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.ArrayType
            End Get
        End Property

        ''' <summary>
        ''' Returns <see cref="TypeKind"/> of the symbol.
        ''' </summary>
        ''' <returns><see cref="TypeKind.Array"/></returns>
        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Array
            End Get
        End Property

        ''' <summary>
        ''' Get the symbol that logically contains this symbol. 
        ''' </summary>
        ''' <returns>Nothing</returns>
        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Gets the locations where this symbol was originally defined, either in source
        ''' or metadata. Some symbols (for example, partial classes) may be defined in more
        ''' than one location.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        ''' <summary>
        ''' Get the syntax node(s) where this symbol was declared in source.
        ''' </summary>
        ''' <returns>
        ''' An empty read-only array.
        ''' </returns>
        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitArrayType(Me, arg)
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend MustOverride Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers

        Public Overrides Function Equals(obj As Object) As Boolean
            If (Me Is obj) Then
                Return True
            End If

            Dim other = TryCast(obj, ArrayTypeSymbol)

            If (other Is Nothing OrElse Not other.HasSameShapeAs(Me) OrElse Not other.ElementType.Equals(ElementType)) Then
                Return False
            End If

            ' Make sure custom modifiers are the same.
            Dim [mod] As ImmutableArray(Of CustomModifier) = CustomModifiers
            Dim otherMod As ImmutableArray(Of CustomModifier) = other.CustomModifiers

            Dim count As Integer = [mod].Length

            If (count <> otherMod.Length) Then
                Return False
            End If

            For i As Integer = 0 To count - 1 Step 1
                If (Not [mod](i).Equals(otherMod(i))) Then
                    Return False
                End If
            Next

            ' Make sure bounds are the same.
            Return HasSameSizesAndLowerBoundsAs(other)
        End Function

        Public Overrides Function GetHashCode() As Integer
            ' Following the C# implementation to avoid recursion
            ' We don't want to blow the stack if we have a type like T[][][][][][][][]....[][],
            ' so we do not recurse until we have a non-array. Rather, hash all the ranks together
            ' And then hash that with the "T" type.

            Dim hashCode = 0
            Dim current As TypeSymbol = Me
            While (current.TypeKind = TypeKind.Array)
                Dim cur = DirectCast(current, ArrayTypeSymbol)
                hashCode = Hash.Combine(cur.Rank, hashCode)
                current = cur.ElementType
            End While

            Return Hash.Combine(current, hashCode)
        End Function

#Region "Use-Site Diagnostics"

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            ' Check type.
            Dim elementErrorInfo As DiagnosticInfo = DeriveUseSiteErrorInfoFromType(Me.ElementType)

            If elementErrorInfo IsNot Nothing AndAlso elementErrorInfo.Code = ERRID.ERR_UnsupportedType1 Then
                Return elementErrorInfo
            End If

            ' Check custom modifiers.
            Dim modifiersErrorInfo As DiagnosticInfo = DeriveUseSiteErrorInfoFromCustomModifiers(Me.CustomModifiers)

            Return MergeUseSiteErrorInfo(elementErrorInfo, modifiersErrorInfo)
        End Function

        Friend Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Return If(Me.ElementType.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes),
                   If(If(BaseTypeNoUseSiteDiagnostics IsNot Nothing, BaseTypeNoUseSiteDiagnostics.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes), Nothing),
                   If(GetUnificationUseSiteDiagnosticRecursive(Me.InterfacesNoUseSiteDiagnostics, owner, checkedTypes),
                      GetUnificationUseSiteDiagnosticRecursive(Me.CustomModifiers, owner, checkedTypes))))

        End Function

#End Region

#Region "IArrayTypeSymbol"

        Private ReadOnly Property IArrayTypeSymbol_ElementType As ITypeSymbol Implements IArrayTypeSymbol.ElementType
            Get
                Return Me.ElementType
            End Get
        End Property

        Private ReadOnly Property IArrayTypeSymbol_Rank As Integer Implements IArrayTypeSymbol.Rank
            Get
                Return Me.Rank
            End Get
        End Property

        Private ReadOnly Property IArrayTypeSymbol_CustomModifiers As ImmutableArray(Of CustomModifier) Implements IArrayTypeSymbol.CustomModifiers
            Get
                Return Me.CustomModifiers
            End Get
        End Property

        Private Function IArrayTypeSymbol_Equals(symbol As IArrayTypeSymbol) As Boolean Implements IArrayTypeSymbol.Equals
            Return Me.Equals(TryCast(symbol, ArrayTypeSymbol))
        End Function

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitArrayType(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitArrayType(Me)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitArrayType(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitArrayType(Me)
        End Function

#End Region

        Private MustInherit Class SZOrMDArray
            Inherits ArrayTypeSymbol

            Private ReadOnly _elementType As TypeSymbol
            Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly _systemArray As NamedTypeSymbol ' The base class - System.Array

            Public Sub New(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), systemArray As NamedTypeSymbol)
                Debug.Assert(elementType IsNot Nothing)
                Debug.Assert(systemArray IsNot Nothing)

                _elementType = elementType
                _systemArray = systemArray
                _customModifiers = customModifiers.NullToEmpty()
            End Sub

            Public NotOverridable Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _customModifiers
                End Get
            End Property

            Public NotOverridable Overrides ReadOnly Property ElementType As TypeSymbol
                Get
                    Return _elementType
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
                Get
                    Return _systemArray
                End Get
            End Property

            ''' <summary>
            ''' Substitute the given type substitution within this type, returning a new type. If the
            ''' substitution had no effect, return Me. 
            ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
            ''' !!! All other code should use Construct methods.                                        !!! 
            ''' </summary>
            Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
                ' Create a new array symbol with substitutions applied.
                Dim oldElementType = New TypeWithModifiers(_elementType, _customModifiers)
                Dim newElementType As TypeWithModifiers = oldElementType.InternalSubstituteTypeParameters(substitution)
                If newElementType <> oldElementType Then
                    Dim newArray As ArrayTypeSymbol

                    If Me.IsSZArray Then
                        Dim newInterfaces As ImmutableArray(Of NamedTypeSymbol) = Me.InterfacesNoUseSiteDiagnostics
                        If newInterfaces.Length > 0 Then
                            newInterfaces = newInterfaces.SelectAsArray(Function([interface], map) DirectCast([interface].InternalSubstituteTypeParameters(map).AsTypeSymbolOnly(), NamedTypeSymbol), substitution)
                        End If

                        newArray = New SZArray(newElementType.Type, newElementType.CustomModifiers, _systemArray, newInterfaces)

                    ElseIf Me.HasDefaultSizesAndLowerBounds
                        newArray = New MDArray(newElementType.Type, newElementType.CustomModifiers, Me.Rank, _systemArray)

                    Else
                        newArray = New MDArrayWithSizesAndBounds(newElementType.Type, newElementType.CustomModifiers, Me.Rank, Me.Sizes, Me.LowerBounds, _systemArray)
                    End If

                    Return New TypeWithModifiers(newArray)
                Else
                    Return New TypeWithModifiers(Me) ' substitution had no effect on the element type
                End If
            End Function
        End Class

        ''' <summary>
        ''' Represents SZARRAY - zero-based one-dimensional array 
        ''' </summary>
        Private NotInheritable Class SZArray
            Inherits SZOrMDArray

            Private ReadOnly _interfaces As ImmutableArray(Of NamedTypeSymbol) ' Empty or IList(Of ElementType) and possibly IReadOnlyList(Of ElementType)

            Public Sub New(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), systemArray As NamedTypeSymbol, interfaces As ImmutableArray(Of NamedTypeSymbol))
                MyBase.New(elementType, customModifiers, systemArray)

                Debug.Assert(interfaces.Length <= 2)
                _interfaces = interfaces
            End Sub

            Public Overrides ReadOnly Property Rank As Integer
                Get
                    Return 1
                End Get
            End Property

            Friend Overrides ReadOnly Property IsSZArray As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
                Get
                    Return _interfaces
                End Get
            End Property

            Friend Overrides ReadOnly Property HasDefaultSizesAndLowerBounds As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Represents MDARRAY - multi-dimensional array (possibly of rank 1)
        ''' </summary>
        Private Class MDArray
            Inherits SZOrMDArray

            Private ReadOnly _rank As Integer

            Public Sub New(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), rank As Integer, systemArray As NamedTypeSymbol)
                MyBase.New(elementType, customModifiers, systemArray)

                Debug.Assert(rank >= 1)
                _rank = rank
            End Sub

            Public NotOverridable Overrides ReadOnly Property Rank As Integer
                Get
                    Return _rank
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property IsSZArray As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
                Get
                    Return ImmutableArray(Of NamedTypeSymbol).Empty
                End Get
            End Property

            Friend Overrides ReadOnly Property HasDefaultSizesAndLowerBounds As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

        Private NotInheritable Class MDArrayWithSizesAndBounds
            Inherits MDArray

            Private ReadOnly _sizes As ImmutableArray(Of Integer)
            Private ReadOnly _lowerBounds As ImmutableArray(Of Integer)

            Public Sub New(
                elementType As TypeSymbol,
                customModifiers As ImmutableArray(Of CustomModifier),
                rank As Integer,
                sizes As ImmutableArray(Of Integer),
                lowerBounds As ImmutableArray(Of Integer),
                systemArray As NamedTypeSymbol
            )
                MyBase.New(elementType, customModifiers, rank, systemArray)

                Debug.Assert(Not sizes.IsDefaultOrEmpty OrElse Not lowerBounds.IsDefault)
                Debug.Assert(lowerBounds.IsDefaultOrEmpty OrElse (Not lowerBounds.IsEmpty AndAlso (lowerBounds.Length <> rank OrElse Not lowerBounds.All(Function(b) b = 0))))
                _sizes = sizes.NullToEmpty()
                _lowerBounds = lowerBounds
            End Sub

            Friend Overrides ReadOnly Property Sizes As ImmutableArray(Of Integer)
                Get
                    Return _sizes
                End Get
            End Property

            Friend Overrides ReadOnly Property LowerBounds As ImmutableArray(Of Integer)
                Get
                    Return _lowerBounds
                End Get
            End Property

            Friend Overrides ReadOnly Property HasDefaultSizesAndLowerBounds As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class

    End Class
End Namespace
