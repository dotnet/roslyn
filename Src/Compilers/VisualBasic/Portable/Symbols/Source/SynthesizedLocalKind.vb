' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum SynthesizedLocalKind As Short
        LoweringTemp = CommonSynthesizedLocalKind.LoweringTemp
        None = CommonSynthesizedLocalKind.None       ' not a temp or a regular nameless temp, i.e., it is not synthesized.
        FirstLongLived = 0

        ''' <summary>
        ''' Values greater than or equal to <see cref="FirstIgnoredByExpressionCompiler"/> and
        ''' less than or equal to <see cref="LastIgnoredByExpressionCompiler"/> should be flagged
        ''' so that they will be ignored by the expression compiler.
        ''' </summary>
        FirstIgnoredByExpressionCompiler = 0

        Lock = 2
        [Using]
        ForEachEnumerator
        ForEachArray
        ForEachArrayIndex
        LockTaken
        [With]

        ForLimit
        ForStep
        ForLoopObject
        ForDirection

        'TODO:
        ' degenerate select key (can we EnC when stopped on case?)

        ' XmlInExpressionLambda locals are always lifted and must have distinct names.
        XmlInExpressionLambda

        ' TODO: I am not sure we need these
        OnErrorActiveHandler
        OnErrorResumeTarget
        OnErrorCurrentStatement
        OnErrorCurrentLine

        ''' <summary>
        ''' Values greater than or equal to <see cref="FirstIgnoredByExpressionCompiler"/> and
        ''' less than or equal to <see cref="LastIgnoredByExpressionCompiler"/> should be flagged
        ''' so that they will be ignored by the expression compiler.
        ''' </summary>
        LastIgnoredByExpressionCompiler = OnErrorCurrentLine

        StateMachineReturnValue
        StateMachineException
        StateMachineCachedState

        LambdaDisplayClass ' Local variable that holds on the display class instance

        ''' <summary>
        ''' Return value variable of lambdas and operators, which can't be accessed from user code.
        ''' </summary>
        FunctionReturnValue
    End Enum

    Module SynthesizedLocalKindExtensions
        <Extension>
        Friend Function IsLongLived(ByVal kind As SynthesizedLocalKind) As Boolean
            Return kind >= SynthesizedLocalKind.FirstLongLived
        End Function

        <Extension>
        Friend Function IsNamed(ByVal kind As SynthesizedLocalKind, optimizations As OptimizationLevel) As Boolean
            If optimizations = OptimizationLevel.Debug Then
                Return IsLongLived(kind)
            End If

            Select Case kind
                ' The following variables should be named whenever we emit any debugging information,
                ' so that EE can recognize these variables by name. Synthesized variables that EE doesn't 
                ' need to know about don't need to be named unless we are emitting debug info for EnC.
                Case SynthesizedLocalKind.LambdaDisplayClass
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension>
        Friend Function PdbAttributes(ByVal kind As SynthesizedLocalKind) As UInteger
            Return If(SynthesizedLocalKind.FirstIgnoredByExpressionCompiler <= kind AndAlso kind <= SynthesizedLocalKind.LastIgnoredByExpressionCompiler,
                Cci.PdbWriter.HiddenLocalAttributesValue,
                Cci.PdbWriter.DefaultLocalAttributesValue)
        End Function
    End Module

End Namespace