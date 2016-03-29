'******************************************************************************
'* PropPageDesignerRootComponent.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************
Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' This class represents the root component of the Application Designer.
    ''' </summary>
    ''' <remarks></remarks>
    <Designer(GetType(PropPageDesignerRootDesigner), GetType(IRootDesigner))> _
    Public NotInheritable Class PropPageDesignerRootComponent
        Inherits Component

        'Private cache for important data
        Private m_Hierarchy As IVsHierarchy
        Private m_ItemId As UInt32
        Private m_RootDesigner As PropPageDesignerRootDesigner
        Private m_Name As String = "PropPageDesignerRootComponent"

        Public ReadOnly Property Name() As String
            Get
                Return m_Name
            End Get
        End Property



        ''' <summary>
        '''   Returns the root designer that is associated with this component, i.e., the
        '''   designer which is showing the UI to the user which allows this component's
        '''   resx file to be edited by the user.
        ''' </summary>
        ''' <value>The associated ResourceEditorRootDesigner.</value>
        ''' <remarks></remarks>
        Public ReadOnly Property RootDesigner() As PropPageDesignerRootDesigner
            Get
                If m_RootDesigner Is Nothing Then
                    'Not yet cached - get this info from the designer host
                    Debug.Assert(Not Container Is Nothing)
                    Dim Host As IDesignerHost = CType(Container, IDesignerHost)
                    m_RootDesigner = CType(Host.GetDesigner(Me), PropPageDesignerRootDesigner)
                End If

                Debug.Assert(Not m_RootDesigner Is Nothing, "Don't have an associated designer?!?")
                Return m_RootDesigner
            End Get
        End Property

        ''' <summary>
        ''' The IVsHierarchy associated with the AppDesigner node
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property Hierarchy() As IVsHierarchy
            Get
                Return m_Hierarchy
            End Get
            Set(ByVal Value As IVsHierarchy)
                m_Hierarchy = Value
            End Set
        End Property

        ''' <summary>
        ''' The ItemId associated with the AppDesigner node
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property ItemId() As System.UInt32
            Get
                Return m_ItemId
            End Get
            Set(ByVal Value As System.UInt32)
                m_ItemId = Value
            End Set
        End Property

    End Class

End Namespace
