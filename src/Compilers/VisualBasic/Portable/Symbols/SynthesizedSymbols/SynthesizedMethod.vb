' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Base for synthesized Lambda methods. 
    ''' Just provides a bunch of defaults
    ''' </summary>
    Friend MustInherit Class SynthesizedMethod
        Inherits SynthesizedMethodBase

        Private ReadOnly _isShared As Boolean
        Private ReadOnly _name As String
        Private ReadOnly _syntaxNodeOpt As SyntaxNode

        Friend Sub New(
                syntaxNode As SyntaxNode,
                containingSymbol As NamedTypeSymbol,
                name As String,
                isShared As Boolean
            )
            MyBase.New(containingSymbol)
            Me._syntaxNodeOpt = syntaxNode
            Me._isShared = isShared
            Me._name = name
        End Sub

        Private Shared ReadOnly s_typeSubstitutionFactory As Func(Of Symbol, TypeSubstitution) =
                Function(container) DirectCast(container, SynthesizedMethod).TypeMap

        Friend Shared ReadOnly CreateTypeParameter As Func(Of TypeParameterSymbol, Symbol, TypeParameterSymbol) =
                Function(typeParameter, container) New SynthesizedClonedTypeParameterSymbol(typeParameter, container, typeParameter.Name, s_typeSubstitutionFactory)

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
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return ImmutableArray(Of ParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Dim type = ContainingAssembly.GetSpecialType(SpecialType.System_Void)
                ' WARN: We assume that if System_Void was not found we would never reach 
                '       this point because the error should have been/processed generated earlier
                Debug.Assert(type.GetUseSiteInfo().DiagnosticInfo Is Nothing)
                Return type
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Dim sourceType = TryCast(ContainingSymbol, SourceMemberContainerTypeSymbol)

            ' if parent is not from source, it must be a frame.
            ' frame is already marked as generated, no need to mark members.
            If sourceType Is Nothing Then
                Return
            End If

            ' Attribute: System.Runtime.CompilerServices.CompilerGeneratedAttribute()
            AddSynthesizedAttribute(attributes, sourceType.DeclaringCompilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
        End Sub

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return Nothing
            End Get
        End Property

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
                Return _isShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return ReturnType.IsVoidType()
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Dim node As SyntaxNode = Me.Syntax
                Dim asLambda = TryCast(node, LambdaExpressionSyntax)
                If asLambda IsNot Nothing Then
                    node = asLambda.SubOrFunctionHeader
                Else
                    Dim asMethod = TryCast(node, MethodBlockBaseSyntax)
                    If asMethod IsNot Nothing Then
                        node = asMethod.BlockStatement
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

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return _syntaxNodeOpt
            End Get
        End Property

        Friend Overridable ReadOnly Property TypeMap As TypeSubstitution
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property
    End Class
End Namespace
