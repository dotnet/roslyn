' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis
    Public Class NotImplementedAnalyzerLoader
        Implements IAnalyzerAssemblyLoader

        Public Sub AddDependencyLocation(fullPath As String) Implements IAnalyzerAssemblyLoader.AddDependencyLocation
            Throw New NotImplementedException()
        End Sub

        Public Function LoadFromPath(fullPath As String) As Assembly Implements IAnalyzerAssemblyLoader.LoadFromPath
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
