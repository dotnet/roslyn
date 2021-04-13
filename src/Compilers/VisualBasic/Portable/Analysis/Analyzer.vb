' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class Analyzer

        ''' <summary>
        ''' Analyzes method body for error conditions such as definite assignments, unreachable code etc...
        ''' 
        ''' This analysis is done when doing the full compile or when responding to GetCompileDiagnostics.
        ''' This method assume that the trees are already bound and will not do any rewriting/lowering
        ''' It is possible and common for this analysis to be done in the presence of errors.
        ''' </summary>
        Friend Shared Sub AnalyzeMethodBody(method As MethodSymbol,
                                            body As BoundBlock,
                                            diagnostics As DiagnosticBag)

            Debug.Assert(diagnostics IsNot Nothing)

            Dim diagBag As DiagnosticBag = diagnostics

            If method.IsImplicitlyDeclared AndAlso method.AssociatedSymbol IsNot Nothing AndAlso
               method.AssociatedSymbol.IsMyGroupCollectionProperty Then
                diagBag = DiagnosticBag.GetInstance()
            End If

            FlowAnalysisPass.Analyze(method, body, diagBag)

            ' the ForLoopVerification only just produces diagnostics. This should be done even if the 
            ' tree already has diagnostics
            ForLoopVerification.VerifyForLoops(body, diagBag)

            If diagBag IsNot diagnostics Then
                DirectCast(method.AssociatedSymbol, SynthesizedMyGroupCollectionPropertySymbol).RelocateDiagnostics(diagBag, diagnostics)
                diagBag.Free()
            End If
        End Sub
    End Class
End Namespace

