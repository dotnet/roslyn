Imports Microsoft.VisualStudio.Editors.Interop

Namespace Microsoft.VisualStudio.Editors.Common

    Friend Class WmUserConstants
        Friend Const WM_REFPAGE_REFERENCES_REFRESH As Integer = win.WM_USER + 21
        Friend Const WM_REFPAGE_IMPORTCHANGED As Integer = win.WM_USER + 22
        Friend Const WM_REFPAGE_IMPORTS_REFRESH As Integer = win.WM_USER + 24
        Friend Const WM_PAGE_POSTVALIDATION As Integer = win.WM_USER + 25
        Friend Const WM_UPDATE_PROPERTY_GRID As Integer = win.WM_USER + 26
        Friend Const WM_REFPAGE_SERVICEREFERENCES_REFRESH As Integer = win.WM_USER + 27
    End Class

End Namespace

