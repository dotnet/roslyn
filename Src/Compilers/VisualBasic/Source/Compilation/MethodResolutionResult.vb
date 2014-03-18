' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private ReadOnly m_Candidate As OverloadResolution.CandidateAnalysisResult
        Private ReadOnly m_IsValid As Boolean

        Friend Sub New(candidate As OverloadResolution.CandidateAnalysisResult, isValid As Boolean)
            Debug.Assert(Not isValid OrElse candidate.State = OverloadResolution.CandidateAnalysisResultState.Applicable)

            m_Candidate = candidate
            m_IsValid = isValid
        End Sub

        ''' <summary>
        ''' The method or property considered during overload resolution.
        ''' </summary>
        Public ReadOnly Property Member As TMember
            Get
                Return DirectCast(m_Candidate.Candidate.UnderlyingSymbol, TMember)
            End Get
        End Property

        ''' <summary>
        ''' Indicates why the compiler accepted or rejected the method during overload resolution.
        ''' </summary>
        Public ReadOnly Property Resolution As MemberResolutionKind
            Get
                If m_Candidate.State = OverloadResolution.CandidateAnalysisResultState.HasUnsupportedMetadata Then
                    Return MemberResolutionKind.HasUseSiteError
                End If

                Return CType(m_Candidate.State, MemberResolutionKind)
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the compiler accepted this method as the sole correct result of overload resolution.
        ''' </summary>
        Public ReadOnly Property IsValid As Boolean
            Get
                Return m_IsValid
            End Get
        End Property

        ''' <summary>
        ''' Returns true if the method is considered in its expanded param array form.
        ''' </summary>
        Friend ReadOnly Property IsExpandedParamArrayForm As Boolean
            Get
                Return m_Candidate.IsExpandedParamArrayForm
            End Get
        End Property

        Friend Function ToCommon(Of TSymbol As ISymbol)() As CommonMemberResolutionResult(Of TSymbol)
            Return New CommonMemberResolutionResult(Of TSymbol)(
                DirectCast(DirectCast(Me.Member, ISymbol), TSymbol),
                ConvertResolution(Me.Resolution),
                Me.IsValid)
        End Function

        Private Shared Function ConvertResolution(resolution As MemberResolutionKind) As CommonMemberResolutionKind
            Select Case resolution
                Case MemberResolutionKind.Applicable
                    Return CommonMemberResolutionKind.Applicable
                Case MemberResolutionKind.HasUseSiteError
                    Return CommonMemberResolutionKind.UseSiteError
                Case MemberResolutionKind.TypeInferenceFailed
                    Return CommonMemberResolutionKind.TypeInferenceFailed
                Case Else
                    Return CommonMemberResolutionKind.Worse
            End Select
        End Function
    End Structure
End Namespace