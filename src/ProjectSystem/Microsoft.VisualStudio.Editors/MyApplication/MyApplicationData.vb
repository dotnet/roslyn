Imports System

Namespace Microsoft.VisualStudio.Editors.MyApplication

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)> _
    Friend NotInheritable Class MyApplicationData

        Public Sub New()
            'Set defaults that don't match empty constructor
            EnableVisualStyles = True

            'Default authentication mode to "Windows"
            AuthenticationMode = Global.Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.Windows

            m_SaveMySettingsOnExit = True
        End Sub

        Private m_MySubMain As Boolean 'True indicates My.MyApplication will be StartupObject
        Private m_MainFormNoRootNS As String 'Form for My.MyApplication to instantiate for main window (without the root namespace)
        Private m_SingleInstance As Boolean
        Private m_ShutdownMode As Integer
        Private m_EnableVisualStyles As Boolean
        Private m_AuthenticationMode As Integer
        Private m_SplashScreenNoRootNS As String 'Splash screen to use (without the root namespace)
        Private m_SaveMySettingsOnExit As Boolean 'Whether to save My.Settings on shutdown

        Private m_Dirty As Boolean

        Public Property IsDirty() As Boolean
            Get
                Return m_Dirty
            End Get
            Set(ByVal value As Boolean)
                m_Dirty = value
            End Set
        End Property

        Public Property MySubMain() As Boolean 'True indicates My.MyApplication will be StartupObject
            Get
                Return m_MySubMain
            End Get
            Set(ByVal value As Boolean)
                m_MySubMain = value
                IsDirty = True
            End Set
        End Property

        Public Property MainFormNoRootNS() As String 'Form for My.MyApplication to instantiate for main window (not including the root namespace)
            Get
                Return m_MainFormNoRootNS
            End Get
            Set(ByVal value As String)
                m_MainFormNoRootNS = value
                IsDirty = True
            End Set
        End Property

        Public Property SingleInstance() As Boolean
            Get
                Return m_SingleInstance
            End Get
            Set(ByVal value As Boolean)
                m_SingleInstance = value
                IsDirty = True
            End Set
        End Property

        Public Property ShutdownMode() As Integer
            Get
                Return m_ShutdownMode
            End Get
            Set(ByVal value As Integer)
                m_ShutdownMode = value
                IsDirty = True
            End Set
        End Property

        Public Property EnableVisualStyles() As Boolean
            Get
                Return m_EnableVisualStyles
            End Get
            Set(ByVal value As Boolean)
                m_EnableVisualStyles = value
                IsDirty = True
            End Set
        End Property

        Public Property AuthenticationMode() As Integer
            Get
                Return m_AuthenticationMode
            End Get
            Set(ByVal value As Integer)
                m_AuthenticationMode = value
                IsDirty = True
            End Set
        End Property

        Public Property SplashScreenNoRootNS() As String
            Get
                Return m_SplashScreenNoRootNS
            End Get
            Set(ByVal value As String)
                m_SplashScreenNoRootNS = value
                IsDirty = True
            End Set
        End Property

        Public Property SaveMySettingsOnExit() As Boolean
            Get
                Return m_SaveMySettingsOnExit
            End Get
            Set(ByVal value As Boolean)
                m_SaveMySettingsOnExit = value
                IsDirty = True
            End Set
        End Property


    End Class

End Namespace
