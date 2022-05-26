' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Indicates whether the compiler accepted or rejected the method during overload resolution.
    ''' </summary>
    Friend Enum MemberResolutionKind

        Applicable = OverloadResolution.CandidateAnalysisResultState.Applicable

        HasUseSiteError = OverloadResolution.CandidateAnalysisResultState.HasUseSiteError
        Ambiguous = OverloadResolution.CandidateAnalysisResultState.Ambiguous
        BadGenericArity = OverloadResolution.CandidateAnalysisResultState.BadGenericArity
        ArgumentCountMismatch = OverloadResolution.CandidateAnalysisResultState.ArgumentCountMismatch
        TypeInferenceFailed = OverloadResolution.CandidateAnalysisResultState.TypeInferenceFailed
        ArgumentMismatch = OverloadResolution.CandidateAnalysisResultState.ArgumentMismatch
        GenericConstraintsViolated = OverloadResolution.CandidateAnalysisResultState.GenericConstraintsViolated
        RequiresNarrowing = OverloadResolution.CandidateAnalysisResultState.RequiresNarrowing
        RequiresNarrowingNotFromObject = OverloadResolution.CandidateAnalysisResultState.RequiresNarrowingNotFromObject
        ExtensionMethodVsInstanceMethod = OverloadResolution.CandidateAnalysisResultState.ExtensionMethodVsInstanceMethod
        Shadowed = OverloadResolution.CandidateAnalysisResultState.Shadowed
        LessApplicable = OverloadResolution.CandidateAnalysisResultState.LessApplicable

    End Enum

    ''' <summary>
    ''' Represents the results of overload resolution for a single method.
    ''' </summary>
    Friend Structure MemberResolutionResult(Of TMember As Symbol)

        Private ReadOnly _candidate As OverloadResolution.CandidateAnalysisResult
        Private ReadOnly _isValid As Boolean

        Friend Sub New(candidate As OverloadResolution.CandidateAnalysisResult, isValid As Boolean)
            Debug.Assert(Not isValid OrElse candidate.State = OverloadResolution.CandidateAnalysisResultState.Applicable)

            _candidate = candidate
            _isValid = isValid
        End Sub

        ''' <summary>
        ''' The method or property considered during overload resolution.
        ''' </summary>
        Public ReadOnly Property Member As TMember
            Get
                Return DirectCast(_candidate.Candidate.UnderlyingSymbol, TMember)
            End Get
        End Property

        ''' <summary>
        ''' Indicates why the compiler accepted or rejected the method during overload resolution.
        ''' </summary>
        Public ReadOnly Property Resolution As MemberResolutionKind
            Get
                If _candidate.State = OverloadResolution.CandidateAnalysisResultState.HasUnsupportedMetadata Then
                    Return MemberResolutionKind.HasUseSiteError
                End If

                Return CType(_candidate.State, MemberResolutionKind)
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the compiler accepted this method as the sole correct result of overload resolution.
        ''' </summary>
        Public ReadOnly Property IsValid As Boolean
            Get
                Return _isValid
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the method is considered in its expanded param array form.
        ''' </summary>
        Friend ReadOnly Property IsExpandedParamArrayForm As Boolean
            Get
                Return _candidate.IsExpandedParamArrayForm
            End Get
        End Property
    End Structure
End Namespace
