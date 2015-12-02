'-----------------------------------------------------------------------------------------------------------
'
'  Copyright (c) Microsoft Corporation.  All rights reserved.
'
'-----------------------------------------------------------------------------------------------------------
Imports VSLangProj110

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Class OutputTypeComboBoxValue

        Private m_Value As UInteger
        Private m_DisplayName As String

        Public Sub New(value As UInteger)
            m_Value = value

            Select Case m_Value
                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_WinExe)
                    m_DisplayName = SR.GetString(SR.PPG_WindowsApp)

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_Exe)
                    m_DisplayName = SR.GetString(SR.PPG_CommandLineApp)

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_Library)
                    m_DisplayName = SR.GetString(SR.PPG_WindowsClassLib)

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_WinMDObj)
                    m_DisplayName = SR.GetString(SR.PPG_WinMDObj)

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_AppContainerExe)
                    m_DisplayName = SR.GetString(SR.PPG_AppContainerExe)

                Case Else
                    m_DisplayName = SR.GetString(SR.PPG_UnknownOutputType, m_Value)

            End Select
        End Sub

        Public ReadOnly Property Value As UInteger
            Get
                Return m_Value
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return m_DisplayName
        End Function

    End Class

End Namespace
