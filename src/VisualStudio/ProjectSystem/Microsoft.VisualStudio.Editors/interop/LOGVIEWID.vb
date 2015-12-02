 '------------------------------------------------------------------------------
'/ <copyright from='1997' to='2001' company='Microsoft Corporation'>           
'/    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'/    Information Contained Herein is Proprietary and Confidential.            
'/ </copyright>                                                                
'------------------------------------------------------------------------------

Imports System.Diagnostics
Imports System

Namespace Microsoft.VisualStudio.Editors.Interop
    Friend Class LOGVIEWID
        '---------------------------------------------------------------------------
        ' define LOGVIEWID's here!
        '---------------------------------------------------------------------------
        'cpp_quote("#define LOGVIEWID_Primary GUID_NULL")
        Public Shared LOGVIEWID_Primary As Guid = Guid.Empty

        '---------------------------------------------------------------------------
        ' The range 7651a700-06e5-11d1-8ebd-00a0c90f26ea to
        ' 7651a750-06e5-11d1-8ebd-00a0c90f26ea has been reserved for LOGVIEWID's
        ' these were taken from VSSHELL.IDL
        '---------------------------------------------------------------------------
        Public Shared LOGVIEWID_Debugging As New Guid("{7651a700-06e5-11d1-8ebd-00a0c90f26ea}")
        Public Shared LOGVIEWID_Code As New Guid("{7651a701-06e5-11d1-8ebd-00a0c90f26ea}")
        Public Shared LOGVIEWID_Designer As New Guid("{7651a702-06e5-11d1-8ebd-00a0c90f26ea}")
        Public Shared LOGVIEWID_TextView As New Guid("{7651a703-06e5-11d1-8ebd-00a0c90f26ea}")

        ' cmdidOpenWith handlers should pass this LOGVIEWID along to OpenStandardEditor to get the "Open With" dialog
        Public Shared LOGVIEWID_UserChooseView As New Guid("{7651a704-06e5-11d1-8ebd-00a0c90f26ea}")
    End Class 'LOGVIEWID
End Namespace
