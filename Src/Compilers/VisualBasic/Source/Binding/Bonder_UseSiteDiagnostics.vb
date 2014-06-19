'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------


Namespace Microsoft.CodeAnalysis.VisualBasic.Semantics
    Partial Friend Class Binder

        ''' <summary>
        ''' Appends diagnostics from useSiteDiagnostics into diagnostics and returns True if there were any errors.
        ''' </summary>
        Friend Shared Function AppendUseSiteDiagnostics(
            node As SyntaxNode,
            useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            diagnostics As DiagnosticBag
        ) As Boolean

            If useSiteDiagnostics.IsNullOrEmpty Then
                Return False
            End If

            For Each info In useSiteDiagnostics
                Debug.Assert(info.Severity = DiagnosticSeverity.Error)
                ReportDiagnostic(diagnostics, node, info)
            Next

            Return True
        End Function

        Friend Shared Function AppendUseSiteDiagnostics(
            node As SyntaxNodeOrToken,
            useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            diagnostics As DiagnosticBag
        ) As Boolean

            If useSiteDiagnostics.IsNullOrEmpty Then
                Return False
            End If

            For Each info In useSiteDiagnostics
                Debug.Assert(info.Severity = DiagnosticSeverity.Error)
                ReportDiagnostic(diagnostics, node, info)
            Next

            Return True
        End Function

        Friend Shared Function AppendUseSiteDiagnostics(
            location As Location,
            useSiteDiagnostics As HashSet(Of DiagnosticInfo),
            diagnostics As DiagnosticBag
        ) As Boolean

            If useSiteDiagnostics.IsNullOrEmpty Then
                Return False
            End If

            For Each info In useSiteDiagnostics
                Debug.Assert(info.Severity = DiagnosticSeverity.Error)
                ReportDiagnostic(diagnostics, location, info)
            Next

            Return True
        End Function

    End Class
End Namespace
