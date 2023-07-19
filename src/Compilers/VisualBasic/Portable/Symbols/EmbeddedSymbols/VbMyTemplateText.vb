' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On
Option Compare Binary

#If TARGET = "module" AndAlso _MYTYPE = "" Then
#Const _MYTYPE="Empty"
#End If

#If _MYTYPE = "WindowsForms" Then

#Const _MYFORMS = True
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "WindowsForms"

#ElseIf _MYTYPE = "WindowsFormsWithCustomSubMain" Then

#Const _MYFORMS = True
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Console"

#ElseIf _MYTYPE = "Windows" OrElse _MYTYPE = "" Then

#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Windows"

#ElseIf _MYTYPE = "Console" Then

#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Windows"
#Const _MYCOMPUTERTYPE = "Windows"
#Const _MYAPPLICATIONTYPE = "Console"

#ElseIf _MYTYPE = "Web" Then

#Const _MYFORMS = False
#Const _MYWEBSERVICES = False
#Const _MYUSERTYPE = "Web"
#Const _MYCOMPUTERTYPE = "Web"

#ElseIf _MYTYPE = "WebControl" Then

#Const _MYFORMS = False
#Const _MYWEBSERVICES = True
#Const _MYUSERTYPE = "Web"
#Const _MYCOMPUTERTYPE = "Web"

#ElseIf _MYTYPE = "Custom" Then

#ElseIf _MYTYPE <> "Empty" Then

#Const _MYTYPE = "Empty"

#End If

#If _MYTYPE <> "Empty" Then

Namespace My

#If _MYAPPLICATIONTYPE = "WindowsForms" OrElse _MYAPPLICATIONTYPE = "Windows" OrElse _MYAPPLICATIONTYPE = "Console" Then

    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("MyTemplate", "11.0.0.0")> _
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> Partial Friend Class MyApplication

#If _MYAPPLICATIONTYPE = "WindowsForms" Then
        Inherits Global.Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
#If TARGET = "winexe" Then
        <Global.System.STAThread(), Global.System.Diagnostics.DebuggerHidden(), Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
        Friend Shared Sub Main(ByVal Args As String())
            Try
               Global.System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(MyApplication.UseCompatibleTextRendering())
            Finally
            End Try               
            My.Application.Run(Args)
        End Sub
#End If

#ElseIf _MYAPPLICATIONTYPE = "Windows" Then
        Inherits Global.Microsoft.VisualBasic.ApplicationServices.ApplicationBase
#ElseIf _MYAPPLICATIONTYPE = "Console" Then
        Inherits Global.Microsoft.VisualBasic.ApplicationServices.ConsoleApplicationBase	
#End If '_MYAPPLICATIONTYPE = "WindowsForms"

    End Class

#End If '#If _MYAPPLICATIONTYPE = "WindowsForms" Or _MYAPPLICATIONTYPE = "Windows" or _MYAPPLICATIONTYPE = "Console"

#If _MYCOMPUTERTYPE <> "" Then

    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("MyTemplate", "11.0.0.0")> _
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> Partial Friend Class MyComputer

#If _MYCOMPUTERTYPE = "Windows" Then
        Inherits Global.Microsoft.VisualBasic.Devices.Computer
#ElseIf _MYCOMPUTERTYPE = "Web" Then
        Inherits Global.Microsoft.VisualBasic.Devices.ServerComputer
#End If
        <Global.System.Diagnostics.DebuggerHidden()> _
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        Public Sub New()
            MyBase.New()
        End Sub
    End Class
#End If

    <Global.Microsoft.VisualBasic.HideModuleName()> _
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("MyTemplate", "11.0.0.0")> _
    Friend Module MyProject

#If _MYCOMPUTERTYPE <> "" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.Computer")> _
        Friend ReadOnly Property Computer() As MyComputer
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_ComputerObjectProvider.GetInstance()
            End Get
        End Property

        Private ReadOnly m_ComputerObjectProvider As New ThreadSafeObjectProvider(Of MyComputer)
#End If

#If _MYAPPLICATIONTYPE = "Windows" Or _MYAPPLICATIONTYPE = "WindowsForms" Or _MYAPPLICATIONTYPE = "Console" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.Application")> _
        Friend ReadOnly Property Application() As MyApplication
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_AppObjectProvider.GetInstance()
            End Get
        End Property
        Private ReadOnly m_AppObjectProvider As New ThreadSafeObjectProvider(Of MyApplication)
#End If

#If _MYUSERTYPE = "Windows" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.User")> _
        Friend ReadOnly Property User() As Global.Microsoft.VisualBasic.ApplicationServices.User
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_UserObjectProvider.GetInstance()
            End Get
        End Property
        Private ReadOnly m_UserObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.ApplicationServices.User)
#ElseIf _MYUSERTYPE = "Web" Then
        <Global.System.ComponentModel.Design.HelpKeyword("My.User")> _
        Friend ReadOnly Property User() As Global.Microsoft.VisualBasic.ApplicationServices.WebUser
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_UserObjectProvider.GetInstance()
            End Get
        End Property
        Private ReadOnly m_UserObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.ApplicationServices.WebUser)
#End If

#If _MYFORMS = True Then

#Const STARTUP_MY_FORM_FACTORY = "My.MyProject.Forms"

        <Global.System.ComponentModel.Design.HelpKeyword("My.Forms")> _
        Friend ReadOnly Property Forms() As MyForms
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_MyFormsObjectProvider.GetInstance()
            End Get
        End Property

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        <Global.Microsoft.VisualBasic.MyGroupCollection("System.Windows.Forms.Form", "Create__Instance__", "Dispose__Instance__", "My.MyProject.Forms")> _
        Friend NotInheritable Class MyForms
            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Shared Function Create__Instance__(Of T As {New, Global.System.Windows.Forms.Form})(ByVal Instance As T) As T
                If Instance Is Nothing OrElse Instance.IsDisposed Then
                    If m_FormBeingCreated IsNot Nothing Then
                        If m_FormBeingCreated.ContainsKey(GetType(T)) = True Then
                            Throw New Global.System.InvalidOperationException(Global.Microsoft.VisualBasic.CompilerServices.Utils.GetResourceString("WinForms_RecursiveFormCreate"))
                        End If
                    Else
                        m_FormBeingCreated = New Global.System.Collections.Hashtable()
                    End If
                    m_FormBeingCreated.Add(GetType(T), Nothing)
                    Try
                        Return New T()
                    Catch ex As Global.System.Reflection.TargetInvocationException When ex.InnerException IsNot Nothing
                        Dim BetterMessage As String = Global.Microsoft.VisualBasic.CompilerServices.Utils.GetResourceString("WinForms_SeeInnerException", ex.InnerException.Message)
                        Throw New Global.System.InvalidOperationException(BetterMessage, ex.InnerException)
                    Finally
                        m_FormBeingCreated.Remove(GetType(T))
                    End Try
                Else
                    Return Instance
                End If
            End Function

            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Sub Dispose__Instance__(Of T As Global.System.Windows.Forms.Form)(ByRef instance As T)
                instance.Dispose()
                instance = Nothing
            End Sub

            <Global.System.Diagnostics.DebuggerHidden()> _
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Public Sub New()
               MyBase.New()
            End Sub

            <Global.System.ThreadStatic()> Private Shared m_FormBeingCreated As Global.System.Collections.Hashtable

            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function Equals(ByVal o As Object) As Boolean
                Return MyBase.Equals(o)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function GetHashCode() As Integer
                Return MyBase.GetHashCode
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Friend Overloads Function [GetType]() As Global.System.Type
                Return GetType(MyForms)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)> Public Overrides Function ToString() As String
                Return MyBase.ToString
            End Function
        End Class

        Private m_MyFormsObjectProvider As New ThreadSafeObjectProvider(Of MyForms)

#End If

#If _MYWEBSERVICES = True Then

        <Global.System.ComponentModel.Design.HelpKeyword("My.WebServices")> _
        Friend ReadOnly Property WebServices() As MyWebServices
             <Global.System.Diagnostics.DebuggerHidden()> _
             Get
                Return m_MyWebServicesObjectProvider.GetInstance()
            End Get
        End Property

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        <Global.Microsoft.VisualBasic.MyGroupCollection("System.Web.Services.Protocols.SoapHttpClientProtocol", "Create__Instance__", "Dispose__Instance__", "")> _
        Friend NotInheritable Class MyWebServices

            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Public Overrides Function Equals(ByVal o As Object) As Boolean
                Return MyBase.Equals(o)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Public Overrides Function GetHashCode() As Integer
                Return MyBase.GetHashCode
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Friend Overloads Function [GetType]() As Global.System.Type
                Return GetType(MyWebServices)
            End Function
            <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never), Global.System.Diagnostics.DebuggerHidden()> _
            Public Overrides Function ToString() As String
                Return MyBase.ToString
            End Function

           <Global.System.Diagnostics.DebuggerHidden()> _
           Private Shared Function Create__Instance__(Of T As {New})(ByVal instance As T) As T
                If instance Is Nothing Then
                    Return New T()
                Else
                    Return instance
                End If
            End Function

            <Global.System.Diagnostics.DebuggerHidden()> _
            Private Sub Dispose__Instance__(Of T)(ByRef instance As T)
                instance = Nothing
            End Sub

            <Global.System.Diagnostics.DebuggerHidden()> _
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Public Sub New()
               MyBase.New()
            End Sub
        End Class

        Private ReadOnly m_MyWebServicesObjectProvider As New ThreadSafeObjectProvider(Of MyWebServices)
#End If

#If _MYTYPE = "Web" Then

        <Global.System.ComponentModel.Design.HelpKeyword("My.Request")> _
        Friend ReadOnly Property Request() As Global.System.Web.HttpRequest
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Dim CurrentContext As Global.System.Web.HttpContext = Global.System.Web.HttpContext.Current
                If CurrentContext IsNot Nothing Then
                    Return CurrentContext.Request
                End If
                Return Nothing
            End Get
        End Property

        <Global.System.ComponentModel.Design.HelpKeyword("My.Response")> _
        Friend ReadOnly Property Response() As Global.System.Web.HttpResponse
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Dim CurrentContext As Global.System.Web.HttpContext = Global.System.Web.HttpContext.Current
                If CurrentContext IsNot Nothing Then
                    Return CurrentContext.Response
                End If
                Return Nothing
            End Get
        End Property

        <Global.System.ComponentModel.Design.HelpKeyword("My.Application.Log")> _
        Friend ReadOnly Property Log() As Global.Microsoft.VisualBasic.Logging.AspLog
            <Global.System.Diagnostics.DebuggerHidden()> _
            Get
                Return m_LogObjectProvider.GetInstance()
            End Get
        End Property

        Private ReadOnly m_LogObjectProvider As New ThreadSafeObjectProvider(Of Global.Microsoft.VisualBasic.Logging.AspLog)

#End If  '_MYTYPE="Web"

        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
        <Global.System.Runtime.InteropServices.ComVisible(False)> _
        Friend NotInheritable Class ThreadSafeObjectProvider(Of T As New)
            Friend ReadOnly Property GetInstance() As T
#If TARGET = "library" Then
                <Global.System.Diagnostics.DebuggerHidden()> _
                Get
                    Dim Value As T = m_Context.Value
                    If Value Is Nothing Then
                        Value = New T
                        m_Context.Value() = Value
                    End If
                    Return Value
                End Get
#Else
                <Global.System.Diagnostics.DebuggerHidden()> _
                Get
                    If m_ThreadStaticValue Is Nothing Then m_ThreadStaticValue = New T
                    Return m_ThreadStaticValue
                End Get
#End If
            End Property

            <Global.System.Diagnostics.DebuggerHidden()> _
            <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Never)> _
            Public Sub New()
                MyBase.New()
            End Sub

#If TARGET = "library" Then
            Private ReadOnly m_Context As New Global.Microsoft.VisualBasic.MyServices.Internal.ContextValue(Of T)
#Else
            <Global.System.Runtime.CompilerServices.CompilerGenerated(), Global.System.ThreadStatic()> Private Shared m_ThreadStaticValue As T
#End If
        End Class
    End Module
End Namespace
#End If
