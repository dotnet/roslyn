Imports Microsoft.VisualStudio.Editors.Common
Imports System.ComponentModel
Imports System.Windows.Forms
Imports VSLangProj110
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Base class for the C# / VB build property pages
    ''' </summary>
    ''' <remarks></remarks>
    Friend MustInherit Class BuildPropPageBase
        Inherits PropPageUserControlBase

        Protected Const Const_OutputTypeEx As String = "OutputTypeEx"

        Private Function IsPrefer32BitSupportedForPlatformTarget() As Boolean

            ' Get the current value of PlatformTarget

            Dim controlValue As Object = GetControlValueNative("PlatformTarget")

            If PropertyControlData.IsSpecialValue(controlValue) Then
                ' Property is missing or indeterminate
                Return False
            End If

            If Not TypeOf controlValue Is String Then
                Return False
            End If

            ' Prefer32Bit is only allowed for AnyCPU

            Dim stringValue As String = CStr(controlValue)

            If String.IsNullOrEmpty(stringValue) Then
                ' Allow if the value is blank (means AnyCPU)
                Return True
            End If

            Return String.Compare(stringValue, "AnyCPU", StringComparison.Ordinal) = 0

        End Function

        Private Function IsPrefer32BitSupportedForOutputType() As Boolean

            ' Get the current value of OutputTypeEx

            Dim propertyValue As Object = Nothing

            Try

                If Not GetCurrentProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, propertyValue) Then
                    Return False
                End If

            Catch exc As InvalidCastException
                Return False
            Catch exc As NullReferenceException
                Return False
            Catch ex As TargetInvocationException
                Return False
            End Try

            If Not TypeOf propertyValue Is UInteger Then
                Return False
            End If

            Dim uintValue As UInteger = CUInt(propertyValue)

            ' Prefer32Bit is only allowed for Exe based output types

            Return uintValue = prjOutputTypeEx.prjOutputTypeEx_AppContainerExe OrElse
                   uintValue = prjOutputTypeEx.prjOutputTypeEx_Exe OrElse
                   uintValue = prjOutputTypeEx.prjOutputTypeEx_WinExe

        End Function

        Private Function IsPrefer32BitSupportedForTargetFramework() As Boolean

            Return IsTargetingDotNetFramework45OrAbove(Me.ProjectHierarchy) OrElse
                   IsAppContainerProject(Me.ProjectHierarchy)

        End Function

        Private Function IsPrefer32BitSupported() As Boolean

            Return IsPrefer32BitSupportedForPlatformTarget() AndAlso
                   IsPrefer32BitSupportedForOutputType() AndAlso
                   IsPrefer32BitSupportedForTargetFramework()

        End Function

        ' Holds the last value the Prefer32Bit check box had when enabled (or explicity
        ' set by the project system), so that the proper state is restored if the 
        ' control is disabled and then later enabled
        Private lastPrefer32BitValue As Boolean

        Protected Sub RefreshEnabledStatusForPrefer32Bit(control As CheckBox)

            Dim enabledBefore As Boolean = control.Enabled

            If control.Enabled Then
                Me.lastPrefer32BitValue = control.Checked
            End If

            EnableControl(control, IsPrefer32BitSupported())

            If enabledBefore AndAlso Not control.Enabled Then
                ' If transitioning from enabled to disabled, clear the checkbox.  When disabled, we
                ' want to show an unchecked checkbox regardless of the underlying property value.
                control.Checked = False

            ElseIf Not enabledBefore AndAlso control.Enabled Then

                ' If transitioning from disabled to enabled, restore the value of the checkbox.
                control.Checked = Me.lastPrefer32BitValue

            End If

        End Sub

        Protected Function Prefer32BitSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            If PropertyControlData.IsSpecialValue(value) Then
                ' Don't do anything if the value is missing or indeterminate
                Return False
            End If

            If Not TypeOf value Is Boolean Then
                ' Don't do anything if the value isn't of the expected type
                Return False
            End If

            If control.Enabled Then
                CType(control, CheckBox).Checked = CBool(value)
            Else
                ' The project is setting the property value while the control is disabled, so store the
                ' value for when the control is enabled
                Me.lastPrefer32BitValue = CBool(value)
            End If

            Return True
        End Function

        Protected Function Prefer32BitGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If Not control.Enabled Then

                ' If the control is not enabled, the checked state does not reflect the actual value
                ' of the property (the checkbox is always unchecked when disabled).  So we return the
                ' property value that we cached from when the control was last enabled (or explicitly
                ' set by the the project system while the control was disabled)
                value = lastPrefer32BitValue
                Return True
            End If

            Dim checkBox As CheckBox = CType(control, CheckBox)
            value = checkBox.Checked

            Return True
        End Function
    End Class

End Namespace
