Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop
Imports OleInterop = Microsoft.VisualStudio.OLE.Interop

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' This page encapsulates all the data for a property page
    ''' </summary>
    ''' <remarks></remarks>
    Public Class PropertyPageInfo
        Implements System.IDisposable

        Private m_Guid As Guid 'The GUID for the property page
        Private m_IsConfigPage As Boolean 'True if the page's properties can have different values in different configurations

        Private m_ComPropPageInstance As OleInterop.IPropertyPage
        Private m_Info As OleInterop.PROPPAGEINFO
        Private m_Site As PropertyPageSite
        Private m_LoadException As Exception 'The exception that occurred while loading the page, if any
        Private m_ParentView As ApplicationDesignerView 'The owning application designer view
        Private m_LoadAlreadyAttempted As Boolean 'Whether or not we've attempted to load this property page

        Private Const REGKEY_CachedPageTitles As String = "\ProjectDesigner\CachedPageTitles"
        Private Const REGVALUE_CachedLocaleId As String = "LocaleID"



        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Guid">The GUID to create the property page</param>
        ''' <param name="IsConfigurationDependentPage">Whether or not the page has different values for each configuration (e.g. the Debug page)</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal ParentView As ApplicationDesignerView, ByVal Guid As Guid, ByVal IsConfigurationDependentPage As Boolean)
            Debug.Assert(Not Guid.Equals(System.Guid.Empty), "Empty guid?")
            Debug.Assert(ParentView IsNot Nothing)
            Me.m_ParentView = ParentView
            Me.m_Guid = Guid
            Me.m_IsConfigPage = IsConfigurationDependentPage
        End Sub


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
                Try
                    If m_ComPropPageInstance IsNot Nothing Then
                        m_ComPropPageInstance.Deactivate()
                        m_ComPropPageInstance.SetPageSite(Nothing)
                        If Marshal.IsComObject(m_ComPropPageInstance) Then
                            Marshal.ReleaseComObject(m_ComPropPageInstance)
                        End If
                        m_ComPropPageInstance = Nothing
                    End If
                    If m_Site IsNot Nothing Then
                        m_Site.Dispose()
                        m_Site = Nothing
                    End If
                Catch ex As OutOfMemoryException
                    Throw
                Catch ex As Threading.ThreadAbortException
                    Throw
                Catch ex As StackOverflowException
                    Throw
                Catch ex As Exception
                    'Ignore everything else
                End Try

            End If
        End Sub

#End Region


        ''' <summary>
        ''' The GUID for the property page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property Guid() As Guid
            Get
                Return m_Guid
            End Get
        End Property


        ''' <summary>
        ''' True if the page's properties can have different values in different configurations
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsConfigPage() As Boolean
            Get
                Return m_IsConfigPage
            End Get
        End Property


        ''' <summary>
        ''' The exception that occurred while loading the page, if any
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property LoadException() As Exception
            Get
                Return m_LoadException
            End Get
        End Property


        ''' <summary>
        ''' Returns the IPropertyPage for the property page
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly Property ComPropPageInstance() As OleInterop.IPropertyPage
            Get
                TryLoadPropertyPage()
                Return m_ComPropPageInstance
            End Get
        End Property


        ''' <summary>
        ''' Returns the PropertyPageSite for the property page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property Site() As PropertyPageSite
            Get
                TryLoadPropertyPage()
                Return m_Site
            End Get
        End Property


        ''' <summary>
        ''' Attempts to load the COM object for the property page, if it has not already
        '''   been attempted.  Does not throw on failure, but rather sets the LoadException
        '''   field to the exception which resulted.
        ''' </summary>
        ''' <remarks>Overridable for unit testing.</remarks>
        Public Overridable Sub TryLoadPropertyPage()
            Debug.Assert(m_ParentView IsNot Nothing)
            If m_LoadAlreadyAttempted Then
                Return
            End If
            Debug.Assert(m_LoadException Is Nothing)
            m_LoadAlreadyAttempted = True

            Try
                Common.Switches.TracePDPerf("*** PERFORMANCE WARNING: Attempting to load property page " & m_Guid.ToString)
                Dim LocalRegistry As ILocalRegistry
                LocalRegistry = CType(m_ParentView.GetService(GetType(ILocalRegistry)), ILocalRegistry)

                If LocalRegistry Is Nothing Then
                    Debug.Fail("Unabled to obtain ILocalRegistry")
                    m_LoadException = New ArgumentNullException("ParentView")
                    Return
                End If

                'Have to use array of 1 because of interop
                Dim PageInfos As OleInterop.PROPPAGEINFO() = New OleInterop.PROPPAGEINFO(0 + 1) {}

                Dim PageObject As Object
                Dim ComPropertyPageInstance As OleInterop.IPropertyPage

                Dim ObjectPtr As IntPtr

                VSErrorHandler.ThrowOnFailure(LocalRegistry.CreateInstance(m_Guid, Nothing, NativeMethods.IID_IUnknown, win.CLSCTX_INPROC_SERVER, ObjectPtr))
                Try
                    PageObject = Marshal.GetObjectForIUnknown(ObjectPtr)
                Finally
                    Marshal.Release(ObjectPtr)
                End Try

                ComPropertyPageInstance = CType(PageObject, OleInterop.IPropertyPage)

                'Save the IPropertyPage object
                m_ComPropPageInstance = ComPropertyPageInstance

                'Set the page site
                m_Site = New PropertyPageSite(m_ParentView, ComPropertyPageInstance)

                'Get the property page's PAGEINFO for later use
                ComPropertyPageInstance.GetPageInfo(PageInfos)
                m_Info = PageInfos(0)

                Common.Switches.TracePDPerf("  [Loaded property page '" & m_Info.pszTitle & "']")

#If DEBUG Then
                'Verify that loading the property page actually gave us the same title as the
                '  cached version.
                If m_Info.pszTitle IsNot Nothing AndAlso CachedTitle IsNot Nothing Then
                    Debug.Assert(m_Info.pszTitle.Equals(CachedTitle), _
                        "The page title retrieved from cache ('" & CachedTitle & "') was not the same as that retrieved by " _
                        & "loading the page ('" & m_Info.pszTitle & "')")
                End If
#End If

                'Cache the title for future use
                CachedTitle = m_Info.pszTitle

            Catch Ex As Exception When Not AppDesCommon.IsUnrecoverable(Ex)
                'Debug.Fail("Unable to create property page with guid " & m_Guid.ToString() & vbCrLf & Ex.Message)
                If m_ComPropPageInstance IsNot Nothing Then
                    'IPropertyPage.GetPageInfo probably failed - if that didn't 
                    ' succeed, then nothing much else will likely work on the page either
                    If Marshal.IsComObject(m_ComPropPageInstance) Then
                        Marshal.ReleaseComObject(m_ComPropPageInstance)
                    End If
                    m_ComPropPageInstance = Nothing
                Else
                    'Page failed to load
                End If

                m_LoadException = Ex 'Save this to display later
            End Try
        End Sub


        ''' <summary>
        ''' Retrieves the title for the property page.  This title is cached on the
        '''   machine after the first time we've loaded the property page, so calling 
        '''   this property tries *not* load the property page if it's not
        '''   already loaded.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' PERF: This property used a cached version of the title to avoid having to
        '''   instantiate the COM object for the property page.
        ''' </remarks>
        Public ReadOnly Property Title() As String
            Get
                If m_LoadAlreadyAttempted Then
                    If m_LoadException Is Nothing AndAlso m_Info.pszTitle <> "" Then
                        Debug.Assert(m_LoadAlreadyAttempted AndAlso m_LoadException Is Nothing)
                        Common.Switches.TracePDPerf("PropertyPageInfo.Title: Property page was already loaded, returning from m_Info: '" & m_Info.pszTitle & "'")
                        Return m_Info.pszTitle
                    Else
                        Common.Switches.TracePDPerf("PropertyPageInfo.Title: Previously attempted to load property page and failed, returning empty title")
                        Return String.Empty
                    End If
                Else
                    'Do we have a cached version?
                    Dim Cached As String = CachedTitle
                    If Cached <> "" Then
                        Common.Switches.TracePDPerf("PropertyPageInfo.Title: Retrieved page title from cache: " & Cached)
                        Return CachedTitle
                    End If

                    'No cache, we have no choice but to load the property page and ask it for the title
                    TryLoadPropertyPage() 'This will cache the newly-obtained title.
                    Return m_Info.pszTitle
                End If
            End Get
        End Property


        ''' <summary>
        ''' Gets the current locale ID that's being used by the project designer.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property CurrentLocaleID() As UInteger
            Get
                Return CType(m_ParentView, IPropertyPageSiteOwner).GetLocaleID()
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the name of the registry value name to place into the
        '''   registry for this property page.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property CachedTitleValueName() As String
            Get
                'We must include both the property page GUID and the locale ID
                '  so that we react properly to user language changes.
                Return m_Guid.ToString() & "," & CurrentLocaleID.ToString()
            End Get
        End Property


        ''' <summary>
        ''' Attempts to retrieve or set the cached title of this page from the registry.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Property CachedTitle() As String
            Get
                Dim KeyPath As String = m_ParentView.DTEProject.DTE.RegistryRoot & REGKEY_CachedPageTitles
                Dim Key As Win32.RegistryKey = Nothing
                Try
                    Key = Win32.Registry.CurrentUser.OpenSubKey(KeyPath)
                    If Key IsNot Nothing Then
                        Dim ValueObject As Object = Key.GetValue(CachedTitleValueName)
                        If TypeOf ValueObject Is String Then
                            'Found a cached version
                            Return DirectCast(ValueObject, String)
                        End If
                    End If
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Debug.Fail("Got exception trying to get cached page title: " & ex.ToString)
                Finally
                    If Key IsNot Nothing Then
                        Key.Close()
                    End If
                End Try

                'No cached title yet.
                Return Nothing
            End Get
            Set(ByVal value As String)
                If value Is Nothing Then
                    value = String.Empty
                End If
                Dim Key As Win32.RegistryKey = Nothing
                Try
                    Key = Win32.Registry.CurrentUser.CreateSubKey(m_ParentView.DTEProject.DTE.RegistryRoot & REGKEY_CachedPageTitles)
                    If Key IsNot Nothing Then
                        Key.SetValue(CachedTitleValueName, value, Win32.RegistryValueKind.String)
                    End If
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Debug.Fail("Unable to cache page title: " & ex.Message)
                Finally
                    If Key IsNot Nothing Then
                        Key.Close()
                    End If
                End Try
            End Set
        End Property

    End Class

End Namespace
