' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    '''  The interface was implemented by PropPageDesignerView, the appDesigner view will fire this event when the current designer is activated or deactivated...
    ''' </summary>
    ''' <remarks></remarks>
    <ComVisible(False)> _
    Public Interface IVsEditWindowNotify
        Sub OnActivated(ByVal activated As Boolean)
    End Interface

End Namespace
