' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Editors.AppDesInterop

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    Friend Class WmUserConstants
        Public Const WM_REFPAGE_REFERENCES_REFRESH As Integer = win.WM_USER + 21
        Public Const WM_REFPAGE_IMPORTCHANGED As Integer = win.WM_USER + 22
        Public Const WM_REFPAGE_IMPORTS_REFRESH As Integer = win.WM_USER + 24
        Public Const WM_PAGE_POSTVALIDATION As Integer = win.WM_USER + 25
        Public Const WM_UPDATE_PROPERTY_GRID As Integer = win.WM_USER + 26
        Public Const WM_REFPAGE_SERVICEREFERENCES_REFRESH As Integer = win.WM_USER + 27
    End Class

End Namespace

