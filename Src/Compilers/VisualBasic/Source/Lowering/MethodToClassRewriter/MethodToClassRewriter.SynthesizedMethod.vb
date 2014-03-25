' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class MethodToClassRewriter(Of TProxy)
        Inherits BoundTreeRewriter

        ''' <summary>
        ''' Base for synthesized Lambda methods. 
        ''' Just provides a bunch of defaults
        ''' </summary>
        Friend MustInherit Class SynthesizedMethod
            Inherits MethodSymbol

            Private ReadOnly m_isShared As Boolean
            Private ReadOnly m_containingType As NamedTypeSymbol
            Private ReadOnly m_name As String
            Private ReadOnly m_SyntaxNode As VisualBasicSyntaxNode
            Private m_lazyMeParameter As ParameterSymbol

            Friend Sub New(
                syntaxNode As VisualBasicSyntaxNode,
                containingSymbol As NamedTypeSymbol,
                name As String,
                isShared As Boolean
            )
                Debug.Assert(syntaxNode IsNot Nothing)

                Me.m_SyntaxNode = syntaxNode
                Me.m_isShared = isShared
                Me.m_name = name
                Me.m_containingType = containingSymbol
            End Sub

            Private Shared ReadOnly TypeSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
                Function(container) DirectCast(container, SynthesizedMethod).TypeMap

            Friend Shared ReadOnly CreateTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
                Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter, container, typeParameter.Name, TypeSubstitutionFactory)

            ''' <summary>
            ''' Creates a clone of the local with a new containing symbol and type.
            ''' Note that the new parameter gets no syntaxRef as it is supposed to get 
            ''' all the values it needs from the original parameter.
            ''' </summary>
            Friend Shared Function WithNewContainerAndType(
                             newContainer As Symbol,
                             newType As TypeSymbol,
                             origParameter As ParameterSymbol) As ParameterSymbol

                Dim flags As SourceParameterFlags = Nothing

                If origParameter.IsByRef Then
                    flags = flags Or SourceParameterFlags.ByRef
                Else
                    flags = flags Or SourceParameterFlags.ByVal
                End If

                If origParameter.IsParamArray Then
                    flags = flags Or SourceParameterFlags.ParamArray
                End If

                If origParameter.IsOptional Then
                    flags = flags Or SourceParameterFlags.Optional
                End If

                Return SourceComplexParameterSymbol.Create(
                    newContainer,
                    origParameter.Name,
                    origParameter.Ordinal,
                    newType,
                    origParameter.Locations.FirstOrDefault,
                    syntaxRef:=Nothing,
                    flags:=flags,
                    defaultValueOpt:=origParameter.ExplicitDefaultConstantValue)

            End Function

            Public Overrides ReadOnly Property Name As String
                Get
                    Return m_name
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return ImmutableArray(Of ParameterSymbol).Empty
                End Get
            End Property

            Friend Overrides ReadOnly Property MeParameter As ParameterSymbol
                Get
                    If IsShared Then
                        Return Nothing
                    Else
                        If m_lazyMeParameter Is Nothing Then
                            Interlocked.CompareExchange(Of ParameterSymbol)(m_lazyMeParameter, New MeParameterSymbol(Me), Nothing)
                        End If

                        Return m_lazyMeParameter
                    End If
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Dim type = ContainingAssembly.GetSpecialType(SpecialType.System_Void)
                    ' WARN: We assume that if System_Void was not found we would never reach 
                    '       this point because the error should have been/processed generated earlier
                    Debug.Assert(type.GetUseSiteErrorInfo() Is Nothing)
                    Return type
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(attributes)

                Dim sourceType = TryCast(ContainingSymbol, SourceMemberContainerTypeSymbol)

                ' if parent is not from source, it must be a frame.
                ' frame is already marked as generated, no need to mark members.
                If sourceType Is Nothing Then
                    Return
                End If

                ' Attribute: System.Runtime.CompilerServices.CompilerGeneratedAttribute()
                AddSynthesizedAttribute(attributes, sourceType.DeclaringCompilation.SynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            End Sub

            Public Overrides ReadOnly Property IsVararg As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides ReadOnly Property AssociatedSymbol As Symbol
                Get
                    Return Nothing
                End Get
            End Property

            Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
                Get
                    Return If(IsShared, Microsoft.Cci.CallingConvention.Default, Microsoft.Cci.CallingConvention.HasThis) Or
                            If(IsGenericMethod, Microsoft.Cci.CallingConvention.Generic, Microsoft.Cci.CallingConvention.Default)
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return m_containingType
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
                Get
                    Return m_containingType
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return Accessibility.Public
                End Get
            End Property

            Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
                Get
                    Return ImmutableArray(Of MethodSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property IsExtensionMethod As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsExternalMethod As Boolean
                Get
                    Return False
                End Get
            End Property

            Public NotOverridable Overrides Function GetDllImportData() As DllImportData
                Return Nothing
            End Function

            Friend NotOverridable Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
                Get
                    Return Nothing
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
                Get
                    Return Nothing
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
                Throw ExceptionUtilities.Unreachable
            End Function

            Public Overrides ReadOnly Property IsMustOverride As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsNotOverridable As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverloads As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverridable As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverrides As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return m_isShared
                End Get
            End Property

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return ReturnType.IsVoidType()
                End Get
            End Property

            Public Overrides ReadOnly Property IsAsync As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsIterator As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
                Return ImmutableArray(Of String).Empty
            End Function

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Dim node As VisualBasicSyntaxNode = Me.Syntax
                    Dim asLambda = TryCast(node, LambdaExpressionSyntax)
                    If asLambda IsNot Nothing Then
                        node = asLambda.Begin
                    Else
                        Dim asMethod = TryCast(node, MethodBlockBaseSyntax)
                        If asMethod IsNot Nothing Then
                            node = asMethod.Begin
                        End If
                    End If

                    Return ImmutableArray.Create(Of SyntaxReference)(node.GetReference)
                End Get
            End Property

            Public Overrides ReadOnly Property MethodKind As MethodKind
                Get
                    Return MethodKind.Ordinary
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return ImmutableArray(Of CustomModifier).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    Return ImmutableArray(Of TypeSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return ImmutableArray(Of TypeParameterSymbol).Empty
                End Get
            End Property

            Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
                Get
                    Return m_SyntaxNode
                End Get
            End Property

            Friend Overridable ReadOnly Property TypeMap As TypeSubstitution
                Get
                    Throw ExceptionUtilities.Unreachable
                End Get
            End Property

            Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Return Nothing
                End Get
            End Property
        End Class

    End Class
End Namespace
