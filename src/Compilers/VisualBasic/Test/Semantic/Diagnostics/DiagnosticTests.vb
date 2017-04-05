' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization

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
                ERRID.ERR_CannotUseGenericBaseTypeAcrossAssemblyBoundaries ' Not reported. See ImportsBinder.ShouldReportUseSiteErrorForAlias.
            }
            For Each id As ERRID In [Enum].GetValues(GetType(ERRID))
                If id >= ERRID.ERRWRN_Last Then
                    Continue For
                End If
                If Array.IndexOf(excludedIds, id) >= 0 Then
                    Continue For
                End If
                Dim message = ErrorFactory.IdToString(id, CultureInfo.InvariantCulture)
                Assert.False(String.IsNullOrEmpty(message))
            Next
        End Sub

    End Class

End Namespace