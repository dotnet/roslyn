' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            diagnostics As DiagnosticBag)
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

        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return DirectCast(_syntaxReference.GetSyntax(), VisualBasicSyntaxNode)
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Dim syntax As VisualBasicSyntaxNode = Me.Syntax
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
            diagnostics As DiagnosticBag,
            ByRef resultType As TypeSymbol,
            ByRef returnType As TypeSymbol)

            Dim submissionReturnType = If(compilation.SubmissionReturnType, GetType(Object))
            Dim taskT = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)
            Dim useSiteDiagnostic = taskT.GetUseSiteErrorInfo()
            If useSiteDiagnostic IsNot Nothing Then
                diagnostics.Add(useSiteDiagnostic, NoLocation.Singleton)
            End If
            resultType = compilation.GetTypeByReflectionType(submissionReturnType, diagnostics)
            returnType = taskT.Construct(resultType)
        End Sub

    End Class

End Namespace