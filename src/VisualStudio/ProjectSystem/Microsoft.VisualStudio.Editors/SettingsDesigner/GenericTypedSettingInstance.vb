'------------------------------------------------------------------------------
' <copyright from='2003' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'
Imports System
Imports System.Diagnostics
Imports System.Collections
Imports System.Runtime.Serialization

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner


    '@ <summary>
    '@ Generic strongly typed setting instance
    '@ </summary>
    '@ <remarks></remarks>
    < _
    Serializable(), _
    System.ComponentModel.Design.HelpKeyword("ApplicationSetting") _
    > _
    Friend Class GenericTypedSettingInstance(Of T)
        Inherits Microsoft.VisualStudio.Editors.SettingsDesigner.DesignTimeSettingInstance
        Implements ISerializable

        '@ <summary>
        '@ My type
        '@ </summary>
        '@ <value></value>
        '@ <remarks></remarks>
        Public Overrides ReadOnly Property SettingType() As Type
            Get
                Return GetType(T)
            End Get
        End Property

        '@ <summary>
        '@ My value
        '@ </summary>
        '@ <value></value>
        '@ <remarks></remarks>
        <SRDescription(SR.SD_DESCR_Value)> _
        Public Shadows Property Value() As T
            Get
                Return DirectCast(MyBase.Value, T)
            End Get
            Set(ByVal Value As T)
                MyBase.Value = Value
            End Set
        End Property

        'See .NET Framework Developer's Guide, "Custom Serialization" for more information
        Protected Sub New(ByVal Info As SerializationInfo, ByVal Context As StreamingContext)
            MyBase.New(Info, Context)
        End Sub

        Public Sub New()
            MyBase.New(GetType(T).FullName)
        End Sub

        Public Sub New(ByVal DisplayTypeName As String)
            MyBase.New(DisplayTypeName)
        End Sub

        <System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter:=True)> _
        Protected Overrides Sub GetObjectData(ByVal Info As SerializationInfo, ByVal Context As StreamingContext)
            MyBase.GetObjectData(Info, Context)
        End Sub

    End Class
End Namespace
