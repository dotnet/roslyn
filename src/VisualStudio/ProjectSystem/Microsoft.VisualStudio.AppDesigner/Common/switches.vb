Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Editors.PropertyPages
Imports System
Imports System.Diagnostics
Imports VB = Microsoft.VisualBasic

Namespace Microsoft.VisualStudio.Editors.AppDesCommon


#If 0 Then

     Using these is a three-step process.

     1) Define a switch
        E.g.:

      Friend Class Switches
        .
        .
        .
        Public Shared FileWatcher As New TraceSwitch("FileWatcher", "Trace the resource editor FileWatcher class.")
        .
        .
        .
      End Class

     2) Please also add your switch to the example <system.diagnostics> section below, so everyone can simply copy this
        directly into their devenv.exe.config file.


     3) Use it in your code
        E.g.:

        Debug.WriteLineIf(Common.Switches.FileWatcher.TraceVerbose, "FileWatcher: file changed")

       or define a function to use:

        <Conditional("DEBUG")> _
        Public Shared Sub Trace(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
            #If DEBUG Then 'NOTE: The Conditional("DEBUG") attribute keeps callsites from compiling a call
                           '  to the function, but it does *not* keep the function body from getting compiled
            Debug.WriteLineIf(Common.Switches.FileWatcher.TraceVerbose, "FileWatcher: " & String.Format(Message, FormatArguments))
            #End If
        End Sub


     4) Add tracing info to the devenv.exe.config file (make sure you've got the one in the directory you're
        actually running devenv from).  This is done in the configuration section.

        E.g.:

        <?xml version ="1.0"?>
           .
           .
           .
        <configuration>
           .
           .
           .

           ***COPY THE FOLLOWING SECTION INTO YOUR DEVENV.EXE.CONFIG FILE INTO THE CONFIGURATION SECTION:

            <system.diagnostics>
                <switches>
                    <add name="UndoEngine" value="0" />
                    <add name="RSEResourceSerializationService" value="0" />
                    <add name="RSEFileWatcher" value="0" />
                    <add name="RSEAddRemoveResources" value="0" />
                    <add name="RSEVirtualStringTable" value="0" />
                    <add name="RSEVirtualListView" value="0" />
                    <add name="RSEDelayCheckErrors" value="0" />
                    <add name="RSEDisableHighQualityThumbnails" value="false" />
                    <add name="DFContextMenu" value="0" />
                    <add name="ResourcesFolderService" value="0" />
                    <add name="DEBUGPBRS" value="0" />
                    <add name="RSEFindReplace" value="0" />
                    <add name="MSVBE_SCC" value="0" />
                    <add name="PDDesignerActivations" value="0" />
                    <add name="PDFocus" value="0" />
                    <add name="PDUndo" value="0" />
                    <add name="PDProperties" value="0" />
                    <add name="PDApplicationType" value="0" />
                    <add name="PDConfigs" value="0" />
                    <add name="PDPerf" value="0" />
                    <add name="PDCmdTarget" value="0" />
                    <add name="PDAlwaysUseSetParent" value="false" />
                    <add name="PDMessageRouting" value="0" />
                    <add name="SDSyncUserConfig" value="0" />
                    <add name="SDSerializeSettings" value="0" />
                    <add name="PDAddVBWPFApplicationPageToAllProjects" value="false" />
                    <add name="PDAccessModifierCombobox" value="0" />
                    <add name="PDLinqImports" value="0" />

                    <add name="WCF_Config_FileChangeWatch" value="0" />
                    <add name="WCF_ASR_DebugServiceInfoNodes" value="0" />

                    <add name="MyExtensibilityTrace" value="0" />

                    <!-- Uncomment one of the following to overload the SKU for the project designer -->
                    <!-- <add name="PDSku" value="Express" /> -->
                    <!-- <add name="PDSku" value="Standard" /> -->
                    <!-- <add name="PDSku" value="VSTO" /> -->
                    <!-- <add name="PDSku" value="Professional" /> -->
                    <!-- <add name="PDSku" value="AcademicStudent" /> -->
                    <!-- <add name="PDSku" value="DownloadTrial" /> -->
                    <!-- <add name="PDSku" value="Enterprise" /> -->

                    <!-- Uncomment one of the following to overload the sub-SKU for the project designer -->
                    <!-- <add name="PDSubSku" value="VC" /> -->
                    <!-- <add name="PDSubSku" value="VB" /> -->
                    <!-- <add name="PDSubSku" value="CSharp" /> -->
                    <!-- <add name="PDSubSku" value="Architect" /> -->
                    <!-- <add name="PDSubSku" value="IDE" /> -->
                    <!-- <add name="PDSubSku" value="JSharp" /> -->
                    <!-- <add name="PDSubSku" value="Web" /> -->

                </switches>
            </system.diagnostics>


           *** END OF SECTION TO COPY

           .
           .
           .
        </configuration>


    5) modify the values of any switches you want to turn on

     Note: the valid "value" values (levels) are as follows:

        0      Off
        1      Error
        2      Warning
        3      Info
        4      Verbose    -> this is generally what you want when you want to enable one of these switches in the config file

#End If


    ''' <summary>
    ''' Contains predefined switches for enabling/disabling trace output or code instrumentation.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class Switches

        '------------- Resource Editor -------------

        ''' <summary>
        ''' Trace for the ResourceEditor.FileWatcher class
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEFileWatcher As New TraceSwitch("RSEFileWatcher", "Trace the resource editor FileWatcher class.")

        ''' <summary>
        ''' Tracing for the ResourceEditor.ResourceSerializationService class
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEResourceSerializationService As New TraceSwitch("RSEResourceSerializationService", "Trace the resource editor ResourceSerializationService class.")

        ''' <summary>
        ''' Track adding and removing resources in the resource editor
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEAddRemoveResources As New TraceSwitch("RSEAddRemoveResources", "Trace adding/removing resources in the resource editor")

        ''' <summary>
        ''' Trace virtual mode methods in the resource editor's string table
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEVirtualStringTable As New TraceSwitch("RSEVirtualStringTable", "Trace virtual mode methods in the resource editor's string table")

        ''' <summary>
        ''' Trace virtual mode methods in the resource editor's listview
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEVirtualListView As New TraceSwitch("RSEVirtualListView", "Trace virtual mode methods in the resource editor's listview")

        ''' <summary>
        ''' Trace the delayed checking of errors in resources
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEDelayCheckErrors As New TraceSwitch("RSEDelayCheckErrors", "Trace the delayed checking of errors in resources")

        ''' <summary>
        ''' Disable high-quality options on the Graphics object when creating thumbnails in the resource editor
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEDisableHighQualityThumbnails As New BooleanSwitch("RSEDisableHighQualityThumbnails", "Disable high-quality options on the Graphics object when creating thumbnails in the resource editor")

        ''' <summary>
        ''' Trace find/replace in the resource editor
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared RSEFindReplace As New TraceSwitch("RSEFindReplace", "Trace find/replace in the resource editor")



        '------------- Designer Framework -------------



        ''' <summary>
        ''' Trace the showing of context menus via the base control classes in DesignerFramework
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared DFContextMenu As New TraceSwitch("DFContextMenu", "Trace the showing of context menus via the base control classes in DesignerFramework")



        '------------- Common switches for Microsoft.VisualStudio.Editors -------------



        ''' <summary>
        ''' Trace source code control integration behavior in Microsoft.VisualStudio.Editors.dll
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared MSVBE_SCC As New TraceSwitch("MSVBE_SCC", "Trace source code control integration behavior in Microsoft.VisualStudio.Editors.dll")



        '------------- Project Designer -------------



        ''' <summary>
        ''' Trace when the active designer changes in the project designer
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDDesignerActivations As New TraceSwitch("PDDesignerActivations", "Trace when the active designer changes in the project designer")

        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDFocus As New TraceSwitch("PDFocus", "Trace project designer focus-related events")

        ''' <summary>
        ''' Trace behavior of multiple-value undo and redo
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDUndo As New TraceSwitch("PDUndo", "Trace behavior of multiple-value undo and redo")

        ''' <summary>
        ''' Trace the creation and dirtying of properties, apply, etc.
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDProperties As New TraceSwitch("PDProperties", "Trace the creation and dirtying of properties in property pages, apply, etc.")

        ''' <summary>
        ''' Trace mapping of application type - output type, MyType
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDApplicationType As New TraceSwitch("PDApplicationType", "Trace mapping of application type properties")

        ''' <summary>
        ''' Trace the functionality of extender properties
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDExtenders As New TraceSwitch("PDExtenders", "Trace the functionality of extender properties")

        ''' <summary>
        ''' Trace configuration setup and changes tracking in the project designer
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDConfigs As New TraceSwitch("PDConfigs", "Trace configuration setup and changes tracking in the project designer")

        ''' <summary>
        ''' Trace performance issues for the project designer
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDPerf As New TraceSwitch("PDPerf", "Trace performance issues for the project designer")

        ''' <summary>
        ''' Trace command routing (CmdTargetHelper, etc.) in the project designer
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDCmdTarget As New TraceSwitch("PDCmdTarget", "Trace command routing (CmdTargetHelper, etc.) in the project designer")

        ''' <summary>
        ''' Always use native ::SetParent() instead of setting the WinForms Parent property for property page hosting.
        ''' This is useful for testing the hosting of pages as it would occur for non-native pages.
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDAlwaysUseSetParent As New BooleanSwitch("PDAlwaysUseSetParent", "Always use native ::SetParent() instead of setting the WinForms Parent property for property page hosting")

        ''' <summary>
        ''' Traces message routing in the project designer and its property pages
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDMessageRouting As New TraceSwitch("PDMessageRouting", "Traces message routing in the project designer and its property pages")

        ''' <summary>
        ''' Overrides the SKU edition value for the project designer
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDSku As New EnumSwitch(Of VSProductSKU.VSASKUEdition)("PDSku", "Overrides the SKU edition value for the project designer")

        ''' <summary>
        ''' Overrides the Sub-SKU edition value for the project designer
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared PDSubSku As New EnumSwitch(Of VSProductSKU.VSASubSKUEdition)("PDSubSku", "Overrides the Sub-SKU edition value for the project designer")

        Public Shared PDAddVBWPFApplicationPageToAllProjects As New BooleanSwitch("PDAddVBWPFApplicationPageToAllProjects", _
            "Add the VB WPF Application property page to all projects, even non-WPF projects.  This allows for debugging " _
            & "this page without the new WPF flavor")

        Public Shared PDAccessModifierCombobox As New TraceSwitch("PDAccessModifierCombobox", "Traces the access modifier combobox functionality")

        Public Shared PDLinqImports As New TraceSwitch("PDLinqImports", "Traces the adding and removing of Linq imports during target framework upgrade/downgrade")

        '------------- Settings Designer -------------
        Public Shared SDSyncUserConfig As New TraceSwitch("SDSyncUserConfig", "Trace synhronization/deletion of user.config files")

        ''' <summary>
        ''' Tracing whenever we read/write .settings and/or app.config files...
        ''' </summary>
        ''' <remarks></remarks>
        Public Shared SDSerializeSettings As New TraceSwitch("SDSerializeSettings", "Serialization/deserialization of settings")

        '------------- WCF Tooling -------------

        Public Shared WCF_Config_FileChangeWatch As New TraceSwitch("WCF_Config_FileChangeWatch", "Changes to configuration files in the current project")

        Public Shared WCF_ASR_DebugServiceInfoNodes As New TraceSwitch("WCF_ASR_DebugServiceInfoNodes", "Displays additional information about the ServiceInfoNodes in the Services treeview in the ASR dialog")

        '------------- MyExtensibility -------------
        Public Shared MyExtensibilityTraceSwitch As New TraceSwitch("MyExtensibilityTrace", "Trace switch for MyExtensibility Feature")

        '--------------- Functions (optional, but make usage easier) ------------------

#Region "Utility functions"

#If DEBUG Then
        ''' <summary>
        ''' Uses String.Format if there are arguments, otherwise simply returns the string.
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function Format(ByVal Message As String, ByVal ParamArray FormatArguments() As Object) As String
            If FormatArguments Is Nothing OrElse FormatArguments.Length = 0 Then
                Return Message
            Else
                Try
                    Return String.Format(Message, FormatArguments)
                Catch ex As FormatException
                    'If there was an exception trying to format (e.g., the Message contained the {} characters), just
                    '  return the string - this stuff is only for debug 
                    Return Message
                End Try
            End If

            Return String.Empty
        End Function
#End If


#If DEBUG Then
        ''' <summary>
        ''' Formats a Win32 message into a friendly form for debugging/tracing purposes
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function FormatWin32Message(ByVal msg As System.Windows.Forms.Message) As String
            Dim str As New Text.StringBuilder()
            Dim MsgType As String = Nothing
            Select Case msg.Msg
                Case win.WM_KEYDOWN
                    MsgType = "WM_KEYDOWN"
                Case win.WM_KEYUP
                    MsgType = "WM_KEYUP"
                Case win.WM_SETFOCUS
                    MsgType = "WM_SETFOCUS"
                Case win.WM_CHAR
                    MsgType = "WM_CHAR"
                Case win.WM_SYSCHAR
                    MsgType = "WM_SYSCHAR"

                Case Else
                    If PDMessageRouting.Level >= TraceLevel.Verbose Then
                        MsgType = "0x" & Microsoft.VisualBasic.Hex(msg.Msg)
                    Else
                        Return Nothing
                    End If
            End Select
            str.Append("MSG{" & MsgType & ", HWND=0x" & VB.Hex(msg.HWnd.ToInt32))

            'Get the HWND's text
            Dim WindowText As New String(" "c, 30)
            Dim CharsCopied As Integer = AppDesInterop.NativeMethods.GetWindowText(msg.HWnd, WindowText, WindowText.Length)
            If CharsCopied > 0 Then
                WindowText = WindowText.Substring(0, CharsCopied)
                str.Append(" """ & WindowText & """")
            End If

            str.Append("}")
            Return str.ToString()
        End Function
#End If


#If DEBUG Then
        Private Shared m_TimeCodeStart As Date
        Private Shared m_FirstTimeCodeTaken As Boolean
#End If

        Public Shared Function TimeCode() As String
#If DEBUG Then
            If Not m_FirstTimeCodeTaken Then
                ResetTimeCode()
            End If

            Dim ts As TimeSpan = Microsoft.VisualBasic.Now.Subtract(m_TimeCodeStart)
            Return ts.TotalSeconds.ToString("0000.00000") & VB.vbTab
            'Return n.ToString("hh:mm:ss.") & Microsoft.VisualBasic.Format(n.Millisecond, "000") & VB.vbTab
#Else
            Return ""
#End If
        End Function

        <Conditional("DEBUG")> _
        Public Shared Sub ResetTimeCode()
#If DEBUG Then
            m_TimeCodeStart = VB.Now
            m_FirstTimeCodeTaken = True
#End If
        End Sub


#Region "EnumSwitch(Of T)"

        ''' <summary>
        ''' A Switch which has a simple enum value (either as integer or string representation)
        ''' </summary>
        ''' <remarks></remarks>
        Public Class EnumSwitch(Of T)
            Inherits Switch

            Public Sub New(ByVal DisplayName As String, ByVal Description As String)
                MyBase.New(DisplayName, Description)
                Debug.Assert(GetType(System.Enum).IsAssignableFrom(GetType(T)), "EnumSwitch() requires an Enum as a type parameter")
            End Sub

            ''' <summary>
            ''' True iff the switch has a non-empty value
            ''' </summary>
            ''' <value></value>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public ReadOnly Property ValueDefined() As Boolean
                Get
                    Return MyBase.Value <> "" AndAlso CInt(System.Convert.ChangeType(Me.Value, TypeCode.Int32)) <> 0
                End Get
            End Property

            ''' <summary>
            ''' Gets/sets the current value of the switch
            ''' </summary>
            ''' <value></value>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Shadows Property Value() As T
                Get
                    Return CType(System.Enum.Parse(GetType(T), MyBase.Value), T)
                End Get
                Set(ByVal value As T)
                    MyBase.Value = value.ToString()
                End Set
            End Property

            ''' <summary>
            ''' Interprets the new (string-based) correctly, based on the string or
            '''   integeger representation.
            ''' </summary>
            ''' <remarks></remarks>
            Protected Overrides Sub OnValueChanged()
                SwitchSetting = CInt(System.Convert.ChangeType(Value, TypeCode.Int32))
            End Sub

        End Class

#End Region

#End Region

        ''' <summary>
        ''' Trace messages for the MSVBE_SCC flag
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TraceSCC(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(MSVBE_SCC.TraceVerbose, "MSVBE_SCC: " & Format(Message, FormatArguments))
#End If
        End Sub

        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDFocus(ByVal Level As TraceLevel, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDFocus.Level >= Level, "PDFocus:" & VB.vbTab & TimeCode() & Format(Message, FormatArguments))
#End If
        End Sub

        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDUndo(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDUndo.TraceVerbose, "PDUndo: " & Format(Message, FormatArguments))
#End If
        End Sub


        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDProperties(ByVal Level As TraceLevel, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDProperties.Level >= Level, "PDProperties: " & Format(Message, FormatArguments))
#End If
        End Sub


        ''' <summary>
        ''' Trace the functionality of extender properties
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDExtenders(ByVal Level As TraceLevel, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDExtenders.Level >= Level, "PDExtenders: " & Format(Message, FormatArguments))
#End If
        End Sub


        ''' <summary>
        ''' Trace configuration setup and changes tracking in the project designer
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDConfigs(ByVal TraceLevel As TraceLevel, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDConfigs.Level >= TraceLevel, "PDConfigs: " & Format(Message, FormatArguments))
#End If
        End Sub

        <Conditional("DEBUG")> _
        Public Shared Sub TracePDConfigs(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            TracePDConfigs(TraceLevel.Verbose, Message, FormatArguments)
#End If
        End Sub


        ''' <summary>
        ''' Trace configuration setup and changes tracking in the project designer
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDPerf(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDPerf.TraceInfo, "PDPerf:" & VB.vbTab & TimeCode() & Format(Message, FormatArguments))
#End If
        End Sub

        <Conditional("DEBUG")> _
        Public Shared Sub TracePDPerfBegin(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            If PDPerf.TraceInfo Then
                TracePDPerf("BEGIN: " & Message)
            Else
                TracePDPerf(Message)
            End If
#End If
        End Sub

        <Conditional("DEBUG")> _
        Public Shared Sub TracePDPerfEnd(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            If PDPerf.TraceInfo Then
                TracePDPerf("  END: " & Message)
            Else
                'Don't bother with this message unless it's verbose
            End If
#End If
        End Sub

        <Conditional("DEBUG")> _
        Public Shared Sub TracePDPerf(ByVal e As Windows.Forms.LayoutEventArgs, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            TracePDPerf(Message, FormatArguments)
            TraceOnLayout(e)
#End If
        End Sub

#If DEBUG Then
        Private Shared Sub TraceOnLayout(ByVal e As Windows.Forms.LayoutEventArgs)
            If PDPerf.TraceInfo Then
                Trace.WriteLine("  AffectedControl=" & DebugToString(e.AffectedControl))
                Trace.WriteLine("  AffectedComponent=" & DebugToString(e.AffectedComponent))
                Trace.WriteLine("  AffectedProperty=" & DebugToString(e.AffectedProperty))
                If PDPerf.TraceVerbose Then
                    Trace.WriteLine(New System.Diagnostics.StackTrace().ToString)
                End If
            End If
        End Sub
#End If



        ''' <summary>
        ''' Trace configuration setup and changes tracking in the project designer
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDCmdTarget(ByVal TraceLevel As TraceLevel, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDCmdTarget.Level >= TraceLevel, "PDCmdTarget: " & Format(Message, FormatArguments))
#End If
        End Sub


        ''' <summary>
        ''' Trace Win32 message routing
        ''' </summary>
        ''' <param name="TraceLevel"></param>
        ''' <param name="Message"></param>
        ''' <param name="msg"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDMessageRouting(ByVal TraceLevel As TraceLevel, ByVal Message As String, ByVal msg As Windows.Forms.Message)
#If DEBUG Then
            If PDMessageRouting.Level >= TraceLevel Then
                Dim FormattedMessage As String = FormatWin32Message(msg)
                If FormattedMessage IsNot Nothing Then
                    Trace.WriteLine("PDMessageRouting: " & Message & ": " & FormattedMessage)
                End If
            End If
#End If
        End Sub


        ''' <summary>
        ''' Trace Win32 message routing
        ''' </summary>
        ''' <param name="TraceLevel"></param>
        ''' <param name="Message"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDMessageRouting(ByVal TraceLevel As TraceLevel, ByVal Message As String)
#If DEBUG Then
            Trace.WriteLineIf(PDMessageRouting.Level >= TraceLevel, "PDMessageRouting: " & Message)
#End If
        End Sub

        ''' <summary>
        ''' Traces the access modifier combobox functionality
        ''' </summary>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub TracePDAccessModifierCombobox(ByVal traceLevel As TraceLevel, ByVal message As String)
#If DEBUG Then
            Trace.WriteLineIf(PDAccessModifierCombobox.Level >= traceLevel, "PDAccessModifierCombobox: " & message)
#End If
        End Sub


        ''' <summary>
        ''' Trace serialization of settings
        ''' </summary>
        ''' <param name="tracelevel"></param>
        ''' <param name="message"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Overloads Shared Sub TraceSDSerializeSettings(ByVal tracelevel As TraceLevel, ByVal message As String)
#If DEBUG Then
            Trace.WriteLineIf(SDSerializeSettings.Level >= tracelevel, message)
#End If
        End Sub

        ''' <summary>
        ''' Trace serialization of settings
        ''' </summary>
        ''' <param name="tracelevel"></param>
        ''' <param name="formatString"></param>
        ''' <param name="parameters"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Overloads Shared Sub TraceSDSerializeSettings(ByVal tracelevel As TraceLevel, ByVal formatString As String, ByVal ParamArray parameters() As Object)
#If DEBUG Then
            Trace.WriteLineIf(SDSerializeSettings.Level >= tracelevel, String.Format(formatString, parameters))
#End If
        End Sub

        <Conditional("DEBUG")> _
        Public Overloads Shared Sub TracePDLinqImports(ByVal tracelevel As TraceLevel, ByVal formatString As String, ByVal ParamArray parameters() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDLinqImports.Level >= tracelevel, Format(formatString, parameters))
#End If
        End Sub

        ''' <summary>
        ''' Trace changes to one of the monitored configuration files 
        ''' </summary>
        ''' <param name="tracelevel"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Overloads Shared Sub TraceWCFConfigFileChangeWatch(ByVal tracelevel As TraceLevel, ByVal formatString As String, ByVal ParamArray parameters() As Object)
#If DEBUG Then
            Trace.WriteLineIf(WCF_Config_FileChangeWatch.Level >= tracelevel, String.Format(formatString, parameters))
#End If
        End Sub


        ''' <summary>
        ''' Trace changes to one of the monitored configuration files 
        ''' </summary>
        ''' <param name="tracelevel"></param>
        ''' <param name="message"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Overloads Shared Sub TraceWCFConfigFileChangeWatch(ByVal tracelevel As TraceLevel, ByVal message As String)
#If DEBUG Then
            Trace.WriteLineIf(WCF_Config_FileChangeWatch.Level >= tracelevel, message)
#End If
        End Sub


        <Conditional("DEBUG")> _
        Public Shared Sub TracePDPerfBegin(ByVal e As Windows.Forms.LayoutEventArgs, ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
#If DEBUG Then
            TracePDPerfBegin(Message, FormatArguments)
            TraceOnLayout(e)
#End If
        End Sub

        <Conditional("DEBUG")> _
        Public Shared Sub TraceMyExtensibility(ByVal traceLevel As TraceLevel, ByVal message As String)
#If DEBUG Then
            Trace.WriteLineIf(MyExtensibilityTraceSwitch.Level >= traceLevel, String.Format("MyExtensibility {0} {1}: ", Date.Now.ToLongDateString(), Date.Now.ToLongTimeString()))
            Trace.WriteLineIf(MyExtensibilityTraceSwitch.Level >= traceLevel, message)
#End If
        End Sub

    End Class

End Namespace
