Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner
Imports System.ComponentModel.Design
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing
Imports Interop = Microsoft.VisualStudio.Editors.AppDesInterop
Imports System.Runtime.InteropServices
Imports System.Collections.Specialized
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.ComponentModel

Imports System.Windows.Forms.Design
Imports Microsoft.VisualStudio.Shell
Imports VB = Microsoft.VisualBasic
Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.PropertyPages


    ''' <summary>
    ''' Base class for managed property pages internal used in VisualStudio
    ''' </summary>
    ''' <remarks></remarks>
    <ComVisible(False)> _
    Public Class PropPageUserControlBase
        Inherits System.Windows.Forms.UserControl
        Implements IPropertyPageInternal
        Implements IVsProjectDesignerPage
        Implements IVsDebuggerEvents
        Implements IVsBroadcastMessageEvents

#Region " Windows Form Designer generated code "

        Dim m_UIShell5Service As Microsoft.VisualStudio.Shell.Interop.IVsUIShell5

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Protected Sub New(ByVal serviceProvider As Microsoft.VisualStudio.Shell.ServiceProvider)
            MyBase.New()
            Me.SuspendLayout()

            Me.Text = SR.GetString(SR.PPG_PropertyPageControlName)
            Me.AccessibleRole = System.Windows.Forms.AccessibleRole.PropertyPage
            Me.m_ServiceProvider = serviceProvider

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            Me.BackColor = PropPageBackColor

            'Add any initialization after the InitializeComponent() call
            AddToRunningTable()

            Me.ResumeLayout(False) 'False: layout will happen later, not needed here
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            'Release unmanaged resources

            If disposing Then
                CleanupCOMReferences()

                If IsInProjectCheckoutSection Then
                    'It is possible for a source code checkout operation to cause a project reload (and thus the closing/disposal of 
                    '  the project designer and all property pages) during an attempt to change a property's value.  It is difficult
                    '  for WinForms to gracefully recover from the disposal of the property page and controls in the middle of any apply, 
                    '  so we delay the main Dispose() until after the apply is finished.
                    'We do go ahead and get rid of COM references and do general clean-up, though.  This includes removing our 
                    '  listening to events from the environment, etc.
                    'We do *not* call in to the base's Dispose(), because that will get rid of the controls, and that's what we're
                    '  trying to avoid right now.

                    Trace.WriteLine("***** PropPageUserControlBase.Dispose(): Being forcibly deactivated during an checkout.  Disposal of controls will be delayed until after the current callstack is finished.")
                    m_ProjectReloadedDuringCheckout = True

                    For Each page As PropPageUserControlBase In m_ChildPages.Values

                        Dim HostDialog As PropPageHostDialog = GetPropPageHostDialog(page)

                        ' Notify child pages that the project reloaded happened if they are
                        ' listening for such a change
                        If HostDialog IsNot Nothing AndAlso _
                           HostDialog.PropPage IsNot Nothing AndAlso _
                           HostDialog.PropPage.IsInProjectCheckoutSection Then

                            HostDialog.PropPage.m_ProjectReloadedDuringCheckout = True
                        End If
                    Next page

                Else
                    Debug.Assert(m_SuspendPropertyChangeListeningDispIds.Count = 0, "Missing a ResumePropertyChangeListening() call?")

                    RemoveFromRunningTable()
                    If Not (components Is Nothing) Then
                        components.Dispose()
                    End If

                    'Dispose all child pages.  A child page normally stays open if an exception
                    '  occurs during the OK button click, so we have to force it closed.
                    For Each page As PropPageUserControlBase In m_ChildPages.Values
                        Dim HostDialog As PropPageHostDialog = GetPropPageHostDialog(page)
                        If HostDialog IsNot Nothing Then
                            If HostDialog.PropPage IsNot Nothing Then
                                HostDialog.Dispose()
                            End If
                            HostDialog.Dispose()
                        End If
                    Next page
                    m_ChildPages.Clear()

                    MyBase.Dispose(disposing)
                End If 'If m_fIsApplying...

            End If 'If disposing...
        End Sub


        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Me.SuspendLayout()

            '
            'PropPageUserControlBase
            '
            Me.Name = "PropPageUserControlBase"
            Me.Size = New System.Drawing.Size(528, 296)
            Me.AutoSize = False

            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub

#End Region

        ''' <summary>
        ''' Specifies the source or cause of a property change notification
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum PropertyChangeSource
            Direct    'This property is being changed directly through a PropertyControlData on the page.  Normally should be ignored, the UI will be updated automatically after the change.
            Indirect  'Change was caused indirectly when a PropertyControlData changed a separate property on the page.  For instance, changing the StartupObject property causes the project system to modify several other properties.
            External  'The property was changed externally to the page (via another page or through the DTE or other separate UI), or else it was not changed through a PropertyControlData.
        End Enum

        'Used for caching property change notification until after Apply is done
        Private Class PropertyChange
            Public DispId As Integer
            Public Source As PropertyChangeSource

            Public Sub New(ByVal DispId As Integer, ByVal Source As PropertyChangeSource)
                Me.DispId = DispId
                Me.Source = Source
            End Sub
        End Class

        'True iff the property page is currently in initialization code
        Public m_fInsideInit As Boolean

        'Property page is currently dirty
        Protected m_IsDirty As Boolean

        'Backs the CanApplyNow property
        Private m_CanApplyNow As Boolean = True

        'The site passed in to SetPageSite.  May be Nothing if not currently sited.
        Private m_Site As IPropertyPageSiteInternal

        'May be used by derived property pages to cache their PropertyControlData in their ControlData property override.
        '  Not used directly by the architecture, which should always make a get property call to ControlData.
        Protected m_ControlData As PropertyControlData()

        'The set of objects that were passed in to SetObjects.  These are the objects whose properties are displayed in the property page.
        '  NOTE: Normally you should *not* use this property directly, but rather should call RawPropertiesObjects from the
        '  associated PropertyControlData, as it may override the set of objects to use.
        Public m_Objects As Object()

        'An array of (extended) property descriptor collections pulled from each of the objects passed in 
        '  to SetObjects.
        Public m_ObjectsPropertyDescriptorsArray() As PropertyDescriptorCollection

        'The set of extended objects based on the objects that were passed in to SetObjects.
        '  NOTE: Normally you should *not* use this property directly, but rather should call ExtendedPropertiesObjects from the
        '  associated PropertyControlData, as it may override the set of objects to use.
        Public m_ExtendedObjects As Object()

        'The DTE object associated with the objects passed to SetObjects.  If there is none, this is Nothing.
        Private m_DTE As EnvDTE.DTE

        'The EnvDTE.Project object associated with the objects passed to SetObjects.  If there is none, this is Nothing.
        Private m_DTEProject As EnvDTE.Project

        ' Hook up to build events so we can enable/disable the property 
        ' page while building
        Private WithEvents m_buildEvents As EnvDTE.BuildEvents

        'A collection of (extended) property descriptors which have been collected from the project itself.  These
        '  properties were *not* passed in to us through SetObjects, and are not configuration-dependent.  Some 
        '  configuration-dependent pages need to display certain non-configuration properties, which can be
        '  found in this collection.  Note that for a non-configuration page, this set of property descriptors
        '  will be the same as the set that was passed in to us through SetObjects.
        Public m_CommonPropertyDescriptors As PropertyDescriptorCollection

        'The ProjectProperties object from a VSLangProj-based project (VB, C#, J#).  Used for querying
        '  common project properties in these types of projects.  Note that this object was *not* passed
        '  in through SetObjects, rather we obtained it by querying for the project.  It is used
        '  for common property handling.  See comments for m_CommonPropertyDescriptors.
        Private m_ProjectPropertiesObject As VSLangProj.ProjectProperties

        'Cached result from RawPropertiesObjectsOfAllProperties
        Private m_CachedRawPropertiesSuperset As Object()

        Private m_ServiceProvider As Microsoft.VisualStudio.Shell.ServiceProvider 'Cached service provider
        Private m_ProjectHierarchy As IVsHierarchy 'The IVsHierarchy for the current project

        'Debug mode stuff

        Private m_CurrentDebugMode As Shell.Interop.DBGMODE

        ' Cached IVsDebugger from shell in case we don't have a service provider at
        ' shutdown so we can undo our event handler
        Private m_VsDebugger As IVsDebugger
        Private m_VsDebuggerEventsCookie As UInteger

        'True iff multiple projects are currently selected
        Private m_MultiProjectSelect As Boolean

        Private m_UIShellService As IVsUIShell
        Private m_UIShell2Service As IVsUIShell2

        Private Shared RunningPropertyPages As New ArrayList

        'Child property pages that have been shown are cached here
        Private m_ChildPages As New Dictionary(Of Type, PropPageUserControlBase)
        Protected m_ScalingCompleted As Boolean

        Private m_PageRequiresScaling As Boolean = True

        ' When true, the dialog is not scaled automatically
        ' Currently only used by the Publish page because it isn't a normal page
        Private m_ManualPageScaling As Boolean = False

        'Backcolor for all property pages
        Public Shared ReadOnly PropPageBackColor As Color = System.Drawing.SystemColors.Control

        Private m_activated As Boolean = True
        Private m_inDelayValidation As Boolean

        'Saves whether the page should be enabled or disabled, according to the page's subclass.
        '  However, this state is only honored while the project is not running, etc.
        Private m_PageEnabledState As Boolean = True

        'A list of all connected property listeners
        Private m_PropertyListeners As New ArrayList '(Of PropertyListener)

        'Whether the page should be enabled based only on the debug state of the application
        Private m_PageEnabledPerDebugMode As Boolean = True

        'Whether the page should be enabled based on if we are building or not
        Private m_PageEnabledPerBuildMode As Boolean = True

        'True iff this property page is configuration-specific (like the compile page) instead of
        '  configuration-independent (like the application pages)
        Private m_fConfigurationSpecificPage As Boolean

        'Dispids which the page is changing manually, and which are to be ignored while listening for
        '  property changes
        Private m_SuspendPropertyChangeListeningDispIds As New List(Of Integer)

        'DISPID_UNKNOWN
        Public DISPID_UNKNOWN As Integer = Interop.win.DISPID_UNKNOWN

        'Cookie for use with IVsShell.{Advise,Unadvise}BroadcastMessages
        Private m_CookieBroadcastMessages As UInteger
        Private m_VsShellForUnadvisingBroadcastMessages As IVsShell

        'True if we're in the middle of an Apply
        Private m_fIsApplying As Boolean

        'A list of properties from which we received a property change notification
        '  while another property on the page while being changed or an apply was
        '  in progress.  They will be sent off after the change/apply is done.
        Private m_CachedPropertyChanges As List(Of PropertyChange)

        'True if the property page was forcibly closed (by SCC) during an apply.  In this case, we want to
        '  delay disposing our controls, and exit the apply and events as soon as possible to avoid possible
        '  problems since the project may have been closed down from under us.
        Private m_ProjectReloadedDuringCheckout As Boolean

        'When positive, the property page is in a project checkout section, which means that the project
        '  file might get checked out, which means that it is possible the checkout will cause a reload
        '  of the project.  
        Private m_CheckoutSectionCount As Integer

        ''' <summary>
        '''  Return a set of control groups. We validate the editing inside a control group when the focus moves out of it.
        '''    There is one exception: the delay validation only works for warning messages, fatal errors are still reported immediately when the change is applied
        ''' </summary>
        ''' <remarks>Only pages supporting delay-validation need return value</remarks>
        Protected Overridable ReadOnly Property ValidationControlGroups() As Control()()
            Get
                Return Nothing
            End Get
        End Property

        Public ReadOnly Property ProjectHierarchy() As IVsHierarchy
            Get
                Return m_ProjectHierarchy
            End Get
        End Property

        ''' <summary>
        ''' Determines if the page should be scaled automatically.  
        '''   WARNING: It is recommended to turn this off and use AutoScaleMode.Font.
        ''' </summary>
        ''' <value>True if the page should not be scaled automatically</value>
        ''' <remarks></remarks>
        Protected Property PageRequiresScaling() As Boolean 'CONSIDER: This should be opt-in, not opt-out
            Get
                Return m_PageRequiresScaling
            End Get
            Set(ByVal Value As Boolean)
                m_PageRequiresScaling = Value
            End Set
        End Property

        ''' <summary>
        ''' Determines if the page should be scaled in SetObjects.
        ''' </summary>
        ''' <value>true if the page should not be scaled</value>
        ''' <remarks>Used only by the Publish page for now</remarks>
        Protected Property ManualPageScaling() As Boolean
            Get
                Return m_ManualPageScaling
            End Get
            Set(ByVal value As Boolean)
                m_ManualPageScaling = value
            End Set
        End Property

        ''' <summary>
        '''  Return true if the page can be resized...
        ''' </summary>
        Public Overridable ReadOnly Property PageResizable() As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Determines whether the property page is enabled or not.  However, it also takes into
        '''   consideration other states (e.g., whether the application is running or in break mode),
        '''   and keeps the page disabled during break mode.  Once the app stops, the page is set
        '''   to the state requested here.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Shadows Property Enabled() As Boolean
            Get
                Return m_PageEnabledState
            End Get
            Set(ByVal value As Boolean)
                m_PageEnabledState = value
                SetEnabledState()
            End Set
        End Property

        ''' <summary>
        ''' Determines whether the property page is activated
        ''' </summary>
        ''' <remarks></remarks>
        Protected ReadOnly Property IsActivated() As Boolean
            Get
                Return m_activated
            End Get
        End Property

        ''' <summary>
        ''' Sets whether the page is actually enabled or disabled, based on the protected Enabled
        '''   property plus internal state, such as whether the project is in run mode.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub SetEnabledState()
            Dim ShouldBeEnabled As Boolean = m_PageEnabledState AndAlso m_PageEnabledPerDebugMode AndAlso m_PageEnabledPerBuildMode AndAlso m_Objects IsNot Nothing
            MyBase.Enabled = ShouldBeEnabled
        End Sub

        Private Sub AddToRunningTable()
            SyncLock RunningPropertyPages
                RunningPropertyPages.Add(Me)
            End SyncLock
        End Sub

        Private Sub RemoveFromRunningTable()
            SyncLock RunningPropertyPages
                RunningPropertyPages.Remove(Me)
            End SyncLock
        End Sub

        ''' <summary>
        ''' Enumerates the running property pages to find the requested property value.
        ''' </summary>
        ''' <param name="dispid">DISPID of property value being requested.</param>
        ''' <param name="obj">Value of property being requested.</param>
        ''' <returns>True if property value returned, False if property not found.</returns>
        ''' <remarks>If multiple pages host a property, this will return the first value found.</remarks>
        Protected Shared Function GetPropertyFromRunningPages(ByVal SourcePage As PropPageUserControlBase, ByVal dispid As Integer, ByRef obj As Object) As Boolean
            Debug.Assert(SourcePage.CommonPropertiesObject IsNot Nothing)
            SyncLock RunningPropertyPages
                For Each page As PropPageUserControlBase In RunningPropertyPages
                    'We must restrict the set of pages that we inspect to those running against the same project.
                    '  Therefore we check for a match against CommonPropertiesObject.  Note that checking against
                    '  DTEProject is not okay because not all project types support that.
                    If page.CommonPropertiesObject IsNot Nothing AndAlso page.CommonPropertiesObject Is SourcePage.CommonPropertiesObject Then
                        If page.GetProperty(dispid, obj) Then
                            Return True
                        End If
                    End If
                Next
            End SyncLock

            Return False
        End Function

        ''' <summary>
        ''' Returns the actual site (IPropertyPageSite, rather than IPropertyPageSiteInternal) for the property page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property PropertyPageSite() As OLE.Interop.IPropertyPageSite
            Get
                If m_Site IsNot Nothing Then
                    Dim OleSite As OLE.Interop.IPropertyPageSite = _
                        DirectCast(m_Site.GetService(GetType(OLE.Interop.IPropertyPageSite)), OLE.Interop.IPropertyPageSite)
                    Debug.Assert(OleSite IsNot Nothing, "IPropertyPageSiteInternal didn't provide an IPropertyPageSite through GetService")
                    Return OleSite
                End If

                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Does a GetService call via the property page site
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property GetServiceFromPropertyPageSite(ByVal ServiceType As Type) As Object
            Get
                If m_Site IsNot Nothing Then
                    Dim OleSite As OLE.Interop.IPropertyPageSite = PropertyPageSite
                    Dim sp As IServiceProvider = TryCast(OleSite, IServiceProvider)
                    Debug.Assert(sp IsNot Nothing, "Property page site didn't provide a managed service provider")
                    If sp IsNot Nothing Then
                        Return sp.GetService(ServiceType)
                    End If
                End If

                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the object to be used for querying for "common" property values.  The object
        '''   used may vary, depending on the project type and what it supports.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' </remarks>
        Public ReadOnly Property CommonPropertiesObject() As Object
            Get
                If m_ProjectPropertiesObject IsNot Nothing Then
                    'Used by VB, J#, C#-based projects
                    Return m_ProjectPropertiesObject
                Else
                    'C++ projects.
                    If m_DTEProject IsNot Nothing Then
                        Debug.Assert(m_DTEProject.Object IsNot Nothing)
                        Return m_DTEProject.Object
                    Else
                        Return Nothing 'This is possible if we've already been cleaned up
                    End If
                End If
            End Get
        End Property


        ''' <summary>
        ''' Returns the raw set of objects in use by this property page.  This will generally be the set of objects
        '''   passed in to the page through SetObjects.  However, it may be modified by subclasses to contain a superset
        '''   or subset for special purposes.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Function RawPropertiesObjects(ByVal Data As PropertyControlData) As Object()
            Return Data.RawPropertiesObjects
        End Function


        ''' <summary>
        ''' Returns the extended objects created from the raw set of objects in use by this property page.  This will generally be 
        '''   based on the set of objects passed in to the page through SetObjects.  However, it may be modified by subclasses to 
        '''   contain a superset or subset for special purposes.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Function ExtendedPropertiesObjects(ByVal Data As PropertyControlData) As Object()
            Return Data.ExtendedPropertiesObjects
        End Function


        ''' <summary>
        ''' True iff this property page is configuration-specific (like the compile page) instead of
        '''   configuration-independent (like the application pages)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsConfigurationSpecificPage() As Boolean
            Get
                Return m_fConfigurationSpecificPage
            End Get
        End Property


        ''' <summary>
        ''' Causes listening to property changes to be suspended until an equal number of
        '''   ResumePropertyChangeListening calls have been made
        ''' </summary>
        ''' <param name="DispId">The DISPID to ignore changes from.  If DISPID_UNKNOWN, then all property changes will be ignored.</param>
        ''' <remarks></remarks>
        Public Sub SuspendPropertyChangeListening(ByVal DispId As Integer)
            m_SuspendPropertyChangeListeningDispIds.Add(DispId)
        End Sub


        ''' <summary>
        ''' Causes listening to property changes to be resumed after an equal number of
        '''   SuspendPropertyChangeListening/ResumeropertyChangeListening pairs have been made
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub ResumePropertyChangeListening(ByVal DispId As Integer)
            m_SuspendPropertyChangeListeningDispIds.Remove(DispId)
            CheckPlayCachedPropertyChanges()
        End Sub


        ''' <summary>
        ''' Returns true if any property on the current page is currently being changed.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function PropertyOnPageBeingChanged() As Boolean
            Return m_SuspendPropertyChangeListeningDispIds.Count > 0
        End Function


        ''' <summary>
        ''' Gets the list of all raw properties objects from all properties hosted on this page
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' This may be a superset of the objects passed in to SetObjects because some properties can override their
        '''   behavior and display a different set of objects.  These are cached after the first call and not updated
        '''   again until another SetObjects call.
        ''' </remarks>
        Private ReadOnly Property RawPropertiesObjectsOfAllProperties() As Object()
            Get
                If m_CachedRawPropertiesSuperset Is Nothing Then
                    If m_Objects Is Nothing Then
                        Debug.Fail("m_Objects is nothing")
                        Return New Object() {}
                    End If
                    Dim Superset As New Hashtable(m_Objects.Length)
                    For Each Data As PropertyControlData In Me.ControlData
                        Dim RawObjects As Object() = Data.RawPropertiesObjects
                        Debug.Assert(RawObjects IsNot Nothing)
                        If RawObjects IsNot Nothing Then
                            For Each Obj As Object In RawObjects
                                If Not Superset.ContainsKey(Obj) Then
                                    Superset.Add(Obj, Obj)
                                End If
                            Next
                        End If
                    Next
                    ReDim m_CachedRawPropertiesSuperset(Superset.Count - 1)
                    Superset.Values.CopyTo(m_CachedRawPropertiesSuperset, 0)
                End If

                Return m_CachedRawPropertiesSuperset
            End Get
        End Property

        ''' <summary>
        ''' Attempts to get a property's current value, without doing any type conversion.  Returns
        '''   True on success and False if the property is not found.
        ''' </summary>
        ''' <param name="dispid">The property's DISPID to look up.</param>
        ''' <param name="obj">[out] Returns the property's value, if found.</param>
        ''' <returns>True on success and False if the property is not found.</returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetProperty(ByVal dispid As Integer, ByRef obj As Object) As Boolean
            obj = Nothing
            For Each _controlData As PropertyControlData In ControlData
                If _controlData.DispId = dispid Then
                    If _controlData.IsMissing Then
                        Return False
                    End If

                    'Debug.Assert(Not _controlData.IsConfigurationSpecificProperty) 'CONSIDER: probably convert this function into GetCommonProperty, we're okay if it's a common property (then there's only one extender to work with) - NOTE: we hit this on the C# build page when you check the XML Documentation File checkbox if the textbox is empty (querying for OutputPath)
                    obj = _controlData.GetPropertyValueNative(m_ExtendedObjects(0)) 'CONSIDER: This is what we've been doing (passing in first extender object), but it looks wrong
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Attempts to retrieve the current value of a property as currently stored in any page
        '''   in the project designer, even if that property has not yet been persisted (non-immediate
        '''   apply mode).  It does this by first trying to locate the property in the PropertyControlData
        '''   of all other pages, then by checking if it's a common property, then finally by checking the
        '''   page's own PropertyControlData info.
        ''' </summary>
        ''' <param name="dispid">The DISPID of the property to search for</param>
        ''' <param name="PropertyName">The property name of the property to search for.  Required for a common properties
        '''   look-up if this property is not defined as a PropertyControlData on the calling page.  Otherwise it's optional.</param>
        ''' <param name="obj"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' The property name and DISPIDs must both refer to the same property.
        ''' </remarks>
        Protected Function GetCurrentProperty(ByVal dispid As Integer, ByVal PropertyName As String, ByRef obj As Object) As Boolean
            PropertyName = common.Utils.NothingToEmptyString(PropertyName) 'Nothing not allowed in GetCommonPropertyDescriptor()

            'Check current property pages
            If GetPropertyFromRunningPages(Me, dispid, obj) Then
#If DEBUG Then
                'Since we don't know which source the property value will come from, let's ensure that enough
                '  information was given that it *could* come from any source.  Otherwise the function may
                '  sometimes succeed (if it's found in an open property page) and sometimes not (when that other
                '  page isn't open).

                Dim IsCommonProperty As Boolean = (GetCommonPropertyDescriptor(PropertyName) IsNot Nothing)
                Dim IsPropertyOnThisPage As Boolean = False

                For Each _controlData As PropertyControlData In ControlData
                    If _controlData.DispId = dispid Then
                        Debug.Assert(Not _controlData.IsMissing, "How could this property get successfully retrieved from one page but be IsMissing in another?")
                        Debug.Assert(_controlData.PropertyName.Equals(PropertyName, StringComparison.Ordinal), "GetCurrentProperty: PropertyName doesn't match DISPID")
                        IsPropertyOnThisPage = True
                        Exit For
                    End If
                Next

                Debug.Assert(IsCommonProperty OrElse IsPropertyOnThisPage, _
                    "GetCurrentProperty: Property was found in an open page, so this time the function will succeed.  However, the property was not " _
                    & "found as a common property or a property on this page, so the same query would fail if the other page were not open.  This probably " _
                    & "indicates an error in the caller.")
#End If

                'Property value successfully retrieved
                Return True
            End If

            'If it's not available on an open page, try the common properties
            Dim prop As PropertyDescriptor
            prop = GetCommonPropertyDescriptor(PropertyName)
            If prop IsNot Nothing Then
                obj = GetCommonPropertyValueNative(prop)
                Return True
            End If

            'Try getting the value from a PropertyControlData on the current page.
            If GetProperty(dispid, obj) Then
                Return True
            End If

            Return False
        End Function


        ''' <summary>
        ''' Restore the control's current value from the InitialValue (used when the user cancels the dialog)
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub RestoreInitialValues()
            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                For Each _controlData As PropertyControlData In ControlData
                    _controlData.RestoreInitialValue()
                Next _controlData
            Finally
                m_fInsideInit = InsideInitSave
                'Update current dirty state for the page
                IsDirty = IsAnyPropertyDirty()
            End Try
        End Sub


        ''' <summary>
        ''' Updates all properties so that they refresh their UI from the property
        '''   store's current values
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub RefreshPropertyValues()
            Common.Switches.TracePDProperties(TraceLevel.Warning, "*** [" & Me.GetType.Name & "] Refreshing all property values")

            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                For Each Data As PropertyControlData In Me.ControlData
                    Data.RefreshValue()
                Next
            Finally
                m_fInsideInit = InsideInitSave
                IsDirty = IsAnyPropertyDirty()
            End Try
        End Sub


        ''' <summary>
        ''' Indicates if the user has selected multiple projects in the Solution Explorer
        ''' </summary>
        ''' <value></value>
        ''' <remarks>When the user selects multiple projects, certain functionality is disabled</remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Public ReadOnly Property MultiProjectSelect() As Boolean
            Get
                Return m_MultiProjectSelect
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Protected ReadOnly Property ServiceProvider() As Microsoft.VisualStudio.Shell.ServiceProvider
            Get
                If m_ServiceProvider Is Nothing Then
                    Dim isp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = Nothing

                    If m_Site IsNot Nothing Then
                        isp = TryCast(m_Site.GetService(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    End If

                    If isp Is Nothing AndAlso DTE IsNot Nothing Then
                        isp = TryCast(DTE, Microsoft.VisualStudio.OLE.Interop.IServiceProvider)
                    End If
                    If isp IsNot Nothing Then
                        m_ServiceProvider = New Microsoft.VisualStudio.Shell.ServiceProvider(isp)
                    End If
                End If
                Return m_ServiceProvider
            End Get
        End Property

        ''' <summary>
        ''' Returns the DTE object associated with the objects passed to SetObjects.  If there is none, returns Nothing.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Protected ReadOnly Property DTE() As EnvDTE.DTE
            Get
                Return m_DTE
            End Get
        End Property

        ''' <summary>
        ''' Returns the EnvDTE.Project object associated with the objects passed to SetObjects.  If there is none, returns Nothing.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Public ReadOnly Property DTEProject() As EnvDTE.Project
            Get
                Return m_DTEProject
            End Get
        End Property

        ''' <summary>
        ''' Returns the ProjectProperties object from a VSLangProj-based project (VB, C#, J#).  Used for querying
        '''   common project properties in these types of projects.  Should only be used if you are certain of the
        '''   project type.  Otherwise, use CommonPropertiesObject.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Public ReadOnly Property ProjectProperties() As VSLangProj.ProjectProperties
            Get
                Return m_ProjectPropertiesObject
            End Get
        End Property


        'Enables or disables the given control on the page.  However, if the control is associated with
        '  a property on the page, and that property is hidden or read-only, the enabled state of the control
        '  will not be changed.
        Protected Sub EnableControl(ByVal control As Control, ByVal enabled As Boolean)
            If control Is Nothing Then
                Debug.Fail("control is nothing")
                Return
            End If

            For Each pcd As PropertyControlData In Me.ControlData
                If pcd.FormControl Is control OrElse _
                (pcd.AssociatedControls IsNot Nothing AndAlso Array.IndexOf(pcd.AssociatedControls, control) >= 0) Then
                    'The control is associated with this property control data
                    pcd.EnableAssociatedControl(control, enabled)
                    Return
                End If
            Next

            'If it wasn't associated with a PropertyControlData, we can go ahead and enable/disable it directly
            control.Enabled = enabled
        End Sub


#Region "Rude checkout support"

        ''' <summary>
        '''Before any code which may check out the project file, a property page must call this function.  This
        '''  alerts the page to the fact that we might get an unexpected Dispose() during this period, and if so,
        '''  to interpret it as meaning that the project file was checked out and updated, causing a project
        '''  reload.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub EnterProjectCheckoutSection()
            Debug.Assert(m_CheckoutSectionCount >= 0, "Bad m_CheckoutCriticalSectionCount count")
            m_CheckoutSectionCount = m_CheckoutSectionCount + 1
        End Sub


        ''' <summary>
        '''After any code which may check out the project file, a property page must call this function.  This
        '''  alerts the page to the fact that the code which might cause a project checkout is finished running.
        '''  If a Dispose occurred during the interval between the EnterProjectCheckoutSection and 
        '''  LeaveProjectCheckoutSection calls, the disposal of the controls on the property page will be delayed
        '''  (but CleanUpCOMReferences *will* get called immediately) by via a PostMessage() call to allow
        '''  the property page to more easily recover from this situation.  The flag ReloadedDuringCheckout
        '''  will be set to true.  After the project file checkout is successful, derived property pages should
        '''  check this flag and exit as soon as possible if it is true.  If it's true, the project file probably
        '''  has been zombied, and the latest changes to the page made by the user will be lost, so there will be 
        '''  no need to attempt to save properties.
        ''' 
        ''' Expected coding pattern:
        ''' 
        '''  EnterProjectCheckoutSection()
        '''  Try
        '''    ...
        '''    CallMethodWhichMayCauseProjectFileCheckout
        '''    If ReloadedDuringCheckout Then
        '''      Return
        '''    End If
        '''    ...
        '''  Finally
        '''    LeaveProjectCheckoutSection()
        '''  End Try
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub LeaveProjectCheckoutSection()
            m_CheckoutSectionCount = m_CheckoutSectionCount - 1
            Debug.Assert(m_CheckoutSectionCount >= 0, "Mismatched EnterProjectCheckoutSection/LeaveProjectCheckoutSection calls")
            If m_CheckoutSectionCount = 0 AndAlso m_ProjectReloadedDuringCheckout Then
                Try
                    Trace.WriteLine("**** Deactivate happened during a checkout.  Now that Apply is finished, queueing a delayed Dispose() call for the page.")
                    If Not IsHandleCreated Then
                        CreateHandle()
                    End If
                    Debug.Assert(IsHandleCreated AndAlso Not Handle.Equals(IntPtr.Zero), "We should have a handle still.  Without it, BeginInvoke will fail.")
                    BeginInvoke(New MethodInvoker(AddressOf DelayedDispose))
                Catch ex As Exception
                    ' At this point, all we can do is to avoid crashing the shell. 
                    Debug.Fail(String.Format("Failed to queue a delayed Dispose for the property page: {0}", ex))
                End Try
            End If
        End Sub


        ''' <summary>
        ''' If true, the project has been reloaded between a call to EnterProjectCheckoutSection and 
        '''   LeaveProjectCheckoutSection.  See EnterProjectCheckoutSection() for more information.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ProjectReloadedDuringCheckout() As Boolean
            Get
                Return m_ProjectReloadedDuringCheckout
            End Get
        End Property


        ''' <summary>
        ''' If true, a call to EnterProjectCheckoutSection has been made, and the matching LeaveProjectCheckoutSection
        '''   call has not yet been made.
        ''' </summary>
        ''' <remarks></remarks>
        Protected ReadOnly Property IsInProjectCheckoutSection() As Boolean
            Get
                Return m_CheckoutSectionCount > 0
            End Get
        End Property


        ''' <summary>
        ''' Called in a delayed fashion (via PostMessage) after a LeaveProjectCheckoutSection call if the
        '''   project was forcibly reloaded during the project checkout section.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DelayedDispose()
            'Set this flag back to false so that subclasses which override Dispose() know when it's
            '  safe to Dispose of their controls.
            m_ProjectReloadedDuringCheckout = False
            Dispose()
        End Sub


        ''' <summary>
        ''' Checks out the project file.  After calling this function, the caller should check
        '''   the ProjectReloaded flag and exit if it's true.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub CheckoutProjectFile(ByRef ProjectReloaded As Boolean)
            Dim SccManager As New AppDesDesignerFramework.SourceCodeControlManager(ServiceProvider, ProjectHierarchy)
            EnterProjectCheckoutSection()
            Try
                Common.Switches.TracePDProperties(TraceLevel.Warning, "Making sure the project file is checked out...")
                SccManager.ManageFile(DTEProject.FullName)
                SccManager.EnsureFilesEditable()
            Finally
                ProjectReloaded = ProjectReloadedDuringCheckout
                LeaveProjectCheckoutSection()
            End Try
        End Sub

#End Region


#Region "IPropertyPageInternal"
        ''' <summary>
        ''' Calls apply method on the page and child pages
        ''' Notifies class after completion by calling OnApplyComplete
        ''' </summary>
        ''' <remarks>Called by ComClass wrapper which maps IPropertyPage2::Apply to here</remarks>
        Private Sub IPropertyPageInternal_Apply() Implements IPropertyPageInternal.Apply
            Apply()
        End Sub

        ''' <summary>
        ''' Provides keyword for help system lookup.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetF1HelpKeyword() As String
            Return Nothing
        End Function

        ''' <summary>
        ''' Implements IPropertyPageInternal.Help
        ''' </summary>
        ''' <param name="HelpDir">Not used.</param>
        ''' <remarks></remarks>
        Private Sub IProperyPageInternal_Help(ByVal HelpDir As String) Implements IPropertyPageInternal.Help
            AppDesDesignerFramework.DesignUtil.DisplayTopicFromF1Keyword(ServiceProvider, GetF1HelpKeyword)
        End Sub

        ''' <summary>
        ''' Brings up help for the given help topic
        ''' </summary>
        ''' <param name="HelpTopic">The help string that identifiers the help topic.</param>
        ''' <remarks></remarks>
        Public Overridable Sub Help(ByVal HelpTopic As String)
            AppDesDesignerFramework.DesignUtil.DisplayTopicFromF1Keyword(ServiceProvider, HelpTopic)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function IsPageDirty() As Boolean Implements IPropertyPageInternal.IsPageDirty

            If IsDirty Then
                Return True
            End If

            'Check child pages for any dirty state
            For Each page As PropPageUserControlBase In m_ChildPages.Values
                If page.IsPageDirty() Then
                    Return True
                End If
            Next page
            Return False

        End Function

        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub CleanupCOMReferences()
            Dim i As Integer

            If m_Objects IsNot Nothing Then
                For i = 0 To m_Objects.Length - 1
                    m_Objects(i) = Nothing
                Next i
            End If
            m_Objects = Nothing

            If m_ExtendedObjects IsNot Nothing Then
                For i = 0 To m_ExtendedObjects.Length - 1
                    m_ExtendedObjects(i) = Nothing
                Next
            End If
            m_ExtendedObjects = Nothing

            DisconnectPropertyNotify()
            DisconnectDebuggerEvents()
            DisconnectBroadcastMessages()
            DisconnectBuildEvents()

            m_DTEProject = Nothing
            m_DTE = Nothing
            m_ProjectPropertiesObject = Nothing
            m_ServiceProvider = Nothing
            m_Site = Nothing
            m_CachedRawPropertiesSuperset = Nothing

            'Ask all child pages to clean themselves up
            For Each page As PropPageUserControlBase In m_ChildPages.Values
                page.SetObjects(Nothing)
            Next page
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="objects"></param>
        ''' <remarks></remarks>
        Private Sub CheckMultipleProjectsSelected(ByVal objects() As Object)
            If objects Is Nothing OrElse objects.Length <= 1 Then
                'Cannot be multiple projects
            Else
                Dim NextProj, FirstProj As IVsHierarchy
                FirstProj = GetProjectHierarchyFromObject(objects(0))
                For i As Integer = 1 To objects.Length - 1
                    NextProj = GetProjectHierarchyFromObject(objects(i))
                    If (NextProj Is Nothing OrElse NextProj IsNot FirstProj) Then
                        Me.m_MultiProjectSelect = True
                        Return
                    End If
                Next
            End If
            Me.m_MultiProjectSelect = False
        End Sub

        ''' <summary>
        ''' Given a project Object, retrieves the hierarchy
        ''' </summary>
        ''' <param name="ThisObj"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetProjectHierarchyFromObject(ByVal ThisObj As Object) As IVsHierarchy

            Dim Hier As IVsHierarchy = Nothing
            Dim ItemId As UInteger

            If TypeOf ThisObj Is IVsCfgBrowseObject Then
                VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsCfgBrowseObject).GetProjectItem(Hier, ItemId))
            ElseIf TypeOf ThisObj Is IVsBrowseObject Then
                VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsBrowseObject).GetProjectItem(Hier, ItemId))
            Else
                Debug.Fail("Not an IVsBrowseObject, not an IVsCfgBrowseObject")
            End If

            Return Hier
        End Function

        ''' <summary>
        ''' Provides an array of IUnknown pointers for the objects associated with the property page.
        ''' </summary>
        ''' <param name="objects"></param>
        ''' <remarks>
        '''   The property page host uses this method to communicate to the property page (which gets delegated to
        '''   this class) the objects whose properties are to be displayed.  The property values are read and set
        '''   via these objects.
        ''' 
        '''   IMPORTANT NOTE: Depending on the property page host and project, the objects may be IVsCfg objects 
        '''   or they may be something else.
        '''</remarks>
        Private Sub IPropertyPageInternal_SetObjects(ByVal objects() As Object) Implements IPropertyPageInternal.SetObjects
            SetObjects(objects)
        End Sub


        ''' <summary>
        ''' Debug-only method to print the properties in a property descriptor collection to trace output.
        ''' </summary>
        ''' <param name="DebugMessage">The message to be printed first to trace output.</param>
        ''' <param name="Properties">The properties to print to trace output.</param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Private Sub TraceTypeDescriptorCollection(ByVal DebugMessage As String, ByVal Properties As PropertyDescriptorCollection)
#If DEBUG Then
            If Common.Switches.PDExtenders.TraceVerbose Then
                Trace.WriteLine("PDExtenders: " & DebugMessage)
                Trace.Indent()
                For Each Prop As PropertyDescriptor In Properties
                    Trace.WriteLine(Prop.Name & " [" & VB.TypeName(Prop) & "]" & ": " & Prop.PropertyType.Name)
                Next
                Trace.Unindent()
            End If
#End If
        End Sub


        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="site"></param>
        ''' <remarks></remarks>
        Private Sub SetPageSite(ByVal site As IPropertyPageSiteInternal) Implements IPropertyPageInternal.SetPageSite
            DisconnectDebuggerEvents()
            m_Site = site

            If site IsNot Nothing Then
                'PERF: set the Font as early as possible to avoid flicker
                ScaleWindowToCurrentFont()
                ConnectBroadcastMessages()
            End If

            'Also change site of any existing child pages
            For Each page As PropPageUserControlBase In m_ChildPages.Values
                Dim childSite As ChildPageSite = Nothing
                If site IsNot Nothing Then
                    childSite = New ChildPageSite(page, site, m_PropPageUndoSite)
                End If
                page.SetPageSite(childSite)
            Next page

            If site IsNot Nothing Then
                ConnectDebuggerEvents()
            End If

            Debug.Assert(PropertyPageSite IsNot Nothing)
            OnSetSite(PropertyPageSite)
        End Sub

        ''' <summary>
        ''' Called when the property page's IPropertyPageSite is set.
        ''' </summary>
        ''' <param name="site"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub OnSetSite(ByVal site As OLE.Interop.IPropertyPageSite)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="dispid"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub EditProperty(ByVal dispid As Integer) Implements IPropertyPageInternal.EditProperty
            Dim cntrl As Control = GetPropertyControl(dispid)
            If cntrl IsNot Nothing Then
                'SetFocus here
                cntrl.Select()
            End If
        End Sub


#End Region

        ''' <summary>
        ''' Calls apply method on the page and child pages
        ''' Notifies class after completion by calling OnApplyComplete
        ''' </summary>
        ''' <remarks>Called by ComClass wrapper which maps IPropertyPage2::Apply to here</remarks>
        Public Overridable Sub Apply()
            Dim Successful As Boolean
            Try
                'NOTE: The code today doesn't really work to allow the parent page do batch saving for child pages
                ' We do need create a transacation here, if there is no existing one (created by parent page). 
                ' When something fails, who created the transaction should rollback it, but no other page should do that. 
                ' However, the child page should never pop error message, but wrap all failure message to an exception.
                ' The page starting the transaction should merge error messages, and show to the user one time.
                If IsDirty Then
                    Me.ApplyPageChanges()
                End If

                For Each page As PropPageUserControlBase In m_ChildPages.Values
                    If page.IsDirty() Then
                        page.Apply()
                    End If
                Next page
                Successful = True
            Finally
                If Not m_ProjectReloadedDuringCheckout Then
                    OnApplyComplete(Successful)
                End If
            End Try
        End Sub

        ''' <summary>
        ''' Provides an array of IUnknown pointers for the objects associated with the property page.
        ''' </summary>
        ''' <param name="objects"></param>
        ''' <remarks>
        '''   The property page host uses this method to communicate to the property page (which gets delegated to
        '''   this class) the objects whose properties are to be displayed.  The property values are read and set
        '''   via these objects.
        ''' 
        '''   IMPORTANT NOTE: Depending on the property page host and project, the objects may be IVsCfg objects 
        '''   or they may be something else.
        '''</remarks>
        Public Overridable Sub SetObjects(ByVal objects() As Object)
            Debug.Assert(m_Site IsNot Nothing OrElse (objects Is Nothing OrElse objects.Length = 0), "SetObjects() called (with non-null objects), but we are not sited!")
            m_Objects = Nothing

            m_fConfigurationSpecificPage = False
            m_CachedRawPropertiesSuperset = Nothing

            'Clean up any previous event handlers
            DisconnectPropertyNotify()

            CheckMultipleProjectsSelected(objects)

            If (objects Is Nothing) OrElse (objects.Length = 0) OrElse MultiProjectSelect() Then
                EnableAllControls(False)
                SetEnabledState()
                CleanupCOMReferences()
                Return
            End If

            If Not TypeOf objects Is Object() Then
                Debug.Fail("Objects must be an array of Object, not an array of anything else!")
                Throw New ArgumentException
            End If

            m_fInsideInit = True
            Try
                ' We need make a copy here. Different page shouldn't share a same copy of array!
                m_Objects = CType(objects.Clone(), Object())
                SetEnabledState()
                '
                'Get the basic interfaces necessary for interacting with the project system and shell
                ' DTE
                ' ExtensibilityObjects
                '
                m_DTE = Nothing
                m_DTEProject = Nothing

                Dim Hier As IVsHierarchy = Nothing
                Dim ItemId As UInteger
                Dim ThisObj As Object = m_Objects(0)

                'Get the IVSHierarchy for the project (we ignore the returned ItemId), and while we're at it,
                '  figure out whether this page is specific to individual configurations or common to all
                If TypeOf ThisObj Is IVsCfgBrowseObject Then
                    'The object(s) passed in are configuration-specific
                    m_fConfigurationSpecificPage = True
                    VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsCfgBrowseObject).GetProjectItem(Hier, ItemId))
                ElseIf TypeOf ThisObj Is IVsBrowseObject Then
                    'The object(s) passed in are common to all configurations
                    m_fConfigurationSpecificPage = False
                    VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsBrowseObject).GetProjectItem(Hier, ItemId))
                Else
                    Debug.Fail("Object passed in to SetObjects() must be an IVsBrowseObject or IVsCfgBrowseObject")
                    Throw New NotSupportedException
                End If
                Debug.Assert(Hier IsNot Nothing, "Should have thrown")
                m_ProjectHierarchy = Hier

                Dim hr As Integer
                Dim obj As Object = Nothing

                hr = Hier.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, obj)

                If TypeOf obj Is EnvDTE.Project Then
                    m_DTEProject = CType(obj, EnvDTE.Project)
                    m_DTE = m_DTEProject.DTE
                End If

                hr = Hier.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_BrowseObject, obj)
                If TypeOf obj Is VSLangProj.ProjectProperties Then
                    m_ProjectPropertiesObject = CType(obj, VSLangProj.ProjectProperties)
                ElseIf m_DTEProject IsNot Nothing Then
                    If TypeOf m_DTEProject.Object Is VSLangProj.ProjectProperties Then
                        m_ProjectPropertiesObject = CType(m_DTEProject.Object, VSLangProj.ProjectProperties)
                    Else
                        'Must be a C++ project - CommonPropertiesObject will return m_DTEProject.Object
                        m_ProjectPropertiesObject = Nothing
                    End If
                    obj = Nothing
                End If
                obj = Nothing

                'Get the Extender Objects for the properties
                'This must done after getting the DTE so that ServiceProvider can be obtained

                Dim aem As Microsoft.VisualStudio.Editors.PropertyPages.AutomationExtenderManager = _
                    Microsoft.VisualStudio.Editors.PropertyPages.AutomationExtenderManager.GetAutomationExtenderManager(ServiceProvider)

                '... First for the actual objects passed in to SetObjects
                Try
                    m_ExtendedObjects = aem.GetExtendedObjects(objects)
                    Debug.Assert(m_ExtendedObjects IsNot Nothing, "Extended objects unavailable")

                    m_ObjectsPropertyDescriptorsArray = New PropertyDescriptorCollection(objects.Length - 1) {}
                    For i As Integer = 0 To objects.Length - 1
                        Debug.Assert(objects(i) IsNot Nothing)
#If DEBUG Then
                        If Common.Switches.PDExtenders.TraceVerbose Then
                            TraceTypeDescriptorCollection("*** Non-extended properties collection for objects #" & i, TypeDescriptor.GetProperties(objects(i)))
                        End If
#End If
                        If TypeOf m_ExtendedObjects(i) Is ICustomTypeDescriptor Then 'Extenders were found and added, so we need to get the property descriptors for the set of properties including extenders
                            m_ObjectsPropertyDescriptorsArray(i) = CType(m_ExtendedObjects(i), ICustomTypeDescriptor).GetProperties(New Attribute() {})
                            Common.Switches.TracePDExtenders(TraceLevel.Info, "*** Properties collection #" & i & " contains extended properties.")
                            TraceTypeDescriptorCollection("*** Extended properties collection for objects #" & i, m_ObjectsPropertyDescriptorsArray(i))
                        Else
                            'No extenders
                            m_ObjectsPropertyDescriptorsArray(i) = System.ComponentModel.TypeDescriptor.GetProperties(objects(i))
                            Common.Switches.TracePDExtenders(TraceLevel.Info, "*** Properties collection #" & i & " does not contain extended properties.")
                        End If
                    Next i
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Debug.Fail("An exception was thrown trying to get extended objects for the properties" & vbCrLf & ex.ToString)
                    Throw
                End Try

                '... Then for common properties of the project which are not configuration-specific
                If objects.Length = 1 AndAlso CommonPropertiesObject Is objects(0) Then
                    'The object passed in to us *is* the common project properties object.  We've already calculated the
                    '  extended properties from that, so no need to repeat it (can be performance intensive).
                    Debug.Assert(Not m_fConfigurationSpecificPage)
                    m_CommonPropertyDescriptors = m_ObjectsPropertyDescriptorsArray(0)
                Else
                    Try
                        Dim ExtendedCommonProperties() As Object = aem.GetExtendedObjects(New Object() {CommonPropertiesObject})
                        Debug.Assert(ExtendedCommonProperties IsNot Nothing, "Extended objects unavailable for common properties")
                        Debug.Assert(ExtendedCommonProperties.Length = 1)
#If DEBUG Then
                        If Common.Switches.PDExtenders.TraceVerbose Then
                            TraceTypeDescriptorCollection("*** Non-extended common properties collection", TypeDescriptor.GetProperties(CommonPropertiesObject))
                        End If
#End If
                        If TypeOf ExtendedCommonProperties(0) Is ICustomTypeDescriptor Then 'Extenders were found and added, so we need to get the property descriptors for the set of properties including extenders
                            m_CommonPropertyDescriptors = DirectCast(ExtendedCommonProperties(0), ICustomTypeDescriptor).GetProperties(New Attribute() {})
                            TraceTypeDescriptorCollection("*** Extended common properties collection", m_CommonPropertyDescriptors)
                        Else
                            'No extenders
                            m_CommonPropertyDescriptors = System.ComponentModel.TypeDescriptor.GetProperties(CommonPropertiesObject)
                            Common.Switches.TracePDExtenders(TraceLevel.Info, "*** Common properties collection does not contain extended properties.")
                        End If
                    Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                        Debug.Fail("An exception was thrown trying to get extended objects for the common properties" & vbCrLf & ex.ToString)
                        Throw
                    End Try
                End If

                InitializeAllProperties()
                InitPage()

                ScaleWindowToCurrentFont()
                ConnectBroadcastMessages()
                ConnectDebuggerEvents()
                ConnectPropertyNotify()
                ConnectBuildEvents()
            Finally
                m_fInsideInit = False
            End Try

            'Also call SetObjects for any existing child pages
            For Each page As PropPageUserControlBase In m_ChildPages.Values
                page.SetObjects(objects)
            Next page
        End Sub


#Region "Control event handlers for derived form"

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub AddChangeHandlers()

            For Each _controlData As PropertyControlData In ControlData
                _controlData.AddChangeHandlers()
            Next _controlData

        End Sub

#End Region


#Region "Search for PropertyControlData and controls on the page"

        ''' <summary>
        ''' Looks up a control on this page by numeric id and returns it, if found, else returns Nothing.
        ''' </summary>
        ''' <param name="PropertyId"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function GetPropertyControl(ByVal PropertyId As Integer) As Control
            For Each ControlData As PropertyControlData In Me.ControlData
                If ControlData.DispId = PropertyId Then
                    Return ControlData.FormControl
                End If
            Next ControlData

            Return Nothing
        End Function


        ''' <summary>
        ''' Looks up a PropertyControlData on this page by numeric id and returns it, if found, else returns Nothing.
        ''' </summary>
        ''' <param name="PropertyId"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' Whether the PropertyControlData is found or not does not depend on whether the associated property
        '''   is found in the project system or not, but just on whether the PropertyControlData object was
        '''   offered up by the page in the page's implementation of the ControlData property.
        ''' To determine if a property is supported by the project system, use GetControlDataFromXXX().IsMissing
        ''' </remarks>
        Protected Overridable Function GetPropertyControlData(ByVal PropertyId As Integer) As PropertyControlData
            For Each ControlData As PropertyControlData In Me.ControlData
                If ControlData.DispId = PropertyId Then
                    Return ControlData
                End If
            Next ControlData

            Return Nothing
        End Function


        ''' <summary>
        ''' Looks up a PropertyControlData on this page by name and returns it, if found, else returns Nothing.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <remarks>
        ''' Whether the PropertyControlData is found or not does not depend on whether the associated property
        '''   is found in the project system or not, but just on whether the PropertyControlData object was
        '''   offered up by the page in the page's implementation of the ControlData property.
        ''' To determine if a property is supported by the project system, use GetControlDataFromXXX().IsMissing
        ''' </remarks>
        Protected Overridable Function GetPropertyControlData(ByVal PropertyName As String) As PropertyControlData
            For Each pcd As PropertyControlData In Me.ControlData
                If pcd.PropertyName.Equals(PropertyName, StringComparison.Ordinal) Then
                    Return pcd
                End If
            Next

            Return Nothing
        End Function

#End Region


#Region "ControlData"

        ''' <summary>
        ''' List of PropertyControlData structures which the base class uses
        ''' to read and write property values and initialize the UI.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>This should normally be overridden in the derived class to provide the page's specific list of
        '''   PropertyControlData.  No need to call the base's default version.
        ''' </remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Protected Overridable ReadOnly Property ControlData() As PropertyControlData()
            Get
                Return New PropertyControlData() {}
            End Get
        End Property

#End Region

#Region "Delay validation helpers"
        Private m_delayValidationGroup As Integer = -1              ' the control group ID: we need do validation when focus moves out of this group
        Private m_delayValidationQueue As ListDictionary            ' it hosts a list of controlData objects, which need to be validated


        ''' <summary>
        ''' Return a control group which contains the control, return -1 when we can not find one
        ''' </summary>
        ''' <param name="dataControl"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function FindControlGroup(ByVal dataControl As Control) As Integer
            Dim group As Control()() = ValidationControlGroups
            If group IsNot Nothing Then
                For i As Integer = 0 To group.Length - 1
                    Dim list As Control() = group(i)
                    For j As Integer = 0 To list.Length - 1
                        If list(j) Is dataControl Then
                            Return i
                        End If
                    Next
                Next
            End If
            Return -1
        End Function

        ''' <summary>
        ''' Check whether the focus is still in the control group
        ''' </summary>
        ''' <param name="groupID"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsFocusInControlGroup(ByVal groupID As Integer) As Boolean
            Dim controlList As Control() = ValidationControlGroups(groupID)
            For Each c As Control In controlList
                If c.ContainsFocus Then
                    Return True
                End If
            Next
            Return False
        End Function

        ''' <summary>
        ''' ignore validating one control, it will be removed from delay-validation list
        ''' </summary>
        ''' <param name="dataControl"></param>
        ''' <remarks></remarks>
        Protected Sub SkipValidating(ByVal dataControl As Control)
            If m_delayValidationQueue IsNot Nothing Then
                For Each item As PropertyControlData In ControlData
                    If item.FormControl Is dataControl Then
                        m_delayValidationQueue.Remove(item)
                        Return
                    End If
                Next item
            End If
        End Sub

        ''' <summary>
        ''' add a control to the delay-validation list
        ''' </summary>
        ''' <param name="dataControl"></param>
        ''' <remarks></remarks>
        Protected Sub DelayValidate(ByVal dataControl As Control)
            Dim controlGroup As Integer = FindControlGroup(dataControl)
            Debug.Assert(controlGroup >= 0, "The control doesn't belong to a group?")
            If controlGroup >= 0 Then
                Dim items As New ArrayList
                For Each _controlData As PropertyControlData In ControlData
                    If _controlData.FormControl Is dataControl Then
                        items.Add(_controlData)
                        DelayValidate(controlGroup, items)
                        Return
                    End If
                Next _controlData
            End If
        End Sub

        ''' <summary>
        ''' add a list of controlData to the delay-validation list
        ''' </summary>
        ''' <param name="controlGroup"></param>
        ''' <param name="items"></param>
        ''' <remarks></remarks>
        Private Sub DelayValidate(ByVal controlGroup As Integer, ByVal items As ArrayList)
            Dim oldItems As ListDictionary = Nothing
            If m_delayValidationGroup < 0 Then

                m_delayValidationGroup = controlGroup
                If m_delayValidationQueue Is Nothing Then
                    m_delayValidationQueue = New ListDictionary()
                End If
                HookDelayValidationEvents()

            ElseIf m_delayValidationGroup <> controlGroup Then
                UnhookDelayValidationEvents()

                oldItems = m_delayValidationQueue
                m_delayValidationGroup = controlGroup
                m_delayValidationQueue = New ListDictionary()

                HookDelayValidationEvents()
            Else
                Debug.Assert(m_delayValidationQueue IsNot Nothing, "Why we didn't create the queue")
            End If

            For Each item As Object In items
                m_delayValidationQueue.Item(item) = Nothing
            Next

            ' We need process the existing delay validation queue if it belongs to another control group
            If oldItems IsNot Nothing Then
                ProcessDelayValidationQueue(oldItems, False)
            End If
        End Sub

        ''' <summary>
        ''' Start validating the objects in the delay-validation list
        ''' </summary>
        ''' <param name="canThrow"></param>
        ''' <returns>return false if the validation failed</returns>
        ''' <remarks></remarks>
        Protected Function ProcessDelayValidationQueue(ByVal canThrow As Boolean) As Boolean
            Dim oldItems As ListDictionary = m_delayValidationQueue

            If m_delayValidationGroup >= 0 Then
                UnhookDelayValidationEvents()
            End If

            m_delayValidationGroup = -1
            m_delayValidationQueue = Nothing

            If oldItems IsNot Nothing Then
                Return ProcessDelayValidationQueue(oldItems, canThrow)
            End If
            Return True
        End Function

        ''' <summary>
        ''' Start validating the objects in a delay-validation list
        ''' </summary>
        ''' <param name="items"></param>
        ''' <returns>return false if the validation failed</returns>
        ''' <remarks>a helper function</remarks>
        Private Function ProcessDelayValidationQueue(ByVal items As ListDictionary, ByVal canThrow As Boolean) As Boolean
            Dim firstControl As Control = Nothing
            Dim finalMessage As String = String.Empty
            Dim finalResult As ValidationResult = ValidationResult.Succeeded
            Dim currentState As Boolean = m_inDelayValidation

            m_inDelayValidation = True
            Try
                ' do validation for all items in the queue, but we collect all messages, and report them one time...
                For Each _controlData As PropertyControlData In items.Keys
                    Dim message As String = Nothing
                    Dim returnControl As Control = Nothing
                    Dim result As ValidationResult = ValidateProperty(_controlData, message, returnControl)

                    If result <> ValidationResult.Succeeded Then
                        If finalResult = ValidationResult.Succeeded Then
                            finalResult = ValidationResult.Warning
                            finalMessage = _controlData.DisplayPropertyName & ":" & vbCrLf & message
                        Else
                            finalMessage = finalMessage & vbCrLf & vbCrLf & _controlData.DisplayPropertyName & ":" & vbCrLf & message
                        End If

                        If firstControl Is Nothing Then
                            If returnControl Is Nothing Then
                                firstControl = _controlData.FormControl
                            Else
                                firstControl = returnControl
                            End If
                        End If
                    End If
                Next _controlData

                If finalResult <> ValidationResult.Succeeded Then
                    If canThrow Then
                        Throw New ValidationException(finalResult, finalMessage, firstControl)
                    Else
                        Dim caption As String = SR.GetString(SR.APPDES_Title)
                        Dim dialogResult As DialogResult = AppDesDesignerFramework.DesignerMessageBox.Show(ServiceProvider, finalMessage, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        If firstControl IsNot Nothing Then
                            firstControl.Focus()
                            If TypeOf firstControl Is TextBox Then
                                CType(firstControl, TextBox).SelectAll()
                            End If
                        End If
                    End If
                    Return False
                End If
            Finally
                m_inDelayValidation = currentState
            End Try
            Return True
        End Function

        ''' <summary>
        ''' Add event handler to monitor focus
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub HookDelayValidationEvents()
            Dim controlList As Control() = ValidationControlGroups(m_delayValidationGroup)
            For Each c As Control In controlList
                AddHandler c.Leave, AddressOf OnLeavingControlGroup
            Next
        End Sub

        ''' <summary>
        ''' remove event handler to monitor focus
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub UnhookDelayValidationEvents()
            Dim controlList As Control() = ValidationControlGroups(m_delayValidationGroup)
            For Each c As Control In controlList
                RemoveHandler c.Leave, AddressOf OnLeavingControlGroup
            Next
        End Sub

        ''' <summary>
        ''' the event handler to handle focus leaving
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub OnLeavingControlGroup(ByVal sender As Object, ByVal e As System.EventArgs)
            PostValidation()
        End Sub

        ''' <summary>
        ''' Post a message to start validation
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PostValidation()
            ' NOTE: We always post the message, but start validation only the focus is really out.
            ' We delay check this, so we don't get into the problem when winForm hasn't sync status correctly, which IS a problem in some cases (tab).
            Microsoft.VisualStudio.Editors.AppDesInterop.NativeMethods.PostMessage(Handle, Microsoft.VisualStudio.Editors.AppDesCommon.WmUserConstants.WM_PAGE_POSTVALIDATION, 0, 0)
        End Sub

#End Region

#Region "Apply page change helpers"
        ''' <summary>
        ''' Code executed before property values are updated.
        ''' </summary>
        ''' <remarks>This is called after the ILangPropertyProvideBatchUpdate.BeginBatch has been called.</remarks>
        Protected Overridable Sub PreApplyPageChanges()
        End Sub

        ''' <summary>
        ''' Code to validate the page
        ''' </summary>
        ''' <remarks>This is called when changes are saved or applied to the object</remarks>
        Protected Sub ValidatePageChanges(ByVal allowDelayValidation As Boolean)
            Dim firstControl As Control = Nothing
            Dim finalMessage As String = String.Empty
            Dim finalResult As ValidationResult = ValidationResult.Succeeded

            Dim delayValidationOK As Boolean = allowDelayValidation
            Dim delayValidationGroup As Integer = -1
            Dim delayValidationQueue As ArrayList = Nothing

            For Each _controlData As PropertyControlData In ControlData
                If _controlData.IsDirty Then
                    Dim message As String = Nothing
                    Dim returnControl As Control = Nothing
                    Dim result As ValidationResult = ValidateProperty(_controlData, message, returnControl)

                    If result <> ValidationResult.Succeeded Then
                        If returnControl Is Nothing Then
                            returnControl = _controlData.FormControl
                        End If

                        If finalResult = ValidationResult.Succeeded Then
                            finalResult = result
                            finalMessage = _controlData.DisplayPropertyName & ":" & vbCrLf & message
                            firstControl = returnControl
                        Else
                            finalMessage = finalMessage & vbCrLf & vbCrLf & _controlData.DisplayPropertyName & ":" & vbCrLf & message
                            If finalResult <> ValidationResult.Failed Then
                                finalResult = result
                            End If
                        End If

                        ' Delay validation process, we only allow it when all updated controls are in the same group...
                        If delayValidationOK Then
                            If result = ValidationResult.Warning AndAlso returnControl IsNot Nothing Then
                                Dim group As Integer = FindControlGroup(returnControl)
                                If delayValidationGroup < 0 Then
                                    delayValidationGroup = group
                                    delayValidationQueue = New ArrayList()
                                    delayValidationQueue.Add(_controlData)
                                ElseIf delayValidationGroup <> group Then
                                    delayValidationOK = False
                                Else
                                    Debug.Assert(delayValidationQueue IsNot Nothing)
                                    delayValidationQueue.Add(_controlData)
                                End If
                            Else
                                delayValidationOK = False
                            End If
                        End If

                    End If
                End If
NextControl:
            Next _controlData

            If finalResult <> ValidationResult.Succeeded Then
                If finalResult = ValidationResult.Warning AndAlso delayValidationOK Then
                    DelayValidate(delayValidationGroup, delayValidationQueue)
                    PostValidation()    ' we need start validating if the focus has already moved out.
                Else
                    ' We should report the error if it is not a warning...
                    Throw New ValidationException(finalResult, finalMessage, firstControl)
                End If
            End If
        End Sub

        ''' <summary>
        ''' validate a property
        ''' </summary>
        ''' <param name="controlData"></param>
        ''' <param name="message"></param>
        ''' <param name="returnControl"></param>
        ''' <returns></returns>
        ''' <remarks>Different pages should override it to do the validation</remarks>
        Protected Overridable Function ValidateProperty(ByVal controlData As PropertyControlData, ByRef message As String, ByRef returnControl As Control) As ValidationResult
            Return ValidationResult.Succeeded
        End Function


        ''' <summary>
        ''' Attempts to check out the files which are necessary in order to apply the currently-dirty properties
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CheckOutFilesForApply()
            Dim Control As Control = Nothing 'The control which caused the exception, if known\
            Dim FirstDirtyControl As Control = Nothing 'The first dirty control
            Try
                'Attempt to batch up the files which need to be checked out for all of the dirty properties.
                '  It is possible for a checkout to cause the project file to get updated to a newer version,
                '  which will cause the project to be reloaded.  That in turn causes the project designer to get
                '  shut down in the middle of the apply.  If that happens, we need to try to exit as early as possible.
                'Thus, checking out the files early has two advantages: 1) keeps it to a single checkout prompt, 2)
                '  helps us shut down more gracefully if the checkout causes a project reload.
                If m_ServiceProvider IsNot Nothing AndAlso m_ProjectHierarchy IsNot Nothing Then
                    Dim SccManager As New AppDesDesignerFramework.SourceCodeControlManager(m_ServiceProvider, m_ProjectHierarchy)
                    For Each Data As PropertyControlData In ControlData
                        If Data.IsDirty Then
                            Dim ValueHasChanged As Boolean = False
                            Try
                                'Is the property value different from the stored one?  If not, there's no need to
                                '  check out the file (this is important for correct Undo functionality).
                                Dim OldValues() As Object = Data.AllInitialValuesExpanded
                                Dim NewValues() As Object = Data.GetControlValueMultipleValues()

                                If OldValues Is Nothing OrElse NewValues Is Nothing Then
                                    Debug.Fail("OldValues or NewValues is Nothing")
                                Else
                                    If OldValues.Length <> NewValues.Length Then
                                        Debug.Fail("OldValues.Length <> NewValues.Length")
                                    Else
                                        For i As Integer = 0 To OldValues.Length - 1
                                            If Not PropertyControlData.ObjectsAreEqual(OldValues(i), NewValues(i)) Then
                                                'One of the values has changed.
                                                ValueHasChanged = True
                                            End If
                                        Next
                                    End If
                                End If

                            Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                                Debug.Fail("Failure trying to compare old/new values in PropertyControlDataSetValueHelper.SetValue")
                                ValueHasChanged = True
                            End Try

                            'If the value is the same as the current value then no need to try
                            '  setting anything.  This allows the user to set the value back to the
                            '  original value and have it be accepted even if it fails some of the
                            '  extra validation that we do (e.g., an empty GUID).
                            If ValueHasChanged Then
                                If FirstDirtyControl Is Nothing Then
                                    FirstDirtyControl = Data.FormControl
                                End If
                                Control = Data.FormControl

                                Dim AffectedFiles() As String = Data.FilesToCheckOut() 'This can cause checkout exceptions if files have to be added to the project
                                For Each File As String In AffectedFiles
                                    SccManager.ManageFile(File)
                                Next
                            End If
                        End If
                    Next

                    If SccManager.ManagedFiles.Count > 0 Then
                        Common.Switches.TracePDProperties(TraceLevel.Warning, "Calling QueryEdit on these files: " & String.Join(", ", SccManager.ManagedFiles.ToArray()))
                        SccManager.EnsureFilesEditable()

                        If m_ProjectReloadedDuringCheckout Then
                            'Project is reloading.  Can't try to change the property values, we just need to exit as soon as possible.
                            Trace.WriteLine("**** Dispose was forced while we were trying to check out files.  No property changes were made, exiting apply.")
                            Return
                        End If
                    End If
                Else
                    Debug.Fail("Service provider or hierarchy missing - can't QueryEdit files before property set")
                End If

            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                'ApplyPageChanges() handles ValidationException specially, we need to 
                'Be sure to set the inner exception, so that if it was a checkout cancel, the
                '  message box later knows to ignore it.

                If Control Is Nothing Then
                    'Error wasn't with a specific property's control, so it might have been from the
                    '  batch checkout.  Use the first dirty control.
                    Control = FirstDirtyControl
                End If

                Throw New ValidationException(ValidationResult.Failed, ex.Message, Control, innerexception:=ex)
            End Try

        End Sub

        Private Sub VerifyPropertiesWhichMayReloadProjectAreLast()
#If DEBUG Then
            Dim APreviousPropertyHadProjectMayBeReloadedDuringPropertySetFlag As Boolean = False
            For Each _controlData As PropertyControlData In ControlData
                Dim ProjectMayBeReloadedDuringPropertySet As Boolean = (0 <> (_controlData.GetFlags() And ControlDataFlags.ProjectMayBeReloadedDuringPropertySet))
                If ProjectMayBeReloadedDuringPropertySet Then
                    APreviousPropertyHadProjectMayBeReloadedDuringPropertySetFlag = True
                ElseIf APreviousPropertyHadProjectMayBeReloadedDuringPropertySetFlag Then
                    Debug.Fail("Properties with the ProjectMayBeReloadedDuringPropertySet flag should always come last " _
                        & "in the property list so that they will be set last.  Otherwise there may be " _
                        & "other property changes requests to be applied which will get ignored if the project " _
                        & "is reloaded.")
                    Return
                End If
            Next
#End If
        End Sub

        ''' <summary>
        ''' Flushes user changes to the underlying project system properties.
        ''' </summary>
        ''' <remarks>Properties are only flushed if they are marked as dirty.</remarks>
        Protected Overridable Sub ApplyPageChanges()
            Debug.Assert(Not Me.MultiProjectSelect, "Apply should not be occuring with multiple projects selected")
            Debug.Assert(Not m_ProjectReloadedDuringCheckout)
            Dim control As System.Windows.Forms.Control = Nothing
            Dim Transaction As DesignerTransaction = Nothing
            Dim Succeeded As Boolean = False
            Dim ProjectReloadWasValid As Boolean = False

            VerifyPropertiesWhichMayReloadProjectAreLast()

            'The objects which we have called ILangPropertyProvideBatchUpdate.BeginBatch on (and which need a corresponding
            '  EndBatch).  This could be a superset of m_Objects because individual properties can proffer objects not in
            '  m_Objects.  Entries which have not called BeginBatch or which do not support it will be Nothing.
            Dim BatchObjects() As AppDesInterop.ILangPropertyProvideBatchUpdate = Nothing
            Dim vsProjectBuildSystem As IVsProjectBuildSystem = Nothing

            Debug.Assert(Not m_fIsApplying)
            m_fIsApplying = True
            EnterProjectCheckoutSection()
            Try
                ' we should validate the page before appling any change...
                ' NOTE: We will call this twice for a dialog page, because it has been called on SaveCurrentValue. We don't hope it fail this time.
                '  We should consider whether we shouldn't pop error message when the designer is not activated unless when CommitPendingEdit...
                ValidatePageChanges(True)

                CheckOutFilesForApply()
                If m_ProjectReloadedDuringCheckout Then
                    Return
                End If

                ' Note that we should always batch up our changes, specifically because of properties like
                '   VB's RootNamespace which will force all generators within the project to run, and some
                '   generators add references while running which causes the project system to commit property
                '   changes to the compiler unless we've set the BatchEdit flags. For VB this is bad because VB 
                '   executes a symbolic-rename when it sees the root-namespace changing, but we want this to 
                '   happen after our property-set is applied and not in the middle because a random generator 
                '   happened to run.

                Debug.Assert(ProjectHierarchy IsNot Nothing, "no hierarchy?")
                vsProjectBuildSystem = TryCast(ProjectHierarchy, IVsProjectBuildSystem)
                Debug.Assert(vsProjectBuildSystem IsNot Nothing, "hierarchy is not IVsProjectBuildSystem?")

                Try
                    ' The project-system actually has two batching methods, and as it turns out, the
                    '   ILangPropertyProvideBatchUpdate will validate it's batch lock count with the
                    '   count in vsProjectBuildSystem, but not the other way around. What currently
                    '   happens is that some generators use the IVsProjectBuildSystem batching
                    '   mechanism, and since it doesn't check the ILangPropertyProvideBatchUpdate
                    '   batch lock count, the project-system applies changes when the generator
                    '   releases its batch lock count, even if we're still in the middle of setting
                    '   properties. To guard against that, we need to lock both batch mechanisms.
                    '
                    If (vsProjectBuildSystem IsNot Nothing) Then
                        vsProjectBuildSystem.StartBatchEdit()
                    End If

                    'Batch up property changes.  This does not affect how properties are persisted, but rather it tells the project system
                    '  to wait until all changes have been made before notifying the compiler of the changes.  This keeps the compiler from
                    '  doing things like restarting compilation several times in a row, if multiple properties have been changed.
                    'Note that this must happen before PreApplyPageChanges
                    BatchObjects = New AppDesInterop.ILangPropertyProvideBatchUpdate(RawPropertiesObjectsOfAllProperties.Length - 1 + 1) {} '+1 for CommonPropertiesObject
                    Dim i As Integer = 0
                    Dim BatchObject As AppDesInterop.ILangPropertyProvideBatchUpdate

                    'First the common properties object
                    BatchObject = TryCast(CommonPropertiesObject, AppDesInterop.ILangPropertyProvideBatchUpdate)
                    If BatchObject IsNot Nothing Then
                        Try
                            BatchObject.BeginBatch()
                            BatchObjects(i) = BatchObject
                        Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                            Debug.Fail("ILangPropertyProvideBatchUpdate.BeginBatch() failed, ignoring: " & ex.ToString)
                        End Try
                    End If
                    '... then individual objects from SetObjects
                    i += 1
                    For Each Obj As Object In RawPropertiesObjectsOfAllProperties
                        BatchObject = TryCast(Obj, AppDesInterop.ILangPropertyProvideBatchUpdate)
                        If BatchObject IsNot Nothing Then
                            Try
                                BatchObject.BeginBatch()
                                BatchObjects(i) = BatchObject
                            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                                Debug.Fail("ILangPropertyProvideBatchUpdate.BeginBatch() failed, ignoring: " & ex.ToString)
                            End Try
                        End If
                        i += 1
                    Next

                    PreApplyPageChanges()
                    If m_ProjectReloadedDuringCheckout Then
                        Return
                    End If

                    Transaction = GetTransaction()

                    For Each _controlData As PropertyControlData In ControlData
                        Dim ProjectMayBeReloadedDuringPropertySet As Boolean = (0 <> (_controlData.GetFlags() And ControlDataFlags.ProjectMayBeReloadedDuringPropertySet))

                        'Track the current control for determining which control focus
                        'should be returned when an exception occurs
                        control = _controlData.FormControl

                        'Apply any changes
                        Try
                            Debug.Assert(_controlData.DispId >= 0)
                            _controlData.ApplyChanges()
                        Catch ex As ProjectReloadedException
                            'If setting a property causes the project to get reloaded,
                            '  exit ASAP.  Our project references are now invalid.
                            Debug.Assert(ProjectReloadedDuringCheckout, "This should already have been set")
                            m_ProjectReloadedDuringCheckout = True
                            If ProjectMayBeReloadedDuringPropertySet Then
                                ProjectReloadWasValid = True
                            End If

                            'Check if there any any other pending property changes on this page (this shouldn't
                            '  happen if the advice in VerifyPropertiesWhichMayReloadProjectAreLast is
                            '  followed, unless there's more than one property on a page which can cause a 
                            '  project reload).
                            For Each cd As PropertyControlData In ControlData
                                If cd IsNot _controlData AndAlso cd.IsDirty Then
                                    ShowErrorMessage(SR.GetString(SR.PPG_ProjectReloadedSomePropertiesMayNotHaveBeenSet))
                                    Exit For
                                End If
                            Next

                            Throw
                        Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                            'Be sure to set the inner exception, so that if it was a checkout cancel, the
                            '  message box later knows to ignore it.
                            If TypeOf ex Is System.Reflection.TargetInvocationException Then
                                ex = ex.InnerException
                            End If
                            Throw New ValidationException(ValidationResult.Failed, _controlData.DisplayPropertyName & ":" & vbCrLf & ex.Message, control, innerexception:=ex)
                        End Try
                    Next _controlData

                    'Clear 'control' to prevent setting focus inside Finally
                    control = Nothing

                    PostApplyPageChanges()

                    CommitTransaction(Transaction)
                    Succeeded = True

                Catch ex As ProjectReloadedException
                    'Just exit as soon as possible
                    Return

                Catch ex As Exception
                    If Transaction IsNot Nothing Then
                        Transaction.Cancel()
                    End If
                    Throw

                Finally
                    m_fIsApplying = False

                    If m_ProjectReloadedDuringCheckout Then

                        Debug.Assert(ProjectReloadWasValid, "The project was reloaded during an attempt to change properties.  This might" _
                            & " indicate that SCC updated the file during a checkout.  But that implies that we didn't check out all necessary" _
                            & " files before trying to change the property.  Please check that all PropertyControlData have the correct set of necessary" _
                            & " files.  If setting this property can validly cause the project to get reloaded, such as is the case for the" _
                            & " TargetFramework property, then be sure the property's PropertyControlData has the ProjectMayBeReloadedDuringPropertySet" _
                            & " flag.")

                    Else

                        'Notify batch update that we're done changing property values.
                        If BatchObjects IsNot Nothing Then
                            For Each BatchObject As AppDesInterop.ILangPropertyProvideBatchUpdate In BatchObjects
                                If BatchObject IsNot Nothing Then
                                    Try
                                        BatchObject.EndBatch()
                                    Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)

                                        'This will fail if there are build problems or validation problems when the project system
                                        '  tries to persist the requested property changes to the build system.  Sometimes this indicates
                                        '  a problem with the project file or the project system.  Other times it's simply a result
                                        '  of the fact that the project system isn't able to validate all properties fully until they're
                                        '  sent to the compiler.
                                        'We shouldn't throw an exception here, because that would cause problems with the page and 
                                        '  we would likely continue getting this same error every time any additional property change
                                        '  is made.  Normally there will be an error on compilation if an invalid property has been
                                        '  passed to the compiler.  So our best bet is to ignore exceptions if they occur.

                                        Trace.WriteLine("ILangPropertyProvideBatchUpdate.EndBatch failed, ignoring:" & vbCrLf & Common.DebugMessageFromException(ex))
                                    End Try
                                End If
                            Next
                        End If

                        ' unlock this one last because ILangPropertyProvideBatchUpdate checks the
                        '   IVsProjectBuildSystem batch lock count before applying properties, but
                        '   IVsProjectBuildSystem does not check ILangPropertyProvideBatchUpdate's
                        '   batch lock count, so by freeing this one last, we make the best attempt
                        '   at ensuring properties get pushed into the compiler only once.
                        '
                        If vsProjectBuildSystem IsNot Nothing Then
                            Try
                                'If m_DeactivateDuringApply = True, then the following may assert about the project being uninitialized.
                                '  In reality, it is zombied, and in this scenario the assertion can be ignored.
                                vsProjectBuildSystem.EndBatchEdit()
                            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)

                                'This will fail if there are build problems or validation problems when the project system
                                '  tries to persist the requested property changes to the build system.  Sometimes this indicates
                                '  a problem with the project file or the project system.  Other times it's simply a result
                                '  of the fact that the project system isn't able to validate all properties fully until they're
                                '  sent to the compiler.
                                'We shouldn't throw an exception here, because that would cause problems with the page and 
                                '  we would likely continue getting this same error every time any additional property change
                                '  is made.  Normally there will be an error on compilation if an invalid property has been
                                '  passed to the compiler.  So our best bet is to ignore exceptions if they occur.
                                Trace.WriteLine("IVsProjectBuildSystem.EndBatchEdit failed, ignoring:" & vbCrLf & Common.DebugMessageFromException(ex))
                            End Try
                        End If

                        'NOTE: It's possible for source code control to have updated the project file
                        '  while we were trying to set a property.  In this case, m_Site may now be Nothing, so
                        '  we have to guard against its use.  And we should shut down as quickly as possible if
                        '  this does happen.  We normally try to avoid this scenario by checking the files out first, but we're being defensive.
                        If m_Site Is Nothing Then
                            Debug.Fail("How did the site get removed if not because of a dispose during apply?")
                        End If
                        If m_Site IsNot Nothing Then
                            If control IsNot Nothing Then
                                control.Focus()
                            End If

                            'Are we still dirty?
                            m_IsDirty = False 'Setting to false beforehand causes notification on IsDirty assignment when still dirty
                            Dim ShouldBeDirty As Boolean = IsAnyPropertyDirty()
                            If Not Succeeded AndAlso Not m_Site.IsImmediateApply() Then
                                'If the page is not immediate apply, and the apply did not succeed, we need to keep the page
                                '  dirty.  It's possible the page was marked dirty separately of any property (e.g. the
                                '  Unused References dialog), and if the page is not dirty, the next OK click will have no effect.
                                ShouldBeDirty = True
                            End If
                            IsDirty = ShouldBeDirty

                            CheckPlayCachedPropertyChanges()
                        End If

                    End If 'If m_ProjectReloadedDuringCheckout Then ...

                End Try
            Catch validateEx As ValidationException
                If Not m_ProjectReloadedDuringCheckout AndAlso m_Site IsNot Nothing Then
                    If validateEx.InnerException IsNot Nothing _
                    AndAlso (Common.IsCheckoutCanceledException(validateEx.InnerException) OrElse TypeOf validateEx.InnerException Is CheckoutException) Then
                        'If there was a check-out exception, it's possible some of the
                        '  properties are in a half-baked state.  For an immediate-apply page,
                        '  it's not appropriate to have UI that's in an incorrect state, so restore
                        '  all the properties to their original state, and reset the dirty state of the
                        '  properties and the page.  For immediate apply, the user has to click OK/Cancel
                        '  so this is not an issue there.
                        If m_Site.IsImmediateApply Then
                            Try
                                RestoreInitialValues()
                            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                                Debug.Fail("Exception occurred trying to refresh all properties' UI: " & ex.ToString)
                            End Try
                        End If
                    End If

                    validateEx.RestoreFocus()
                End If

                Throw
            Finally
                m_fIsApplying = False
                LeaveProjectCheckoutSection()
            End Try

        End Sub

        ''' <summary>
        ''' Get a localized name for the undo transaction.  This name appears in the
        '''   Undo/Redo history dropdown in Visual Studio.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function GetTransactionDescription() As String
            Dim Description As String
            Dim PropertyNames As String = Nothing

            'Currently we generate a string like this:
            '
            '  "Change Property: <propname1>, <propname2>, ..."
            '

            For Each _controlData As PropertyControlData In ControlData
                If _controlData.IsDirty Then
                    If PropertyNames = "" Then
                        PropertyNames = _controlData.DisplayPropertyName
                    Else
                        PropertyNames = PropertyNames & ", " & _controlData.DisplayPropertyName  'CONSIDER: localize comma delimiter?
                    End If
#If DEBUG Then
                    If Common.Switches.PDUndo.TraceWarning Then
                        PropertyNames &= " [OldValue=" & Common.Utils.DebugToString(_controlData.InitialValue) & "] "
                    End If
#End If
                End If
            Next _controlData
            Description = SR.GetString(SR.PPG_UndoTransaction, New String() {PropertyNames})
            Return Description
        End Function

        Public Overridable Function GetTransaction() As DesignerTransaction
            If Me.m_PropPageUndoSite IsNot Nothing Then
                Dim Description As String = GetTransactionDescription()
                Return m_PropPageUndoSite.GetTransaction(Description)
            End If
            Return Nothing
        End Function

        Public Overridable Sub CommitTransaction(ByVal Transaction As DesignerTransaction)
            If Transaction IsNot Nothing Then
                Transaction.Commit()
            End If
        End Sub


        ''' <summary>
        ''' This is fired before a property has been updated in the project system either through direct
        '''   interaction with the UI by the user, or by the property page code.  Does not fire in response
        '''   to changes that take place as a result of code unrelated to the property pages (i.e., through
        '''   DTE manipulation by other code).  Required for Undo/Redo support.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="PropDesc"></param>
        ''' <remarks></remarks>
        Public Overridable Sub OnPropertyChanging(ByVal PropertyName As String, ByVal PropDesc As PropertyDescriptor)
            If Me.m_PropPageUndoSite IsNot Nothing Then
                m_PropPageUndoSite.OnPropertyChanging(PropertyName, PropDesc)
            End If
        End Sub


        ''' <summary>
        ''' This is fired after a property has been updated in the project system either through direct
        '''   interaction with the UI by the user, or by the property page code.  Does not fire in response
        '''   to changes that take place as a result of code unrelated to the property pages (i.e., through
        '''   DTE manipulation by other code).  Required for Undo/Redo support.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <remarks></remarks>
        Public Overridable Sub OnPropertyChanged(ByVal PropertyName As String, ByVal PropDesc As PropertyDescriptor, ByVal OldValue As Object, ByVal NewValue As Object)
            If Me.m_PropPageUndoSite IsNot Nothing Then
                m_PropPageUndoSite.OnPropertyChanged(PropertyName, PropDesc, OldValue, NewValue)
            End If
        End Sub


        ''' <summary>
        ''' Overridable sub to allow derived page processing
        ''' after apply button processing has been done.
        ''' </summary>
        ''' <param name="ApplySuccessful"></param>
        ''' <remarks>
        ''' This is where the page would be refreshed to reflect any changes made in the data.
        '''   Called only after all processing has been done.
        ''' </remarks>
        Protected Overridable Sub OnApplyComplete(ByVal ApplySuccessful As Boolean)

        End Sub

        ''' <summary>
        ''' Called by ApplyChanges after property values have been updated, 
        ''' but before ILangPropertyProvideBatchUpdate.EndBatch.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub PostApplyPageChanges()

        End Sub
#End Region

#Region "Page Initialization code"

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overridable Sub PreInitPage()
        End Sub

        ''' <summary>
        ''' This procedure is called to populate the property values in the UI
        ''' </summary>
        ''' <remarks>This is not usually overridden.  Derived pages should normally
        ''' only override PreInitPage and PostInitPage</remarks>
        Protected Overridable Sub InitPage()
            PreInitPage()

            'Initialize all the property values so they are accessible
            'in user callbacks
            For Each _controlData As PropertyControlData In ControlData
                _controlData.InitPropertyValue()
            Next _controlData

            'Now update the UI with the property data
            For Each _controlData As PropertyControlData In ControlData
                _controlData.InitPropertyUI()
            Next _controlData

            PostInitPage()
        End Sub

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overridable Sub PostInitPage()
        End Sub

#End Region

#Region "Property getter/setter helpers"
        ''' <summary>
        ''' Returns the PropertyDescriptor given the name of the property.
        ''' </summary>
        ''' <param name="PropertyName">Name of the property requested.</param>
        ''' <returns>The PropertyDescriptor for the property.</returns>
        ''' <remarks></remarks>
        Protected Function GetPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            Return m_ObjectsPropertyDescriptorsArray(0)(PropertyName)
        End Function
#End Region

#Region "Common property getter/setter helpers"

        '
        '
        ' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        '
        '


        ''' <summary>
        ''' Returns the PropertyDescriptor of a common property using the name of the property
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' </remarks>
        Public Function GetCommonPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            Return m_CommonPropertyDescriptors.Item(PropertyName)
        End Function


        ''' <summary>
        ''' Retrieves the current value of this property in the project (not the current value in the 
        '''   control on the property page as it has been edited by the user).
        ''' No type conversion is performed on the return value.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' </remarks>
        Protected Function GetCommonPropertyValueNative(ByVal prop As PropertyDescriptor) As Object
            Dim commonPropObject = CommonPropertiesObject

            If commonPropObject IsNot Nothing Then
                Return PropertyControlData.GetCommonPropertyValueNative(prop, commonPropObject)
            Else
                Throw New InvalidOperationException("CommonPropertiesObject is not set")
            End If
        End Function


        ''' <summary>
        ''' Retrieves the current value of a property in the project (not the current value in the 
        '''   control on the property page as it has been edited by the user).
        ''' No type conversion is performed on the return value.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' This must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' </remarks>
        Protected Function GetCommonPropertyValueNative(ByVal PropertyName As String) As Object
            Return GetCommonPropertyValueNative(GetCommonPropertyDescriptor(PropertyName))
        End Function


        ''' <summary>
        ''' Retrieves the current value of a property in the project (not the current value 
        '''   in the control on the property page as it has been edited by the user).
        ''' The value retrieved is converted using the property's type converter.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' </remarks>
        Protected Function GetCommonPropertyValue(ByVal prop As PropertyDescriptor) As Object
            Return PropertyControlData.GetCommonPropertyValue(prop, Me.CommonPropertiesObject)
        End Function


        ''' <summary>
        ''' Retrieves the current value of a property in the project (not the current value 
        '''   in the control on the property page as it has been edited by the user).
        ''' The value retrieved is converted using the property's type converter.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' </remarks>
        Protected Function GetCommonPropertyValue(ByVal PropertyName As String) As Object
            Return GetCommonPropertyValue(GetCommonPropertyDescriptor(PropertyName))
        End Function


        ''' <summary>
        ''' Sets the current value of a common property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is first converted into native format using the property's type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        Protected Sub SetCommonPropertyValue(ByVal prop As PropertyDescriptor, ByVal Value As Object)
            Dim _TypeConverter As TypeConverter = prop.Converter

            If (_TypeConverter IsNot Nothing) AndAlso _TypeConverter.GetStandardValuesSupported Then
                Value = _TypeConverter.ConvertFrom(Value)
            End If

            SetCommonPropertyValueNative(prop, Value)
        End Sub


        ''' <summary>
        ''' Sets the current value of a common property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is first converted into native format using the property's type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        Protected Sub SetCommonPropertyValue(ByVal PropertyName As String, ByVal value As Object)
            SetCommonPropertyValue(GetCommonPropertyDescriptor(PropertyName), value)
        End Sub


        ''' <summary>
        ''' Sets the current value of a common property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is first converted into native format using the property's type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        Protected Sub SetCommonPropertyValueNative(ByVal prop As PropertyDescriptor, ByVal Value As Object)
            SuspendPropertyChangeListening(DISPID_UNKNOWN)
            Try
                PropertyControlData.SetCommonPropertyValueNative(prop, Value, Me.CommonPropertiesObject)
            Finally
                ResumePropertyChangeListening(DISPID_UNKNOWN)
            End Try
        End Sub


        ''' <summary>
        ''' Sets the current value of a common property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is first converted into native format using the property's type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        Protected Sub SetCommonPropertyValueNative(ByVal PropertyName As String, ByVal Value As Object)
            SuspendPropertyChangeListening(DISPID_UNKNOWN)
            Try
                SetCommonPropertyValueNative(GetCommonPropertyDescriptor(PropertyName), Value)
            Finally
                ResumePropertyChangeListening(DISPID_UNKNOWN)
            End Try
        End Sub

#End Region

#Region "Control getter/setter helpers"

        ''' <summary>
        ''' Gets the current value of the given property in the UI
        ''' </summary>
        ''' <param name="name"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetControlValue(ByVal name As String) As Object
            Dim pcd As PropertyControlData = GetPropertyControlData(name)
            Return pcd.GetControlValue()
        End Function

        ''' <summary>
        ''' Gets the current value of the given property in the UI. Does not
        '''   perform any type conversions.
        ''' </summary>
        ''' <param name="name"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetControlValueNative(ByVal name As String) As Object
            Dim pcd As PropertyControlData = GetPropertyControlData(name)
            Return pcd.GetControlValueNative()
        End Function

        ''' <summary>
        ''' Returns a single value for the current value of a property.  If all of the extenders return the same value, 
        '''   the return value of the function is that single value.  If any of the values differ, then 
        '''   PropertyControlData.Indeterminate is returned.  If the property descriptor is missing,
        '''   PropertyControlData.MissingProperty is returned.
        ''' </summary>
        ''' <param name="Descriptor">The property descriptor for the property to get the value from</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function TryGetNonCommonPropertyValue(ByVal Descriptor As PropertyDescriptor) As Object
            Dim extenders As Object() = Me.m_ExtendedObjects
            Return PropertyControlData.TryGetNonCommonPropertyValueNative(Descriptor, extenders)
        End Function
#End Region


#Region "Dirty flag detection helpers"
        ''' <summary>
        ''' Applies changes to the given control, if CanApplyNow is True.  Otherwise
        '''   changes will be applied later when requested.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <remarks></remarks>
        Protected Sub ApplyChanges(ByVal sender As Object)
            If m_fInsideInit Then
                Return
            End If
            Debug.Assert(TypeOf sender Is System.Windows.Forms.Control, "Unexpected object type")

            Common.Switches.TracePDProperties(TraceLevel.Info, "ApplyChanges(" & sender.GetType.Name & ")")

            'Save CanApplyNow to reset
            Dim SaveApplyNow As Boolean = CanApplyNow
            CanApplyNow = True
            Try
                For Each _controlData As PropertyControlData In ControlData
                    If (_controlData.FormControl Is sender) AndAlso _controlData.IsDirty Then
                        'Control is dirty, force an apply (will only do so if immediate apply mode)
                        IsDirty = True
                        Exit For
                    End If
                Next _controlData
            Finally
                CanApplyNow = SaveApplyNow
            End Try
        End Sub


        ''' <summary>
        ''' Marks the page as dirty.  Note that individual properties
        '''   must be set dirty (which cause this method to be called)
        '''   in order for any changes to actually be applied.
        ''' </summary>
        ''' <param name="ReadyToApply">If True, the change is applied immediately (if in immediate apply mode).  If False, the change is
        '''   not applied immediately, but rather it will be applied later, on the next apply.  If multiple properties are going to be set
        '''   dirty, only the last SetDirty() call should pass in True for ReadyToApply.  Doing it this way will cause all of the properties
        '''   to be changed in the same undo/redo transaction (apply batches up all current changes into a single transaction).
        ''' </param>
        ''' <remarks></remarks>
        Public Sub SetDirty(ByVal ReadyToApply As Boolean)
            If m_fInsideInit Then
                Return
            End If

            Common.Switches.TracePDProperties(TraceLevel.Info, "SetDirty(ReadyToApply:=" & ReadyToApply & ")")

            'Save CanApplyNow to reset
            Dim SaveApplyNow As Boolean = CanApplyNow
            CanApplyNow = ReadyToApply
            Try
                IsDirty = True
            Finally
                CanApplyNow = SaveApplyNow
            End Try
        End Sub


        ''' <summary>
        ''' Marks the page as dirty.  Note that individual properties
        '''   must be set dirty (which cause this method to be called)
        '''   in order for any changes to actually be applied.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="ReadyToApply">If True, the change is applied immediately (if in immediate apply mode).  If False, the change is
        '''   not applied immediately, but rather it will be applied later, on the next apply.  If multiple properties are going to be set
        '''   dirty, only the last SetDirty() call should pass in True for ReadyToApply.  Doing it this way will cause all of the properties
        '''   to be changed in the same undo/redo transaction (apply batches up all current changes into a single transaction).
        ''' </param>
        ''' <remarks></remarks>
        Public Sub SetDirty(ByVal sender As Object, ByVal ReadyToApply As Boolean)
            If m_fInsideInit Then
                Return
            End If
            Debug.Assert(TypeOf sender Is System.Windows.Forms.Control, "Unexpected object type")

            Common.Switches.TracePDProperties(TraceLevel.Info, "SetDirty(<sender>, ReadyToApply:=" & ReadyToApply & ")")

            'Save CanApplyNow to reset
            Dim SaveApplyNow As Boolean = CanApplyNow
            CanApplyNow = ReadyToApply
            Try
                For Each _controlData As PropertyControlData In ControlData
                    If _controlData.FormControl Is sender Then
                        'Check if the control is in a long edit (like typing a string in a textbox)
                        '  where you would not want to immediately save until the edit was complete
                        _controlData.IsDirty = True
                    End If
                Next _controlData
                IsDirty = True
            Finally
                CanApplyNow = SaveApplyNow
            End Try
        End Sub


        ''' <summary>
        ''' Marks the property associated with the given control (as well as this page)
        '''   as dirty.  
        '''   Note that individual properties must be set dirty (which cause this method 
        '''   to be called) in order for any changes to actually be applied.
        ''' </summary>
        ''' <param name="sender">The control to set as dirty</param>
        ''' <remarks>
        ''' If the CanApplyNow property is true, then changes will be applied immediately.
        ''' </remarks>
        Protected Sub SetDirty(ByVal sender As Object)
            SetDirty(sender, CanApplyNow)
        End Sub


        ''' <summary>
        ''' Marks the property with the given DISPID (as well as this page)
        '''   as dirty.  
        '''   Note that individual properties must be set dirty (which cause this method 
        '''   to be called) in order for any changes to actually be applied.
        ''' </summary>
        ''' <param name="dispid">The DISPID of the property to set as dirty.</param>
        ''' <remarks>
        ''' If the CanApplyNow property is true, then changes will be applied immediately.
        ''' </remarks>
        Protected Sub SetDirty(ByVal dispid As Integer)
            SetDirty(dispid, CanApplyNow)
        End Sub


        ''' <summary>
        ''' Marks the property with the given DISPID (as well as this page)
        '''   as dirty.  
        '''   Note that individual properties must be set dirty (which cause this method 
        '''   to be called) in order for any changes to actually be applied.
        ''' </summary>
        ''' <param name="dispid">The DISPID of the property to set as dirty.</param>
        ''' <param name="ReadyToApply">If True, the change is applied immediately (if in immediate apply mode).  If False, the change is
        '''   not applied immediately, but rather it will be applied later, on the next apply.  If multiple properties are going to be set
        '''   dirty, only the last SetDirty() call should pass in True for ReadyToApply.  Doing it this way will cause all of the properties
        '''   to be changed in the same undo/redo transaction (apply batches up all current changes into a single transaction).
        ''' </param>
        ''' <remarks></remarks>
        Protected Sub SetDirty(ByVal dispid As Integer, ByVal ReadyToApply As Boolean)
            If m_fInsideInit Then
                Return
            End If

            Common.Switches.TracePDProperties(TraceLevel.Info, "SetDirty(" & dispid & ", ReadyToApply:=" & ReadyToApply & ")")

            'Save CanApplyNow to reset
            Dim SaveApplyNow As Boolean = CanApplyNow
            CanApplyNow = ReadyToApply
            Try
                For Each _controlData As PropertyControlData In ControlData
                    If _controlData.DispId = dispid Then
                        _controlData.IsDirty = True
                    End If
                Next _controlData
                IsDirty = True
            Finally
                CanApplyNow = SaveApplyNow
            End Try
        End Sub


        ''' <summary>
        ''' Returns whether the property associated with the given control has been dirtied.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetDirty(ByVal sender As Object) As Boolean
            Debug.Assert(TypeOf sender Is System.Windows.Forms.Control, "Unexpected object type")

            For Each _controlData As PropertyControlData In ControlData
                If _controlData.FormControl Is sender Then
                    'Check if the control is in a long edit (like typing a string in a textbox)
                    '  where you would not want to immediately save until the edit was complete
                    Return _controlData.IsDirty
                End If
            Next _controlData
            Throw Common.CreateArgumentException("sender")
        End Function


        ''' <summary>
        ''' Clears the dirty bit for all properties on this page
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub ClearIsDirty()
            IsDirty = False
            For Each _controlData As PropertyControlData In ControlData
                _controlData.IsDirty = False
            Next _controlData
        End Sub


        ''' <summary>
        ''' Returns true iff any property on this page is dirty
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function IsAnyPropertyDirty() As Boolean
            For Each _controlData As PropertyControlData In ControlData
                If _controlData.IsDirty Then
                    Return True
                End If
            Next _controlData
            Return False
        End Function

        ''' <summary>
        ''' Enables or disables all controls associated with properties on this page.  The default implementation
        '''   uses the information already in the PropertyControlData for the page.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub EnableAllControls(ByVal _enabled As Boolean)
            For Each _controlData As PropertyControlData In ControlData
                _controlData.EnableControls(_enabled)
            Next _controlData
        End Sub

        ''' <summary>
        ''' Calls Initialize on all PropertyControlData for this page
        ''' </summary>
        ''' <remarks></remarks>
        Protected Sub InitializeAllProperties()
#If DEBUG Then
            'Verify that PropertyControlData DISPIDs are unique
            Dim PropIds As New List(Of Integer)
            For Each pcd As PropertyControlData In ControlData
                If PropIds.Contains(pcd.DispId) Then
                    Debug.Fail("DISPIDs for the properties on this page are not unique - DISPID=" & pcd.DispId)
                Else
                    PropIds.Add(pcd.DispId)
                End If
            Next
#End If
            For Each pcd As PropertyControlData In ControlData
                pcd.Initialize(Me)
            Next
            ClearIsDirty()
        End Sub

        ''' <summary>
        ''' Override this method to return a property descriptor for user-defined properties in a page.
        ''' </summary>
        ''' <param name="PropertyName">The property to return a property descriptor for.</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This method must be overridden to handle all user-defined properties defined in a page.  The easiest way to implement
        '''   this is to return a new instance of the UserPropertyDescriptor class, which was created for that purpose.
        ''' </remarks>
        Public Overridable Function GetUserDefinedPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            Return Nothing
        End Function

        ''' <summary>
        ''' Takes a value from the property store, and converts it into the UI-displayable form
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function ReadUserDefinedProperty(ByVal PropertyName As String, ByRef Value As Object) As Boolean
            Return False
        End Function

        ''' <summary>
        ''' Takes a value from the UI, converts it and writes it into the property store
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function WriteUserDefinedProperty(ByVal PropertyName As String, ByVal Value As Object) As Boolean
            Return False
        End Function

        Protected Property CanApplyNow() As Boolean
            Get
                Return m_CanApplyNow
            End Get
            Set(ByVal Value As Boolean)
                m_CanApplyNow = Value
            End Set
        End Property

        ''' <summary>
        ''' Indicates whether this property page is dirty or not.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <Browsable(False)> _
        Public Property IsDirty() As Boolean
            Get
                Return m_IsDirty
            End Get
            Set(ByVal Value As Boolean)
                If m_fInsideInit Then
                    Return
                End If

                Dim StatusChangeFlags As PROPPAGESTATUS
                'IF not dirty, just send clean status
                If Not Value Then
                    StatusChangeFlags = PROPPAGESTATUS.Clean Or PROPPAGESTATUS.Validate
                ElseIf CanApplyNow Then
                    StatusChangeFlags = PROPPAGESTATUS.Dirty Or PROPPAGESTATUS.Validate
                Else
                    'Just set dirty state, Apply should not be done at this moment
                    StatusChangeFlags = PROPPAGESTATUS.Dirty
                End If

                m_IsDirty = Value
                If m_Site IsNot Nothing Then
                    m_Site.OnStatusChange(StatusChangeFlags)
                End If
            End Set
        End Property

#End Region

        ''' <summary>
        ''' Use to display Error message through the VSShellService
        ''' </summary>
        ''' <param name="ex">The exception to get the error message from</param>
        ''' <remarks></remarks>
        Public Sub ShowErrorMessage(ByVal ex As Exception)
            ShowErrorMessage(ex, ex.HelpLink)
        End Sub

        ''' <summary>
        ''' Use to display Error message through the VSShellService
        ''' </summary>
        ''' <param name="ex">The exception to get the error message from</param>
        ''' <param name="HelpLink">The help link to use</param>
        ''' <remarks></remarks>
        Protected Sub ShowErrorMessage(ByVal ex As Exception, ByVal HelpLink As String)
            Dim Caption As String = SR.GetString(SR.APPDES_Title)
            AppDesDesignerFramework.DesignerMessageBox.Show(ServiceProvider, "", ex, Caption, HelpLink)
        End Sub

        ''' <summary>
        ''' Use to display Error message through the VSShellService
        ''' </summary>
        ''' <param name="errorMessage">The error message to display</param>
        ''' <remarks></remarks>
        Protected Sub ShowErrorMessage(ByVal errorMessage As String)
            ShowErrorMessage(errorMessage, "")
        End Sub

        ''' <summary>
        ''' Use to display Error message through the VSShellService
        ''' </summary>
        ''' <param name="errorMessage">The error message to display</param>
        ''' <param name="ex">The exception to include in the message.  The exception's message will be on a second line after errorMessage.</param>
        ''' <remarks></remarks>
        Protected Sub ShowErrorMessage(ByVal errorMessage As String, ByVal ex As Exception)
            Dim Caption As String = SR.GetString(SR.APPDES_Title)
            AppDesDesignerFramework.DesignerMessageBox.Show(ServiceProvider, errorMessage, ex, Caption)
        End Sub

        ''' <summary>
        ''' Use to display Error message through the VSShellService
        ''' </summary>
        ''' <param name="errorMessage">The error message to display</param>
        ''' <param name="HelpLink">The help link to use</param>
        ''' <remarks></remarks>
        Protected Sub ShowErrorMessage(ByVal errorMessage As String, ByVal HelpLink As String)
            Dim Caption As String = SR.GetString(SR.APPDES_Title)
            AppDesDesignerFramework.DesignerMessageBox.Show(ServiceProvider, errorMessage, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, HelpLink)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <Browsable(False), _
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Protected ReadOnly Property VsUIShellService() As IVsUIShell
            Get
                If (m_UIShellService Is Nothing) Then
                    m_UIShellService = CType(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                End If
                Return m_UIShellService
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <Browsable(False), _
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Protected ReadOnly Property VsUIShell2Service() As IVsUIShell2
            Get
                If (m_UIShell2Service Is Nothing) Then
                    Dim VsUiShell As IVsUIShell = Nothing
                    If Common.Utils.VBPackageInstance IsNot Nothing Then
                        VsUiShell = TryCast(Common.Utils.VBPackageInstance.GetService(GetType(IVsUIShell)), IVsUIShell)
                    ElseIf ServiceProvider IsNot Nothing Then
                        VsUiShell = TryCast(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                    End If
                    If VsUiShell IsNot Nothing Then
                        m_UIShell2Service = TryCast(VsUiShell, IVsUIShell2)
                    End If
                End If
                Return m_UIShell2Service
            End Get
        End Property

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property VsUIShell5Service() As IVsUIShell5
            Get
                If (m_UIShell5Service Is Nothing) Then
                    Dim VsUiShell As IVsUIShell = Nothing
                    If Common.Utils.VBPackageInstance IsNot Nothing Then
                        VsUiShell = TryCast(Common.Utils.VBPackageInstance.GetService(GetType(IVsUIShell)), IVsUIShell)
                    ElseIf ServiceProvider IsNot Nothing Then
                        VsUiShell = TryCast(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)
                    End If
                    If VsUiShell IsNot Nothing Then
                        m_UIShell5Service = TryCast(VsUiShell, IVsUIShell5)
                    End If
                End If
                Return m_UIShell5Service
            End Get
        End Property

        ''' <summary>
        ''' Adds the given file to the project
        ''' </summary>
        ''' <param name="FileName">Full path to the file to add</param>
        ''' <remarks></remarks>
        Protected Overloads Function AddFileToProject(ByVal FileName As String) As EnvDTE.ProjectItem
            Return AddFileToProject(FileName, True)
        End Function

        ''' <summary>
        ''' Adds the given file to the project
        ''' </summary>
        ''' <param name="FileName">Full path to the file to add</param>
        ''' <param name="CopyFile">If true, the file is copied to the project using DTEProject.AddFromFileCopy, otherwise DTEProject.AddFromFile is used</param>
        ''' <remarks></remarks>
        Protected Overloads Function AddFileToProject(ByVal FileName As String, ByVal CopyFile As Boolean) As EnvDTE.ProjectItem
            Dim ProjectItems As EnvDTE.ProjectItems = DTEProject.ProjectItems
            Return AddFileToProject(DTEProject.ProjectItems, FileName, CopyFile)
        End Function

        ''' <summary>
        ''' Adds the given file to the ProjectItems
        ''' </summary>
        ''' <param name="ProjectItems">The ProjectsItem to add the file to</param>
        ''' <param name="FileName">Full path to the file to add</param>
        ''' <param name="CopyFile">If true, the file is copied to the project using DTEProject.AddFromFileCopy, otherwise DTEProject.AddFromFile is used</param>
        ''' <remarks>Will throw an exception on failure.</remarks>
        Protected Overloads Function AddFileToProject(ByVal ProjectItems As EnvDTE.ProjectItems, ByVal FileName As String, ByVal CopyFile As Boolean) As EnvDTE.ProjectItem
            Debug.Assert(IO.Path.IsPathRooted(FileName), "FileName passed to AddFileToProject should be a full path")

            'Canonicalize the file name
            FileName = IO.Path.GetFullPath(FileName)


            'First see if it is already in the project
            For Each ProjectItem As EnvDTE.ProjectItem In ProjectItems
                If ProjectItem.FileNames(1).Equals(FileName, StringComparison.OrdinalIgnoreCase) Then
                    Return ProjectItem
                End If
            Next

            If CopyFile Then
                Return ProjectItems.AddFromFileCopy(FileName)
            Else
                Return ProjectItems.AddFromFile(FileName)
            End If
        End Function


        ''' <summary>
        ''' Adds the given file to the ProjectItems and sets the BuildAction property.
        ''' </summary>
        ''' <param name="ProjectItems">The ProjectsItem to add the file to</param>
        ''' <param name="FileName">Full path to the file to add</param>
        ''' <param name="CopyFile">If true, the file is copied to the project using DTEProject.AddFromFileCopy, otherwise DTEProject.AddFromFile is used</param>
        ''' <param name="BuildAction">The value to set the BuildAction property to (if it exists for this project).</param>
        ''' <remarks>Will throw an exception on failure.</remarks>
        Protected Overloads Function AddFileToProject(ByVal ProjectItems As EnvDTE.ProjectItems, ByVal FileName As String, ByVal CopyFile As Boolean, ByVal BuildAction As VSLangProj.prjBuildAction) As EnvDTE.ProjectItem
            Dim NewProjectItem As EnvDTE.ProjectItem = AddFileToProject(ProjectItems, FileName, CopyFile)
            If NewProjectItem IsNot Nothing Then
                AppDesCommon.DTEUtils.SetBuildAction(NewProjectItem, BuildAction)
            End If

            Return NewProjectItem
        End Function


        ''' <summary>
        ''' Returns the PropPageHostDialog that is hosting the given page.
        ''' </summary>
        ''' <param name="ChildPage"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetPropPageHostDialog(ByVal ChildPage As PropPageUserControlBase) As PropPageHostDialog
            Return TryCast(ChildPage.ParentForm, PropPageHostDialog)
        End Function

        ''' <summary>
        ''' Displays a child property page.
        ''' </summary>
        ''' <param name="title">Title to be shown for the property page.</param>
        ''' <param name="PageType">Type of property page class to be instantiated.</param>
        ''' <returns>DialogResult returned from the host dialog.</returns>
        ''' <remarks></remarks>
        Protected Function ShowChildPage(ByVal Title As String, ByVal PageType As System.Type) As DialogResult
            Return ShowChildPage(Title, PageType, Nothing)
        End Function

        ''' <summary>
        ''' Displays a child property page.
        ''' </summary>
        ''' <param name="title">Title to be shown for the property page.</param>
        ''' <param name="PageType">Type of property page class to be instantiated.</param>
        ''' <param name="F1Keyword">Help keyword.  If empty or Nothing, the property page itself will be queried for the help topic.</param>
        ''' <returns>DialogResult returned from the host dialog.</returns>
        ''' <remarks></remarks>
        Protected Function ShowChildPage(ByVal Title As String, ByVal PageType As System.Type, ByVal F1Keyword As String) As DialogResult
            Dim Page As PropPageUserControlBase

            If m_Site Is Nothing Then
                Debug.Fail("Can't show a child page if we're not sited")
                Throw New System.InvalidOperationException
            End If

            If m_ChildPages.ContainsKey(PageType) Then
                'Already created
                Page = m_ChildPages(PageType)

                'Refresh to make sure the page has picked up all current values (even with property listening that sometimes
                '  can be an issue)
                Try
                    Page.SetObjects(m_Objects)
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    ShowErrorMessage(SR.GetString(SR.APPDES_ErrorLoadingPropPage) & vbCrLf & Common.DebugMessageFromException(ex))
                    Return DialogResult.Cancel
                End Try
            Else
                'Not yet created, create, site and initialize
                Try
                    Page = CType(System.Activator.CreateInstance(PageType), PropPageUserControlBase)
                    m_ChildPages.Add(PageType, Page)
                    Dim ChildPageSite As New ChildPageSite(Page, m_Site, m_PropPageUndoSite)
                    Page.SetPageSite(ChildPageSite)
                    If TypeOf Page Is IVsProjectDesignerPage Then
                        DirectCast(Page, IVsProjectDesignerPage).SetSite(ChildPageSite)
                    End If
                    Page.SetObjects(m_Objects)
                Catch ex As Exception
                    m_ChildPages.Remove(PageType)
                    AppDesCommon.RethrowIfUnrecoverable(ex)
                    ShowErrorMessage(SR.GetString(SR.APPDES_ErrorLoadingPropPage) & vbCrLf & Common.DebugMessageFromException(ex))
                    Return DialogResult.Cancel
                End Try
            End If

            Dim Dialog As PropPageHostDialog = GetPropPageHostDialog(Page)
            If Dialog Is Nothing Then
                Dialog = New PropPageHostDialog(ServiceProvider, F1Keyword)
                Dialog.PropPage = Page
            End If

            Dialog.Text = Title
            Dim Result As DialogResult = DialogResult.OK

            ' This page may be closed before ShowDialog returns if a project reload happens (SCC / target framework change)
            ' Call EnterProjectCheckoutSection so that the Dispose on this page doesn't happen while the child dialog is
            ' up (will result in focus problems).  The Dispose will happen in LeaveProjectCheckoutSection if appropriate

            EnterProjectCheckoutSection()
            Try
                Result = Dialog.ShowDialog()
            Finally
                LeaveProjectCheckoutSection()
            End Try

            'It's possible the site has been torn down now (iff SCC caused a project re-load), so guard against that
            If m_Site IsNot Nothing Then
                'Dirty state may have been changed, cancel needs to cleanup
                If IsPageDirty() Then
                    m_Site.OnStatusChange(PROPPAGESTATUS.Dirty Or PROPPAGESTATUS.Validate)
                Else
                    m_Site.OnStatusChange(PROPPAGESTATUS.Clean)
                End If
            End If

            Return Result
        End Function

        ''' <summary>
        ''' Browses for a directory that's relative to the project directory
        ''' </summary>
        ''' <param name="InitialDirectory">The initial directory for the dialog, relative to the project directory.  Can be Nothing or empty.</param>
        ''' <param name="DialogTitle">The title to use for the browse dialog.</param>
        ''' <param name="NewValue">The newly-selected directory, relative to the project directory, if True was returned.  Always returned with a backslash at the end.</param>
        ''' <returns>True if the user selected a directory, otherwise False.</returns>
        ''' <remarks></remarks>
        Protected Function GetDirectoryViaBrowseRelativeToProject(ByVal InitialDirectory As String, ByVal DialogTitle As String, ByRef NewValue As String) As Boolean
            Return GetDirectoryViaBrowseRelative(InitialDirectory, GetProjectPath(), DialogTitle, NewValue)
        End Function

        ''' <summary>
        ''' Browses for a directory that's relative to a given path
        ''' </summary>
        ''' <param name="RelativeInitialDirectory">The initial directory for the dialog, relative to BasePath.  Can be Nothing or empty.</param>
        ''' <param name="BasePath">The path to which the InitialDirectory and NewValue are relative to.</param>
        ''' <param name="DialogTitle">The title to use for the browse dialog.</param>
        ''' <param name="NewRelativePath">The newly-selected directory, relative to BasePath, if True was returned.  Always returned with a backslash at the end.</param>
        ''' <returns>True if the user selected a directory, otherwise False.</returns>
        ''' <remarks></remarks>
        Protected Function GetDirectoryViaBrowseRelative(ByVal RelativeInitialDirectory As String, ByVal BasePath As String, ByVal DialogTitle As String, ByRef NewRelativePath As String) As Boolean
            RelativeInitialDirectory = Trim(RelativeInitialDirectory)
            BasePath = Trim(BasePath)

            'Make the initial path relative to the base path
            If BasePath <> "" Then
                RelativeInitialDirectory = Path.Combine(BasePath, RelativeInitialDirectory)
            End If

            If GetDirectoryViaBrowse(RelativeInitialDirectory, DialogTitle, NewRelativePath) Then
                'Make the output path relative to the base path
                If BasePath <> "" Then
                    NewRelativePath = GetRelativeDirectoryPath(BasePath, NewRelativePath)
                End If

                NewRelativePath = Common.Utils.AppendBackslash(NewRelativePath)
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Browses for a directory.
        ''' </summary>
        ''' <param name="InitialDirectory">The initial directory for the dialog.  Can be Nothing or empty.</param>
        ''' <param name="DialogTitle">The title to use for the browse dialog.</param>
        ''' <param name="NewValue">The newly-selected directory, if True was returned.  Always returned with a backslash at the end.</param>
        ''' <returns>True if the user selected a directory, otherwise False.</returns>
        ''' <remarks></remarks>
        Protected Function GetDirectoryViaBrowse(ByVal InitialDirectory As String, ByVal DialogTitle As String, ByRef NewValue As String) As Boolean
            Dim Success As Boolean

            ' Browsing for directory.
            Const BIF_RETURNONLYFSDIRS As Integer = &H1     'For finding a folder to start document searching
            'Const BIF_DONTGOBELOWDOMAIN As Integer = &H2    'For starting the Find Computer
            'Const BIF_STATUSTEXT As Integer = &H4           'Top of the dialog has 2 lines of text for BROWSEINFO.lpszTitle and one line if
            '    this flag is set.  Passing the message BFFM_SETSTATUSTEXTA to the hwnd can set the
            '    rest of the text.  This is not used with BIF_USENEWUI and BROWSEINFO.lpszTitle gets
            '    all three lines of text.
            'Const BIF_RETURNFSANCESTORS As Integer = &H8
            'Const BIF_EDITBOX As Integer = &H10                 'Add an editbox to the dialog
            'Const BIF_VALIDATE As Integer = &H20                'Insist on valid result (or CANCEL)
            'Const BIF_NEWDIALOGSTYLE As Integer = &H40          'Use the new dialog layout with the ability to resize
            '    Caller needs to call OleInitialize() before using this API

            'Const BIF_USENEWUI As Integer = (BIF_NEWDIALOGSTYLE Or BIF_EDITBOX)
            'Const BIF_BROWSEINCLUDEURLS As Integer = &H80       '    Allow URLs to be displayed or entered. (Requires BIF_USENEWUI)
            'Const BIF_BROWSEFORCOMPUTER As Integer = &H1000    '    Browsing for Computers.
            'Const BIF_BROWSEFORPRINTER As Integer = &H2000     '    Browsing for Printers
            'Const BIF_BROWSEINCLUDEFILES As Integer = &H4000   '    Browsing for Everything
            'Const BIF_SHAREABLE As Integer = &H8000            '    sharable resources displayed (remote shares, requires BIF_USENEWUI)
            '
            Dim uishell As Microsoft.VisualStudio.Shell.Interop.IVsUIShell = _
                CType(ServiceProvider.GetService(GetType(Shell.Interop.IVsUIShell).GUID), Shell.Interop.IVsUIShell)

            Dim DirName As String

            InitialDirectory = Trim(InitialDirectory)
            If InitialDirectory = "" Then
                InitialDirectory = ""
            Else
                Try
                    'Path needs a backslash at the end, or it will be interpreted as a directory + filename
                    InitialDirectory = Path.GetFullPath(Common.Utils.AppendBackslash(InitialDirectory))
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    InitialDirectory = ""
                End Try
            End If

            Const MAX_DIR_NAME As Integer = 512
            Dim browseinfo As Shell.Interop.VSBROWSEINFOW()
            Dim stringMemPtr As IntPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(MAX_DIR_NAME * 2 + 2)
            Try
                browseinfo = New Shell.Interop.VSBROWSEINFOW(0) {}
                browseinfo(0).lStructSize = CUInt(System.Runtime.InteropServices.Marshal.SizeOf(browseinfo(0)))
                browseinfo(0).hwndOwner = Me.Handle
                browseinfo(0).pwzInitialDir = InitialDirectory
                browseinfo(0).pwzDlgTitle = DialogTitle
                browseinfo(0).nMaxDirName = MAX_DIR_NAME
                browseinfo(0).pwzDirName = stringMemPtr
                browseinfo(0).dwFlags = BIF_RETURNONLYFSDIRS

                Dim hr As Integer = uishell.GetDirectoryViaBrowseDlg(browseinfo)
                If VSErrorHandler.Succeeded(hr) Then
                    DirName = System.Runtime.InteropServices.Marshal.PtrToStringUni(stringMemPtr)
                    If Microsoft.VisualBasic.Right(DirName, 1) <> System.IO.Path.DirectorySeparatorChar Then
                        DirName &= System.IO.Path.DirectorySeparatorChar
                    End If
                    NewValue = DirName
                    Success = True
                Else
                    'User cancelled out of dialog
                End If
            Finally
                System.Runtime.InteropServices.Marshal.FreeHGlobal(stringMemPtr)
            End Try

            Return Success

        End Function

        ''' <summary>
        ''' Browses for a File.
        ''' </summary>
        ''' <param name="InitialDirectory">The initial directory for the dialog.  Can be Nothing or empty.</param>
        ''' <param name="NewValue">The newly-selected file, if True was returned.</param>
        ''' <param name="Filter"></param>
        ''' <returns>True if the user selected a file, otherwise False.</returns>
        ''' <remarks></remarks>
        Protected Function GetFileViaBrowse(ByVal InitialDirectory As String, ByRef NewValue As String, ByVal Filter As String) As Boolean
            Dim fileNames As ArrayList = Common.Utils.GetFilesViaBrowse(ServiceProvider, Me.Handle, InitialDirectory, SR.GetString(SR.PPG_SelectFileTitle), Filter, 0, False)
            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                NewValue = CStr(fileNames(0))
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Gets the project's path
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetProjectPath() As String
            Return CStr(GetCommonPropertyValueNative("FullPath")) 'CONSIDER: This won't work for all project types, e.g. ASP.NET when project path is a URL
        End Function

        ''' <summary>
        ''' Returns a project-relative path, given an absolute path.
        ''' </summary>
        ''' <param name="DirectoryPath">File or Directory path to make relative</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetProjectRelativeDirectoryPath(ByVal DirectoryPath As String) As String
            Return GetRelativeDirectoryPath(GetProjectPath(), DirectoryPath)
        End Function

        ''' <summary>
        ''' Returns a project-relative path to a file, given an absolute path.
        ''' </summary>
        ''' <param name="FilePath">File or Directory path to make relative</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetProjectRelativeFilePath(ByVal FilePath As String) As String
            Return GetRelativeFilePath(GetProjectPath(), FilePath)
        End Function


        ''' <summary>
        ''' Given a base path and a full path to a directory, returns the path of the full path relative to the base path.  Note: does
        '''   *not* return relative paths that begin with "..\", instead in this case it returns the original full path.
        '''   This is what Everett did.
        ''' </summary>
        ''' <param name="BasePath">The base path (with or without backslash at the end)</param>
        ''' <param name="DirectoryPath">The full path to the directory (with or without backslash at the end)</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetRelativeDirectoryPath(ByVal BasePath As String, ByVal DirectoryPath As String) As String
            Dim RelativePath As String = ""

            BasePath = Common.Utils.AppendBackslash(BasePath)
            DirectoryPath = Common.Utils.AppendBackslash(DirectoryPath)

            If DirectoryPath = "" Then
                DirectoryPath = ""
            End If

            If BasePath = "" Then
                Return DirectoryPath
            End If

            ' Remove the project directory path
            If String.Compare(BasePath, Microsoft.VisualBasic.Strings.Left(DirectoryPath, Len(BasePath)), StringComparison.OrdinalIgnoreCase) = 0 Then
                Dim ch As Char = CChar(Mid(DirectoryPath, Len(BasePath), 1))
                If ch = System.IO.Path.DirectorySeparatorChar OrElse ch = System.IO.Path.AltDirectorySeparatorChar Then
                    RelativePath = Mid(DirectoryPath, Len(BasePath) + 1)
                ElseIf ch = ChrW(0) Then
                    RelativePath = ""
                End If
            Else
                RelativePath = DirectoryPath
            End If

            Return RelativePath
        End Function

        ''' <summary>
        ''' Given a base path and a full path to a file, returns the path of the full path relative to the base path.  Note: does
        '''   *not* return relative paths that begin with "..\", instead in this case it returns the original full path.
        '''   This is what Everett did.
        ''' </summary>
        ''' <param name="BasePath">The base path.</param>
        ''' <param name="FilePath">The full path.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function GetRelativeFilePath(ByVal BasePath As String, ByVal FilePath As String) As String
            Debug.Assert(VB.Right(FilePath, 1) <> Path.DirectorySeparatorChar AndAlso VB.Right(FilePath, 1) <> Path.AltDirectorySeparatorChar, "Passed in a directory instead of a file to RelativeFilePath")

            If Len(FilePath) > 0 Then
                Dim FileDirectory As String = System.IO.Path.GetDirectoryName(FilePath)
                Return Path.Combine(GetRelativeDirectoryPath(BasePath, FileDirectory), Path.GetFileName(FilePath))
            Else
                Return ""
            End If
        End Function

        Protected ReadOnly Property IsUndoEnabled() As Boolean
            Get
                Return (m_PropPageUndoSite IsNot Nothing)
            End Get
        End Property


#Region "Project Kind and language"

        ''' <summary>
        ''' If there is a DTEProject associated with the objects passed in to SetObjects, returns the kind of project
        '''   that it is (a GUID as a string).  Otherwise, returns empty string.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Don't cache this as it can change with a SetObjects call.
        ''' </remarks>
        Protected ReadOnly Property ProjectKind() As String
            Get
                Debug.Assert(m_DTEProject IsNot Nothing, "Can't get ProjectKind because DTEProject not available")
                If m_DTEProject IsNot Nothing Then
                    Return m_DTEProject.Kind
                Else
                    Return String.Empty
                End If
            End Get
        End Property

        ''' <summary>
        ''' If there is a DTEProject associated with the objects passed in to SetObjects, returns the language of project
        '''   that it is (a GUID as a string).  Otherwise, returns empty string.
        ''' </summary>
        ''' <value></value>
        Protected ReadOnly Property ProjectLanguage() As String
            Get
                Debug.Assert(m_DTEProject IsNot Nothing, "Can't get ProjectLanguage because DTEProject not available")
                If m_DTEProject IsNot Nothing Then
                    Dim codeModel As EnvDTE.CodeModel = m_DTEProject.CodeModel
                    If codeModel IsNot Nothing Then
                        Return codeModel.Language
                    End If
                End If
                Return String.Empty
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff the project associated with the properties being displayed is a VB project.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsVBProject() As Boolean
            Return String.Compare(ProjectLanguage, EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB, StringComparison.OrdinalIgnoreCase) = 0
        End Function

        ''' <summary>
        ''' Returns True iff the project associated with the properties being displayed is a C# project.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsCSProject() As Boolean
            Return String.Compare(ProjectLanguage, EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp, StringComparison.OrdinalIgnoreCase) = 0
        End Function

        ''' <summary>
        ''' Returns True iff the project associated with the properties being displayed is a J# project.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsJSProject() As Boolean
            Return String.Compare(ProjectLanguage, EnvDTE80.CodeModelLanguageConstants2.vsCMLanguageJSharp, StringComparison.OrdinalIgnoreCase) = 0
        End Function

#End Region

#Region "IVsProjectDesignerPage"
        Private m_PropPageUndoSite As IVsProjectDesignerPageSite


        ''' <summary>
        ''' Searches for a property control data that may be in a child page.  The property name contains
        '''   the type of child page if the property is to be found on a child page (in the 
        '''   format 
        ''' 
        '''     ChildPageFullTypeName:ChildPropertyName
        ''' 
        ''' If the ChildPageFullTypeName portion exists, the property control data is retrieved from that
        '''   child page, otherwise it is retrieved from this page.
        ''' 
        ''' Property names with this encoding are used in relation to undo/redo functionality of child pages.
        ''' </summary>
        ''' <param name="EncodedPropertyName">The property name to search, which may contain a prefix indicating it
        '''   should be found in a child page.</param>
        ''' <returns></returns>
        ''' <remarks>See the ChildPageSite class for more information.</remarks>
        Private Function GetNestedPropertyControlData(ByVal EncodedPropertyName As String) As PropertyControlData
            If EncodedPropertyName Is Nothing Then
                EncodedPropertyName = ""
            End If

            Dim NestingCharIndex As Integer = EncodedPropertyName.IndexOf(ChildPageSite.NestingCharacter)
            If NestingCharIndex >= 0 Then
                Dim NestedPageTypeName As String = EncodedPropertyName.Substring(0, NestingCharIndex)
                Dim NestedPropertyName As String = EncodedPropertyName.Substring(NestingCharIndex + 1)
                For Each Child As PropPageUserControlBase In m_ChildPages.Values
                    If Child.GetType.FullName.Equals(NestedPageTypeName, StringComparison.Ordinal) Then
                        'Found the page
                        Return Child.GetPropertyControlData(NestedPropertyName)
                    End If
                Next
                Debug.Fail("Unable to find the page that a nested property came from")
                Return Nothing
            Else
                Return GetPropertyControlData(EncodedPropertyName)
            End If
        End Function

        ''' <summary>
        ''' Gets the current value for the given property.  This value will be serialized using the binary serializer, and saved for
        '''   use later by Undo and Redo operations.
        ''' </summary>
        ''' <param name="PropertyName">The name of the property whose current value is being queried.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function IVsProjectDesignerPage_GetPropertyValue(ByVal PropertyName As String) As Object Implements IVsProjectDesignerPage.GetProperty
            'Return the last saved value, not the current edit
            Dim Data As PropertyControlData = GetNestedPropertyControlData(PropertyName)
            If Data Is Nothing Then
                Debug.Fail("IVsProjectDesignerPage.GetPropertyValue: PropertyName passed in was not recognized")
                Throw AppDesCommon.CreateArgumentException("PropertyName")
            End If

            Dim Value As Object = Data.InitialValue
            If Value Is PropertyControlData.Indeterminate Then
                Debug.Fail("IVsProjectDesignerPage.GetProperty() should never return Indeterminate.  Why isn't this property being handled through GetPropertyMultipleValues?")
                Return Nothing
            End If
            If Value Is PropertyControlData.MissingProperty Then
                Debug.Fail("IVsProjectDesignerPage.GetProperty() should never return IsMissing.  How did this function get called if the property is missing?")
                Return Nothing
            End If
            If PropertyControlData.IsSpecialValue(Value) Then
                Debug.Fail("")
                Return Nothing
            End If

            Return Value
        End Function

        ''' <summary>
        ''' If the given value should be an enum but is not, converts it to the enum.
        ''' </summary>
        ''' <param name="Prop"></param>
        ''' <param name="Value"></param>
        ''' <remarks>
        ''' During undo/redo serialization/deserialization, enums may be converted to integral
        '''   types.  This converts them back to an enum so that undo code will work properly.
        ''' </remarks>
        Private Sub ConvertToEnum(ByVal Prop As PropertyControlData, ByRef Value As Object)
            If Prop IsNot Nothing AndAlso Prop.PropDesc IsNot Nothing Then
                If Prop.PropDesc.PropertyType.IsEnum AndAlso Value IsNot Nothing AndAlso Not Value.GetType.IsEnum Then
                    Value = Prop.PropDesc.Converter.ConvertTo(Value, Prop.PropDesc.PropertyType)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Tells the property page to set the given value for the given property.  This is called by the Project Designer
        '''   to handle Undo and Redo operations.  The page should set the given property's value and also update its UI 
        '''   for the given property.
        ''' </summary>
        ''' <param name="PropertyName">The name of the property whose current value should be changed.</param>
        ''' <param name="Value">The value to set into the given property.</param>
        ''' <remarks></remarks>
        Protected Overridable Sub IVsProjectDesignerPage_SetPropertyValue(ByVal PropertyName As String, ByVal Value As Object) Implements IVsProjectDesignerPage.SetProperty
            Debug.Assert(Not PropertyControlData.IsSpecialValue(Value))
            Dim _ControlData As PropertyControlData = GetNestedPropertyControlData(PropertyName)
            If _ControlData Is Nothing Then
                Debug.Fail("IVsProjectDesignerPage.GetPropertyValue: PropertyName passed in was not recognized")
                Throw AppDesCommon.CreateArgumentException("PropertyName")
            End If


            'Convert to an enum if it should be but is not
            ConvertToEnum(_ControlData, Value)

            _ControlData.SetInitialValues(Value)
            _ControlData.SetPropertyValueNative(Value)
            'Forces refresh UI from persisted property value
            _ControlData.InitPropertyUI()
        End Sub


        ''' <summary>
        ''' Returns true if the given property supports returning and setting multiple values at the same time in order to support
        '''   Undo and Redo operations when multiple configurations are selected by the user.  This function should always return the
        '''   same value for a given property (i.e., it does not depend on whether multiple configurations have currently been passed in
        '''   to SetObjects, but simply whether this property supports multiple-value undo/redo).
        ''' </summary>
        ''' <param name="PropertyName">The name of the property.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function IVsProjectDesignerPage_SupportsMultipleValueUndo(ByVal PropertyName As String) As Boolean Implements IVsProjectDesignerPage.SupportsMultipleValueUndo
            Dim Data As PropertyControlData = GetNestedPropertyControlData(PropertyName)
            If Data Is Nothing Then
                Debug.Fail("Couldn't find the requested property in IVsProjectDesignerPage_SupportsMultipleValueUndo")
                Return False
            End If

            Return SupportsMultipleValueUndo(Data)
        End Function


        ''' <summary>
        ''' Returns true if the given property supports returning and setting multiple values at the same time in order to support
        '''   Undo and Redo operations when multiple configurations are selected by the user.  This function should always return the
        '''   same value for a given property (i.e., it does not depend on whether multiple configurations have currently been passed in
        '''   to SetObjects, but simply whether this property supports multiple-value undo/redo).
        ''' </summary>
        ''' <param name="Data">The PropertyControlData to check for support.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function SupportsMultipleValueUndo(ByVal Data As PropertyControlData) As Boolean
            If Data Is Nothing Then
                Throw New ArgumentNullException
            End If

            Return Data.SupportsMultipleValueUndo()
        End Function


        ''' <summary>
        ''' Gets the current values for the given property, one for each of the objects (configurations) that may be affected by a property
        '''   change and need to be remembered for Undo purposes.  The set of objects passed back normally should be the same objects that
        '''   were given to the page via SetObjects (but this is not required).
        '''   This function is called for a property if SupportsMultipleValueUndo returns true for that property.  If 
        ''' SupportsMultipleValueUndo returns false, or this function returns False, then GetProperty is called instead.
        ''' </summary>
        ''' <param name="PropertyName">The property to read values from</param>
        ''' <param name="Objects">[out] The set of objects (configurations) whose properties should be remembered by Undo</param>
        ''' <param name="Values">[out] The current values of the property for each configuration (corresponding to Objects)</param>
        ''' <returns>True if the property has multiple values to be read.</returns>
        ''' <remarks></remarks>
        Protected Overridable Function IVsProjectDesignerPage_GetPropertyMultipleValues(ByVal PropertyName As String, ByRef Objects As Object(), ByRef Values As Object()) As Boolean Implements IVsProjectDesignerPage.GetPropertyMultipleValues
            Dim Data As PropertyControlData = GetNestedPropertyControlData(PropertyName)
            If Data Is Nothing OrElse Data.IsMissing Then
                Throw AppDesCommon.CreateArgumentException("PropertyName")
            End If

            If Not SupportsMultipleValueUndo(Data) Then
                Debug.Fail("Shouldn't have been called for multiple values if we don't support it")
                Throw New NotSupportedException
            End If

            Objects = Data.RawPropertiesObjects 'We need the raw objects, not the extended objects
            Values = Data.AllInitialValuesExpanded

            Debug.Assert(Values IsNot Nothing)
            If Values.Length <> Objects.Length Then
                Debug.Fail("Unexpected length of properties array in relation to its configurations")
                Throw New Package.InternalException
            End If

            Return True
        End Function


        ''' <summary>
        ''' Tells the property page to set the given values for the given properties, one for each of the objects (configurations) passed
        '''   in.  This property is called if the corresponding previous call to GetPropertyMultipleValues succeeded, otherwise
        '''   SetProperty is called instead.
        ''' Note that the Objects values are not required to be a subset of the objects most recently passed in through SetObjects.
        ''' </summary>
        ''' <param name="propertyName">The name of the property whose values (across multiple configurations) should be changed.</param>
        ''' <param name="objects">The set of objects (configurations) which should have their value changed for the given property.</param>
        ''' <param name="values">The set of new values which correspond to the set of objects passed in.</param>
        ''' <remarks></remarks>
        Protected Overridable Sub IVsProjectDesignerPage_SetPropertyValueMultipleValues(ByVal PropertyName As String, ByVal Objects() As Object, ByVal Values() As Object) Implements IVsProjectDesignerPage.SetPropertyMultipleValues
            If Objects Is Nothing OrElse Objects.Length = 0 OrElse Values Is Nothing OrElse Values.Length <> Objects.Length Then
                Debug.Fail("unexpected")
                Throw AppDesCommon.CreateArgumentException("Objects, Values")
            End If

            Dim Data As PropertyControlData = GetNestedPropertyControlData(PropertyName)
            If Data Is Nothing OrElse Data.IsMissing Then
                Throw AppDesCommon.CreateArgumentException("PropertyName")
            End If

            If Not SupportsMultipleValueUndo(Data) Then
                Debug.Fail("Shouldn't have been called for multiple values if we don't support it")
                Throw New NotSupportedException
            End If

            'Convert values to an enum if they should be but are not
            For i As Integer = 0 To Values.Length - 1
                ConvertToEnum(Data, Values(i))
            Next

            Data.SetInitialValues(Values)
            Data.SetPropertyValueNativeMultipleValues(Objects, Values)

            'Forces refresh UI from persisted property value
            Data.InitPropertyUI()
        End Sub


        ''' <summary>
        ''' Provides the property page undo site to the property page
        ''' </summary>
        ''' <param name="site"></param>
        ''' <remarks></remarks>
        Private Sub IVsProjectDesignerPage_SetSite(ByVal Site As IVsProjectDesignerPageSite) Implements IVsProjectDesignerPage.SetSite
            m_PropPageUndoSite = Site
        End Sub


        ''' <summary>
        ''' Finish all pending validations
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' Return false if validation failed, and the customer wants to fix it (not ignore it)
        ''' </remarks>
        Private Function FinishPendingValidations() As Boolean Implements IVsProjectDesignerPage.FinishPendingValidations
            Return ProcessDelayValidationQueue(False)
        End Function


        ''' <summary>
        ''' Called when the page is activated or deactivated
        ''' </summary>
        ''' <param name="activated">True if the page has been activated, or False if it has been deactivated.</param>
        ''' <remarks></remarks>
        Private Sub OnActivated(ByVal activated As Boolean) Implements IVsProjectDesignerPage.OnActivated
            If m_activated <> activated Then
                m_activated = activated
                OnPageActivated(activated)
            End If
        End Sub

        ''' <summary>
        ''' Called when the page is activated or deactivated
        ''' </summary>
        ''' <param name="activated"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub OnPageActivated(ByVal activated As Boolean)
        End Sub


#End Region

        Private Function OnModeChange(ByVal dbgmodeNew As Shell.Interop.DBGMODE) As Integer Implements Shell.Interop.IVsDebuggerEvents.OnModeChange
            Me.UpdateDebuggerStatus(dbgmodeNew)
        End Function

        ''' <summary>
        ''' Pick font to use in this dialog page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property GetDialogFont() As Font
            Get
                If ServiceProvider IsNot Nothing Then
                    Dim uiSvc As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                    If uiSvc IsNot Nothing Then
                        Return CType(uiSvc.Styles("DialogFont"), Font)
                    End If
                End If

                Debug.Fail("Couldn't get a IUIService... cheating instead :)")

                Return Form.DefaultFont
            End Get
        End Property


        ''' <summary>
        ''' Set font and scale page accordingly
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub ScaleWindowToCurrentFont()
            SetDialogFont(PageRequiresScaling)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="msg"></param>
        ''' <param name="wParam"></param>
        ''' <param name="lParam"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function OnBroadcastMessage(ByVal msg As UInteger, ByVal wParam As System.IntPtr, ByVal lParam As System.IntPtr) As Integer Implements Shell.Interop.IVsBroadcastMessageEvents.OnBroadcastMessage
            If msg = AppDesInterop.win.WM_SETTINGCHANGE Then
                If Me.IsHandleCreated Then
                    m_ScalingCompleted = False
                    SetDialogFont(PageRequiresScaling)
                End If
            End If
        End Function

        ''' <summary>
        ''' Set font and scale page accordingly
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub SetDialogFont(ByVal ScaleDialog As Boolean)
            If ManualPageScaling Then
                Exit Sub
            End If

            'Only scale once
            If m_ScalingCompleted Then
                Return
            End If

            'Set flag now whether we succeed or not
            m_ScalingCompleted = True

            ' Update font & scale accordingly...
            Dim Dialog As Control = CType(Me, Control)

            If Dialog Is Nothing Then
                Debug.Fail("Couldn't get Control to display to the user")
                Return
            End If

            If Dialog.Controls.Count() >= 1 Then
                Dim OldFont As Font = Me.Font
                Dim NewFont As Font = GetDialogFont()

                If Not OldFont.Equals(NewFont) OrElse Me.ManualPageScaling Then
                    If ScaleDialog Then
                        'WARNING: This is no longer the recommended code path for a property
                        'WARNING:   page.  This code manually tries to scale controls on the
                        'WARNING:   page according to font.  But it does not handle table layout
                        'WARNING:   panels well.
                        'WARNING: Instead it is recommended to turn this off by setting 
                        'WARNING:   PageRequiresScaling to False and setting AutoScaleMode to
                        'WARNING:   AutoScaleMode.Font in the page.

#If False Then
			'CONSIDER: Unfortunately, some project flavor pages hit this, causing suite failures,
                        '  and it is late to fix them for this product cycle.  But this assertion should be
                        '  enabled in the future and any failures should be fixed up.

                        Debug.Assert(MyBase.AutoScaleMode <> AutoScaleMode.Font, _
                                     "Warning: This property page has AutoScaleMode set to Font, but is " _
                                     & "also set up to have the page scale controls.  The recommendation is " _
                                     & "to use AutoScaleMode.Font and turn off the page's scaling by setting PageRequiresScaling to False")
#End If

                        'Note: Some pages still use this (e.g., the Security page)
                        Dim OldSize As SizeF = GetFontScaleSize(OldFont)
                        Dim NewSize As SizeF = GetFontScaleSize(NewFont)
                        Dim dx As Single = NewSize.Width / OldSize.Width
                        Dim dy As Single = NewSize.Height / OldSize.Height

                        Dialog.Scale(New SizeF(dx, dy))

                        Dialog.Font = NewFont

                        ' Adjust Minimum/MaximunSize
                        Dim maxSize As Size = MaximumSize

                        If Me.AutoSize Then
                            MinimumSize = GetPreferredSize(System.Drawing.Size.Empty)
                        Else
                            MinimumSize = ScaleSize(MinimumSize, dx, dy)
                        End If

                        If Not MaximumSize.IsEmpty Then
                            MaximumSize = ScaleSize(maxSize, dx, dy)
                        End If
                    Else

                        Dialog.Font = NewFont

                    End If
                End If
            End If
        End Sub


        ''' <summary>
        '''   Calculate Font Size to scale the page
        '''  NOTE: We copied from WinForm
        ''' </summary>
        Private Shared Function GetFontScaleSize(ByVal font As Font) As SizeF

            Dim dx As Single = 9.0
            Dim dy As Single = font.Height

            Try
                Using graphic As Graphics = Graphics.FromHwnd(IntPtr.Zero)
                    Dim magicString As String = "The quick brown fox jumped over the lazy dog."
                    Dim magicNumber As Double = 44.549996948242189 ' chosen for compatibility with older versions of windows forms, but approximately magicString.Length
                    Dim stringWidth As Single = graphic.MeasureString(magicString, font).Width
                    dx = CType(stringWidth / magicNumber, Single)
                End Using
            Catch ex As OutOfMemoryException
                ' We may get an bogus OutOfMemoryException
                ' (which is a critical exception - according to ClientUtils.IsCriticalException())
                ' from GDI+. So we can't use ClientUtils.IsCriticalException here and rethrow.
            End Try

            Return New SizeF(dx, dy)
        End Function

        ''' <summary>
        '''   NOTE: We override GetPreferredSize, because the one from WinForm doesn't do the job correctly.
        '''      We should consider to remove it if WinForm team fixes the issue
        ''' </summary>
        Public Overrides Function GetPreferredSize(ByVal startSize As Size) As Size
            Dim preferredSize As Size = MyBase.GetPreferredSize(startSize)
            If Controls.Count() = 1 AndAlso Controls(0).Dock = DockStyle.Fill Then
                startSize.Width = Math.Max(startSize.Width - Padding.Horizontal, 0)
                startSize.Height = Math.Max(startSize.Height - Padding.Vertical, 0)
                Dim neededSize As Size = Controls(0).GetPreferredSize(startSize)
                neededSize += Padding.Size
                preferredSize.Width = Math.Max(neededSize.Width, preferredSize.Width)
                preferredSize.Height = Math.Max(neededSize.Height, preferredSize.Height)
            End If
            Return preferredSize
        End Function

        ''' <summary>
        '''     Scales a given size with the provided values.
        ''' </summary>
        Private Function ScaleSize(ByVal startSize As Size, ByVal x As Single, ByVal y As Single) As Size
            If Not GetStyle(ControlStyles.FixedWidth) Then
                startSize.Width = CInt(Math.Round(startSize.Width * x))
            End If
            If Not GetStyle(ControlStyles.FixedHeight) Then
                startSize.Height = CInt(Math.Round(startSize.Height * y))
            End If
            Return startSize
        End Function

        ''' <summary>
        ''' Causes the property descriptors to refresh their cache of standard values
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub RefreshPropertyStandardValues()
            'This can be accomplished by doing a GetProperty on the objects in question.  They'll automatically
            '  update the caches of related PropertyDescriptors

            Try
                'Do this for the objects passed in to SetObjects as well as the common properties
                Dim ExtendedObjects(m_ExtendedObjects.Length + 1 - 1) As Object '+1 for common properties object
                m_ExtendedObjects.CopyTo(ExtendedObjects, 0)
                ExtendedObjects(ExtendedObjects.Length - 1) = CommonPropertiesObject

                Dim ObjectsPropertyDescriptorsArray(ExtendedObjects.Length - 1) As PropertyDescriptorCollection
                For i As Integer = 0 To ExtendedObjects.Length - 1
                    Debug.Assert(ExtendedObjects(i) IsNot Nothing)
                    If TypeOf ExtendedObjects(i) Is ICustomTypeDescriptor Then
                        ObjectsPropertyDescriptorsArray(i) = CType(ExtendedObjects(i), ICustomTypeDescriptor).GetProperties(New Attribute() {})
                    Else
                        ObjectsPropertyDescriptorsArray(i) = System.ComponentModel.TypeDescriptor.GetProperties(ExtendedObjects(i))
                    End If

                    'We don't actually need to do anything with the properties that we got, we can throw them away.
                    '  Just the act of retrieving them causes all property descriptors for that object to refresh
                Next i
            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                Debug.Fail("An exception was thrown trying to get extended objects for the properties to refresh their standard values" & vbCrLf & ex.ToString)

                'Ignore
            End Try
        End Sub


        ''' <include file='doc\Form.uex' path='docs/doc[@for="Form.ProcessDialogKey"]/*' />
        ''' <devdoc>
        '''     Processes a dialog key. Overrides Control.processDialogKey(). For forms, this
        '''     method would implement handling of the RETURN, and ESCAPE keys in dialogs.
        ''' The method performs no processing on keys that include the ALT or
        '''     CONTROL modifiers.
        ''' </devdoc>
        Protected Overrides Function ProcessDialogKey(ByVal keyData As System.Windows.Forms.Keys) As Boolean
            If (keyData And (Keys.Alt Or Keys.Control)) = Keys.None Then
                Dim keyCode As Keys = keyData And Keys.KeyCode

                Select Case keyCode
                    Case Keys.Return
                        'User pressed <RETURN>.  If the currently control is a button, then we must click it.  This is
                        '  normally handled by the form's ProcessDialogKey, but since there is no form involved in the
                        '  modeless case (just this user control hosted inside a native window), we must handle it here.
                        If ParentForm Is Nothing AndAlso ActiveControl IsNot Nothing AndAlso TypeOf ActiveControl Is IButtonControl AndAlso ActiveControl.Focused Then
                            DirectCast(ActiveControl, IButtonControl).PerformClick()
                            Return True 'We handled the message
                        End If

                        'User pressed <RETURN> on a textbox?  Then commit his/her changes.
                        '...But only if the currently active, focused control is a TextBox (not a derived class), and 
                        '  the form is dirty, and it's immediate commit and not in a dialog, and it's not a multi-line 
                        '  textbox.
                        If ParentForm Is Nothing _
                        AndAlso m_Site IsNot Nothing AndAlso m_Site.IsImmediateApply() _
                        AndAlso ActiveControl IsNot Nothing _
                        AndAlso ActiveControl.GetType() Is GetType(TextBox) _
                        AndAlso ActiveControl.Focused _
                        AndAlso ActiveControl.Enabled _
                        AndAlso Not CType(ActiveControl, TextBox).Multiline _
                        Then
                            If Me.IsDirty Then
                                Common.Switches.TracePDProperties(TraceLevel.Warning, "*** ENTER pressed, calling SetDirty(True) on page")
                                SetDirty(True)
                            End If
                            Return True 'We handled the message
                        End If

                End Select
            End If

            Return MyBase.ProcessDialogKey(keyData)
        End Function


        <EditorBrowsable(EditorBrowsableState.Advanced)> _
        Protected Overrides Sub WndProc(ByRef m As Message)
            If m.Msg = Microsoft.VisualStudio.Editors.AppDesCommon.WmUserConstants.WM_PAGE_POSTVALIDATION Then

                If m_delayValidationGroup >= 0 AndAlso m_activated AndAlso Not m_inDelayValidation AndAlso Not IsFocusInControlGroup(m_delayValidationGroup) Then
                    ProcessDelayValidationQueue(False)
                End If
            Else
                MyBase.WndProc(m)
            End If
        End Sub


        ''' <summary>
        ''' Gets the F1 keyword to push into the user context for this property page
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetHelpContextF1Keyword() As String Implements IPropertyPageInternal.GetHelpContextF1Keyword
            Return GetF1HelpKeyword()
        End Function


#Region "Disable during debug mode and build"

        ''' <summary>
        ''' Determines whether the entire property page should be disabled while building.  Override to change default behavior.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overridable ReadOnly Property DisableOnBuild() As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Determines whether the entire property page should be disabled while in debugging mode.  Override to change default behavior.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected Overridable ReadOnly Property DisableOnDebug() As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Determines if the given mode should cause us to be disabled.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function DisableWhenDebugMode(ByVal mode As DBGMODE) As Boolean
            Return (mode <> DBGMODE.DBGMODE_Design)
        End Function


        ''' <summary>
        ''' Hook up with the debugger event mechanism to determine current debug mode
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectDebuggerEvents()
            If DisableOnDebug AndAlso ServiceProvider IsNot Nothing AndAlso m_VsDebuggerEventsCookie = 0 Then
                If m_VsDebugger Is Nothing Then
                    m_VsDebugger = CType(ServiceProvider.GetService(GetType(IVsDebugger)), IVsDebugger)
                End If
                If m_VsDebugger IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.AdviseDebuggerEvents(Me, m_VsDebuggerEventsCookie))

                    Dim mode As DBGMODE() = New DBGMODE() {DBGMODE.DBGMODE_Design}
                    'Get the current mode
                    VSErrorHandler.ThrowOnFailure(m_VsDebugger.GetMode(mode))
                    UpdateDebuggerStatus(mode(0))
                Else
                    Debug.Fail("Cannot obtain IVsDebugger from shell")
                    UpdateDebuggerStatus(DBGMODE.DBGMODE_Design)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Unhook event notification for debugger 
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisconnectDebuggerEvents()
            If m_VsDebugger IsNot Nothing AndAlso m_VsDebuggerEventsCookie <> 0 Then
                VSErrorHandler.ThrowOnFailure(m_VsDebugger.UnadviseDebuggerEvents(m_VsDebuggerEventsCookie))
                m_VsDebuggerEventsCookie = 0
            End If
            m_VsDebugger = Nothing
        End Sub

        ''' <summary>
        ''' Hook up event notifications for broadcast messages (e.g. font/size changes)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectBroadcastMessages()
            If ManualPageScaling Then
                'Not need if we're not automatically scaling the page
                Exit Sub
            End If

            If m_CookieBroadcastMessages = 0 AndAlso m_VsShellForUnadvisingBroadcastMessages Is Nothing Then
                If ServiceProvider IsNot Nothing Then
                    m_VsShellForUnadvisingBroadcastMessages = DirectCast(ServiceProvider.GetService(GetType(IVsShell)), IVsShell)
                End If
                If m_VsShellForUnadvisingBroadcastMessages IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(m_VsShellForUnadvisingBroadcastMessages.AdviseBroadcastMessages(Me, m_CookieBroadcastMessages))
                Else
                    Debug.Fail("Unable to get IVsShell for broadcast messages")
                End If
            End If
        End Sub

        ''' <summary>
        ''' Unhook event notifications for broadcast messages (e.g. font/size changes)
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisconnectBroadcastMessages()
            If m_CookieBroadcastMessages <> 0 AndAlso m_VsShellForUnadvisingBroadcastMessages IsNot Nothing Then
                VSErrorHandler.ThrowOnFailure(m_VsShellForUnadvisingBroadcastMessages.UnadviseBroadcastMessages(m_CookieBroadcastMessages))
                m_CookieBroadcastMessages = 0
            End If
            m_VsShellForUnadvisingBroadcastMessages = Nothing
        End Sub

        ''' <summary>
        ''' Enable or disable the page based on debug mode
        ''' </summary>
        ''' <param name="mode"></param>
        ''' <remarks></remarks>
        Private Sub UpdateDebuggerStatus(ByVal mode As DBGMODE)
            m_CurrentDebugMode = mode
            If DisableOnDebug AndAlso DisableWhenDebugMode(mode) Then
                m_PageEnabledPerDebugMode = False
            Else
                m_PageEnabledPerDebugMode = True
            End If
            SetEnabledState()
        End Sub

        ''' <summary>
        ''' A build has started - disable/enable page
        ''' </summary>
        ''' <param name="scope"></param>
        ''' <param name="action"></param>
        ''' <remarks></remarks>
        Private Sub BuildBegin(ByVal scope As EnvDTE.vsBuildScope, ByVal action As EnvDTE.vsBuildAction) Handles m_buildEvents.OnBuildBegin
            Debug.Assert(DisableOnBuild, "Why did we get a BuildBegin event when we shouldn't be listening?")
            m_PageEnabledPerBuildMode = False
            SetEnabledState()
        End Sub

        ''' <summary>
        ''' A build has finished - disable/enable page
        ''' </summary>
        ''' <param name="scope"></param>
        ''' <param name="action"></param>
        ''' <remarks></remarks>
        Private Sub BuildDone(ByVal scope As EnvDTE.vsBuildScope, ByVal action As EnvDTE.vsBuildAction) Handles m_buildEvents.OnBuildDone
            Debug.Assert(DisableOnBuild, "Why did we get a BuildDone event when we shouldn't be listening?")
            m_PageEnabledPerBuildMode = True
            SetEnabledState()
        End Sub

        ''' <summary>
        ''' Start listening to build events and set our initial build status
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectBuildEvents()
            If Me.DisableOnBuild Then
                If m_DTE IsNot Nothing Then
                    m_buildEvents = Me.m_DTE.Events.BuildEvents
                    ' We only hook up build events if we have to...
                    Dim monSel As IVsMonitorSelection = DirectCast(GetServiceFromPropertyPageSite(GetType(IVsMonitorSelection)), IVsMonitorSelection)

                    If monSel IsNot Nothing Then
                        Dim solutionBuildingCookie As UInteger
                        monSel.GetCmdUIContextCookie(New System.Guid(UIContextGuids.SolutionBuilding), solutionBuildingCookie)
                        Dim isActiveFlag As Integer
                        monSel.IsCmdUIContextActive(solutionBuildingCookie, isActiveFlag)
                        m_PageEnabledPerBuildMode = Not CBool(isActiveFlag)
                    Else
                        Debug.Fail("No service provider - we don't know if we are building (assuming not)")
                        m_PageEnabledPerBuildMode = True
                    End If
                Else
                    Debug.Fail("No DTE - can't hook up build events - we don't know if start/stop building...")
                    m_PageEnabledPerBuildMode = True
                End If
                'BUGFIX: Dev11#45255 
                SetEnabledState()
            End If
        End Sub

        ''' <summary>
        ''' Stop listening for build events
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub DisconnectBuildEvents()
            m_buildEvents = Nothing
        End Sub


#End Region

#Region "Property change listening"

        ''' <summary>
        ''' Start listening to project property change notifications
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ConnectPropertyNotify()
            'Unhook all old property notification sinks
            DisconnectPropertyNotify()

#If DEBUG Then
            Dim i As Integer = 0
#End If
            For Each Obj As Object In m_Objects
                Dim DebugSourceName As String = Nothing 'For retail, an empty source name is fine
#If DEBUG Then
                'Get a reasonable name for the source in debug mode (for tracing)
                DebugSourceName = "Objects #" & i
                If m_fConfigurationSpecificPage Then
                    Try
                        'Get the configuration's name
                        Dim Cfg As IVsCfg = TryCast(Obj, IVsCfg)
                        If Cfg IsNot Nothing Then
                            Cfg.get_DisplayName(DebugSourceName)
                        End If
                    Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    End Try
                Else
                    DebugSourceName &= " (Common Properties - non-config page)"
                End If
                i += 1
#End If
                AttemptConnectPropertyNotifyObject(Obj, DebugSourceName)
            Next

            If m_fConfigurationSpecificPage Then
                'Also need to listen to changes on common properties (only needed for config-specific pages because
                '  for non-config-specific pages, m_Objects *is* the common properties object
                AttemptConnectPropertyNotifyObject(CommonPropertiesObject, "Common Project Properties")
            End If
        End Sub


        ''' <summary>
        ''' Attempts to hook up IPropertyNotifySink to the given object.  Ignored if it
        '''   fails or is not supported for that object.
        ''' </summary>
        ''' <param name="EventSource">The object to try listening to property changes on.</param>
        ''' <param name="DebugSourceName">A debug name for the source</param>
        ''' <remarks></remarks>
        Private Sub AttemptConnectPropertyNotifyObject(ByVal EventSource As Object, ByVal DebugSourceName As String)
            Dim Listener As PropertyListener = PropertyListener.TryCreate(Me, EventSource, DebugSourceName, m_ProjectHierarchy, TypeOf EventSource Is IVsCfgBrowseObject)
            If Listener IsNot Nothing Then
                m_PropertyListeners.Add(Listener)
            End If
        End Sub


        ''' <summary>
        ''' Stop listening to project property change notifications
        ''' </summary> 'we persist this without the root namespace
        ''' <remarks></remarks>
        Private Sub DisconnectPropertyNotify()
            For Each Listener As PropertyListener In m_PropertyListeners
                Listener.Dispose()
            Next
            m_PropertyListeners.Clear()
        End Sub


        ''' <summary>
        ''' Called by a PropertyListener to update a property's UI from the current value when it has been changed by code other
        '''   than direct manipulation by the user via the property page.
        ''' Notifies a sink that the [bindable] property specified by dispID has changed. If dispID is 
        '''   DISPID_UNKNOWN, then multiple properties have changed together. The client (owner of the sink) 
        '''   should then retrieve the current value of each property of interest from the object that 
        '''   generated the notification.
        ''' </summary>
        ''' <param name="DISPID">[in] Dispatch identifier of the property that changed, or DISPID_UNKNOWN if multiple properties have changed.</param>
        ''' <remarks>
        ''' S_OK is returned in all cases even when the sink does not need [bindable] properties or when some 
        '''   other failure has occurred. In short, the calling object simply sends the notification and cannot 
        '''   attempt to use an error code (such as E_NOTIMPL) to determine whether to not send the notification 
        '''   in the future. Such semantics are not part of this interface.
        ''' </remarks>
        Public Sub OnExternalPropertyChanged(ByVal DISPID As Integer, ByVal DebugSourceName As String)
            Common.Switches.TracePDProperties(TraceLevel.Verbose, "[" & Me.GetType.Name & "] External property changed: DISPID=" & DISPID & ", Source=" & DebugSourceName)
            If m_fInsideInit Then
                Exit Sub
            End If

            'Are we currently in the process of changing this exact property through PropertyControlData?  If so, the change should normally
            '  be ignored.
            Dim SuspendAllChanges As Boolean = m_SuspendPropertyChangeListeningDispIds.Contains(DISPID_UNKNOWN)
            Dim ChangeIsDirect As Boolean = SuspendAllChanges _
                OrElse (DISPID <> DISPID_UNKNOWN AndAlso m_SuspendPropertyChangeListeningDispIds.Contains(DISPID))
            Debug.Assert(Common.Utils.Implies(m_fIsApplying, PropertyOnPageBeingChanged()), "If we're applying, the change should have come through a PropertyControlData")

            Dim Source As PropertyChangeSource = PropertyChangeSource.External

            Dim IsInternalToPage As Boolean = ChangeIsDirect OrElse Me.PropertyOnPageBeingChanged() OrElse m_fIsApplying
            If ChangeIsDirect Then
                Debug.Assert(Me.PropertyOnPageBeingChanged() OrElse m_fIsApplying)
                Common.Switches.TracePDProperties(TraceLevel.Verbose, "  (Direct - property is being changed by this page itself)")
                Source = PropertyChangeSource.Direct
            Else
                If IsInternalToPage Then
                    Source = PropertyChangeSource.Indirect
                End If
            End If


            Common.Switches.TracePDProperties(TraceLevel.Verbose, "  Source=" & Source.ToString())
            If IsInternalToPage Then
                'We don't want to send the notification now - this might mess up the UI for properties
                '  which are still dirty and need to have their current values intact in order to be
                '  correctly applied.  Queue this notification for later.
                If m_CachedPropertyChanges Is Nothing Then
                    m_CachedPropertyChanges = New List(Of PropertyChange)
                End If
                m_CachedPropertyChanges.Add(New PropertyChange(DISPID, Source))
            Else
                'Safe to send the notification now
                OnExternalPropertyChanged(DISPID, Source)
            End If
        End Sub


        ''' <summary>
        ''' Called whenever the property page detects that a property defined on this property page is changed in the
        '''   project system.  Property changes made directly by an apply or through PropertyControlData will not come 
        '''   through this method.
        ''' </summary>
        ''' <param name="DISPID">[in] Dispatch identifier of the property that changed, or DISPID_UNKNOWN if multiple properties have changed.</param>
        ''' <param name="Source">The source/cause of the property change.</param>
        ''' <remarks>
        ''' The default implementation OnExternalPropertyChanged(PropertyControlData, Source) if it finds a PropertyControlData
        '''   with the given DISPID.
        ''' </remarks>
        Protected Overridable Sub OnExternalPropertyChanged(ByVal DISPID As Integer, ByVal Source As PropertyChangeSource)
            Common.Switches.TracePDProperties(TraceLevel.Verbose, "OnExternalPropertyChanged(DISPID=" & DISPID & ", Source=" & Source.ToString() & ")")
            'Go through all the properties on the page to see if any match the DISPID that changed.
            For Each Data As PropertyControlData In ControlData
                If Data.DispId = DISPID OrElse DISPID = Interop.win.DISPID_UNKNOWN Then
                    OnExternalPropertyChanged(Data, Source)
                    If Data.DispId <> Interop.win.DISPID_UNKNOWN Then
                        'If the DISPID was a specific value, we only have one specific property to update, otherwise
                        '  we need to continue for all properties.
                        Return
                    End If
                End If
            Next
            Common.Switches.TracePDProperties(TraceLevel.Verbose, "  Did not find matching PropertyControlData on this page - ignoring.")
        End Sub


        ''' <summary>
        ''' Called whenever the property page detects that a property defined on this property page is changed in the
        '''   project system.  Property changes made directly by an apply or through PropertyControlData will not come 
        '''   through this method.
        ''' </summary>
        ''' <param name="Data">[in] The PropertyControlData for the property that changed.</param>
        ''' <param name="Source">The source/cause of the property change.</param>
        ''' <remarks>
        ''' The default implementation calls RefreshProperty if the Source is not Direct.
        ''' </remarks>
        Protected Overridable Sub OnExternalPropertyChanged(ByVal Data As PropertyControlData, ByVal Source As PropertyChangeSource)
            If Source <> PropertyChangeSource.Direct Then
                Data.RefreshValue()
            End If
        End Sub


        ''' <summary>
        ''' Checks if it's time to "play" the cached external property change notifications
        '''   that were queued up during an apply or property change.  If so, then it
        '''   sends those notifications off.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CheckPlayCachedPropertyChanges()
            Common.Switches.TracePDProperties(TraceLevel.Verbose, "PlayCachedPropertyChanges()")
            If m_fIsApplying OrElse PropertyOnPageBeingChanged() Then
                Common.Switches.TracePDProperties(TraceLevel.Verbose, "... Ignoring - IsApplying=" & m_fIsApplying & ", PropertyOnPageBeingChanged=" & PropertyOnPageBeingChanged())
                Return
            End If

            If m_CachedPropertyChanges IsNot Nothing Then
                For Each Change As PropertyChange In m_CachedPropertyChanges
                    Try
                        'These are all internal to the page, since they were
                        '  queued up because they occurred during an apply
                        OnExternalPropertyChanged(Change.DispId, Change.Source)
                    Catch ex As Exception When Not Common.Utils.IsUnrecoverable(ex)
                        ShowErrorMessage(ex)
                    End Try
                Next
                m_CachedPropertyChanges = Nothing
            End If
        End Sub


        ''' <summary>
        ''' Notifies a sink that a [requestedit] property is about to change and that the object is asking the sink how to proceed.
        ''' </summary>
        ''' <param name="DISPID"></param>
        ''' <remarks>
        ''' S_OK - The specified property or properties are allowed to change. 
        ''' S_FALSE - The specified property or properties are not allowed to change. The caller must obey this return value by discarding the new property value(s). This is part of the contract of the [requestedit] attribute and this method. 
        ''' 
        ''' The sink may choose to allow or disallow the change to take place. For example, the sink may enforce a read-only state 
        '''   on the property. DISPID_UNKNOWN is a valid parameter to this method to indicate that multiple properties are about to 
        '''   change. In this case, the sink can enforce a global read-only state for all [requestedit] properties in the object, 
        '''   including any specific ones that the sink otherwise recognizes.
        '''
        ''' If the sink allows changes, the object must also make IPropertyNotifySink::OnChanged notifications for any properties 
        '''   that are marked [bindable] in addition to [requestedit].
        '''
        ''' This method cannot be used to implement any sort of data validation. At the time of the call, the desired new value of 
        '''   the property is unavailable and thus cannot be validated. This method's only purpose is to allow the sink to enforce 
        '''   a read-only state on a property.
        ''' </remarks>
        Public Sub OnExternalPropertyRequestEdit(ByVal DISPID As Integer, ByVal DebugSourceName As String)
            'Nothing to do
        End Sub


#End Region


        ''' <summary>
        ''' Used for debug tracing of OnLayout events... 
        ''' </summary>
        ''' <param name="levent"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnLayout(ByVal levent As System.Windows.Forms.LayoutEventArgs)
            AppDesCommon.Switches.TracePDPerfBegin(levent, "PropPageUserControlBase.OnLayout()")
            MyBase.OnLayout(levent)
            AppDesCommon.Switches.TracePDPerfEnd("PropPageUserControlBase.OnLayout()")
        End Sub

    End Class

End Namespace
