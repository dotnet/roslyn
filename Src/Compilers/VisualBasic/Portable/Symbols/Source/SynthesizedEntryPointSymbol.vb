' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents an interactive code entry point that is inserted into the compilation if there is not an existing one.
    ''' </summary>
    Friend NotInheritable Class SynthesizedEntryPointSymbol
        Inherits SynthesizedMethodBase

        Private ReadOnly _containingType As NamedTypeSymbol
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _name As String

        Friend Sub New(containingType As NamedTypeSymbol, returnType As TypeSymbol)
            MyBase.New(containingType)

            _containingType = containingType
            If containingType.ContainingAssembly.IsInteractive Then
                ' TODO: report error if the type doesn't exist
                Dim executionStateType = DeclaringCompilation.GetWellKnownType(WellKnownType.Roslyn_Scripting_Runtime_ScriptExecutionState)
                _parameters = ImmutableArray.Create(Of ParameterSymbol)(New SynthesizedParameterSymbol(Me, executionStateType, ordinal:=0, isByRef:=False, name:="executionState"))
                _name = "<Factory>"
            Else
                _parameters = ImmutableArray(Of ParameterSymbol).Empty
                _name = "<Main>"
            End If

            If Me.DeclaringCompilation.IsSubmission Then
                returnType = Me.DeclaringCompilation.GetSpecialType(SpecialType.System_Object)
            End If

            _returnType = returnType
        End Sub

        Friend Function CreateBody() As BoundBlock
            Return If(DeclaringCompilation.IsSubmission, CreateSubmissionFactoryBody(), CreateScriptBody())
        End Function

        ' Generates:
        ' 
        ' private static void {Main}()
        ' {
        '     new {ThisScriptClass}();
        ' }
        Private Function CreateScriptBody() As BoundBlock
            Dim syntax = VisualBasicSyntaxTree.Dummy.GetRoot()

            Debug.Assert(ContainingType.IsScriptClass)
            Return New BoundBlock(syntax, Nothing,
                ImmutableArray(Of LocalSymbol).Empty,
                ImmutableArray.Create(Of BoundStatement)(
                    New BoundExpressionStatement(syntax,
                        New BoundObjectCreationExpression(syntax,
                            _containingType.InstanceConstructors.Single(),
                            ImmutableArray(Of BoundExpression).Empty,
                            Nothing,
                            _containingType)),
                    New BoundReturnStatement(syntax, Nothing, Nothing, Nothing)))
        End Function

        ' Generates:
        ' 
        ' private static object {Factory}(InteractiveSession session) 
        ' {
        '    T submissionResult;
        '    new {ThisScriptClass}(session, out submissionResult);
        '    return submissionResult;
        ' }
        Private Function CreateSubmissionFactoryBody() As BoundBlock
            Debug.Assert(_containingType.TypeKind = TypeKind.Submission)
            Dim syntax = VisualBasicSyntaxTree.Dummy.GetRoot()

            Dim interactiveSessionParam = New BoundParameter(syntax, Parameters(0), Parameters(0).Type)

            Dim ctor = _containingType.InstanceConstructors.Single()
            Debug.Assert(TypeOf ctor Is SynthesizedSubmissionConstructorSymbol)
            Debug.Assert(ctor.ParameterCount = 2)

            Dim submissionResultType = ctor.Parameters(1).Type
            Dim resultLocal = New SynthesizedLocal(ctor, submissionResultType, SynthesizedLocalKind.None)
            Dim localReference = New BoundLocal(syntax, localSymbol:=resultLocal, isLValue:=True, type:=submissionResultType)

            Dim submissionResult As BoundExpression = localReference
            If submissionResultType.IsStructureType() AndAlso Me._returnType.SpecialType = SpecialType.System_Object Then
                submissionResult = New BoundConversion(syntax, submissionResult, ConversionKind.Widening, False, True, Me._returnType)
            End If

            Return New BoundBlock(syntax, Nothing,
                ImmutableArray.Create(Of LocalSymbol)(resultLocal),
                ImmutableArray.Create(Of BoundStatement)(
                    New BoundExpressionStatement(syntax,
                        New BoundObjectCreationExpression(syntax,
                            ctor,
                            ImmutableArray.Create(Of BoundExpression)(interactiveSessionParam, localReference),
                            Nothing,
                            _containingType)),
                    New BoundReturnStatement(syntax, submissionResult.MakeRValue(), Nothing, Nothing)))
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _returnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
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

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                ' the method is Shared
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return LexicalSortKey.NotInSource
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
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

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return False
        End Function

    End Class
End Namespace

