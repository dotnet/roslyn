' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
    <Export(GetType(IWorkspaceDiagnosticAnalyzerProviderService))>
    Friend Class VisualBasicCompilerDiagnosticAnalyzerProviderService
        Implements IWorkspaceDiagnosticAnalyzerProviderService

        Private ReadOnly _info As HostDiagnosticAnalyzerPackage

        Public Sub New()
            _info = New HostDiagnosticAnalyzerPackage("VisualBasicWorkspace", GetCompilerAnalyzerAssemblies().ToImmutableArray())
        End Sub

        Private Shared Function GetCompilerAnalyzerAssemblies() As IEnumerable(Of String)
            Return {GetType(VisualBasicCompilerDiagnosticAnalyzer).Assembly.Location}
        End Function

        Public Function GetAnalyzerAssemblyLoader() As IAnalyzerAssemblyLoader Implements IWorkspaceDiagnosticAnalyzerProviderService.GetAnalyzerAssemblyLoader
            Return FromFileLoader.Instance
        End Function

        Public Function GetHostDiagnosticAnalyzerPackages() As IEnumerable(Of HostDiagnosticAnalyzerPackage) Implements IWorkspaceDiagnosticAnalyzerProviderService.GetHostDiagnosticAnalyzerPackages
            Return {_info}
        End Function

        Public Class FromFileLoader
            Implements IAnalyzerAssemblyLoader

            Public Shared Instance As FromFileLoader = New FromFileLoader()

            Public Sub AddDependencyLocation(fullPath As String) Implements IAnalyzerAssemblyLoader.AddDependencyLocation
            End Sub

            Public Function LoadFromPath(fullPath As String) As Assembly Implements IAnalyzerAssemblyLoader.LoadFromPath
                Return Assembly.LoadFrom(fullPath)
            End Function
        End Class
    End Class
End Namespace
