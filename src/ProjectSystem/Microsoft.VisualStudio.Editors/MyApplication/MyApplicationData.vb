' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.MyApplication

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)> _
    Friend NotInheritable Class MyApplicationData

        Public Sub New()
            'Set defaults that don't match empty constructor
            EnableVisualStyles = True

            'Default authentication mode to "Windows"
            AuthenticationMode = Global.Microsoft.VisualBasic.ApplicationServices.AuthenticationMode.Windows

            _saveMySettingsOnExit = True
        End Sub

        Private _mySubMain As Boolean 'True indicates My.MyApplication will be StartupObject
        Private _mainFormNoRootNS As String 'Form for My.MyApplication to instantiate for main window (without the root namespace)
        Private _singleInstance As Boolean
        Private _shutdownMode As Integer
        Private _enableVisualStyles As Boolean
        Private _authenticationMode As Integer
        Private _splashScreenNoRootNS As String 'Splash screen to use (without the root namespace)
        Private _saveMySettingsOnExit As Boolean 'Whether to save My.Settings on shutdown

        Private _dirty As Boolean

        Public Property IsDirty() As Boolean
            Get
                Return _dirty
            End Get
            Set(ByVal value As Boolean)
                _dirty = value
            End Set
        End Property

        Public Property MySubMain() As Boolean 'True indicates My.MyApplication will be StartupObject
            Get
                Return _mySubMain
            End Get
            Set(ByVal value As Boolean)
                _mySubMain = value
                IsDirty = True
            End Set
        End Property

        Public Property MainFormNoRootNS() As String 'Form for My.MyApplication to instantiate for main window (not including the root namespace)
            Get
                Return _mainFormNoRootNS
            End Get
            Set(ByVal value As String)
                _mainFormNoRootNS = value
                IsDirty = True
            End Set
        End Property

        Public Property SingleInstance() As Boolean
            Get
                Return _singleInstance
            End Get
            Set(ByVal value As Boolean)
                _singleInstance = value
                IsDirty = True
            End Set
        End Property

        Public Property ShutdownMode() As Integer
            Get
                Return _shutdownMode
            End Get
            Set(ByVal value As Integer)
                _shutdownMode = value
                IsDirty = True
            End Set
        End Property

        Public Property EnableVisualStyles() As Boolean
            Get
                Return _enableVisualStyles
            End Get
            Set(ByVal value As Boolean)
                _enableVisualStyles = value
                IsDirty = True
            End Set
        End Property

        Public Property AuthenticationMode() As Integer
            Get
                Return _authenticationMode
            End Get
            Set(ByVal value As Integer)
                _authenticationMode = value
                IsDirty = True
            End Set
        End Property

        Public Property SplashScreenNoRootNS() As String
            Get
                Return _splashScreenNoRootNS
            End Get
            Set(ByVal value As String)
                _splashScreenNoRootNS = value
                IsDirty = True
            End Set
        End Property

        Public Property SaveMySettingsOnExit() As Boolean
            Get
                Return _saveMySettingsOnExit
            End Get
            Set(ByVal value As Boolean)
                _saveMySettingsOnExit = value
                IsDirty = True
            End Set
        End Property


    End Class

End Namespace
