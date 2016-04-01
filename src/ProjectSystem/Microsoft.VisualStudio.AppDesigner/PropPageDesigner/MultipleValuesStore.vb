' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' A serializable class which is capable of storing different values of a property
    ''' for different configurations, to enable undo/redo in "All configurations" and 
    ''' "All Platforms" modes.
    ''' </summary>
    ''' <remarks></remarks>
    <Serializable()> _
    Public Class MultipleValuesStore

        'Note: the sizes of these arrays are all the same
        Public ConfigNames As String()   'The config name applicable to each stored value
        Public PlatformNames As String() 'The platform name applicable to each stored value
        Public Values As Object()        'The stored values themselves

        Public SelectedConfigName As String 'The currently-selected configuration in the comboboxes.  Empty value indicates "All Configurations".
        Public SelectedPlatformName As String 'The currently-selected platform in the comboboxes.  Empty value indicates "All Configurations".


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="VsCfgProvider">IVsCfgProvider2</param>
        ''' <param name="Objects">The current set of objects (IVsCfg) from which the matching Values were pulled.</param>
        ''' <param name="Values">The values to persist</param>
        ''' <param name="SelectedConfigName">The selected configuration in the drop-down combobox.  Empty string indicates "All Configurations".</param>
        ''' <param name="SelectedPlatformName">The selected platform in the drop-down combobox.  Empty string indicates "All Platforms".</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal VsCfgProvider As IVsCfgProvider2, ByVal Objects() As Object, ByVal Values() As Object, ByVal SelectedConfigName As String, ByVal SelectedPlatformName As String)
            If Values Is Nothing Then
                Throw New ArgumentNullException("Values")
            ElseIf Objects Is Nothing Then
                Throw New ArgumentNullException("Objects")
            End If
            If Values.Length <> Objects.Length Then
                Debug.Fail("Bad array length returned from GetPropertyMultipleValues()")
                Throw Common.CreateArgumentException("Values, Objects")
            End If

            Me.SelectedConfigName = SelectedConfigName
            Me.SelectedPlatformName = SelectedPlatformName

            Me.ConfigNames = New String(Objects.Length - 1) {}
            Me.PlatformNames = New String(Objects.Length - 1) {}
            Me.Values = New Object(Objects.Length - 1) {}

            For i As Integer = 0 To Objects.Length - 1
                Dim Cfg As IVsCfg = TryCast(Objects(i), IVsCfg)
                If Cfg IsNot Nothing Then
                    Dim ConfigName As String = ""
                    Dim PlatformName As String = ""
                    AppDesCommon.ShellUtil.GetConfigAndPlatformFromIVsCfg(Cfg, ConfigName, PlatformName)

#If DEBUG Then
                    Dim Cfg2 As IVsCfg = Nothing
                    VsCfgProvider.GetCfgOfName(ConfigName, PlatformName, Cfg2)
                    Debug.Assert(Cfg2 IsNot Nothing AndAlso Cfg2 Is Cfg, "Unable to correctly decode config name and map it back to the config")
#End If
                    Me.ConfigNames(i) = ConfigName
                    Me.PlatformNames(i) = PlatformName
                    Me.Values(i) = Values(i)
                Else
                    Debug.Fail("Unexpected type passed in to MultipleValues.  Currently only IVsCfg supported.  If it's a common (non-config) property, why are we creating MultipleValues for it?")
                    Throw Common.CreateArgumentException("Values")
                End If
            Next

            DebugTrace("MultiValues constructor")
        End Sub


        ''' <summary>
        ''' Determines the set of configurations which correspond to the stored
        '''   configuration names and platforms.
        ''' </summary>
        ''' <param name="VsCfgProvider"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetObjects(ByVal VsCfgProvider As IVsCfgProvider2) As Object()
            Debug.Assert(ConfigNames IsNot Nothing AndAlso PlatformNames IsNot Nothing AndAlso Values IsNot Nothing)
            Debug.Assert(Values.Length = ConfigNames.Length AndAlso ConfigNames.Length = PlatformNames.Length, "Huh?")

            DebugTrace("MultiValues.GetObjects()")

            'Figure out the configurations which the config/platform name combinations refer to
            Dim Objects() As Object = New Object(ConfigNames.Length - 1) {}
            For i As Integer = 0 To ConfigNames.Length - 1
                Dim Cfg As IVsCfg = Nothing
                If VSErrorHandler.Succeeded(VsCfgProvider.GetCfgOfName(ConfigNames(i), PlatformNames(i), Cfg)) Then
                    Objects(i) = Cfg
                Else
                    Throw New Exception(SR.GetString(SR.PPG_ConfigNotFound_2Args, ConfigNames(i), PlatformNames(i)))
                End If
            Next

            Return Objects
        End Function




        <Conditional("DEBUG")> _
        Public Sub DebugTrace(ByVal Message As String)
#If DEBUG Then
            Debug.Assert(ConfigNames.Length = PlatformNames.Length AndAlso PlatformNames.Length = Values.Length)
            Common.Switches.TracePDUndo(Message)
            Trace.Indent()
            For i As Integer = 0 To ConfigNames.Length - 1
                Common.Switches.TracePDUndo("[" & ConfigNames(i) & "|" & PlatformNames(i) & "] Value=" & Common.DebugToString(Values(i)))
            Next
            Trace.Unindent()
#End If
        End Sub

    End Class

End Namespace
