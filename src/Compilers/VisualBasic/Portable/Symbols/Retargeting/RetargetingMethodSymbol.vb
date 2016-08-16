' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Represents a method in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    ''' another MethodSymbol that is responsible for retargeting symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' </summary>
    Friend NotInheritable Class RetargetingMethodSymbol
        Inherits MethodSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying MethodSymbol, cannot be another RetargetingMethodSymbol.
        ''' </summary>
        Private ReadOnly _underlyingMethod As MethodSymbol

        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        Private _lazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        ''' <summary>
        ''' Retargeted return type attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyReturnTypeCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)

        Private _lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingMethod As MethodSymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingMethod IsNot Nothing)

            If TypeOf underlyingMethod Is RetargetingMethodSymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingMethod = underlyingMethod
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingMethod As MethodSymbol
            Get
                Return _underlyingMethod
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return _underlyingMethod.IsVararg
            End Get
        End Property

        Public Overrides ReadOnly Property IsGenericMethod As Boolean
            Get
                Return _underlyingMethod.IsGenericMethod
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _underlyingMethod.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If (_lazyTypeParameters.IsDefault) Then
                    If Not IsGenericMethod Then
                        _lazyTypeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Else
                        ImmutableInterlocked.InterlockedCompareExchange(_lazyTypeParameters,
                            RetargetingTranslator.Retarget(_underlyingMethod.TypeParameters), Nothing)
                    End If
                End If

                Return _lazyTypeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                If (IsGenericMethod) Then
                    Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
                Else
                    Return ImmutableArray(Of TypeSymbol).Empty
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _underlyingMethod.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return _underlyingMethod.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return _underlyingMethod.IsIterator
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _underlyingMethod.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingMethod.ReturnType, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(_underlyingMethod.ReturnTypeCustomModifiers, _lazyCustomModifiers)
            End Get
        End Property

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return _underlyingMethod.ParameterCount
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If _lazyParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_lazyParameters, RetargetParameters(), Nothing)
                End If

                Return _lazyParameters
            End Get
        End Property

        Private Function RetargetParameters() As ImmutableArray(Of ParameterSymbol)
            Dim list = _underlyingMethod.Parameters
            Dim count = list.Length

            If count = 0 Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            Else
                Dim parameters = New ParameterSymbol(count - 1) {}

                For i As Integer = 0 To count - 1
                    parameters(i) = RetargetingParameterSymbol.CreateMethodParameter(Me, list(i))
                Next

                Return parameters.AsImmutableOrNull()
            End If
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Dim propertyOrEvent = _underlyingMethod.AssociatedSymbol
                Return If(propertyOrEvent Is Nothing, Nothing, RetargetingTranslator.Retarget(propertyOrEvent))
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return _underlyingMethod.IsExtensionMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return _underlyingMethod.MayBeReducibleExtensionMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return _underlyingMethod.IsOverloads
            End Get
        End Property

        Friend Overrides ReadOnly Property IsHiddenBySignature As Boolean
            Get
                Return _underlyingMethod.IsHiddenBySignature
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingMethod.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingMethod.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingMethod.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _underlyingMethod.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _underlyingMethod.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return _underlyingMethod.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _underlyingMethod.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return _underlyingMethod.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return _underlyingMethod.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return _underlyingMethod.IsExternalMethod
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return _underlyingMethod.GetDllImportData()
        End Function

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return _underlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges)
        End Function

        Friend Overrides ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                Return _underlyingMethod.ReturnValueIsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.RetargetingTranslator.Retarget(_underlyingMethod.ReturnTypeMarshallingInformation)
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return _underlyingMethod.ReturnValueMarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property IsAccessCheckedOnOverride As Boolean
            Get
                Return _underlyingMethod.IsAccessCheckedOnOverride
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExternal As Boolean
            Get
                Return _underlyingMethod.IsExternal
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return _underlyingMethod.ImplementationAttributes
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return _underlyingMethod.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return _underlyingMethod.GetSecurityInformation()
        End Function

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _underlyingMethod.IsImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _underlyingMethod.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingMethod, _lazyCustomAttributes)
        End Function

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingMethod, _lazyReturnTypeCustomAttributes, True)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(_underlyingMethod.GetCustomAttributesToEmit(compilationState))
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _retargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingMethod.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingMethod.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _underlyingMethod.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return _underlyingMethod.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataFinal As Boolean
            Get
                Return _underlyingMethod.IsMetadataFinal
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return _underlyingMethod.GetAppliedConditionalSymbols()
        End Function

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return _underlyingMethod.MethodKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return _underlyingMethod.IsMethodKindBasedOnSyntax
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return _underlyingMethod.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyExplicitInterfaceImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(
                        _lazyExplicitInterfaceImplementations,
                        Me.RetargetExplicitInterfaceImplementations(),
                        Nothing)
                End If

                Return _lazyExplicitInterfaceImplementations
            End Get
        End Property

        Private Function RetargetExplicitInterfaceImplementations() As ImmutableArray(Of MethodSymbol)
            Dim impls = Me.UnderlyingMethod.ExplicitInterfaceImplementations
            If impls.IsEmpty Then
                Return impls
            End If

            Dim builder = ArrayBuilder(Of MethodSymbol).GetInstance()
            For i = 0 To impls.Length - 1
                Dim retargeted = RetargetingTranslator.Retarget(impls(i), MethodSignatureComparer.RetargetedExplicitMethodImplementationComparer)
                If retargeted IsNot Nothing Then
                    builder.Add(retargeted)
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If _lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                _lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return _lazyUseSiteErrorInfo
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            ' retargeting symbols refer to a symbol from another compilation, they don't define locals in the current compilation
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
