' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a member variable -- a variable declared as a member of a Class or Structure. 
    ''' </summary>
    Friend MustInherit Class FieldSymbol
        Inherits Symbol
        Implements IFieldSymbol, IFieldSymbolInternal

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Overridable Shadows ReadOnly Property OriginalDefinition As FieldSymbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property OriginalSymbolDefinition As Symbol
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        ''' <summary>
        ''' Gets the type of this variable.
        ''' </summary>
        Public MustOverride ReadOnly Property Type As TypeSymbol

        ''' <summary>
        ''' Gets a value indicating whether this instance has declared type. This means a field was declared with an AsClause
        ''' or in case of const fields with an AsClause whose type is not System.Object
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance has declared type; otherwise, <c>false</c>.
        ''' </value>
        Friend Overridable ReadOnly Property HasDeclaredType As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' The list of custom modifiers, if any, associated with the member variable.
        ''' </summary>
        Public MustOverride ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' If this variable serves as a backing variable for an automatically generated
        ''' property or event, returns that property or event. 
        ''' Otherwise returns Nothing.
        ''' Note, the set of possible associated symbols might be expanded in the future to 
        ''' reflect changes in the languages.
        ''' </summary>
        Public MustOverride ReadOnly Property AssociatedSymbol As Symbol

        ''' <summary>
        ''' Returns true if this variable was declared as ReadOnly 
        ''' </summary>
        Public MustOverride ReadOnly Property IsReadOnly As Boolean Implements IFieldSymbol.IsReadOnly

        ''' <summary>
        ''' Returns true if this field was declared as "const" (i.e. is a constant declaration), or
        ''' is an Enum member.
        ''' </summary>
        Public MustOverride ReadOnly Property IsConst As Boolean

        ''' <summary>
        ''' Gets a value indicating whether this instance is metadata constant. A field is considered to be 
        ''' metadata constant if the field value is a valid default value for a field.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is metadata constant; otherwise, <c>false</c>.
        ''' </value>
        Friend ReadOnly Property IsMetadataConstant As Boolean
            Get
                If Me.IsConst Then
                    Dim specialType = Me.Type.SpecialType
                    Return specialType <> Microsoft.CodeAnalysis.SpecialType.System_DateTime AndAlso specialType <> Microsoft.CodeAnalysis.SpecialType.System_Decimal
                End If

                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is const, but not metadata constant. A field is considered to be 
        ''' const but not metadata constant if the const field's type is either Date or Decimal.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is metadata constant; otherwise, <c>false</c>.
        ''' </value>
        Friend ReadOnly Property IsConstButNotMetadataConstant As Boolean
            Get
                If Me.IsConst Then
                    Dim specialType = Me.Type.SpecialType
                    Return specialType = Microsoft.CodeAnalysis.SpecialType.System_DateTime OrElse specialType = Microsoft.CodeAnalysis.SpecialType.System_Decimal
                End If

                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns false if the field wasn't declared as "const", or constant value was omitted or erroneous.
        ''' True otherwise.
        ''' </summary>
        Public Overridable ReadOnly Property HasConstantValue As Boolean
            Get
                If Not Me.IsConst Then
                    Return False
                End If

                Dim value = GetConstantValue(ConstantFieldsInProgress.Empty)
                Return (value IsNot Nothing) AndAlso Not value.IsBad ' can be null in error scenarios
            End Get
        End Property

        ''' <summary>
        ''' If IsConst returns true, then returns the value of the constant or Enum member.
        ''' If IsConst return false, then returns Nothing.
        ''' </summary>
        Public Overridable ReadOnly Property ConstantValue As Object
            Get
                If Not IsConst Then
                    Return Nothing
                End If

                Dim value = GetConstantValue(ConstantFieldsInProgress.Empty)
                Return If(value IsNot Nothing, value.Value, Nothing) ' can be null in error scenarios
            End Get
        End Property

        ''' <summary>
        ''' Gets the constant value.
        ''' </summary>
        ''' <param name="inProgress">Used to detect dependencies between constant field values.</param>
        Friend MustOverride Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue

        ''' <summary>
        ''' Const fields do not (always) have to be declared with a given type. To get the inferred type determined from
        ''' the initialization this method should be called instead of "Type". For non const field this method returns the
        ''' declared type.
        ''' </summary>
        ''' <param name="inProgress">Used to detect dependencies between constant field values.</param><returns></returns>
        Friend Overridable Function GetInferredType(inProgress As ConstantFieldsInProgress) As TypeSymbol
            Return Type
        End Function

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Field
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitField(Me, arg)
        End Function

        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        '''  True if this symbol has a special name (metadata flag SpecialName is set).
        ''' </summary>
        Friend MustOverride ReadOnly Property HasSpecialName As Boolean

        ''' <summary>
        ''' True if RuntimeSpecialName metadata flag is set for this symbol.
        ''' </summary>
        Friend MustOverride ReadOnly Property HasRuntimeSpecialName As Boolean

        ''' <summary>
        ''' True if NotSerialized metadata flag is set for this symbol.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsNotSerialized As Boolean

        ''' <summary>
        ''' Describes how the field is marshalled when passed to native code.
        ''' Null if no specific marshalling information is available for the field.
        ''' </summary>
        ''' <remarks>PE symbols don't provide this information and always return Nothing.</remarks>
        Friend MustOverride ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData

        ''' <summary>
        ''' Returns the marshalling type of this field, or 0 if marshalling information isn't available.
        ''' </summary>
        ''' <remarks>
        ''' By default this information is extracted from <see cref="MarshallingInformation"/> if available. 
        ''' Since the compiler does only need to know the marshalling type of symbols that aren't emitted 
        ''' PE symbols just decode the type from metadata and don't provide full marshalling information.
        ''' </remarks>
        Friend Overridable ReadOnly Property MarshallingType As UnmanagedType
            Get
                Dim info = MarshallingInformation
                Return If(info IsNot Nothing, info.UnmanagedType, CType(0, UnmanagedType))
            End Get
        End Property

        ''' <summary>
        ''' Offset assigned to the field when the containing type is laid out by the VM.
        ''' Nothing if unspecified.
        ''' </summary>
        Friend MustOverride ReadOnly Property TypeLayoutOffset As Integer?

        ''' <summary>
        ''' Get the "this" parameter for this field.  This is only valid for source fields.
        ''' </summary>
        Friend Overridable ReadOnly Property MeParameter As ParameterSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        ''' <summary>
        ''' Returns true when field is a backing field for a captured frame pointer (typically "Me").
        ''' </summary>
        Friend Overridable ReadOnly Property IsCapturedFrame As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            If Me.IsDefinition Then
                Return New UseSiteInfo(Of AssemblySymbol)(PrimaryDependency)
            End If

            Return Me.OriginalDefinition.GetUseSiteInfo()
        End Function

        Friend Function CalculateUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)

            Debug.Assert(IsDefinition)

            ' Check type.
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = MergeUseSiteInfo(New UseSiteInfo(Of AssemblySymbol)(PrimaryDependency), DeriveUseSiteInfoFromType(Me.Type))

            If useSiteInfo.DiagnosticInfo?.Code = ERRID.ERR_UnsupportedField1 Then
                Return useSiteInfo
            End If

            ' Check custom modifiers.
            Dim modifiersUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromCustomModifiers(Me.CustomModifiers)

            If modifiersUseSiteInfo.DiagnosticInfo?.Code = ERRID.ERR_UnsupportedField1 Then
                Return modifiersUseSiteInfo
            End If

            useSiteInfo = MergeUseSiteInfo(useSiteInfo, modifiersUseSiteInfo)

            ' If the member is in an assembly with unified references, 
            ' we check if its definition depends on a type from a unified reference.
            If useSiteInfo.DiagnosticInfo Is Nothing AndAlso Me.ContainingModule.HasUnifiedReferences Then
                Dim unificationCheckedTypes As HashSet(Of TypeSymbol) = Nothing

                Dim errorInfo As DiagnosticInfo = If(Me.Type.GetUnificationUseSiteDiagnosticRecursive(Me, unificationCheckedTypes),
                                                     GetUnificationUseSiteDiagnosticRecursive(Me.CustomModifiers, Me, unificationCheckedTypes))

                If errorInfo IsNot Nothing Then
                    Debug.Assert(errorInfo.Severity = DiagnosticSeverity.Error)
                    useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(errorInfo)
                End If
            End If

            Return useSiteInfo
        End Function

        ''' <summary>
        ''' Return error code that has highest priority while calculating use site error for this symbol. 
        ''' </summary>
        Protected Overrides ReadOnly Property HighestPriorityUseSiteError As Integer
            Get
                Return ERRID.ERR_UnsupportedField1
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim info As DiagnosticInfo = GetUseSiteInfo().DiagnosticInfo
                Return info IsNot Nothing AndAlso info.Code = ERRID.ERR_UnsupportedField1
            End Get
        End Property

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return Me.ContainingSymbol.EmbeddedSymbolKind
            End Get
        End Property

        ''' <summary>
        ''' Is this a field of a tuple type?
        ''' </summary>
        Public Overridable ReadOnly Property IsTupleField() As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns True when field symbol is not mapped directly to a field in the underlying tuple struct.
        ''' </summary>
        Public Overridable ReadOnly Property IsVirtualTupleField As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this is a field representing a Default element like Item1, Item2...
        ''' </summary>
        Public Overridable ReadOnly Property IsDefaultTupleElement As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' If this is a field of a tuple type, return corresponding underlying field from the
        ''' tuple underlying type. Otherwise, Nothing. In case of a malformed underlying type
        ''' the corresponding underlying field might be missing, return Nothing in this case too.
        ''' </summary>
        Public Overridable ReadOnly Property TupleUnderlyingField() As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' If this field represents a tuple element, returns a corresponding default element field.
        ''' Otherwise returns Nothing
        ''' </summary>
        Public Overridable ReadOnly Property CorrespondingTupleField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property


        ''' <summary>
        ''' If this is a field representing a tuple element,
        ''' returns the index of the element (zero-based).
        ''' Otherwise returns -1
        ''' </summary>
        Public Overridable ReadOnly Property TupleElementIndex As Integer
            Get
                Return -1
            End Get
        End Property

        Friend Function AsMember(newOwner As NamedTypeSymbol) As FieldSymbol
            Debug.Assert(Me Is Me.OriginalDefinition)
            Debug.Assert(newOwner.OriginalDefinition Is Me.ContainingSymbol.OriginalDefinition)
            Return If(TypeSymbol.Equals(newOwner, Me.ContainingType, TypeCompareKind.ConsiderEverything),
                Me,
                DirectCast(DirectCast(newOwner, SubstitutedNamedType).GetMemberForDefinition(Me), FieldSymbol))
        End Function

#Region "IFieldSymbol"

        Private ReadOnly Property IFieldSymbol_AssociatedSymbol As ISymbol Implements IFieldSymbol.AssociatedSymbol
            Get
                Return Me.AssociatedSymbol
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_IsConst As Boolean Implements IFieldSymbol.IsConst
            Get
                Return Me.IsConst
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_IsVolatile As Boolean Implements IFieldSymbol.IsVolatile, IFieldSymbolInternal.IsVolatile
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_IsFixedSizeBuffer As Boolean Implements IFieldSymbol.IsFixedSizeBuffer
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_FixedSize As Integer Implements IFieldSymbol.FixedSize
            Get
                Return 0
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_Type As ITypeSymbol Implements IFieldSymbol.Type
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_NullableAnnotation As NullableAnnotation Implements IFieldSymbol.NullableAnnotation
            Get
                Return NullableAnnotation.None
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_HasConstantValue As Boolean Implements IFieldSymbol.HasConstantValue
            Get
                Return Me.HasConstantValue
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_ConstantValue As Object Implements IFieldSymbol.ConstantValue
            Get
                Return Me.ConstantValue
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_CustomModifiers As ImmutableArray(Of CustomModifier) Implements IFieldSymbol.CustomModifiers
            Get
                Return Me.CustomModifiers
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_OriginalDefinition As IFieldSymbol Implements IFieldSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_CorrespondingTupleField As IFieldSymbol Implements IFieldSymbol.CorrespondingTupleField
            Get
                Return Me.CorrespondingTupleField
            End Get
        End Property

        Private ReadOnly Property IFieldSymbol_HasExplicitTupleElementName As Boolean Implements IFieldSymbol.IsExplicitlyNamedTupleElement
            Get
                Return Not Me.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitField(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitField(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitField(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitField(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitField(Me)
        End Function

#End Region

    End Class
End Namespace
