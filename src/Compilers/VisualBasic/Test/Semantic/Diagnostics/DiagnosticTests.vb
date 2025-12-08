' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class DiagnosticTests
        Inherits BasicTestBase

        ''' <summary>
        ''' Ensure string resources are included.
        ''' </summary>
        <Fact()>
        Public Sub Resources()
            Dim excludedIds = {
                ERRID.Void,
                ERRID.Unknown,
                ERRID.ERR_None,
                ERRID.ERR_CannotUseGenericBaseTypeAcrossAssemblyBoundaries, ' Not reported. See ImportsBinder.ShouldReportUseSiteErrorForAlias.
                ERRID.ERR_NextAvailable,
                ERRID.WRN_NextAvailable,
                ERRID.HDN_NextAvailable,
                ERRID.IDS_NextAvailable
            }
            For Each id As ERRID In [Enum].GetValues(GetType(ERRID))
                If Array.IndexOf(excludedIds, id) >= 0 Then
                    Continue For
                End If
                ' FEATURE_AutoProperties is the first feature in ERRID.
                If id >= ERRID.FEATURE_AutoProperties Then
                    Continue For
                End If
                Dim message = ErrorFactory.IdToString(id, CultureInfo.InvariantCulture)
                Assert.False(String.IsNullOrEmpty(message))
            Next
        End Sub

        ''' <summary>
        ''' ERRID should not have duplicates.
        ''' </summary>
        <WorkItem(20701, "https://github.com/dotnet/roslyn/issues/20701")>
        <Fact()>
        Public Sub NoDuplicates()
            Dim values = [Enum].GetValues(GetType(ERRID))
            Dim [set] = New HashSet(Of ERRID)
            For Each id As ERRID In values
                Assert.True([set].Add(id))
            Next
        End Sub

        <Fact()>
        Public Sub Features()
            Dim excludedFeatures = {
                Feature.InterpolatedStrings, ' https://github.com/dotnet/roslyn/issues/17761
                Feature.InferredTupleNames,
                Feature.NonTrailingNamedArguments
            }
            Dim [set] = New HashSet(Of ERRID)
            For Each feature As Feature In [Enum].GetValues(GetType(Feature))
                If Array.IndexOf(excludedFeatures, feature) >= 0 Then
                    Continue For
                End If
                Dim id = feature.GetResourceId()
                Assert.True([set].Add(id))
            Next
        End Sub

        <Fact>
        Public Sub ErrorMessagesWithinCorrectRanges()
            Const WarningsStart = ERRID.WRN_UseOfObsoleteSymbol2
            Const HiddenInfoStart = ERRID.HDN_UnusedImportClause
            Const IdsStart = ERRID.IDS_ProjectSettingsLocationName
            Const FeatureStart = ERRID.FEATURE_AutoProperties

            Dim legacyWarningsInErrorRange = {ERRID.WRN_BadSwitch, ERRID.WRN_NoConfigInResponseFile, ERRID.FTL_InvalidInputFileName, ERRID.WRN_IgnoreModuleManifest, ERRID.WRN_BadUILang}

            For Each errObj In [Enum].GetValues(GetType(ERRID))
                Dim err = DirectCast(errObj, ERRID)
                Dim errString = err.ToString()
                Select Case err
                    Case ERRID.Void, ERRID.Unknown
                        Continue For

                    Case <= ERRID.ERR_NextAvailable
                        If legacyWarningsInErrorRange.Contains(err) Then
                            Continue For
                        End If

                        Assert.True(errString.StartsWith("ERR_"), GetErrorMessage(errString))

                    Case < WarningsStart
                        Assert.True(False, GetErrorMessage(errString))

                    Case WarningsStart To ERRID.WRN_NextAvailable
                        Assert.True(errString.StartsWith("WRN_"), GetErrorMessage(errString))

                    Case < HiddenInfoStart
                        Assert.True(False, GetErrorMessage(errString))

                    Case HiddenInfoStart To ERRID.HDN_NextAvailable
                        Assert.True(errString.StartsWith("HDN_") OrElse errString.StartsWith("INF_"),
                                    GetErrorMessage(errString))

                    Case < IdsStart
                        Assert.True(False, GetErrorMessage(errString))

                    Case IdsStart To ERRID.IDS_NextAvailable
                        Assert.True(errString.StartsWith("IDS_"), GetErrorMessage(errString))

                    Case < FeatureStart
                        Assert.True(False, GetErrorMessage(errString))

                    Case >= FeatureStart
                        Assert.True(errString.StartsWith("FEATURE_"), GetErrorMessage(errString))
                End Select
            Next

        End Sub

        Private Shared Function GetErrorMessage(str As String) As String
            If str.StartsWith("ERR_") Then
                Return $"Error {str} should use {ERRID.ERR_NextAvailable} and increment the value."
            ElseIf str.StartsWith("WRN_") Then
                Return $"Warning {str} should use {ERRID.WRN_NextAvailable} and increment the value."
            ElseIf str.StartsWith("HDN_") OrElse str.StartsWith("INF_") Then
                Return $"Hidden or info diagnostic {str} should use {ERRID.HDN_NextAvailable} and increment the value."
            ElseIf str.StartsWith("IDS_") Then
                Return $"Id {str} should use {ERRID.IDS_NextAvailable} and increment the value."
            ElseIf str.StartsWith("FEATURE_") Then
                Return $"Feature code should go at the end of {NameOf(ERRID)}"
            Else
                Return $"{str} does not start with an approved prefix. Use ERR_ for errors, WRN_ for warnings, HDN_/INF_ for hidden or info diagnostics, IDS_ for ids, and FEATURE_ for feature codes"
            End If
        End Function

        <Fact>
        Public Sub TestIsBuildOnlyDiagnostic()
            For Each errObj In [Enum].GetValues(GetType(ERRID))
                Dim err = DirectCast(errObj, ERRID)

                ' ErrorFacts.IsBuildOnlyDiagnostic with throw if any new ERRID
                ' is added but not explicitly handled within it.
                ' Update ErrorFacts.IsBuildOnlyDiagnostic if the below call throws.
                Dim isBuildOnly = ErrorFacts.IsBuildOnlyDiagnostic(err)

                Select Case err
                    Case ERRID.ERR_TypeRefResolutionError3,
                         ERRID.ERR_MissingRuntimeHelper,
                         ERRID.ERR_CannotGotoNonScopeBlocksWithClosure,
                         ERRID.ERR_SymbolDefinedInAssembly,
                         ERRID.ERR_AsyncSubMain,
                         ERRID.ERR_EncUpdateFailedMissingSymbol,
                         ERRID.ERR_EncNoPIAReference,
                         ERRID.ERR_EncReferenceToAddedMember,
                         ERRID.ERR_EncUpdateRequiresEmittingExplicitInterfaceImplementationNotSupportedByTheRuntime
                        Assert.True(isBuildOnly, $"Check failed for ERRID.{err}")
                    Case Else
                        Assert.False(isBuildOnly, $"Check failed for ERRID.{err}")
                End Select
            Next
        End Sub
    End Class

End Namespace
