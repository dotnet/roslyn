' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Module SourceGeneratorVerifier
    Public Function Create(Of TSourceGenerator As {ISourceGenerator, New})() As SourceGeneratorVerifier(Of TSourceGenerator)
        Return New SourceGeneratorVerifier(Of TSourceGenerator)
    End Function
End Module
