' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Private ReadOnly m_RetargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying MethodSymbol, cannot be another RetargetingMethodSymbol.
        ''' </summary>
        Private ReadOnly m_UnderlyingMethod As MethodSymbol

        Private m_LazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private m_LazyParameters As ImmutableArray(Of ParameterSymbol)

        Private m_LazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        ''' <summary>
        ''' Retargeted return type attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazyReturnTypeCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private m_LazyExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)

        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingMethod As MethodSymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingMethod IsNot Nothing)

            If TypeOf underlyingMethod Is RetargetingMethodSymbol Then
                Throw New ArgumentException()
            End If

            m_RetargetingModule = retargetingModule
            m_UnderlyingMethod = underlyingMethod
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return m_RetargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingMethod As MethodSymbol
            Get
                Return m_UnderlyingMethod
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return m_UnderlyingMethod.IsVararg
            End Get
        End Property

        Public Overrides ReadOnly Property IsGenericMethod As Boolean
            Get
                Return m_UnderlyingMethod.IsGenericMethod
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return m_UnderlyingMethod.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If (m_LazyTypeParameters.IsDefault) Then
                    If Not IsGenericMethod Then
                        m_LazyTypeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                    Else
                        ImmutableInterlocked.InterlockedCompareExchange(m_LazyTypeParameters,
                            RetargetingTranslator.Retarget(m_UnderlyingMethod.TypeParameters), Nothing)
                    End If
                End If

                Return m_LazyTypeParameters
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
                Return m_UnderlyingMethod.IsSub
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return m_UnderlyingMethod.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return m_UnderlyingMethod.IsIterator
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingMethod.ReturnType, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(m_UnderlyingMethod.ReturnTypeCustomModifiers, m_LazyCustomModifiers)
            End Get
        End Property

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return m_UnderlyingMethod.ParameterCount
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If m_LazyParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(m_LazyParameters, RetargetParameters(), Nothing)
                End If

                Return m_LazyParameters
            End Get
        End Property

        Private Function RetargetParameters() As ImmutableArray(Of ParameterSymbol)
            Dim list = m_UnderlyingMethod.Parameters
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
                Dim propertyOrEvent = m_UnderlyingMethod.AssociatedSymbol
                Return If(propertyOrEvent Is Nothing, Nothing, RetargetingTranslator.Retarget(propertyOrEvent))
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return m_UnderlyingMethod.IsExtensionMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return m_UnderlyingMethod.MayBeReducibleExtensionMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return m_UnderlyingMethod.IsOverloads
            End Get
        End Property

        Friend Overrides ReadOnly Property IsHiddenBySignature As Boolean
            Get
                Return m_UnderlyingMethod.IsHiddenBySignature
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingMethod.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_UnderlyingMethod.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_UnderlyingMethod.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return m_UnderlyingMethod.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return m_UnderlyingMethod.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return m_UnderlyingMethod.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return m_UnderlyingMethod.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return m_UnderlyingMethod.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return m_UnderlyingMethod.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return m_UnderlyingMethod.IsExternalMethod
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return m_UnderlyingMethod.GetDllImportData()
        End Function

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return m_UnderlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges)
        End Function

        Friend Overrides ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                Return m_UnderlyingMethod.ReturnValueIsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.RetargetingTranslator.Retarget(m_UnderlyingMethod.ReturnTypeMarshallingInformation)
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return m_UnderlyingMethod.ReturnValueMarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property IsAccessCheckedOnOverride As Boolean
            Get
                Return m_UnderlyingMethod.IsAccessCheckedOnOverride
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExternal As Boolean
            Get
                Return m_UnderlyingMethod.IsExternal
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return m_UnderlyingMethod.ImplementationAttributes
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return m_UnderlyingMethod.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return m_UnderlyingMethod.GetSecurityInformation()
        End Function

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return m_UnderlyingMethod.IsImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return m_UnderlyingMethod.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(m_UnderlyingMethod, m_LazyCustomAttributes)
        End Function

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(m_UnderlyingMethod, m_LazyReturnTypeCustomAttributes, True)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(m_UnderlyingMethod.GetCustomAttributesToEmit(compilationState))
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_RetargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_UnderlyingMethod.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return m_UnderlyingMethod.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return m_UnderlyingMethod.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return m_UnderlyingMethod.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasFinalFlag As Boolean
            Get
                Return m_UnderlyingMethod.HasFinalFlag
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return m_UnderlyingMethod.GetAppliedConditionalSymbols()
        End Function

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return m_UnderlyingMethod.MethodKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return m_UnderlyingMethod.IsMethodKindBasedOnSyntax
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return m_UnderlyingMethod.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If m_LazyExplicitInterfaceImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(
                        m_LazyExplicitInterfaceImplementations,
                        Me.RetargetExplicitInterfaceImplementations(),
                        Nothing)
                End If

                Return m_LazyExplicitInterfaceImplementations
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
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                m_lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return m_lazyUseSiteErrorInfo
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
            Return m_UnderlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
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
