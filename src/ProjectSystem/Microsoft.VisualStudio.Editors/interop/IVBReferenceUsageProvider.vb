' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Interop

    Friend Enum ReferenceUsageResult
        ReferenceUsageUnknown = 0
        ReferenceUsageOK = 1
        ReferenceUsageWaiting = 2
        ReferenceUsageCompileFailed = 3
        ReferenceUsageError = 4
    End Enum


    <ComImport(), System.Runtime.InteropServices.Guid("12636E2C-D42A-4db3-8795-6F9A6ABD120D"), _
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), _
    CLSCompliant(False)> _
    Friend Interface IVBReferenceUsageProvider
        Function GetUnusedReferences(ByVal Hierarchy As IVsHierarchy, ByRef ReferencePaths As String) As ReferenceUsageResult
        Sub StopGetUnusedReferences(ByVal Hierarchy As IVsHierarchy)
    End Interface

End Namespace

