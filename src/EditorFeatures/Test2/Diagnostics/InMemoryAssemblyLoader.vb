' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests

    Public Class InMemoryAssemblyLoader
        Implements IAnalyzerAssemblyLoader

        Public Sub AddDependencyLocation(fullPath As String) Implements IAnalyzerAssemblyLoader.AddDependencyLocation
        End Sub

        Public Function LoadFromPath(fullPath As String) As Assembly Implements IAnalyzerAssemblyLoader.LoadFromPath
            Dim bytes = File.ReadAllBytes(fullPath)
            Return Assembly.Load(bytes)
        End Function
    End Class
End Namespace
