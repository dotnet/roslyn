' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class SynthesizedInteractiveInitializerMethod
        Inherits SynthesizedMethodBase

        Friend Const InitializerName = "<Initialize>"

        Friend ReadOnly ResultType As TypeSymbol
        Friend ReadOnly FunctionLocal As LocalSymbol
        Friend ReadOnly ExitLabel As LabelSymbol

        Private ReadOnly _syntaxReference As SyntaxReference
        Private ReadOnly _returnType As TypeSymbol

        Friend Sub New(
            syntaxReference As SyntaxReference,
            containingType As SourceMemberContainerTypeSymbol,
            diagnostics As BindingDiagnosticBag)
            MyBase.New(containingType)

            _syntaxReference = syntaxReference
            CalculateReturnType(containingType.DeclaringCompilation, diagnostics, ResultType, _returnType)
            FunctionLocal = New SynthesizedLocal(Me, ResultType, SynthesizedLocalKind.FunctionReturnValue, Syntax)
            ExitLabel = New GeneratedLabelSymbol("exit")
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return InitializerName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsScriptInitializer As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Friend
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return True
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
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _returnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_containingType.Locations()
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return DirectCast(_syntaxReference.GetSyntax(), VisualBasicSyntaxNode)
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Dim syntax As SyntaxNode = Me.Syntax
            Return New BoundBlock(
                syntax,
                Nothing,
                ImmutableArray.Create(FunctionLocal),
                ImmutableArray.Create(Of BoundStatement)(New BoundLabelStatement(syntax, ExitLabel)))
        End Function

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Return DirectCast(m_containingType, SourceMemberContainerTypeSymbol).CalculateSyntaxOffsetInSynthesizedConstructor(localPosition, localTree, isShared:=False)
        End Function

        Private Shared Sub CalculateReturnType(
            compilation As VisualBasicCompilation,
            diagnostics As BindingDiagnosticBag,
            ByRef resultType As TypeSymbol,
            ByRef returnType As TypeSymbol)

            Dim submissionReturnType As Type = Nothing
            If compilation.ScriptCompilationInfo IsNot Nothing Then
                submissionReturnType = compilation.ScriptCompilationInfo.ReturnTypeOpt
            End If

            Dim taskT = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)
            diagnostics.Add(taskT.GetUseSiteInfo(), NoLocation.Singleton)

            ' If no explicit return type is set on ScriptCompilationInfo, default to
            ' System.Object from the target corlib. This allows cross compiling scripts
            ' to run on a target corlib that may differ from the host compiler's corlib.
            ' cf. https://github.com/dotnet/roslyn/issues/8506
            If submissionReturnType Is Nothing Then
                resultType = compilation.GetSpecialType(SpecialType.System_Object)
            Else
                resultType = compilation.GetTypeByReflectionType(submissionReturnType)
            End If
            returnType = taskT.Construct(resultType)
        End Sub

    End Class

End Namespace
