' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Module ExtensionMethods
        <Extension>
        Public Function HasMetadataReference(project As Project, path As String) As Boolean
            Return project.MetadataReferences.Cast(Of PortableExecutableReference).Any(Function(vsReference) String.Equals(vsReference.FilePath, path, StringComparison.OrdinalIgnoreCase))
        End Function
    End Module
End Namespace
