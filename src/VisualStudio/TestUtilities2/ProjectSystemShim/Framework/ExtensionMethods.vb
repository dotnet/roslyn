' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
