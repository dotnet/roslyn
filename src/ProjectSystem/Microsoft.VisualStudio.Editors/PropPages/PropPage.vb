Imports System.Windows.Forms
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    'CONSIDER: move these to the individual page class files, or at least to a separate .vb file

#Region "Com Classes for our property pages"

#Region "Application property pages (VB, C#, J#)"


    'Property page class hierarchy:
    '
    ' ApplicationPropPageBase
    '   + ApplicationPropPageVBBase
    '     + ApplicationPropPageVBWinForms
    '     + ApplicationPropPageVBWPF
    '   + ApplicationPropPage
    '       + CSharpApplicationPropPage
    '
    'The ApplicationPropPage vs CSharpApplicationPropPage split was originally
    '  meant to allow differentiation with J#, but currently J# uses the same 
    '  page that C# does (CSharpApplicationPropPage).
    '


#Region "ApplicationPropPageComClass (Not directly used, inherited from by J#/C#)"

    <System.Runtime.InteropServices.GuidAttribute("1C25D270-6E41-4360-9221-1D22E4942FAD"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class ApplicationPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_ApplicationTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.ApplicationPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.ApplicationPropPage
        End Function

    End Class

#End Region

#Region "ApplicationWithMyPropPageComClass (VB Application property page)"

    'Note: This is the VB Application page (naming is historical)
    <System.Runtime.InteropServices.GuidAttribute("8998E48E-B89A-4034-B66E-353D8C1FDC2E"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class ApplicationWithMyPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_ApplicationTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.ApplicationPropPageVBWinForms)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.ApplicationPropPageVBWinForms
        End Function

    End Class

#End Region

#Region "WPFApplicationWithMyPropPageComClass (VB Application page for WPF)"

    <System.Runtime.InteropServices.GuidAttribute("00aa1f44-2ba3-4eaa-b54a-ce18000e6c5d"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class WPFApplicationWithMyPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase


        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_ApplicationTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.WPF.ApplicationPropPageVBWPF)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.WPF.ApplicationPropPageVBWPF
        End Function

    End Class

#End Region

#Region "CSharpApplicationPropPageComClass (C#/J# Application property page)"

    <System.Runtime.InteropServices.GuidAttribute("5E9A8AC2-4F34-4521-858F-4C248BA31532"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class CSharpApplicationPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_ApplicationTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.CSharpApplicationPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.CSharpApplicationPropPage
        End Function

    End Class

#End Region

#End Region


#Region "CompilePropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("EDA661EA-DC61-4750-B3A5-F6E9C74060F5"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class CompilePropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_CompileTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.CompilePropPage2)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.CompilePropPage2
        End Function


        Protected Overrides Property DefaultSize() As System.Drawing.Size
            Get
                ' This is somewhat hacky, but the compile's size page can sometimes exceed the default
                ' mimimum size for a property page. The PropPageDesignerView will query for this in order to
                ' figure out what the minimum autoscrollsize should be set to, but it will also check 
                ' the size of the actual control and use the min of those two values, so as long as we
                ' we return a default size that is larger than what our maximum minimum size will be, we 
                ' should be fine
                Return New System.Drawing.Size(Integer.MaxValue, Integer.MaxValue)
            End Get
            Set(ByVal value As System.Drawing.Size)
                MyBase.DefaultSize = value
            End Set
        End Property

    End Class

#End Region

#Region "ServicesPropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("43E38D2E-43B8-4204-8225-9357316137A4"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class ServicesPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_Services)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.ServicesPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.ServicesPropPage
        End Function

    End Class

#End Region

#Region "DebugPropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("6185191F-1008-4FB2-A715-3A4E4F27E610"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class DebugPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_DebugTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.DebugPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.DebugPropPage
        End Function

    End Class

#End Region

#Region "VBBasePropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("4E43F4AB-9F03-4129-95BF-B8FF870AF6AB"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class ReferencePropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_ReferencesTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.ReferencePropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.ReferencePropPage
        End Function

    End Class

#End Region

#Region "BuildPropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("A54AD834-9219-4aa6-B589-607AF21C3E26"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class BuildPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_BuildTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.BuildPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.BuildPropPage
        End Function
    End Class

#End Region

#Region "BuildEventsPropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("1E78F8DB-6C07-4d61-A18F-7514010ABD56"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class BuildEventsPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_BuildEventsTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.BuildEventsPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.BuildEventsPropPage
        End Function
    End Class

#End Region

#Region "ReferencePathsPropPageComClass"

    <System.Runtime.InteropServices.GuidAttribute("031911C8-6148-4e25-B1B1-44BCA9A0C45C"), ComVisible(True), CLSCompliantAttribute(False)> _
    Public NotInheritable Class ReferencePathsPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title() As String
            Get
                Return SR.GetString(SR.PPG_ReferencePathsTitle)
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType() As System.Type
            Get
                Return GetType(PropertyPages.ReferencePathsPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PropertyPages.ReferencePathsPropPage
        End Function
    End Class

#End Region

#End Region

End Namespace
