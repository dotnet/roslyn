'-----------------------------------------------------------------------------------------------------------
'
'  Copyright (c) Microsoft Corporation.  All rights reserved.
'
'-----------------------------------------------------------------------------------------------------------

Imports Microsoft.VisualStudio.Shell.Interop
Imports System

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Helpers for the C# language version switch
    ''' </summary>
    Friend NotInheritable Class CSharpLanguageVersionUtilities

        Public Const LanguageVersion_PropertyName As String = "LanguageVersion"

        ''' <summary>
        ''' Helper to get all language versions
        ''' </summary>
        Public Shared Function GetAllLanguageVersions() As CSharpLanguageVersion()

            Return New CSharpLanguageVersion() {
                CSharpLanguageVersion.Default,
                CSharpLanguageVersion.ISO1,
                CSharpLanguageVersion.ISO2,
                CSharpLanguageVersion.Version3,
                CSharpLanguageVersion.Version4,
                CSharpLanguageVersion.Version5,
                CSharpLanguageVersion.Version6}

        End Function

    End Class

End Namespace
