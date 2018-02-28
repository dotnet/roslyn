' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageFeatures

    Friend Class VisualBasicRequiredLanguageVersionService
        Private ReadOnly Property _RequiredVersionsForFeature As New Dictionary(Of Feature, VisualBasicRequiredLanguageVersion)()
        Public Shared ReadOnly Instance As New VisualBasicRequiredLanguageVersionService

        Private Sub New()
            For Each f As Feature In [Enum].GetValues(GetType(Feature))
                Dim requiredVersion = New VisualBasicRequiredLanguageVersion(FeatureExtensions.GetLanguageVersion(f))
                _RequiredVersionsForFeature.Add(f, requiredVersion)
            Next
        End Sub

        Public Function GetRequiredLanguageVersion(f As Feature) As VisualBasicRequiredLanguageVersion
            Return _RequiredVersionsForFeature(f)
        End Function
    End Class

    Friend Class VisualBasicRequiredLanguageVersion
        Inherits RequiredLanguageVersion

        Friend ReadOnly Property Version As LanguageVersion

        Friend Sub New(version As LanguageVersion)
            Me.Version = version
        End Sub

        Public Overrides Function ToString() As String
            Return Version.ToDisplayString()
        End Function
    End Class

End Namespace
