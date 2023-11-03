﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification

Namespace Microsoft.CodeAnalysis.Diagnostics
    ''' <summary>
    ''' Provides Visual Basic analyzers a convenient access to editorconfig options with fallback to IDE default values.
    ''' </summary>
    Friend Structure VisualBasicAnalyzerOptionsProvider
        ''' <summary>
        ''' Document editorconfig options.
        ''' </summary>
        Private ReadOnly _options As IOptionsReader

        ''' <summary>
        ''' Fallback options - the default options in Code Style layer.
        ''' </summary>
        Private ReadOnly _fallbackOptions As IdeAnalyzerOptions

        Public Sub New(options As IOptionsReader, fallbackOptions As IdeAnalyzerOptions)
            _options = options
            _fallbackOptions = fallbackOptions
        End Sub

        Public Sub New(options As IOptionsReader, fallbackOptions As AnalyzerOptions)
            MyClass.New(options, fallbackOptions.GetIdeOptions())
        End Sub

        Public Function GetSimplifierOptions() As VisualBasicSimplifierOptions
            Return New VisualBasicSimplifierOptions(_options, FallbackSimplifierOptions)
        End Function

        Public ReadOnly Property PreferredModifierOrder As CodeStyleOption2(Of String)
            Get
                Return GetOption(VisualBasicCodeStyleOptions.PreferredModifierOrder, FallbackCodeStyleOptions.PreferredModifierOrder)
            End Get
        End Property

        Public ReadOnly Property PreferIsNotExpression As CodeStyleOption2(Of Boolean)
            Get
                Return GetOption(VisualBasicCodeStyleOptions.PreferIsNotExpression, FallbackCodeStyleOptions.PreferIsNotExpression)
            End Get
        End Property

        Public ReadOnly Property PreferSimplifiedObjectCreation As CodeStyleOption2(Of Boolean)
            Get
                Return GetOption(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, FallbackCodeStyleOptions.PreferSimplifiedObjectCreation)
            End Get
        End Property

        Public ReadOnly Property UnusedValueExpressionStatement As CodeStyleOption2(Of UnusedValuePreference)
            Get
                Return GetOption(VisualBasicCodeStyleOptions.UnusedValueExpressionStatement, FallbackCodeStyleOptions.UnusedValueExpressionStatement)
            End Get
        End Property

        Public ReadOnly Property UnusedValueAssignment As CodeStyleOption2(Of UnusedValuePreference)
            Get
                Return GetOption(VisualBasicCodeStyleOptions.UnusedValueAssignment, FallbackCodeStyleOptions.UnusedValueAssignment)
            End Get
        End Property

        Private Function GetOption(Of TValue)([option] As Option2(Of CodeStyleOption2(Of TValue)), defaultValue As CodeStyleOption2(Of TValue)) As CodeStyleOption2(Of TValue)
            Return _options.GetOption([option], defaultValue)
        End Function

        Private ReadOnly Property FallbackSimplifierOptions As VisualBasicSimplifierOptions
            Get
                Return If(DirectCast(_fallbackOptions.CleanupOptions?.SimplifierOptions, VisualBasicSimplifierOptions), VisualBasicSimplifierOptions.Default)
            End Get
        End Property

        Private ReadOnly Property FallbackCodeStyleOptions As VisualBasicIdeCodeStyleOptions
            Get
                Return If(DirectCast(_fallbackOptions.CodeStyleOptions, VisualBasicIdeCodeStyleOptions), VisualBasicIdeCodeStyleOptions.Default)
            End Get
        End Property

        Public Shared Narrowing Operator CType(provider As AnalyzerOptionsProvider) As VisualBasicAnalyzerOptionsProvider
            Return New VisualBasicAnalyzerOptionsProvider(provider.GetAnalyzerConfigOptions(), provider.GetFallbackOptions())
        End Operator

        Public Shared Widening Operator CType(provider As VisualBasicAnalyzerOptionsProvider) As AnalyzerOptionsProvider
            Return New AnalyzerOptionsProvider(provider._options, LanguageNames.VisualBasic, provider._fallbackOptions)
        End Operator
    End Structure

    Friend Module VisualBasicAnalyzerOptionsProviders
        <Extension>
        Public Function GetVisualBasicAnalyzerOptions(options As AnalyzerOptions, syntaxTree As SyntaxTree) As VisualBasicAnalyzerOptionsProvider
            Return New VisualBasicAnalyzerOptionsProvider(options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree).GetOptionsReader(), options)
        End Function

        <Extension>
        Public Function GetVisualBasicAnalyzerOptions(context As SemanticModelAnalysisContext) As VisualBasicAnalyzerOptionsProvider
            Return GetVisualBasicAnalyzerOptions(context.Options, context.SemanticModel.SyntaxTree)
        End Function

        <Extension>
        Public Function GetVisualBasicAnalyzerOptions(context As SyntaxNodeAnalysisContext) As VisualBasicAnalyzerOptionsProvider
            Return GetVisualBasicAnalyzerOptions(context.Options, context.Node.SyntaxTree)
        End Function

        <Extension>
        Public Function GetVisualBasicAnalyzerOptions(context As SyntaxTreeAnalysisContext) As VisualBasicAnalyzerOptionsProvider
            Return GetVisualBasicAnalyzerOptions(context.Options, context.Tree)
        End Function

        <Extension>
        Public Function GetVisualBasicAnalyzerOptions(context As OperationAnalysisContext) As VisualBasicAnalyzerOptionsProvider
            Return GetVisualBasicAnalyzerOptions(context.Options, context.Operation.Syntax.SyntaxTree)
        End Function

        <Extension>
        Public Function GetVisualBasicAnalyzerOptions(context As CodeBlockAnalysisContext) As VisualBasicAnalyzerOptionsProvider
            Return GetVisualBasicAnalyzerOptions(context.Options, context.SemanticModel.SyntaxTree)
        End Function
    End Module
End Namespace
