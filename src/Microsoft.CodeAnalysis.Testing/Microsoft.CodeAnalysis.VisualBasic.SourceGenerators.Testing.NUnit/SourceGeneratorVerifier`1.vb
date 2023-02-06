' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Testing.Verifiers

Public Class SourceGeneratorVerifier(Of TSourceGenerator As {ISourceGenerator, New})
    Inherits VisualBasicSourceGeneratorVerifier(Of TSourceGenerator, NUnitVerifier)
End Class
