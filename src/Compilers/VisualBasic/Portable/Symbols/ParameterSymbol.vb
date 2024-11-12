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
    ''' Represents a parameter of a method or property.
    ''' </summary>
    Friend MustInherit Class ParameterSymbol
        Inherits Symbol
        Implements IParameterSymbol, IParameterSymbolInternal

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Overridable Shadows ReadOnly Property OriginalDefinition As ParameterSymbol
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
        ''' Is this ByRef parameter.
        ''' </summary>
        Public MustOverride ReadOnly Property IsByRef As Boolean

        ''' <summary>
        ''' Is parameter explicitly declared ByRef. Can be different from IsByRef only for
        ''' String parameters of Declare methods.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsExplicitByRef As Boolean

        ''' <summary>
        ''' Is this Out parameter (metadata flag In is set).
        ''' </summary>
        Friend MustOverride ReadOnly Property IsMetadataOut As Boolean

        ''' <summary>
        ''' Is this In parameter (metadata flag Out is set).
        ''' </summary>
        Friend MustOverride ReadOnly Property IsMetadataIn As Boolean

        ''' <summary>
        ''' True if the parameter flows data out of the method.
        ''' </summary>
        Friend ReadOnly Property IsOut As Boolean
            Get
                Return IsByRef AndAlso IsMetadataOut AndAlso Not IsMetadataIn
            End Get
        End Property

        ''' <summary>
        ''' Describes how the parameter is marshalled when passed to native code.
        ''' Null if no specific marshalling information is available for the parameter.
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

        Friend ReadOnly Property IsMarshalAsObject As Boolean
            Get
                Select Case Me.MarshallingType
                    Case UnmanagedType.Interface, UnmanagedType.IUnknown, Cci.Constants.UnmanagedType_IDispatch
                        Return True
                End Select

                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets the type of this parameter.
        ''' </summary>
        Public MustOverride ReadOnly Property Type As TypeSymbol

        ''' <summary>
        ''' The list of custom modifiers, if any, associated with the parameter type.
        ''' </summary>
        Public MustOverride ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Custom modifiers associated with the ref modifier, or an empty array if there are none.
        ''' </summary>
        Public MustOverride ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Gets the ordinal order of this parameter. The first type parameter has ordinal zero.
        ''' </summary>
        Public MustOverride ReadOnly Property Ordinal As Integer

        ''' <summary>
        ''' Returns true if this parameter was declared as a ParamArray. 
        ''' </summary>
        Public MustOverride ReadOnly Property IsParamArray As Boolean Implements IParameterSymbol.IsParams, IParameterSymbol.IsParamsArray

        Private ReadOnly Property IsParamsCollection As Boolean Implements IParameterSymbol.IsParamsCollection
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this parameter was declared as Optional. 
        ''' </summary>
        Public MustOverride ReadOnly Property IsOptional As Boolean

        ''' <summary>
        ''' Returns true if the parameter explicitly specifies a default value to be passed
        ''' when no value is provided as an argument to a call.
        ''' </summary>
        ''' <remarks>
        ''' True if the parameter has a default value defined in source via an optional parameter syntax, 
        ''' or the parameter is from metadata and HasDefault and Optional metadata flags are set,
        ''' or the parameter is from metadata, has Optional flag set and <see cref="System.Runtime.CompilerServices.DateTimeConstantAttribute"/>
        ''' or <see cref="System.Runtime.CompilerServices.DecimalConstantAttribute"/> is specified.
        ''' 
        ''' The default value can be obtained with the <see cref="ExplicitDefaultValue"/> property.
        ''' </remarks>
        Public MustOverride ReadOnly Property HasExplicitDefaultValue As Boolean Implements IParameterSymbol.HasExplicitDefaultValue

        ''' <summary>
        ''' Returns the default value of this parameter. If <see cref="HasExplicitDefaultValue"/>
        ''' returns false, then this property throws an InvalidOperationException.
        ''' </summary>
        Public ReadOnly Property ExplicitDefaultValue As Object
            Get
                If HasExplicitDefaultValue Then
                    Return ExplicitDefaultConstantValue.Value
                End If

                Throw New InvalidOperationException
            End Get
        End Property

        ''' <summary>
        ''' Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        ''' This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns the default value of this parameter as a ConstantValue.
        ''' Return nothing if there isn't a default value.
        ''' </summary>
        Friend MustOverride ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue

        Friend ReadOnly Property ExplicitDefaultConstantValue As ConstantValue
            Get
                Return ExplicitDefaultConstantValue(SymbolsInProgress(Of ParameterSymbol).Empty)
            End Get
        End Property

        Friend MustOverride ReadOnly Property HasOptionCompare As Boolean

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Parameter
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitParameter(Me, arg)
        End Function

        Friend Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

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

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overridable ReadOnly Property IsMe As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Create a new ParameterSymbol with everything the same except the owner. Used for property
        ''' accessor methods, for example.
        ''' </summary>
        ''' <remarks>
        ''' Note: This is only implemented for those subclasses (e.g., SourceParameterSymbol) for which it
        ''' is required. Thus, the base implementation throws an exception instead of being MustOverride.
        ''' </remarks>
        Friend Overridable Function ChangeOwner(newContainingSymbol As Symbol) As ParameterSymbol
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return Me.ContainingSymbol.EmbeddedSymbolKind
            End Get
        End Property

        Friend MustOverride ReadOnly Property IsIDispatchConstant As Boolean

        Friend MustOverride ReadOnly Property IsIUnknownConstant As Boolean

        Friend MustOverride ReadOnly Property IsCallerLineNumber As Boolean

        Friend MustOverride ReadOnly Property IsCallerMemberName As Boolean

        Friend MustOverride ReadOnly Property IsCallerFilePath As Boolean

        ''' <summary>
        ''' The index of the parameter which CallerArgumentExpressionAttribute points to.
        ''' </summary>
        ''' <remarks>
        ''' Returns -1 if there is no valid CallerArgumentExpressionAttribute.
        ''' The situation is different for reduced extension method parameters, where a value
        ''' of -2 is returned for no valid attribute, -1 for 'Me' parameter, and the reduced index (i.e, the original index minus 1) otherwise.
        ''' </remarks>
        Friend MustOverride ReadOnly Property CallerArgumentExpressionParameterIndex As Integer

        Protected Overrides Function IsHighestPriorityUseSiteError(code As Integer) As Boolean
            Return code = ERRID.ERR_UnsupportedType1 OrElse code = ERRID.ERR_UnsupportedCompilerFeature
        End Function

        Public Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim info As DiagnosticInfo = DeriveUseSiteInfoFromParameter(Me).DiagnosticInfo
                Return info IsNot Nothing AndAlso (info.Code = ERRID.ERR_UnsupportedType1 OrElse info.Code = ERRID.ERR_UnsupportedCompilerFeature)
            End Get
        End Property

#Region "IParameterSymbol"

        Private ReadOnly Property IParameterSymbol_IsDiscard As Boolean Implements IParameterSymbol.IsDiscard
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_RefKind As RefKind Implements IParameterSymbol.RefKind, IParameterSymbolInternal.RefKind
            Get
                ' TODO: Should we check if it has the <Out> attribute and return 'RefKind.Out' in
                ' that case?
                Return If(Me.IsByRef, RefKind.Ref, RefKind.None)
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_ScopedKind As ScopedKind Implements IParameterSymbol.ScopedKind
            Get
                Return ScopedKind.None
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_Type As ITypeSymbol Implements IParameterSymbol.Type
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property IParameterSymbolInternal_Type As ITypeSymbolInternal Implements IParameterSymbolInternal.Type
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_NullableAnnotation As NullableAnnotation Implements IParameterSymbol.NullableAnnotation
            Get
                Return NullableAnnotation.None
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_IsOptional As Boolean Implements IParameterSymbol.IsOptional
            Get
                Return Me.IsOptional
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_IsThis As Boolean Implements IParameterSymbol.IsThis
            Get
                Return Me.IsMe
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_RefCustomModifiers As ImmutableArray(Of CustomModifier) Implements IParameterSymbol.RefCustomModifiers
            Get
                Return Me.RefCustomModifiers
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_CustomModifiers As ImmutableArray(Of CustomModifier) Implements IParameterSymbol.CustomModifiers
            Get
                Return Me.CustomModifiers
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_Ordinal As Integer Implements IParameterSymbol.Ordinal
            Get
                Return Me.Ordinal
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_DefaultValue As Object Implements IParameterSymbol.ExplicitDefaultValue
            Get
                Return Me.ExplicitDefaultValue
            End Get
        End Property

        Private ReadOnly Property IParameterSymbol_OriginalDefinition As IParameterSymbol Implements IParameterSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitParameter(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitParameter(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitParameter(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitParameter(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitParameter(Me)
        End Function

#End Region

    End Class
End Namespace
