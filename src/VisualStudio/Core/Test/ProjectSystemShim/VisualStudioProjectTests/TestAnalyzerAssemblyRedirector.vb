' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.IO
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <Export(GetType(IAnalyzerAssemblyRedirector))>
    Friend NotInheritable Class TestAnalyzerAssemblyRedirector
        Implements IAnalyzerAssemblyRedirector

        <ImportingConstructor, Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function RedirectPath(fullPath As String) As String Implements IAnalyzerAssemblyRedirector.RedirectPath
            If fullPath.IndexOf("Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return Path.ChangeExtension(fullPath, ".redirected.dll")
            End If

            Return Nothing
        End Function
    End Class
End Namespace
