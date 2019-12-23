' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module BindingDiagnosticBagExtensions

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(diagnostics As CodeAnalysis.BindingDiagnosticBag, code As ERRID, location As Location) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code)
            diagnostics.DiagnosticBag?.Add(New VBDiagnostic(info, location))
            Return info
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Function Add(diagnostics As CodeAnalysis.BindingDiagnosticBag, code As ERRID, location As Location, ParamArray args As Object()) As DiagnosticInfo
            Dim info = ErrorFactory.ErrorInfo(code, args)
            diagnostics.DiagnosticBag?.Add(New VBDiagnostic(info, location))
            Return info
        End Function

        <System.Runtime.CompilerServices.Extension()>
        Friend Sub Add(diagnostics As CodeAnalysis.BindingDiagnosticBag, info As DiagnosticInfo, location As Location)
            diagnostics.DiagnosticBag?.Add(New VBDiagnostic(info, location))
        End Sub

    End Module
End Namespace
