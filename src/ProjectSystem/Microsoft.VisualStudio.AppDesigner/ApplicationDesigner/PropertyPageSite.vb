Imports EnvDTE
Imports Microsoft.VisualStudio.Editors
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports System.ComponentModel.Design
Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports OleInterop = Microsoft.VisualStudio.OLE.Interop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner


    ''' <summary>
    ''' Povides specific limited functionality we need from the owner of the
    '''   property page site (ApplicationDesignerView).
    ''' </summary>
    ''' <remarks></remarks>
    Public Interface IPropertyPageSiteOwner
        Function GetLocaleID() As UInteger
        Sub DsMsgBox(ByVal ex As Exception, Optional ByVal HelpLink As String = Nothing)
        Sub DelayRefreshDirtyIndicators()
        Function GetService(ByVal ServiceType As Type) As Object
    End Interface

    ''' <summary>
    ''' This class provides the IPropertyPageSite implementation for the property pages
    ''' It also drives immediate apply functionality when 
    ''' </summary>
    ''' <remarks></remarks>
    Public Class PropertyPageSite
        Implements OleInterop.IPropertyPageSite
        Implements IDisposable
        Implements IServiceProvider
        Implements OLE.Interop.IServiceProvider

        Private m_PropPage As OleInterop.IPropertyPage
        Private m_AppDesView As IPropertyPageSiteOwner
        Private m_IsImmediateApply As Boolean = True

        'The service provider to delegate IServiceProvider calls through
        Private m_BackingServiceProvider As IServiceProvider
        Private m_HasBeenSetDirty As Boolean

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="View">An IPropertyPageSiteOwner implementation.  Generally this is the
        '''   ApplicationDesignerView, except for unit testing.</param>
        ''' <param name="PropPage"></param>
        ''' <remarks></remarks>
        Sub New(ByVal View As IPropertyPageSiteOwner, ByVal PropPage As OleInterop.IPropertyPage)
            Debug.Assert(View IsNot Nothing AndAlso PropPage IsNot Nothing)
            m_AppDesView = View
            m_PropPage = PropPage
            PropPage.SetPageSite(Me)
        End Sub


        ''' <summary>
        ''' The service provider to delegate to when responding to QueryService requests (for both
        '''   native and managed IServiceProvider).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property BackingServiceProvider() As IServiceProvider
            Get
                Return m_BackingServiceProvider
            End Get
            Set(ByVal value As IServiceProvider)
#If DEBUG Then
                If value IsNot Nothing Then
                    Dim NativeServiceProvider As OLE.Interop.IServiceProvider = TryCast(value.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Debug.Assert(NativeServiceProvider IsNot Nothing, "The managed IServiceProvider passed in to PropertyPageSite constructor does not wrap a native IServiceProvider")
                End If
#End If
                m_BackingServiceProvider = value
            End Set
        End Property

        ''' <summary>
        ''' Our property page hosting is unusual in that it has the concept of "immediate apply", when all changes
        '''   are immediately applied to the project system (by our telling the page to apply its changes on any
        '''   status change of validate).  Since we ask the page to apply essentially as soon as it's dirty, the page
        '''   immediately marks itself as clean again.  This makes it problematic for us to tell whether a page is
        '''   dirty or not.  So we keep this state as an indicator that the page has had changes made to it at one
        '''   point or another.  The flag will have to be set to False when the project designer is saved, because it
        '''   won't normally be set to False by the page.
        ''' </summary>
        Public Property HasBeenSetDirty() As Boolean
            Get
                Return m_HasBeenSetDirty
            End Get
            Set(ByVal value As Boolean)
                m_HasBeenSetDirty = value
            End Set
        End Property


#Region " IPropertyPageSite"
        Public Const PROPPAGESTATUS_DIRTY As Integer = 1
        Public Const PROPPAGESTATUS_VALIDATE As Integer = 2
        Public Const PROPPAGESTATUS_CLEAN As Integer = 4


        Public Sub GetLocaleID(ByRef pLocaleID As UInteger) Implements OleInterop.IPropertyPageSite.GetLocaleID
            'Try getting the locale ID from the shell
            If m_AppDesView IsNot Nothing Then
                pLocaleID = m_AppDesView.GetLocaleID()
                Return
            End If

            'Fallback
            pLocaleID = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.GetUserDefaultLCID()
        End Sub

        Public Sub GetPageContainer(ByRef ppunk As Object) Implements OleInterop.IPropertyPageSite.GetPageContainer
            ' This method is not implemented as per MSDN guidlines for IPropertyPageSite
            Throw New NotImplementedException
        End Sub

        Public Sub OnStatusChange(ByVal dwFlags As UInteger) Implements OleInterop.IPropertyPageSite.OnStatusChange
            ApplyStatusChange(dwFlags)
        End Sub

        ''' <summary>
        '''  We will apply changes if needed
        ''' </summary>
        ''' <returns>return False if it failed</returns>
        Private Function ApplyStatusChange(ByVal dwFlags As UInteger) As Boolean
            Static InsideOnStatusChange As Boolean

            If InsideOnStatusChange Then
                'This is a case of Apply causing some event to occur
                'such as changing focus because of an exception, etc.
                'This means we just need to ignore this change
                Return False
            End If

            Dim successed As Boolean = True

            Try
                InsideOnStatusChange = True
                ' If the page is dirty and 
                If m_IsImmediateApply AndAlso _
                    (dwFlags And PROPPAGESTATUS_VALIDATE) <> 0 Then
                    If m_PropPage.IsPageDirty() = Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_OK Then
                        Try
                            m_PropPage.Apply()
                        Catch ex As Exception
                            successed = False
                            If Not AppDesCommon.IsCheckoutCanceledException(ex) Then
                                Debug.Assert(m_AppDesView IsNot Nothing, "No project designer - can't show error message: " & ex.ToString())
                                If m_AppDesView IsNot Nothing Then
                                    m_AppDesView.DsMsgBox(ex)
                                End If
                            End If
                        End Try
                    End If
                End If

                If (dwFlags And PROPPAGESTATUS_DIRTY) <> 0 Then
                    m_HasBeenSetDirty = True
                End If
                If (dwFlags And PROPPAGESTATUS_CLEAN) <> 0 Then
                    'If the page marks itself as clean, other than in the case where
                    '  we tell them to Apply in response to validate (which is caught above
                    '  by the check against recursion using InsideOnStatusChange), then we'll
                    '  honor that state with HasBeenSetDirty, assuming they have some unorthodox
                    '  method of getting to a clean state.
                    m_HasBeenSetDirty = False
                End If

                If m_AppDesView IsNot Nothing Then
                    m_AppDesView.DelayRefreshDirtyIndicators()
                End If
            Finally
                'Make sure to renable this code
                InsideOnStatusChange = False
            End Try

            Return successed

        End Function

        ''' <summary>
        ''' Commits any pending changes on the page
        ''' </summary>
        ''' <returns>return False if it failed</returns>
        ''' <remarks></remarks>
        Public Function CommitPendingChanges() As Boolean
            If Not ApplyStatusChange(PROPPAGESTATUS_VALIDATE) Then
                Return False
            End If
            Return True
        End Function

        ''' <summary>
        ''' Instructs the page site to process a keystroke if it desires.
        ''' </summary>
        ''' <param name="pMsg"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This function can be called by a property page to give the site a chance to a process a message
        '''   before the page does.  Return S_OK to indicate we have handled it, S_FALSE to indicate we did not
        '''   process it, and E_NOTIMPL to indicate that the site does not support keyboard processing.
        ''' </remarks>
        Public Function TranslateAccelerator(ByVal pMsg() As Microsoft.VisualStudio.OLE.Interop.MSG) As Integer Implements OleInterop.IPropertyPageSite.TranslateAccelerator
            Common.Switches.TracePDMessageRouting(TraceLevel.Error, "PropertyPageSite.TranslateAccelerator")

            'We're not currently interested in any message filtering from the property pages.
            Return Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.S_FALSE
        End Function
#End Region

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Disposes of any the doc data
        ''' </summary>
        ''' <remarks></remarks>
        Public Overloads Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
        End Sub

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If m_PropPage IsNot Nothing Then
                    m_PropPage = Nothing
                End If
                m_AppDesView = Nothing
                m_BackingServiceProvider = Nothing
            End If
        End Sub
#End Region

#Region "System.IServiceProvider implementation"

        ''' <summary>
        ''' Implements the managed IServiceProvider interface by delegating
        '''   it to the service provider passed in through the constructor.
        ''' </summary>
        ''' <param name="serviceType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetService(ByVal serviceType As System.Type) As Object Implements System.IServiceProvider.GetService
            'A couple of specific services we delegate to the application designer, but other than
            '  those exceptions, we want the services coming from the passed-in backing service (which
            '  will come from the PropPageDesignerView).
            If serviceType Is GetType(PropPageDesigner.ConfigurationState) _
            OrElse serviceType Is GetType(ApplicationDesignerView) _
            Then
                'Delegate to app designer
                If m_AppDesView IsNot Nothing Then
                    Return m_AppDesView.GetService(serviceType)
                Else
                    Return Nothing
                End If
            End If

            If m_BackingServiceProvider IsNot Nothing Then
                Return m_BackingServiceProvider.GetService(serviceType)
            Else
                Debug.Fail("No service provider has been set in the PropertyPageSite")
                Return Nothing
            End If
        End Function

#End Region

#Region "OLE.Interop.IServiceProvider implementation"

        ''' <summary>
        ''' Implements OLE's IServiceProvider interface by delegating
        '''   it to the service provider passed in through the constructor.
        ''' </summary>
        ''' <param name="guidService"></param>
        ''' <param name="riid"></param>
        ''' <param name="ppvObject"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This is important to allow native property pages to get to services
        '''   like the IVsWindowFrame that the property page is hosted in, which is
        '''   necessary in order to hook up help.
        ''' </remarks>
        Public Function QueryService(ByRef guidService As System.Guid, ByRef riid As System.Guid, ByRef ppvObject As System.IntPtr) As Integer Implements OLE.Interop.IServiceProvider.QueryService
            If m_BackingServiceProvider IsNot Nothing Then
                'Get the native service provider which the managed IServiceProvider wraps
                Dim NativeServiceProvider As OLE.Interop.IServiceProvider = TryCast(m_BackingServiceProvider.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                If NativeServiceProvider IsNot Nothing Then
                    Return NativeServiceProvider.QueryService(guidService, riid, ppvObject)
                Else
                    Debug.Fail("Unable to get native IServiceProvider from managed IServiceProvider")
                End If
            Else
                Debug.Fail("No service provider has been set in the PropertyPageSite")
            End If

            Return NativeMethods.E_NOINTERFACE
        End Function

#End Region

    End Class

End Namespace
