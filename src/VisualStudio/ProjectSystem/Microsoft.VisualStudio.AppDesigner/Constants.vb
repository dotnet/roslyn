Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors

    Public NotInheritable Class Constants

        '
        ' Window Styles
        '
        Public Const WS_CHILD As Integer = &H40000000L
        Public Const WS_CLIPSIBLINGS As Integer = &H4000000L
        
    End Class

    <ComVisible(False)> _
    Friend Enum VSITEMIDAPPDES As UInteger
        NIL = &HFFFFFFFFUI '-1
        ROOT = &HFFFFFFFEUI '-2
        SELECTION = &HFFFFFFFDUI '-3
    End Enum

End Namespace
