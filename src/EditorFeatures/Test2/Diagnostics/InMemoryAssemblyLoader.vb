' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
