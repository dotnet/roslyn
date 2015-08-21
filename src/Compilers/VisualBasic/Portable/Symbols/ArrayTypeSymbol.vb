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
    Friend Class ArrayTypeSymbol
        Inherits TypeSymbol
        Implements IArrayTypeSymbol

        Private ReadOnly _elementType As TypeSymbol
        Private ReadOnly _rank As Integer
        Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _systemArray As NamedTypeSymbol ' The base class - System.Array
        Private ReadOnly _interfaces As ImmutableArray(Of NamedTypeSymbol) ' Empty or IList(Of ElementType) and possibly IReadOnlyList(Of ElementType)

        ''' <summary>
        ''' Create a new ArrayTypeSymbol.
        ''' </summary>
        Friend Sub New(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), rank As Integer, compilation As VisualBasicCompilation)
            Me.New(elementType, customModifiers, rank, compilation.Assembly)
        End Sub

        ''' <summary>
        ''' Create a new ArrayTypeSymbol.
        ''' </summary>
        Friend Sub New(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), rank As Integer, declaringAssembly As AssemblySymbol)
            Me.New(elementType,
                   customModifiers,
                   rank,
                   declaringAssembly.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Array),
                   GetArrayInterfaces(elementType, rank, declaringAssembly))
        End Sub

        ''' <summary>
        ''' Create a new ArrayTypeSymbol.
        ''' </summary>
        ''' <param name="elementType">The element type of this array type.</param>
        ''' <param name="customModifiers"> The custom modifiers, if any</param>
        ''' <param name="rank">The rank of this array type.</param>
        ''' <param name="systemArray">Symbol for System.Array</param>
        ''' <param name="interfaces">Symbols for the interfaces of this array. Should be IList(Of ElementType) and possibly IReadOnlyList(Of ElementType) or Nothing.</param>
        Private Sub New(elementType As TypeSymbol, customModifiers As ImmutableArray(Of CustomModifier), rank As Integer, systemArray As NamedTypeSymbol, interfaces As ImmutableArray(Of NamedTypeSymbol))
            Debug.Assert(elementType IsNot Nothing)
            Debug.Assert(systemArray IsNot Nothing)
            Debug.Assert(rank >= 1)
            Debug.Assert(interfaces.Length <= 2)
            Debug.Assert(interfaces.Length = 0 OrElse rank = 1)

            _elementType = elementType
            _rank = rank
            _systemArray = systemArray
            _customModifiers = customModifiers.NullToEmpty()
            _interfaces = interfaces
        End Sub

        Private Shared Function GetArrayInterfaces(elementType As TypeSymbol, rank As Integer, declaringAssembly As AssemblySymbol) As ImmutableArray(Of NamedTypeSymbol)
            If rank = 1 Then
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
            End If

            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ''' <summary>
        ''' Returns the list of custom modifiers, if any, associated with the array.
        ''' </summary>
        Public ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _customModifiers
            End Get
        End Property

        ''' <summary>
        ''' Returns the number of dimensions of this array. A regular single-dimensional array
        ''' has rank 1, a two-dimensional array has rank 2, etc.
        ''' </summary>
        Public ReadOnly Property Rank As Integer
            Get
                Return _rank
            End Get
        End Property

        ''' <summary>
        ''' Returns the type of the elements that are stored in this array.
        ''' </summary>
        Public ReadOnly Property ElementType As TypeSymbol
            Get
                Return _elementType
            End Get
        End Property

        Friend Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
            Get
                Return _systemArray
            End Get
        End Property

        Friend Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return _interfaces
            End Get
        End Property

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

        Private Function GetTypeModifier() As String
            Return "(" & New String(","c, _rank - 1) & ")"
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
        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            ' Create a new array symbol with substitutions applied.
            Dim oldElementType = New TypeWithModifiers(_elementType, _customModifiers)
            Dim newElementType As TypeWithModifiers = oldElementType.InternalSubstituteTypeParameters(substitution)
            If newElementType <> oldElementType Then
                Dim newInterfaces As ImmutableArray(Of NamedTypeSymbol)
                If _interfaces.Length > 0 Then
                    newInterfaces = ImmutableArray.Create(Of NamedTypeSymbol)(DirectCast(_interfaces(0).InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol))
                Else
                    newInterfaces = ImmutableArray(Of NamedTypeSymbol).Empty
                End If

                Return New TypeWithModifiers(New ArrayTypeSymbol(newElementType.Type, newElementType.CustomModifiers, _rank, _systemArray, newInterfaces))
            Else
                Return New TypeWithModifiers(Me) ' substitution had no effect on the element type
            End If
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If (Me Is obj) Then
                Return True
            End If

            Dim other = TryCast(obj, ArrayTypeSymbol)

            If (other Is Nothing OrElse other.Rank <> Rank OrElse Not other.ElementType.Equals(ElementType)) Then
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

            Return True
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

    End Class
End Namespace
