' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicSourceGeneratorVerifier(Of TSourceGenerator As New, TVerifier As {IVerifier, New})
    Inherits SourceGeneratorVerifier(Of TSourceGenerator, VisualBasicSourceGeneratorTest(Of TSourceGenerator, TVerifier), TVerifier)
End Class
