' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
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
                ERRID.ERRWRN_NextAvailable
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

    End Class

End Namespace
