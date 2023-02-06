' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicSourceGeneratorVerifier(Of TSourceGenerator As {ISourceGenerator, New}, TVerifier As {IVerifier, New})
    Inherits SourceGeneratorVerifier(Of TSourceGenerator, VisualBasicSourceGeneratorTest(Of TSourceGenerator, TVerifier), TVerifier)
End Class
