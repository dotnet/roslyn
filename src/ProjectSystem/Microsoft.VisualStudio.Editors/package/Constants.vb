Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors

    Friend NotInheritable Class Constants

        '
        ' Window Styles
        '
        Public Const WS_OVERLAPPED As Integer = &H0L
        Public Const WS_POPUP As Integer = &H80000000
        Public Const WS_CHILD As Integer = &H40000000L
        Public Const WS_MINIMIZE As Integer = &H20000000L
        Public Const WS_VISIBLE As Integer = &H10000000L
        Public Const WS_DISABLED As Integer = &H8000000L
        Public Const WS_CLIPSIBLINGS As Integer = &H4000000L
        Public Const WS_CLIPCHILDREN As Integer = &H2000000L
        Public Const WS_MAXIMIZE As Integer = &H1000000L
        Public Const WS_CAPTION As Integer = &HC00000L                '/* WS_BORDER | WS_DLGFRAME  */
        Public Const WS_BORDER As Integer = &H800000L
        Public Const WS_DLGFRAME As Integer = &H400000L
        Public Const WS_VSCROLL As Integer = &H200000L
        Public Const WS_HSCROLL As Integer = &H100000L
        Public Const WS_SYSMENU As Integer = &H80000L
        Public Const WS_THICKFRAME As Integer = &H40000L
        Public Const WS_GROUP As Integer = &H20000L
        Public Const WS_TABSTOP As Integer = &H10000L

        Public Const WS_MINIMIZEBOX As Integer = &H20000L
        Public Const WS_MAXIMIZEBOX As Integer = &H10000L


        Public Const WS_TILED As Integer = WS_OVERLAPPED
        Public Const WS_ICONIC As Integer = WS_MINIMIZE
        Public Const WS_SIZEBOX As Integer = WS_THICKFRAME
        Public Const WS_TILEDWINDOW As Integer = WS_OVERLAPPEDWINDOW

        '/*
        ' * Common Window Styles
        ' */
        Public Const WS_OVERLAPPEDWINDOW As Integer = (WS_OVERLAPPED Or WS_CAPTION Or WS_SYSMENU Or WS_THICKFRAME Or WS_MINIMIZEBOX Or WS_MAXIMIZEBOX)
        Public Const WS_POPUPWINDOW As Integer = (WS_POPUP Or WS_BORDER Or WS_SYSMENU)
        Public Const WS_CHILDWINDOW As Integer = (WS_CHILD)

        Public Const WS_EX_RIGHT As Integer = &H1000
        Public Const WS_EX_LEFT As Integer = &H0
        Public Const WS_EX_RTLREADING As Integer = &H2000
        Public Const WS_EX_LTRREADING As Integer = &H0
        Public Const WS_EX_LEFTSCROLLBAR As Integer = &H4000
        Public Const WS_EX_RIGHTSCROLLBAR As Integer = &H0

        Public Const WS_EX_CONTROLPARENT As Integer = &H10000
        Public Const WS_EX_STATICEDGE As Integer = &H20000
        Public Const WS_EX_APPWINDOW As Integer = &H40000

        Public Const WS_EX_OVERLAPPEDWINDOW As Integer = (WS_EX_WINDOWEDGE Or WS_EX_CLIENTEDGE)
        Public Const WS_EX_PALETTEWINDOW As Integer = (WS_EX_WINDOWEDGE Or WS_EX_TOOLWINDOW Or WS_EX_TOPMOST)

        '/*
        ' * Extended Window Styles
        ' */
        Public Const WS_EX_DLGMODALFRAME As Integer = &H1L
        Public Const WS_EX_NOPARENTNOTIFY As Integer = &H4L
        Public Const WS_EX_TOPMOST As Integer = &H8L
        Public Const WS_EX_ACCEPTFILES As Integer = &H10L
        Public Const WS_EX_TRANSPARENT As Integer = &H20L

        Public Const WS_EX_MDICHILD As Integer = &H40L
        Public Const WS_EX_TOOLWINDOW As Integer = &H80L
        Public Const WS_EX_WINDOWEDGE As Integer = &H100L
        Public Const WS_EX_CLIENTEDGE As Integer = &H200L
        Public Const WS_EX_CONTEXTHELP As Integer = &H400L

        Public Const DS_3DLOOK As Integer = &H4
        Public Const DS_FIXEDSYS As Integer = &H8
        Public Const DS_NOFAILCREATE As Integer = &H10
        Public Const DS_CONTROL As Integer = &H400
        Public Const DS_CENTER As Integer = &H800
        Public Const DS_CENTERMOUSE As Integer = &H1000
        Public Const DS_CONTEXTHELP As Integer = &H2000


    End Class

    <ComVisible(False)> _
    Friend Enum VSITEMID As UInteger
        NIL = &HFFFFFFFFUI '-1
        ROOT = &HFFFFFFFEUI '-2
        SELECTION = &HFFFFFFFDUI '-3
    End Enum

End Namespace
