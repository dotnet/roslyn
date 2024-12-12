' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.VisualStudio.LanguageServices
Imports Microsoft.VisualStudio.LanguageServices.CSharp
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic

Friend Module Utilities
    Private Const CSharpLanguageServiceDllName As String = "Microsoft.VisualStudio.LanguageServices.CSharp.dll"

    Private Const VisualBasicLanguageServiceDllName As String = "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll"

    Private Const LanguageServiceDllName As String = "Microsoft.VisualStudio.LanguageServices.dll"

    Friend Function EvalResource(resourceReference As String) As String
        ' Start with 1 because to skip @ at the beginning, like @Show_completion_item_filters
        Dim resourcesIdentifier = resourceReference.Substring(1, resourceReference.IndexOf(";") - 1)
        Dim resources = resourceReference.Substring(resourceReference.IndexOf(";") + 1)
        Dim resourceDll As String = Nothing
        ' We reference the string in two ways
        ' 1. "@102;{13c3bbb4-f18f-4111-9f54-a0fb010d9194}" where the guid is the package guid. It is the recommended way to locate string.
        ' 2. "@Analysis;..\\Microsoft.VisualStudio.LanguageServices.dll". It is a special way we asked to locate string
        Dim localizedString As String = Nothing
        Dim culture = New CultureInfo("en")
        Dim packageGuid As Guid = Nothing
        If Guid.TryParse(resources, packageGuid) Then
            If packageGuid = Guids.CSharpPackageId Then
                Return CSharp.VSPackage.ResourceManager.GetString(resourcesIdentifier)
            ElseIf packageGuid = Guids.VisualBasicPackageId Then
                Return VisualBasic.VSPackage.ResourceManager.GetString(resourcesIdentifier)
            Else
                Assert.Fail($"Unexpected package Id: {packageGuid}")
            End If
        Else
            resourceDll = resourceReference.Substring(resourceReference.IndexOf("\\") + 1)
        End If

        Select Case resourceDll
            Case CSharpLanguageServiceDllName
                localizedString = CSharpVSResources.ResourceManager.GetString(resourcesIdentifier, culture)
            Case VisualBasicLanguageServiceDllName
                localizedString = BasicVSResources.ResourceManager.GetString(resourcesIdentifier, culture)
            Case LanguageServiceDllName
                localizedString = ServicesVSResources.ResourceManager.GetString(resourcesIdentifier, culture)
            Case Else
                Assert.Fail($"Resources should only the fetched from {CSharpLanguageServiceDllName}, {VisualBasicLanguageServiceDllName} or {LanguageServiceDllName}.")
        End Select

        Assert.NotNull(localizedString)
        Return localizedString
    End Function

End Module
