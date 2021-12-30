' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis

    Friend Module MissingRuntimeMemberDiagnosticHelper

        Friend Const MyVBNamespace As String = "My"

        ' Details on these types and the feature name which is displayed in the diagnostic
        ' for those items missing in VB Core compilation.
        Private ReadOnly s_metadataNames As New Dictionary(Of String, String) From {
                                                                                    {"Microsoft.VisualBasic.CompilerServices.Operators", "Late binding"},
                                                                                    {"Microsoft.VisualBasic.CompilerServices.NewLateBinding", "Late binding"},
                                                                                    {"Microsoft.VisualBasic.CompilerServices.LikeOperator", "Like operator"},
                                                                                    {"Microsoft.VisualBasic.CompilerServices.ProjectData", "Unstructured exception handling"},
                                                                                    {"Microsoft.VisualBasic.CompilerServices.ProjectData.CreateProjectError", "Unstructured exception handling"}
                                                                                 }

        Friend Function GetDiagnosticForMissingRuntimeHelper(typename As String, membername As String, embedVBCoreRuntime As Boolean) As DiagnosticInfo
            Dim diag As DiagnosticInfo
            ' Depending upon whether the vbruntime embed compilation option is used and this is a function we have intentionally
            ' omitted from VB will determine which diagnostic is reported.
            '  Examples 
            '     (Late binding, old style error handling, like operator, Err Object) - with VB Embed - report new diagnostic
            '     (Late binding, old style error handling, like operator)  - without VB Embed just no reference to microsoft.visualbasic.dll - report old diagnostic
            Dim verifiedTypename As String = ""
            s_metadataNames.TryGetValue(typename, verifiedTypename)
            If embedVBCoreRuntime AndAlso (Not String.IsNullOrEmpty(verifiedTypename)) Then
                'Check to see the compilation options included VB.
                diag = ErrorFactory.ErrorInfo(ERRID.ERR_PlatformDoesntSupport, verifiedTypename)
            Else
                diag = ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper, typename & "." & membername)
            End If

            Return diag
        End Function
    End Module
End Namespace
