' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Testing

<Obsolete(ObsoleteMessages.FrameworkPackages)>
Module SourceGeneratorVerifier
    Public Function Create(Of TSourceGenerator As New)() As SourceGeneratorVerifier(Of TSourceGenerator)
        Return New SourceGeneratorVerifier(Of TSourceGenerator)
    End Function
End Module
