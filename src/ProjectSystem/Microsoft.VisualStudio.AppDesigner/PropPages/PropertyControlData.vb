Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.ComponentModel

Imports VSITEMID = Microsoft.VisualStudio.Editors.VSITEMIDAPPDES

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Class that provides basic read/write of property values to/from the project system
    ''' </summary>
    ''' <remarks></remarks>
    <DebuggerDisplay("{PropertyName}, InitialValue={InitialValue}, IsDirty={IsDirty}")> _
    Public Class PropertyControlData

        Private Shared ReadOnly m_IndeterminateValue As Object = New Object
        Private Shared ReadOnly m_MissingValue As Object = New Object


#If DEBUG Then
        Private m_isInitialized As Boolean
#End If

        'The DISPID that corresponds to the property in the project.  It is important that this DISPID match the actual
        '  DISPID used by the project because a) when properties are changed by a source other than through the property pages,
        '  the DISPID is used to determine which property has changed, b) these are used by IPropertyPageInternal.EditProperty support.
        'For properties which do not correspond to project properties (user-persisted), any unique integer can be used, but it is
        '  best to keep them out of the range of properties implemented by the project system (the VB, C# and J# projects use integers
        '  above 10000).
        Private m_DispId As Integer 'A numeric id for this property

        'The name of the property in the project system's extensibility.  The name must match the project's property because properties 
        '  are looked up by name (not DISPID).  This allows project flavors to hide one property and add another with the same name but
        '  a different DISPID in order to "override" a property.
        Private m_PropertyName As String

        'Localized name for UI (used for Undo/Redo units, for example)
        Public DisplayPropertyName As String

        'The single value which represents the currently-persisted value.  If there are multiple configurations, this
        '  will have the value IndeterminateValue (and m_AllInitialValues will contain the array of differing values).
        '  If the property was not found, this will have the value MissingValue.
        Private m_InitialValue As Object

        'All initial values for all objects (usually configurations) passed in to SetObjects, if multiple values are supported
        '  for this property.  Generally only stored if the values were actually different.  Otherwise, this may
        '  simply be Nothing.
        Private m_AllInitialValues As Object()

        Protected SetCallback As SetDelegate
        Protected GetCallback As GetDelegate
        Protected MultiValueSetCallback As MultiValueSetDelegate
        Protected MultiValueGetCallback As MultiValueGetDelegate

        Public PropDesc As PropertyDescriptor

        'These are controls which should be disabled or hidden if this property is disabled or hidden.
        Public AssociatedControls As System.Windows.Forms.Control()

        Protected Flags As ControlDataFlags
        Protected m_Initializing As Boolean
        Protected m_FormControl As System.Windows.Forms.Control
        Protected m_PropPage As PropPageUserControlBase

        Protected m_isCommitingChange As Boolean

        'Used by PropertyDescriptorSetValue to know whether or not the 
        '  OnValueChanged event fired on the property descriptor
        Private m_OnValueChangedWasFired As Boolean

        'True if the controls associated with this property can be enabled/disabled
        '  (will be false e.g. if the property is hidden or read-only)
        Private m_ControlsCanBeEnabled As Boolean = True

#Region "Delegates"

        ''' <summary>
        ''' Delegate for the PropPageUserControlBase class to call when updating the property's control UI
        ''' </summary>
        ''' <param name="control">The form control from which to get the data (or Nothing if none is associated with this PropertyControlData)</param>
        ''' <param name="prop">The property descriptor associated with this PropertyControlData, or Nothing if none</param>
        ''' <param name="value">The value to set into the control's UI (may be Indeterminate or MissingProperty</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Delegate Function SetDelegate(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

        ''' <summary>
        ''' GetDelegate is called by the base class to retrieve the property from the property's control UI so 
        ''' that it can be written to the project system
        ''' </summary>
        ''' <param name="control">The form control from which to get the data (or Nothing if none is associated with this PropertyControlData)</param>
        ''' <param name="prop">The property descriptor associated with this PropertyControlData, or Nothing if none</param>
        ''' <param name="value">[out] Should be filled in with the value from the control.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Delegate Function GetDelegate(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

        ''' <summary>
        ''' Similar to SetDelegate, except that it allows for a value to be returned for each selected configuration.  This
        '''   is useful in some situations where more control over multi-configuration behavior is needed than the default
        '''   behavior (which gets/sets either a single value that's the same across all configurations or the Indeterminate value).
        ''' </summary>
        ''' <param name="control">The form control from which to get the data (or Nothing if none is associated with this PropertyControlData).</param>
        ''' <param name="prop">The property descriptor associated with this PropertyControlData, or Nothing if none</param>
        ''' <param name="values">The array of values to set into the control's UI.  May not be Nothing, and individual values will not be Indeterminate or MissingProperty.
        '''   Callee is responsible for determining how to deal with multiple values.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Delegate Function MultiValueSetDelegate(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal Values() As Object) As Boolean

        ''' <summary>
        ''' Similar to GetDelegate, except that it allows for a value to be returned for each selected configuration.  This
        '''   is useful in some situations where more control over multi-configuration behavior is needed than the default
        '''   behavior (which gets/sets either a single value that's the same across all configurations or the Indeterminate value).
        ''' </summary>
        ''' <param name="control">The form control from which to get the data (or Nothing if none is associated with this PropertyControlData)</param>
        ''' <param name="prop">The property descriptor associated with this PropertyControlData, or Nothing if none</param>
        ''' <param name="Values">Should be filled in with an array containing the values from the control.  The length of the array must match
        '''   that of RawPropertiesObjects, and must not contain Nothing, Indeterminate or MissingProperty values.
        ''' </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Delegate Function MultiValueGetDelegate(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef Values() As Object) As Boolean

#End Region


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="flags">Additional flags.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal flags As ControlDataFlags)
            Call Me.New(id, name, FormControl, Nothing, Nothing, flags)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control)
            Call Me.New(id, name, FormControl, Nothing, Nothing, ControlDataFlags.None)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="setter">The setter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="getter">The getter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="flags">Additional flags.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal setter As SetDelegate, ByVal getter As GetDelegate, ByVal flags As ControlDataFlags)
            Call Me.New(id, name, FormControl, setter, getter, flags, Nothing)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal AssocControls As System.Windows.Forms.Control())
            Call Me.New(id, name, FormControl, Nothing, Nothing, Nothing, Nothing, ControlDataFlags.None, AssocControls)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="setter">The setter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="getter">The getter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal setter As SetDelegate, ByVal getter As GetDelegate)
            Call Me.New(id, name, FormControl, setter, getter, ControlDataFlags.None, Nothing)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="setter">The setter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="getter">The getter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="flags">Additional flags.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal setter As MultiValueSetDelegate, ByVal getter As MultiValueGetDelegate, ByVal flags As ControlDataFlags, ByVal AssocControls As System.Windows.Forms.Control())
            Me.New(id, name, FormControl, Nothing, Nothing, setter, getter, flags, AssocControls)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="flags">Additional flags.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal flags As ControlDataFlags, ByVal AssocControls As System.Windows.Forms.Control())
            Me.New(id, name, FormControl, Nothing, Nothing, Nothing, Nothing, flags, AssocControls)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="setter">The setter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="getter">The getter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="flags">Additional flags.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal setter As SetDelegate, ByVal getter As GetDelegate, ByVal flags As ControlDataFlags, ByVal AssocControls As System.Windows.Forms.Control())
            Me.New(id, name, FormControl, setter, getter, Nothing, Nothing, flags, AssocControls)
        End Sub

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="id">The DISPID of the property.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="name">The property name.  See comments at top of PropertyControlData.vb.</param>
        ''' <param name="FormControl">The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.</param>
        ''' <param name="setter">The setter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="getter">The getter delegate, if any, which provides manual handling of getting and setting the property value into the control in the UI.  Note that this is a separate issue of whether a property is user-persisted.  May be Nothing.</param>
        ''' <param name="multiValueSetter">The multi-value setter delegate, if any.  See comments for MultiValueSetDelegate.  May be Nothing.</param>
        ''' <param name="multiValueGetter">The multi-value getter delegate, if any.  See comments for MultiValueGetDelegate.  May be Nothing.</param>
        ''' <param name="flags">Additional flags.</param>
        ''' <param name="AssociatedControls">An array of associated controls which should be enabled/disabled along with the FormControl if a property is not supported in a given project system.  May be Nothing.</param>
        ''' <remarks></remarks>
        Protected Sub New(ByVal id As Integer, ByVal name As String, ByVal FormControl As Control, ByVal setter As SetDelegate, ByVal getter As GetDelegate, ByVal multiValueSetter As MultiValueSetDelegate, ByVal multiValueGetter As MultiValueGetDelegate, ByVal flags As ControlDataFlags, ByVal AssociatedControls As System.Windows.Forms.Control())
            If id < 0 Then 'Don't allow DISPID_UNKNOWN (-1) etc
                Debug.Fail("Property ID must be non-negative")
                Throw AppDesCommon.CreateArgumentException("id")
            End If
            Me.m_DispId = id
            Me.m_PropertyName = name
            Me.m_FormControl = FormControl
            Me.Flags = flags 'Flags must be set before setting IsDirty...
            Debug.Assert(Not IsCommonProperty, "Should not pass in ControlDataFlags.CommonProperty to the PropertyControlData constructor")
            Debug.Assert(Not IsDirty, "Should not pass in ControlDataFlags.IsDirty to the PropertyControlData constructor")
            Me.IsCommonProperty = False
            Me.IsDirty = False
            Me.SetCallback = setter
            Me.GetCallback = getter
            Me.MultiValueSetCallback = multiValueSetter
            Me.MultiValueGetCallback = multiValueGetter
            Me.AssociatedControls = AssociatedControls

            ''DisplayName should be explicity set if it is a localized string
            If Me.DisplayPropertyName = "" Then
                'Use non-localized name if not found
                Me.DisplayPropertyName = name
            End If
        End Sub

        Protected ReadOnly Property ObjectsPropertyDescriptorsArray() As PropertyDescriptorCollection()
            Get
                Return m_PropPage.m_ObjectsPropertyDescriptorsArray
            End Get
        End Property

        ''' <summary>
        ''' The DISPID that corresponds to the property in the project.  See comments at top of PropertyControlData.vb.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property DispId() As Integer
            Get
                Return m_DispId
            End Get
        End Property

        ''' <summary>
        ''' The name of the property in the project system's extensibility.  The name must match the project's property because properties 
        '''  are looked up by name (not DISPID).  This allows project flavors to hide one property and add another with the same name but
        '''  a different DISPID in order to "override" a property.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property PropertyName() As String
            Get
                Return m_PropertyName
            End Get
        End Property

        ''' <summary>
        ''' Returns the page where this property is hosted.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property PropPage() As PropPageUserControlBase
            Get
                Return m_PropPage
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the object to be used for querying for common property values.  The object
        '''   used may vary, depending on the project type and what it supports.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property CommonPropertiesObject() As Object
            Get
                Debug.Assert(m_PropPage IsNot Nothing, "PropertyControlData not initialized?")
                Return m_PropPage.CommonPropertiesObject
            End Get
        End Property


        ''' <summary>
        ''' Returns the raw set of objects in use by this property page.  This will generally be the set of objects
        '''   passed in to the page through SetObjects.  However, it may be modified by subclasses to contain a superset
        '''   or subset for special purposes.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Calls to this property are called once by the PropPageUserControlBase after each SetObjects call, and may
        '''   be cached after that.  Properties should return the same set of values when given the same set of 
        '''   objects and under the same circumstances.
        ''' </remarks>
        Public Overridable ReadOnly Property RawPropertiesObjects() As Object()
            Get
                Debug.Assert(m_PropPage IsNot Nothing, "PropertyControlData not initialized?")
                Debug.Assert(m_PropPage.m_Objects IsNot Nothing)
                Return m_PropPage.m_Objects
            End Get
        End Property


        ''' <summary>
        ''' Returns the extended objects created from the raw set of objects in use by this property page.  This will generally be 
        '''   based on the set of objects passed in to the page through SetObjects.  However, it may be modified by subclasses to 
        '''   contain a superset or subset for special purposes.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overridable ReadOnly Property ExtendedPropertiesObjects() As Object()
            Get
                Debug.Assert(m_PropPage IsNot Nothing, "PropertyControlData not initialized?")
                Debug.Assert(m_PropPage.m_ExtendedObjects IsNot Nothing, "Extended objects array is Nothing")
                Return m_PropPage.m_ExtendedObjects
            End Get
        End Property

        ''' <summary>
        ''' The control, if any, which is automatically managed by this class instance to correspond to the property's value.  May be Nothing.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property FormControl() As Control
            Get
                Return m_FormControl
            End Get
        End Property


        ''' <summary>
        ''' True iff this property is user-persisted (i.e., rather than getting/setting it directly through the project
        '''   system, the storage/retrieval of the property's value is handled by the page).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property IsUserPersisted() As Boolean
            Get
                Return ((Flags And ControlDataFlags.UserPersisted) <> 0)
            End Get
            Set(ByVal Value As Boolean)
                If Value Then
                    Me.Flags = Me.Flags Or ControlDataFlags.UserPersisted
                Else
                    Me.Flags = Me.Flags And (Not ControlDataFlags.UserPersisted)
                End If
            End Set
        End Property

        Public Property IsUserHandledEvents() As Boolean
            Get
                Return ((Flags And ControlDataFlags.UserHandledEvents) <> 0)
            End Get
            Set(ByVal Value As Boolean)
                If Value Then
                    Me.Flags = Me.Flags Or ControlDataFlags.UserHandledEvents
                Else
                    Me.Flags = Me.Flags And (Not ControlDataFlags.UserHandledEvents)
                End If
            End Set
        End Property

        Private ReadOnly Property RefreshAllPropertiesWhenChanged() As Boolean
            Get
                Return 0 <> (Flags And ControlDataFlags.RefreshAllPropertiesWhenChanged)
            End Get
        End Property

        ''' <summary>
        ''' True iff this property was not passed in through SetObjects, but rather was found directly off of the project.
        '''   Such properties are always non-configuration-dependent properties being accessed from a configuration-dependent
        '''   page.
        ''' Note that this is *not* equivalent to whether or not the property is configuration-dependent - no properties on
        '''   a non-configuration-page are "common" properties, even though they are non-configuration-dependent
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property IsCommonProperty() As Boolean
            Get
                Return ((Me.Flags And ControlDataFlags.CommonProperty) = ControlDataFlags.CommonProperty)
            End Get
            Set(ByVal Value As Boolean)
                If Value Then
                    Me.Flags = Me.Flags Or ControlDataFlags.CommonProperty
                Else
                    Me.Flags = Me.Flags And (Not ControlDataFlags.CommonProperty)
                End If
            End Set
        End Property


        ''' <summary>
        ''' Returns true iff this property is configuration-specific.  This is not the opposite of
        '''   a "common" property (but all common properties are non-configuration-specific).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsConfigurationSpecificProperty() As Boolean
            Get
                Debug.Assert(m_PropPage IsNot Nothing)
                If m_PropPage.IsConfigurationSpecificPage Then
                    'On a config-specific page, all properties that were found in the object(s) passed in
                    '  through SetObjects() must be configuration-specific, while "common" properties
                    '  are not.
                    Return Not IsCommonProperty
                Else
                    'All properties on a non-config-specific page are likewise non-config-specific
                    Debug.Assert(Not IsCommonProperty, "All properties on a non-config page must should be found in the object(s) passed in through SetObjects and therefore not be 'common' properties")
                    Return False
                End If
            End Get
        End Property


        ''' <summary>
        ''' True iff this property instance is currently dirty
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property IsDirty() As Boolean
            Get
                Return ((Me.Flags And ControlDataFlags.Dirty) = ControlDataFlags.Dirty)
            End Get
            Set(ByVal Value As Boolean)
                If Value Then
                    If m_PropPage.m_fInsideInit OrElse m_Initializing Then
                        Return
                    End If

                    If m_isCommitingChange Then
                        ' we should prevent committing change twice because, we could pop error message boxes, or check-out box at that time, which could cause LostFocus...
                        Return
                    End If

                    If IsMissing Then
                        ' We treat setting the value of missing as a noop. 
                        ' Consider: Treat read-only properties the same way...
                        Return
                    End If

                    Try
                        m_isCommitingChange = True
                        Me.IsDirtyCore = True
                    Finally
                        m_isCommitingChange = False
                    End Try
                Else
                    Me.IsDirtyCore = False
                End If
            End Set
        End Property

        ''' <summary>
        ''' True iff this property instance is currently dirty
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Property IsDirtyCore() As Boolean
            Get
                Return ((Me.Flags And ControlDataFlags.Dirty) = ControlDataFlags.Dirty)
            End Get
            Set(ByVal Value As Boolean)
                If Value Then
                    AppDesCommon.Switches.TracePDProperties(TraceLevel.Error, "IsDirty := True (" & PropertyName & ")")
                    Me.Flags = Me.Flags Or ControlDataFlags.Dirty
                Else
                    Common.Switches.TracePDProperties(TraceLevel.Error, "IsDirty := False (" & PropertyName & ")")
                    Me.Flags = Me.Flags And (Not ControlDataFlags.Dirty)
                End If
            End Set
        End Property

        ''' <summary>
        ''' The special value constant that indicates that the
        '''   property represented by this class instance is missing in the current
        '''   project.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Public Shared ReadOnly Property MissingProperty() As Object
            Get
                'Note: what is referred to here as "missing" is simply that a flavor's implementation
                '  of IFilterProperties has returned vsFilterPropertiesAll, indicating that the
                '  property should be hidden in the property pages.  The extender wrapper from 
                '  AutomationExtenderManager replaces the property descriptor for such properties
                '  with a ReflectPropertyDescriptor, with Browsable set to False.  What should be
                '  happening is that we check for the Browsable=False on a property descriptor to
                '  know the property should be hidden.  But instead what is happening, due to a
                '  bug that the original architect mistook as intentional behavior, we get an error
                '  trying to get a property value using the ReflectPropertyDescriptors that were there
                '  just to change the Browsable attribute to False.
                'Unfortunately we can't easily change this behavior without affecting backwards 
                '  compatibility, so we have to live with it for now.
                Return m_MissingValue
            End Get
        End Property

        ''' <summary>
        ''' The special value constant that indicates that the
        '''   property has different values in the currently selected configurations
        '''   and therefore cannot be determined.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)> _
        Public Shared ReadOnly Property Indeterminate() As Object
            Get
                Return m_IndeterminateValue
            End Get
        End Property

        ''' <summary>
        ''' Returns true iff the value is the special value that indicates that the
        '''   property has different values in the currently selected configurations
        '''   and therefore cannot be determined.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsIndeterminate() As Boolean
            Get
#If DEBUG Then
                Debug.Assert(m_isInitialized, "Why are we checking if the property is indeterminate before the value is initialized!?")
#End If
                Return (InitialValue Is m_IndeterminateValue)
            End Get
        End Property

        ''' <summary>
        ''' Returns true iff the value is the special value that indicates that the
        '''   property represented by this class instance is missing in the current
        '''   project.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsMissing() As Boolean
            Get
#If DEBUG Then
                Debug.Assert(m_isInitialized, "Why are we checking if the property is missing before the value is initialized!?")
#End If
                Return InitialValue Is m_MissingValue
            End Get
        End Property

        ''' <summary>
        ''' Returns true iff the value is the special indeterminate or missing
        '''   property value.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IsSpecialValue() As Boolean
            Return IsSpecialValue(InitialValue)
        End Function

        ''' <summary>
        ''' Returns true iff the given value is the special indeterminate or missing
        '''   property value.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function IsSpecialValue(ByVal Value As Object) As Boolean
            Return (Value Is m_MissingValue) OrElse (Value Is m_IndeterminateValue)
        End Function

        Public Property IsHidden() As Boolean
            Get
                Return (Me.Flags And ControlDataFlags.Hidden) <> 0
            End Get
            Set(ByVal Value As Boolean)
                If Value Then
                    Me.Flags = Me.Flags Or ControlDataFlags.Hidden
                Else
                    Me.Flags = Me.Flags And (Not ControlDataFlags.Hidden)
                End If
            End Set
        End Property


        ''' <summary>
        ''' The single value which represents the currently-persisted value.  If there are multiple configurations, this
        '''   will have the value IndeterminateValue (and m_AllInitialValues will contain the array of differing values).
        '''   If the property was not found, this will have the value MissingValue.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property InitialValue() As Object
            Get
                Return m_InitialValue
            End Get
        End Property


        ''' <summary>
        ''' All initial values for all objects (usually configurations) passed in to SetObjects, if multiple values are supported
        '''   for this property.  Generally only stored if the values were actually different.  Otherwise, this may
        '''   simply be Nothing.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property AllInitialValues() As Object()
            Get
                Debug.Assert(m_AllInitialValues Is Nothing OrElse m_AllInitialValues.Length = RawPropertiesObjects.Length AndAlso m_AllInitialValues.Length = ExtendedPropertiesObjects.Length, _
                    "AllInitialValues should always be the same length as the array returned by RawPropertiesObjects and ExtendedPropertiesObjects")
                Return m_AllInitialValues
            End Get
        End Property


        ''' <summary>
        ''' Same as AllInitialValues, except that if AllInitialValues is Nothing (indicating that
        '''   the value is the same for all configurations, or that multiple values is not supported
        '''   for this property), then an array is returned that contains the single InitialValue value
        '''   as each element.  Never returns Nothing, but always returns an array of the same size
        '''   as RawPropertiesObjects.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property AllInitialValuesExpanded() As Object()
            Get
                Dim Values() As Object = Me.AllInitialValues
                If Values Is Nothing Then
                    'The property supports multiple values, but does not have a current set of them, which indicates
                    '  either they were all the same value, or there was an error in obtaining them.  Either way, 
                    '  we just propagate the current (single) initial value into all fields 
                    Debug.Assert(Not PropertyControlData.IsSpecialValue(Me.InitialValue))
                    Dim RawObjects() As Object = RawPropertiesObjects
                    Values = New Object(RawObjects.Length - 1) {}
                    For i As Integer = 0 To RawObjects.Length - 1
                        Values(i) = Me.InitialValue
                    Next
                End If

                Debug.Assert(Values IsNot Nothing)
                Return Values
            End Get
        End Property


        ''' <summary>
        ''' Sets the current persisted ("initial") value.  Also sets AllInitialValues to Nothing, indicating
        '''   that the same value is being persisted for all objects (configurations).
        ''' </summary>
        ''' <param name="InitialValue">The value to set as the initial value.</param>
        ''' <remarks></remarks>
        Public Sub SetInitialValues(ByVal InitialValue As Object)
            m_InitialValue = InitialValue
            m_AllInitialValues = Nothing
        End Sub


        ''' <summary>
        ''' Sets the single initial value as well as all initial values, when multi-value undo/redo is supported.
        ''' </summary>
        ''' <param name="InitialValue">The single initial value to set into m_InitialValue</param>
        ''' <param name="AllInitialValues">All initial values for all objects (configurations).  May be Nothing if all values are
        '''   the same as the single initial value, or if InitialValue is IsMissing.</param>
        ''' <remarks></remarks>
        Public Sub SetInitialValues(ByVal InitialValue As Object, ByVal AllInitialValues As Object())
            If AllInitialValues IsNot Nothing AndAlso AllInitialValues.Length = 0 Then
                Throw AppDesCommon.CreateArgumentException("AllInitialValues")
            End If
            Debug.Assert(AllInitialValues IsNot Nothing OrElse InitialValue IsNot Indeterminate)
            Debug.Assert(AllInitialValues Is Nothing _
                OrElse (AllInitialValues.Length = RawPropertiesObjects.Length AndAlso AllInitialValues.Length = ExtendedPropertiesObjects.Length), _
                "AllInitialValues should always be the same length as the array returned by RawPropertiesObjects and ExtendedPropertiesObjects")

            m_InitialValue = InitialValue
            m_AllInitialValues = AllInitialValues
        End Sub


        ''' <summary>
        ''' Sets all the initial values, when multi-value undo/redo is supported.  InitialValue will be automatically
        '''   set as well.
        ''' </summary>
        ''' <param name="AllInitialValues"></param>
        ''' <remarks></remarks>
        Public Sub SetInitialValues(ByVal AllInitialValues As Object())
            If AllInitialValues Is Nothing Then
                Throw New ArgumentNullException("AllInitialValues")
            End If
            If AllInitialValues.Length = 0 Then
                Throw AppDesCommon.CreateArgumentException("AllInitialValues")
            End If

            m_InitialValue = GetValueOrIndeterminateFromArray(AllInitialValues)
            m_AllInitialValues = AllInitialValues
        End Sub


        ''' <summary>
        ''' Sets up the property descriptor by searching for the property in the objects passed to us.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub Initialize(ByVal PropertyPage As PropPageUserControlBase)
            Debug.Assert(PropertyPage IsNot Nothing)
            m_Initializing = True
            AppDesCommon.Switches.TracePDPerfBegin("Property Initialize: " & Me.PropertyName)
            Try
                m_PropPage = PropertyPage
                If Me.IsUserPersisted Then
                    Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.Initialize(" & Me.PropertyName & "): User-persisted")
                    Me.PropDesc = GetUserDefinedPropertyDescriptor()
                    Debug.Assert(Me.PropDesc IsNot Nothing, "Call to GetUserDefinedPropertyDescriptor() returned Nothing for a user-persisted property.  Did you forget to override GetUserDefinedPropertyDescriptor()?")

                ElseIf (Me.PropertyName Is Nothing AndAlso (Me.GetCallback IsNot Nothing)) Then
                    '
                    'just skip, let GetCallback handle this one
                    '
                    Me.PropDesc = GetUserDefinedPropertyDescriptor()
                    Debug.Fail("Unexpected state")
                Else
                    'First look for the property in the objects passed to us through SetObjects - that's the preferred
                    '  place for dealing with properties.
                    Me.PropDesc = m_PropPage.m_ObjectsPropertyDescriptorsArray(0)(PropertyName)
                    If Me.PropDesc IsNot Nothing Then
                        Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.Initialize(" & Me.PropertyName & "): Property found")
                    End If

                    'Only if we didn't find it there, look directly on the project for the property (only for configuration-specific
                    '  pages).  Such properties are called "common" properties, and are used on some config-specific pages
                    '  for special purposes.
                    If PropDesc Is Nothing Then
                        If m_PropPage.m_CommonPropertyDescriptors IsNot Nothing Then
                            Me.PropDesc = m_PropPage.m_CommonPropertyDescriptors.Item(PropertyName)
                            If Me.PropDesc IsNot Nothing Then
                                Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.Initialize(" & Me.PropertyName & "): Common property (not found in SetObjects objects, but found on the project object)")
                                Me.IsCommonProperty = True
                            End If
                        End If
                    End If
                End If

                If Me.PropDesc Is Nothing Then
                    Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.Initialize(" & Me.PropertyName & "): ** Not found, will be disabled **")
                End If
                AppDesCommon.Switches.TracePDPerfEnd("Property Initialize: " & Me.PropertyName)
            Finally
                m_Initializing = False
            End Try
        End Sub


        ''' <summary>
        ''' Initializes the value for the property by getting the value from the project system
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub InitPropertyValue()
            Dim prop As PropertyDescriptor
            Dim Value As Object = Nothing
            Dim AllValues As Object() = Nothing
            Dim Handled As Boolean
            Dim SaveInitialized As Boolean = m_Initializing

            Debug.Assert(Not m_Initializing, "Unhandled state")
            AppDesCommon.Switches.TracePDPerfBegin("InitPropertyValue: " & Me.PropertyName)
            m_Initializing = True
            Try

                prop = Me.PropDesc

                Me.IsHidden = Me.IsHidden Or SKUMatrix.IsHidden(Me.DispId)

                If Me.IsUserPersisted Then
                    Debug.Assert(Not SupportsMultipleValueUndo(), "Not currently supported: user-persisted properties with config-dependent values")
                    'Let the page handle it
                    Try
                        Handled = Me.ReadUserDefinedProperty(Me.PropertyName, Value)
                    Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                        Debug.Fail("Exception reading user-defined property for initial value.  Will disable the property's control and related controls.  If this exception is expected, it should be handled in ReadUserDefinedProperty() and PropertyControlData.MissingProperty should be returned as the value." & vbCrLf _
                            & "PropertyName: " & Me.PropertyName & vbCrLf _
                            & "Exception:" & vbCrLf & ex.ToString())
                        Value = PropertyControlData.MissingProperty
                    End Try

                ElseIf prop Is Nothing Then
                    Value = PropertyControlData.MissingProperty

                ElseIf Me.IsCommonProperty Then
                    Try
                        Value = GetCommonPropertyValueNative()
                    Catch ex As Exception
                        Value = PropertyControlData.MissingProperty
                    End Try

                Else
                    If SupportsMultipleValueUndo() Then
                        'This is a config-specific property that supports multi-value undo/redo.  So get all of the 
                        '  initial() values (one from each config) now.
                        Debug.Assert(IsConfigurationSpecificProperty)
                        GetAllPropertyValuesNative(ExtendedPropertiesObjects, AllValues, Value)
                    Else
                        Value = TryGetPropertyValueNative(ExtendedPropertiesObjects)
                    End If
                End If

                SetInitialValues(Value, AllValues)
                AppDesCommon.Switches.TracePDPerfEnd("InitPropertyValue: " & Me.PropertyName)
            Finally
                m_Initializing = SaveInitialized
            End Try
#If DEBUG Then
            m_isInitialized = True
#End If
        End Sub

        ''' <summary>
        ''' Updates the UI for the property's control (if any), using the current value in Me.InitialValue
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub InitPropertyUI()
            Dim value As Object
            Dim Handled As Boolean
            Dim SaveInitialized As Boolean = m_Initializing
            AppDesCommon.Switches.TracePDPerfBegin("InitPropertyUI: " & Me.PropertyName)
            Debug.Assert(Not m_Initializing, "Unhandled state")

            m_Initializing = True
            Try
                value = Me.InitialValue

                If value IsNot PropertyControlData.MissingProperty Then
                    If Me.MultiValueSetCallback IsNot Nothing Then
                        'Multi-value set - we need to pass in all the values, not just the single one
                        Dim Values() As Object = Me.AllInitialValuesExpanded
                        Handled = Me.MultiValueSetCallback(Me.FormControl, Me.PropDesc, Values)
                    End If

                    If Not Handled Then
                        If Me.SetCallback IsNot Nothing Then
                            Handled = Me.SetCallback(Me.FormControl, Me.PropDesc, value)
                        Else
                            Handled = False
                        End If
                    End If
                Else
                    'No need to call the set callback if the control is hidden.  This is simply likely
                    '  to cause bugs for property pages when flavors hide properties if the page
                    '  doesn't check for MissingProperty.
                End If


                If Me.IsReadOnly Then
                    'The property is read only.  We want to disable the control, but we do want to still set its
                    '  current value, so we let the other If cases go through after this.
                    m_ControlsCanBeEnabled = False

                    If FormControl IsNot Nothing Then
                        SetControlsReadOnly(New Control() {Me.FormControl})
                    End If
                    SetControlsReadOnly(AssociatedControls)
                End If


                If Me.IsHidden OrElse (value Is PropertyControlData.MissingProperty) Then
                    m_ControlsCanBeEnabled = False

                    'Unsupported property for this project type, hide property and associated controls
                    HideOrDisableControls(Me.AssociatedControls, Me.IsHidden)

                    'Disable the field
                    If Me.FormControl IsNot Nothing Then
                        If Me.IsHidden Then
                            Me.FormControl.Visible = False
                            'Go ahead and populate the field for use within the form
                            If Not Handled Then
                                SetControlValue(value)
                                Handled = True
                            End If
                        Else
                            Me.FormControl.Enabled = False
                        End If
                        Common.Switches.TracePDProperties(TraceLevel.Info, "InitPropertyUI(" & Me.PropertyName & "): not found or hidden, so disabling")
                    Else
                        Common.Switches.TracePDProperties(TraceLevel.Info, "InitPropertyUI(" & Me.PropertyName & "): not found or hidden, but no control specified, so nothing to disable")
                    End If

                ElseIf Not Handled AndAlso Me.FormControl IsNot Nothing Then
                    ' If we have a contol associated with this property, and we don't have a set callback, or the 
                    ' set callback didn't handle updating the control (Handled = false), we use the default setter 
                    ' to update the associated control's UI.
                    '
                    SetControlValue(value)
                ElseIf Not Handled Then
                    Debug.Assert(IsHidden, "InitPropertyUI: Non-hidden property '" & PropertyName & ": Setting control value was not handled, and FormControl was not specified, so could not be handled automatically.")
                End If

                AppDesCommon.Switches.TracePDPerfEnd("InitPropertyUI: " & Me.PropertyName)
            Finally
                m_Initializing = SaveInitialized
            End Try
        End Sub


        ''' <summary>
        ''' Enables or disables the given control associated with this property (but never
        '''   enables a control when it has been disabled by a project flavor).
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub EnableAssociatedControl(ByVal control As Control, ByVal Enabled As Boolean)
            EnableAssociatedControls(New Control() {control}, Enabled)
        End Sub


        ''' <summary>
        ''' Enables or disables all the controls associated with this property (but never
        '''   enables a control when it has been disabled by a project flavor).
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub EnableAssociatedControls(ByVal controls() As Control, ByVal Enabled As Boolean)
            If Not m_ControlsCanBeEnabled Then
                'A flavor has disabled this property or made it readonly - we shouldn't enable the controls even
                '  if requested.
                Common.Switches.TracePDProperties(TraceLevel.Warning, "EnableControls - ignoring request to enable controls because property has been disabled: " & Me.PropertyName)
                Return
            End If

            For Each control As Control In controls
                control.Enabled = Enabled
            Next
        End Sub


        ''' <summary>
        ''' Enables or disables all the controls associated with this property (but never
        '''   enables a control when it has been disabled by a project flavor).
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub EnableControls(ByVal Enabled As Boolean)
            'Enable or disable the main control
            If FormControl IsNot Nothing Then
                EnableAssociatedControl(FormControl, Enabled)
            End If

            '... and associated controls
            If AssociatedControls IsNot Nothing Then
                EnableAssociatedControls(AssociatedControls, Enabled)
            End If
        End Sub


        ''' <summary>
        ''' Sets the given controls to a read-only state, if supported by the control, otherwise
        '''   disables it.
        ''' </summary>
        ''' <param name="controls"></param>
        ''' <remarks></remarks>
        Private Sub SetControlsReadOnly(ByVal controls() As Control)
            If controls IsNot Nothing Then
                For Each control As Control In controls
                    If TypeOf control Is TextBoxBase Then
                        CType(control, TextBox).ReadOnly = True
                    Else
                        control.Enabled = False
                    End If
                Next
            End If
        End Sub


        ''' <summary>
        ''' Hides or disables the given set of controls
        ''' </summary>
        ''' <param name="Controls">The controls to hide.  May be Nothing or empty.</param>
        ''' <param name="Hide">If True, the controls are hidden, else they're disabled</param>
        ''' <remarks></remarks>
        Protected Sub HideOrDisableControls(ByVal Controls() As Control, ByVal Hide As Boolean)
            If Controls IsNot Nothing Then
                For Each Control As Control In Controls
                    If Hide Then
                        Control.Visible = False
                    Else
                        Control.Enabled = False
                    End If
                Next
            End If
        End Sub


        ''' <summary>
        ''' Called to get a property descriptor for user-defined properties.
        '''   User pages handle this by specifying ControlDataFlags.UserPersisted, and overriding
        '''   PropPageUserControlBase.WriteUserDefinedProperty, ReadUserDefinedProperty and GetUserDefinedPropertyDescriptor.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' This is normally overridden in the property page, which this method delegates to, and
        '''    not in this class.
        ''' Used only for non-standard properties.  Note that this is completelyi independent of whether a custom
        '''   getter and setter are defined (they control reading a value from and writing it to a control's UI, while this
        '''   mechanism handles persisting and depersisting the property value in the project or other storage once it's 
        '''   obtained from the control).
        ''' </remarks>
        Protected Overridable Function GetUserDefinedPropertyDescriptor() As PropertyDescriptor
            Dim Prop As PropertyDescriptor = m_PropPage.GetUserDefinedPropertyDescriptor(Me.PropertyName)
            Debug.Assert(Prop IsNot Nothing, "Must implement GetUserDefinedPropertyDescriptor in derived class for UserDefined properties")
            Return Prop
        End Function


        ''' <summary>
        ''' Reads a property from a user-defined storage location (instead of automatically from the project properties).
        '''   User pages handle this by specifying ControlDataFlags.UserPersisted, and overriding
        '''   PropPageUserControlBase.WriteUserDefinedProperty, ReadUserDefinedProperty and GetUserDefinedPropertyDescriptor.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks>Used only for non-standard properties.  Note that this is completelyi independent of whether a custom
        '''   getter and setter are defined (they control reading a value from and writing it to a control's UI, while this
        '''   mechanism handles persisting and depersisting the property value in the project or other storage once it's 
        '''   obtained from the control).</remarks>
        Protected Overridable Function ReadUserDefinedProperty(ByVal PropertyName As String, ByRef Value As Object) As Boolean
            If m_PropPage.ReadUserDefinedProperty(Me.PropertyName, Value) Then
                Return True
            End If
            Debug.Fail("Must implement in derived class for UserDefined properties")
            Return False
        End Function

        ''' <summary>
        ''' Writes a property to a user-defined storage location (instead of automatically to the project properties).
        '''   User pages handle this by specifying ControlDataFlags.UserPersisted, and overriding
        '''   PropPageUserControlBase.WriteUserDefinedProperty, ReadUserDefinedProperty and GetUserDefinedPropertyDescriptor.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        ''' <returns></returns>
        ''' <remarks>Used only for non-standard properties.  Note that this is completelyi independent of whether a custom
        '''   getter and setter are defined (they control reading a value from and writing it to a control's UI, while this
        '''   mechanism handles persisting and depersisting the property value in the project or other storage once it's 
        '''   obtained from the control).</remarks>
        Protected Overridable Function WriteUserDefinedProperty(ByVal PropertyName As String, ByVal Value As Object) As Boolean
            If m_PropPage.WriteUserDefinedProperty(Me.PropertyName, Value) Then
                Return True
            End If

            Debug.Fail("Must implement in derived class for UserDefined properties")
            Return True
        End Function

#Region "Control getter/setter helpers"
        Public ReadOnly Property IsReadOnly() As Boolean
            Get
                If PropDesc IsNot Nothing Then
                    Return PropDesc.IsReadOnly
                End If

                Return False
            End Get
        End Property

        Protected Sub SetControlValue(ByVal value As Object)
            Dim control As System.Windows.Forms.Control = Me.FormControl
            Dim _TypeConverter As TypeConverter = Nothing

            If PropDesc IsNot Nothing Then
                _TypeConverter = PropDesc.Converter
            End If

            Debug.Assert(control IsNot Nothing, "Unexpected null argument")

            If TypeOf control Is System.Windows.Forms.TextBox Then
                If value Is PropertyControlData.Indeterminate Then
                    value = ""
                Else
                    If (_TypeConverter IsNot Nothing) Then 'AndAlso _TypeConverter.GetStandardValuesSupported Then
                        value = _TypeConverter.ConvertToString(value)
                    End If
                End If
                DirectCast(control, System.Windows.Forms.TextBox).Text = CType(value, String)

            ElseIf TypeOf control Is System.Windows.Forms.ComboBox Then
                Dim cbx As ComboBox = DirectCast(control, System.Windows.Forms.ComboBox)
                Dim StringValue As String = ""

                If value Is PropertyControlData.Indeterminate Then
                    StringValue = ""
                ElseIf (_TypeConverter IsNot Nothing) Then
                    StringValue = _TypeConverter.ConvertToString(value)
                ElseIf TypeOf value Is String Then
                    'nothing to do
                    StringValue = value.ToString()
                End If

                Debug.Assert(TypeOf StringValue Is String, "value should be string")

                If _TypeConverter IsNot Nothing AndAlso _TypeConverter.GetStandardValuesSupported Then
                    cbx.Items.Clear()
                    For Each o As Object In _TypeConverter.GetStandardValues()
                        cbx.Items.Add(_TypeConverter.ConvertToString(o))
                    Next
                End If

                If value IsNot PropertyControlData.Indeterminate Then
                    'Select the item in the list
                    cbx.SelectedItem = StringValue
                    If cbx.DropDownStyle = ComboBoxStyle.DropDownList Then
                        'If not in the list, add and select
                        If cbx.SelectedIndex = -1 Then
                            cbx.SelectedIndex = cbx.Items.Add(StringValue)
                            cbx.SelectedItem = StringValue
                        End If
                    Else
                        'Not a dropdown list, so just put the text portion of the combobox
                        If cbx.SelectedIndex = -1 Then
                            cbx.Text = StringValue
                        End If
                    End If
                Else
                    'Indeterminate state
                    cbx.SelectedIndex = -1
                End If

            ElseIf TypeOf control Is System.Windows.Forms.CheckBox Then

                Dim chk As CheckBox = DirectCast(control, System.Windows.Forms.CheckBox)
                If value Is PropertyControlData.Indeterminate Then
                    chk.CheckState = CheckState.Indeterminate
                Else
                    'If the checkbox is indeterminate, "Checked" is already considered to be true, so
                    '  force it to come out of indeterminate state first.
                    chk.CheckState = CheckState.Unchecked
                    If TypeOf value Is Boolean Then
                        chk.Checked = DirectCast(value, Boolean)
                    ElseIf _TypeConverter IsNot Nothing AndAlso _TypeConverter.CanConvertTo(GetType(Boolean)) Then
                        chk.Checked = DirectCast(_TypeConverter.ConvertTo(value, GetType(Boolean)), Boolean)
                    Else
                        Try
                            chk.Checked = CBool(value)
                        Catch ex As Exception
                            chk.CheckState = CheckState.Indeterminate
                        End Try
                    End If
                End If

            ElseIf TypeOf control Is System.Windows.Forms.Label Then
                If value Is PropertyControlData.Indeterminate Then
                    DirectCast(control, System.Windows.Forms.Label).Text = ""
                Else
                    DirectCast(control, System.Windows.Forms.Label).Text = CType(value, String)
                End If
                'Labels don't get disabled

            ElseIf control Is Nothing Then
                Debug.Fail("PropPageUserControlBase::InitPage(): Unexpected null control value")
            End If

        End Sub

        Public Overridable Function GetControlValue() As Object
            Dim prop As PropertyDescriptor = Me.PropDesc
            If prop Is Nothing Then
                Return GetControlValue(Me.FormControl, Nothing)
            Else
                Return GetControlValue(Me.FormControl, prop.Converter)
            End If
        End Function


        ''' <summary>
        ''' Retrieves all current values of a property, for controls which support multiple-value get.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetControlValueMultipleValues() As Object()
            'Get the current values from the UI
            Dim values() As Object = Nothing

            If Me.MultiValueGetCallback IsNot Nothing Then
                If Me.MultiValueGetCallback(Me.FormControl, Me.PropDesc, values) Then
                    Debug.Assert(values IsNot Nothing, "MultiValueGetCallback must return a valid array")
                    Debug.Assert(values.Length = RawPropertiesObjects.Length, "MultiValueGetCallback must return an array of the same length as RawPropertiesObjects")
                    Return values
                End If
            End If

            Dim value As Object = GetControlValue()
            Dim RawObjects() As Object = RawPropertiesObjects
            ReDim values(RawObjects.Length - 1)
            For i As Integer = 0 To RawObjects.Length - 1
                values(i) = value
            Next

            Return values
        End Function


        ''' <summary>
        ''' Retrieves the current value of the property's from its UI control (i.e., the value that 
        '''   the user has set in the control on the property page)
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="_TypeConverter"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetControlValue(ByVal control As Control, ByVal _TypeConverter As TypeConverter) As Object

            Dim StringText As String
            Dim value As Object = Nothing
            Dim convertedvalue As Object

            Debug.Assert(MultiValueGetCallback Is Nothing, "GetControlValue doesn't support MultiValueGetCallback")

            If Me.GetCallback IsNot Nothing Then
                If Me.GetCallback(Me.FormControl, Me.PropDesc, value) Then
                    Return value
                End If
            End If

            value = Nothing

            If TypeOf control Is System.Windows.Forms.TextBox Then
                StringText = Trim(DirectCast(control, System.Windows.Forms.TextBox).Text)
                'IF textbox emptied, treat as indeterminate when using multiple objects

                Debug.Assert(Not ExtendedPropertiesObjects Is Nothing, "ExtendedPropertiesObjects was null!")
                If ExtendedPropertiesObjects IsNot Nothing Then
                    If ExtendedPropertiesObjects.Length > 1 _
                    AndAlso TypeOf InitialValue Is String _
                    AndAlso Not Me.IsDirty _
                    AndAlso Not Me.IsCommonProperty _
                    Then
                        'We are showing multiple configurations, and the text has not been changed by the user 
                        '  - return indeterminate value
                        value = PropertyControlData.Indeterminate
                    Else
                        value = StringText
                    End If
                Else
                    ' extended objects was null, default to stringText
                    value = StringText
                End If

            ElseIf TypeOf control Is System.Windows.Forms.ComboBox Then
                Dim cbx As ComboBox = DirectCast(control, System.Windows.Forms.ComboBox)

                If cbx.DropDownStyle = ComboBoxStyle.DropDownList Then
                    If cbx.SelectedIndex = -1 Then
                        value = PropertyControlData.Indeterminate
                    Else
                        value = cbx.SelectedItem
                    End If
                Else
                    StringText = Trim(cbx.Text)
                    Debug.Assert(ExtendedPropertiesObjects IsNot Nothing, "ExtendedPropertiesObjects was null!")
                    If ExtendedPropertiesObjects IsNot Nothing Then
                        If ExtendedPropertiesObjects.Length > 1 _
                        AndAlso TypeOf InitialValue Is String _
                        AndAlso Not Me.IsDirty _
                        AndAlso Not Me.IsCommonProperty _
                        Then
                            'We are showing multiple configurations, and the text has not been changed by the user 
                            '  - return indeterminate value
                            value = PropertyControlData.Indeterminate
                        Else
                            value = StringText
                        End If
                    Else
                        ' extended objects was null, default to stringText
                        value = StringText
                    End If
                End If

            ElseIf TypeOf control Is System.Windows.Forms.CheckBox Then
                Dim chk As CheckBox = DirectCast(control, System.Windows.Forms.CheckBox)

                If chk.CheckState = CheckState.Indeterminate Then
                    value = PropertyControlData.Indeterminate
                Else
                    value = chk.Checked
                End If

            ElseIf TypeOf control Is System.Windows.Forms.Label Then
                'Labels don't get values edited
                Debug.Assert(False, "Labels should be ReadOnly and never changed")

            ElseIf control Is Nothing Then
                Debug.Fail("PropPageUserControlBase.InitPage(): control is Nothing")

            End If

            If (value IsNot Nothing) And (Not value Is PropertyControlData.Indeterminate) Then
                If (_TypeConverter IsNot Nothing) Then
                    If _TypeConverter.CanConvertFrom(Nothing, value.GetType()) Then
                        'If Not _TypeConverter.IsValid(value) AndAlso _
                        '_TypeConverter.GetStandardValuesSupported Then
                        'convertedvalue = _TypeConverter.ConvertFrom(value)
                        'value = convertedvalue
                        If _TypeConverter.IsValid(value) Then
                            convertedvalue = _TypeConverter.ConvertFrom(value)
                            value = convertedvalue
                        End If
                    End If
                End If
            End If
            Return value

        End Function

        Public Function GetControlValueNative() As Object
            Dim prop As PropertyDescriptor = Me.PropDesc
            If prop Is Nothing Then
                Return GetControlValue(Me.FormControl, Nothing)
            Else
                Return GetControlValue(Me.FormControl, prop.Converter)
            End If
        End Function


        ''' <summary>
        ''' Given a list of extenders, returns a single value for the current value of the property.  If all of the
        '''   extenders return the same value, the return value of the function is that single value.  If any of the values
        '''   differ, then PropertyControlData.Indeterminate is returned.  If the property descriptor is missing,
        '''   PropertyControlData.MissingProperty is returned.
        ''' </summary>
        ''' <param name="Extenders">The list of extenders to pass to the descriptor's GetValue function</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function TryGetPropertyValueNative(ByVal Extenders As Object()) As Object
            If IsCommonProperty Then
                Try
                    Return GetCommonPropertyValueNative(Me.PropDesc, CommonPropertiesObject)
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Return PropertyControlData.MissingProperty
                End Try
            Else
                Return TryGetNonCommonPropertyValueNative(Me.PropDesc, Extenders)
            End If
        End Function


        ''' <summary>
        ''' Given a list of extenders, returns a single value for the current value of the property.  If all of the
        '''   extenders return the same value, the return value of the function is that single value.  If any of the values
        '''   differ, then PropertyControlData.Indeterminate is returned.  If the property descriptor is missing,
        '''   PropertyControlData.MissingProperty is returned.
        ''' </summary>
        ''' <param name="Descriptor">The property descriptor for the property to get the value from</param>
        ''' <param name="Extenders">The list of extenders to pass to the descriptor's GetValue function</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function TryGetNonCommonPropertyValueNative(ByVal Descriptor As PropertyDescriptor, ByVal Extenders As Object()) As Object
            Dim Value As Object = Nothing

            If Descriptor Is Nothing Then
                Return MissingProperty
            End If

            If Extenders.Length = 1 Then
                Try
                    Value = GetNonCommonPropertyValueNative(Descriptor, Extenders(0))
                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Value = PropertyControlData.MissingProperty
                End Try
            Else
                Dim AllValues() As Object = Nothing

                'We don't care about the AllValues return value, just Value, which will be either a single value or Indeterminate
                GetAllPropertyValuesNative(Descriptor, Extenders, AllValues, Value)
            End If

            Return Value
        End Function


        ''' <summary>
        ''' Given a list of extenders, returns the property values for this property against each of the
        '''   extenders.  Also returns a single value (the equivalent of a call to TryGetPropertyValueNative(), but
        '''   more efficiently than calling both of these functions).
        ''' </summary>
        ''' <param name="Extenders">The list of extenders to pass to the descriptor's GetValue function</param>
        ''' <param name="Values">[out] An array containing the value for the property using each of the extenders.</param>
        ''' <param name="ValueOrIndeterminate">[out] A single value representing the combination of all values in Values.  If all Values are the same,
        '''   that value is returned.  If they differ, PropertyControlData.Inderminate is returned.  If the property descriptor is missing,
        '''   PropertyControlData.MissingProperty is returned.
        ''' </param>
        ''' <remarks></remarks>
        Public Overridable Sub GetAllPropertyValuesNative(ByVal Extenders As Object(), ByRef Values As Object(), ByRef ValueOrIndeterminate As Object)
            GetAllPropertyValuesNative(Me.PropDesc, Extenders, Values, ValueOrIndeterminate)
        End Sub


        ''' <summary>
        ''' Given a list of extenders, returns the property values for this property against each of the
        '''   extenders.  Also returns a single value (the equivalent of a call to TryGetPropertyValueNative(), but
        '''   more efficiently than calling both of these functions).
        ''' </summary>
        ''' <param name="Descriptor">The property descriptor for the property to get the value from</param>
        ''' <param name="Extenders">The list of extenders to pass to the descriptor's GetValue function</param>
        ''' <param name="Values">[out] An array containing the value for the property using each of the extenders.</param>
        ''' <param name="ValueOrIndeterminate">[out] A single value representing the combination of all values in Values.  If all Values are the same,
        '''   that value is returned.  If they differ, PropertyControlData.Inderminate is returned.  If the property descriptor is missing,
        '''   PropertyControlData.MissingProperty is returned.
        ''' </param>
        ''' <remarks></remarks>
        Public Shared Sub GetAllPropertyValuesNative(ByVal Descriptor As PropertyDescriptor, ByVal Extenders As Object(), ByRef Values As Object(), ByRef ValueOrIndeterminate As Object)
            Values = Nothing
            ValueOrIndeterminate = Nothing

            If Extenders Is Nothing Then
                Throw New ArgumentNullException("Extenders")
            End If

            Dim ReturnValues As Object() = New Object(Extenders.Length - 1) {}

            If Descriptor Is Nothing Then
                Debug.Fail("Why is GetAllPropertyValues() being called for a missing property?")
                ValueOrIndeterminate = MissingProperty
                Values = ReturnValues 'Return the array of Nothing values (defensive)
                Return
            End If

            Try
                For i As Integer = 0 To Extenders.Length - 1
                    ReturnValues(i) = GetNonCommonPropertyValueNative(Descriptor, Extenders(i))
                Next
            Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                Values = New Object(Extenders.Length - 1) {}
                ValueOrIndeterminate = PropertyControlData.MissingProperty
                Return
            End Try

            'Determine if all the values are the same or not
            ValueOrIndeterminate = GetValueOrIndeterminateFromArray(ReturnValues)
            Values = ReturnValues
        End Sub


        ''' <summary>
        ''' Given an array of property values, return the single value which is shared by them all (if they're all the same),
        '''   or else return Indeterminate
        ''' </summary>
        ''' <param name="Values"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetValueOrIndeterminateFromArray(ByVal Values() As Object) As Object
            'Determine if all the values are the same or not
            If Values Is Nothing OrElse Values.Length = 0 Then
                Debug.Fail("Bad Values array")
                Throw New ArgumentNullException("Values")
            End If

            Dim Value As Object = Values(0)
            For i As Integer = 0 + 1 To Values.Length - 1
                'Perform object comparison
                If (Value Is Nothing OrElse Values(i) Is Nothing) Then
                    If Value IsNot Values(i) Then
                        Return PropertyControlData.Indeterminate
                    End If
                ElseIf Not Value.Equals(Values(i)) Then
                    Return PropertyControlData.Indeterminate
                End If
            Next

            'They were all the same
            Return Value
        End Function
#End Region

#Region "Property getter/setter helpers"


        ''' <summary>
        ''' Retrieves the current value of this property (from the project system, not the current value
        '''   visible in the property page's control, which the user may have edited).
        ''' No type conversion takes place.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function GetPropertyValueNative(ByVal Extender As Object) As Object
            Debug.Assert(PropDesc IsNot Nothing, "Calling GetPropertyValueNative() on a property that could not be found [PropDesc Is Nothing]")

            If Me.IsCommonProperty Then
                Return GetCommonPropertyValueNative()
            End If

            Return GetNonCommonPropertyValueNative(PropDesc, Extender)
        End Function


        ''' <summary>
        ''' Retrieves the current value of a property (from the project system, not the current value
        '''   visible in the property page's control, which the user may have edited).
        ''' No type conversion takes place.
        ''' </summary>
        ''' <param name="Descriptor">The type descriptor to get the property value from.</param>
        ''' <param name="Extender">The extender objects to use.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Shared Function GetNonCommonPropertyValueNative(ByVal Descriptor As PropertyDescriptor, ByVal Extender As Object) As Object
            Debug.Assert(Descriptor IsNot Nothing, "Calling GetPropertyValueNative() on a property that could not be found [Descriptor Is Nothing]")
            Debug.Assert(Extender IsNot Nothing)

            'Note: For property get we must pass in the extended object as the component.
            Return Descriptor.GetValue(Extender)
        End Function


        ''' <summary>
        ''' Sets the current value of this property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is first converted using the property's type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' <remarks></remarks>
        Public Overridable Sub SetPropertyValue(ByVal Value As Object)
            m_PropPage.SuspendPropertyChangeListening(Me.DispId)
            Try

                If Value Is PropertyControlData.Indeterminate Then
                    'Don't set any values
                    Return
                End If

                If Me.IsUserPersisted Then
                    Me.WriteUserDefinedProperty(Me.PropertyName, Value)

                Else
                    Dim _TypeConverter As TypeConverter = Nothing

                    If Me.PropDesc IsNot Nothing Then
                        _TypeConverter = Me.PropDesc.Converter
                    End If

                    If (_TypeConverter IsNot Nothing) AndAlso _TypeConverter.GetStandardValuesSupported Then
                        Value = _TypeConverter.ConvertFrom(Value)
                    End If

                    If Me.IsCommonProperty Then
                        SetCommonPropertyValueNative(Value)
                    Else
                        SetNonCommonPropertyValueCore(Value)
                    End If
                End If
            Finally
                m_PropPage.ResumePropertyChangeListening(Me.DispId)

                If RefreshAllPropertiesWhenChanged Then
                    m_PropPage.RefreshPropertyValues()
                End If

            End Try
        End Sub


        ''' <summary>
        ''' Sets the current value of this property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is not converted using any type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' <remarks></remarks>
        Public Overridable Sub SetPropertyValueNative(ByVal Value As Object)
            m_PropPage.SuspendPropertyChangeListening(Me.DispId)

            Try
                If Value Is PropertyControlData.Indeterminate Then
                    'Nothing to do here
                    'Indeterminate values are when the multiple configs are being edited
                    'and the values are different for each config
                    Debug.Fail("Trying to set Indeterminate property value - should we ever reach this code path with multi-value undo/redo available?")
                Else
                    If Me.IsUserPersisted Then
                        Me.WriteUserDefinedProperty(Me.PropertyName, Value)

                    ElseIf Me.IsCommonProperty Then
                        SetCommonPropertyValueNative(Value)
                    Else
                        SetNonCommonPropertyValueCore(Value)
                    End If

                End If

            Finally
                m_PropPage.ResumePropertyChangeListening(Me.DispId)

                If RefreshAllPropertiesWhenChanged Then
                    m_PropPage.RefreshPropertyValues()
                End If

            End Try
        End Sub

        Protected Overridable Sub SetNonCommonPropertyValueCore(ByVal Value As Object)
            Dim Objects As Object() = RawPropertiesObjects
            For i As Integer = 0 To Objects.Length - 1
                'Note: we pass in the raw object instead of the extended object to SetValue because a) for extended property descriptors 
                '  (see AutomationExtenderManager), this argument is not used anyway, and b) for non-extended property descriptors, we must 
                '  pass in the raw object due to the current implementation of Com2PropertyDescriptor.
                '  (For property get we must pass in the extended object).
                PropertyDescriptorSetValue(PropDesc, Objects(i), Value)
            Next
        End Sub

        ''' <summary>
        ''' A version of SetPropertyValueNative that works with multiple values (one value for each
        '''   passed-in object/configuration).  Used during undo/redo.
        ''' </summary>
        ''' <param name="Objects">The objects to set the values on</param>
        ''' <param name="Values">The values to set into the property for each of the objects (configurations)</param>
        ''' <remarks></remarks>
        Public Overridable Sub SetPropertyValueNativeMultipleValues(ByVal Objects As Object(), ByVal Values As Object())
            If Me.IsUserPersisted Then
                'No need to handle this case, it is not currently supported
                Debug.Fail("NYI: User-persisted multiple-value undo/redo")
                Exit Sub
            End If
            If Me.IsCommonProperty Then
                'No need to handle this case, it should not be called for non-config-dependent properties
                Debug.Fail("Shouldn't be setting multiple-config values for common properties")
                Throw New InvalidOperationException
            End If
            If Objects Is Nothing OrElse Values Is Nothing OrElse Objects.Length <> Values.Length Then
                Throw AppDesCommon.CreateArgumentException("Objects")
            End If

            m_PropPage.SuspendPropertyChangeListening(Me.DispId)
            Try
                SetNonCommonPropertyValueMultipleValuesCore(Objects, Values)
            Finally
                m_PropPage.ResumePropertyChangeListening(Me.DispId)

                If Me.RefreshAllPropertiesWhenChanged Then
                    m_PropPage.RefreshPropertyValues()
                End If
            End Try
        End Sub

        Protected Overridable Sub SetNonCommonPropertyValueMultipleValuesCore(ByVal Objects As Object(), ByVal Values As Object())
            For i As Integer = 0 To Objects.Length - 1
                'Note: we pass in the raw object instead of the extended object to SetValue because a) for extended property descriptors 
                '  (see AutomationExtenderManager), this argument is not used anyway, and b) for non-extended property descriptors, we must 
                '  pass in the raw object due to the current implementation of Com2PropertyDescriptor.
                '  (For property get we must pass in the extended object).
                PropertyDescriptorSetValue(Me.PropDesc, Objects(i), Values(i))
            Next
        End Sub


        ''' <summary>
        ''' The PropertyDescriptor implementations ignore checkout cancellation exceptions
        '''   (i.e., when the user has explicitly cancelled a checkout) and simply do
        '''   a no-op with the exception caught and not rethrown.  This method does a
        '''   PropertyDescriptor.SetValue() and if the value was not changed, it throws
        '''   a user canceled exception so that the project designer can cancel the
        '''   current transaction.
        ''' </summary>
        ''' <param name="Descriptor">The property descriptor to invoke SetValue on</param>
        ''' <param name="Component">The component whose property should be changed</param>
        ''' <param name="Value">The new value to set the given property to</param>
        ''' <remarks></remarks>
        Protected Shared Sub PropertyDescriptorSetValue(ByVal Descriptor As PropertyDescriptor, ByVal Component As Object, ByVal Value As Object)
            Dim Helper As New PropertyDescriptorSetValueHelper
            Helper.SetValue(Descriptor, Component, Value)
        End Sub


        ''' <summary>
        ''' Helper class for PropertyDescriptorSetValue - detects if a PropertyDescriptor.SetValue
        '''   fails due to a canceled checkout...
        ''' </summary>
        ''' <remarks></remarks>
        Private Class PropertyDescriptorSetValueHelper
            Private m_ValueChangedWasFired As Boolean

            ''' <summary>
            ''' Performs a SetValue on the given component, descriptor and new value.  Throws if there
            '''   is any exception, including if the user cancels the checkout.
            ''' </summary>
            ''' <param name="Descriptor"></param>
            ''' <param name="Component"></param>
            ''' <param name="Value"></param>
            ''' <remarks></remarks>
            Public Sub SetValue(ByVal Descriptor As PropertyDescriptor, ByVal Component As Object, ByVal Value As Object)
                'Hook up to detect if ValueChanged is fired
                Descriptor.AddValueChanged(Component, AddressOf ValueChanged)

                m_ValueChangedWasFired = False
                Try
                    'Go ahead and do the SetValue.  It will throw if there's an exception
                    '  (other than cancel/checkout exceptions).
                    Descriptor.SetValue(Component, Value)

                    'If we made it here, either the value was successfully changed, or
                    '  the set was canceled by the user in a checkout dialog, etc.
                    If m_ValueChangedWasFired Then
                        'The set was successful
                        Return
                    Else
                        'Must have been canceled by the user.  There will already have been
                        '  a UI indication of the cancelation.  But we need to throw so that the
                        '  current transaction gets cancelled (otherwise the property page may
                        '  show a changed value, and the user may get prompted for additional
                        '  checkouts as we try to process more property changes).
                        Common.Switches.TracePDProperties(TraceLevel.Warning, "*** SetValue() did not return failure, but also did not fire OnValueChanged.  Assuming a checkout or other cancel occurred.")
                        Throw CheckoutException.Canceled
                    End If

                Finally
                    Descriptor.RemoveValueChanged(Component, AddressOf ValueChanged)
                    m_ValueChangedWasFired = False
                End Try
            End Sub

            ''' <summary>
            ''' Helper function for PropertyDescriptorSetValue - handles the OnValueChanged
            '''   event for the property descriptor as it's being set.
            ''' </summary>
            ''' <remarks></remarks>
            Private Sub ValueChanged(ByVal sender As Object, ByVal e As EventArgs)
                Debug.Assert(Not m_ValueChangedWasFired, "OnValueChanged() fired multiple times?")
                m_ValueChangedWasFired = True
            End Sub

        End Class


        ''' <summary>
        ''' Returns true if the given property supports returning and setting multiple values at the same time in order to support
        '''   Undo and Redo operations when multiple configurations are selected by the user.  This function should always return the
        '''   same value for a given property (i.e., it does not depend on whether multiple configurations have currently been passed in
        '''   to SetObjects, but simply whether this property supports multiple-value undo/redo).
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function SupportsMultipleValueUndo() As Boolean
            If Me.IsUserPersisted Then
                'Not currently supported: User-persisted properties that have per-config values
                Return False
            End If

            If Not Me.IsConfigurationSpecificProperty Then
                'No need to support multi-value undo for non-configuration-specific properties, since they 
                '  have only a single value for them for all configs
                Return False
            End If
            Debug.Assert(PropPage.IsConfigurationSpecificPage)

            'Otherwise, yes, we support them.
            Return True
        End Function

#End Region

#Region "Common property getter/setter helpers"


        '----
        ' About 'common' properties
        '----
        '
        '  "Common" properties are properties which are not given to the page through SetObjects
        '  but rather are picked up from the project itself.  For non-configuration-specific
        '  property pages, the notion of "common" properties doesn't really mean anything - the
        '  common properties are the ones that were passed in (configuration-specific properties
        '  are not accessible to a non-configuration-specific page).
        '  But for a configuration-specific page, only the properties which are configuration-specific 
        '  are passed in.  Sometimes a page needs to access non-configuration-specific ("common")
        '  properties (an example is the VB Compile page, which access properties like "Option Explicit"
        '  which is not configuration-specific, and many pages also access the "FullPath" property via
        '  PropPageUserControlBase.ProjectPath().  These properties are pulled directly from the project system
        '  rather than taken from the objects passed in to the property page.



        ''' <summary>
        ''' Returns the PropertyDescriptor of a common property using the name of the property
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' See comments "About 'common' properties"
        ''' </remarks>
        Private Function GetCommonPropertyDescriptor(ByVal PropertyName As String) As PropertyDescriptor
            Return m_PropPage.GetCommonPropertyDescriptor(PropertyName)
        End Function


        ''' <summary>
        ''' Retrieves the current value of this property in the project (not the current value in the 
        '''   control on the property page as it has been edited by the user).
        ''' No type conversion is performed on the return value.
        ''' </summary>
        ''' <param name="Descriptor">The descriptor for the common property.</param>
        ''' <param name="ProjectCommonPropertiesObject">The project object from which to query the property value.</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This must be a common property.
        ''' See comments "About 'common' properties"
        ''' </remarks>
        Public Shared Function GetCommonPropertyValueNative(ByVal Descriptor As PropertyDescriptor, ByVal ProjectCommonPropertiesObject As Object) As Object
            Debug.Assert(Descriptor IsNot Nothing, "Calling GetCommonPropertyValue() on a property that could not be found [Descriptor Is Nothing]")
            Debug.Assert(ProjectCommonPropertiesObject IsNot Nothing)
            Return Descriptor.GetValue(ProjectCommonPropertiesObject)
        End Function


        ''' <summary>
        ''' Retrieves the current value of this property in the project (not the current value in the 
        '''   control on the property page as it has been edited by the user).
        ''' No type conversion is performed on the return value.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' This must be a common property.
        ''' See comments "About 'common' properties"
        ''' </remarks>
        Private Function GetCommonPropertyValueNative() As Object
            Debug.Assert(PropDesc IsNot Nothing, "Calling GetCommonPropertyValue() on a property that could not be found [PropDesc Is Nothing]")
            Debug.Assert(IsCommonProperty)
            Return GetCommonPropertyValueNative(PropDesc, Me.CommonPropertiesObject)
        End Function


        ''' <summary>
        ''' Retrieves the current value of this property in the project (not the current value 
        '''   in the control on the property page as it has been edited by the user).
        ''' The value retrieved is converted using the property's type converter.
        ''' </summary>
        ''' <param name="Descriptor">The descriptor for the common property.</param>
        ''' <param name="ProjectCommonPropertiesObject">The project object from which to query the property value.</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' See comments "About 'common' properties"
        ''' </remarks>
        Public Shared Function GetCommonPropertyValue(ByVal Descriptor As PropertyDescriptor, ByVal ProjectCommonPropertiesObject As Object) As Object
            Debug.Assert(Descriptor IsNot Nothing, "Calling GetCommonPropertyValue() on a property that could not be found [Descriptor Is Nothing]")
            Debug.Assert(ProjectCommonPropertiesObject IsNot Nothing)

            Dim _TypeConverter As TypeConverter
            Dim value As Object

            _TypeConverter = Descriptor.Converter

            value = GetCommonPropertyValueNative(Descriptor, ProjectCommonPropertiesObject)
            If _TypeConverter IsNot Nothing Then
                If _TypeConverter.GetStandardValuesSupported Then
                    value = _TypeConverter.ConvertToString(value)
                End If
            End If
            Return value
        End Function


        ''' <summary>
        ''' Retrieves the current value of this property in the project (not the current value 
        '''   in the control on the property page as it has been edited by the user).
        ''' The value retrieved is converted using the property's type converter.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' See comments "About 'common' properties"
        ''' </remarks>
        Private Function GetCommonPropertyValue() As Object
            Debug.Assert(PropDesc IsNot Nothing, "Calling GetCommonPropertyValue() on a property that could not be found [PropDesc Is Nothing]")
            Debug.Assert(IsCommonProperty)
            Return GetCommonPropertyValue(PropDesc, Me.CommonPropertiesObject)
        End Function


        ''' <summary>
        ''' Sets the current value of this property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is not converted using any type converter.
        ''' </summary>
        ''' <param name="Value"></param>
        ''' <remarks>
        ''' The property must be a common property.
        ''' ''' See comments "About 'common' properties".
        ''' </remarks>
        Private Sub SetCommonPropertyValueNative(ByVal Value As Object)
            Debug.Assert(PropDesc IsNot Nothing, "Calling SetCommonPropertyValueNative() on a property that could not be found [PropDesc Is Nothing]")
            Debug.Assert(IsCommonProperty, "Should not call PropertyControlData.SetCommonPropertyValueNative on a non-common property")
            Debug.Assert(m_PropPage IsNot Nothing)

            m_PropPage.SuspendPropertyChangeListening(DispId)
            Try
                SetCommonPropertyValueNative(PropDesc, Value, CommonPropertiesObject)
            Finally
                m_PropPage.ResumePropertyChangeListening(DispId)
            End Try
        End Sub


        ''' <summary>
        ''' Sets the current value of a common property (into the project system, not into the 
        '''   control on the property page).
        ''' The value is first converted into native format using the property's type converter.
        ''' </summary>
        ''' <param name="Descriptor">The descriptor for the common property.</param>
        ''' <param name="Value">The value to be set into the property (after converting it to native format)</param>
        ''' <param name="ProjectCommonPropertiesObject">The project object from which to query the property value.</param>
        ''' <remarks>
        ''' Must be a common property.
        ''' See "About 'common' properties" in PropertyControlData for information on "common" properties.
        ''' Does *not* suspend property change listening for this property
        ''' </remarks>
        Public Shared Sub SetCommonPropertyValueNative(ByVal Descriptor As PropertyDescriptor, ByVal Value As Object, ByVal ProjectCommonPropertiesObject As Object)
            Debug.Assert(Descriptor IsNot Nothing)
            Debug.Assert(ProjectCommonPropertiesObject IsNot Nothing)

            'Note: we pass in the raw object instead of the extended object to SetValue because a) for extended property descriptors 
            '  (see AutomationExtenderManager), this argument is not used anyway, and b) for non-extended property descriptors, we must 
            '  pass in the raw object due to the current implementation of Com2PropertyDescriptor.
            '  (For property get we must pass in the extended object).
            PropertyDescriptorSetValue(Descriptor, ProjectCommonPropertiesObject, Value)
        End Sub



#End Region


        ''' <summary>
        ''' Restore the initial value of a property into the property's control (or user-persisted store).
        '''   This is done after a non-immmediate (child) property page is canceled in order to restore
        '''   the page's original values into the control.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub RestoreInitialValue()
            If Not Me.IsDirty Then
                Return
            End If

            InitPropertyUI()
            Me.IsDirty = False
        End Sub


        ''' <summary>
        ''' Called to re-retrieve a property's current value and updated its UI with that new value when it has been 
        '''   changed by code other than direct manipulation by the user via the property page.
        ''' </summary>
        ''' <remarks>
        ''' The default implementation calls InitPropertyValue and InitPropertyUI on the property control data.
        ''' </remarks>
        Public Overridable Sub RefreshValue()
            Common.Switches.TracePDProperties(TraceLevel.Info, "*** " & PropertyName & ": Updating property from current value")

            InitPropertyValue()
            InitPropertyUI()
            Me.IsDirty = False
        End Sub


        ''' <summary>
        ''' Marks the property as dirty, so that it will get updated the next time the property 
        '''   page containing this property is applied.
        ''' </summary>
        ''' <param name="ReadyToApply">If True, the change is applied immediately (if in immediate apply mode).  If False, the change is
        '''   not applied immediately, but rather it will be applied later, on the next apply.  If multiple properties are going to be set
        '''   dirty, only the last SetDirty() call should pass in True for ReadyToApply.  Doing it this way will cause all of the properties
        '''   to be changed in the same undo/redo transaction (apply batches up all current changes into a single transaction).
        ''' </param>
        ''' <remarks></remarks>
        Protected Sub SetDirty(ByVal ReadyToApply As Boolean)
            Me.IsDirty = True
            m_PropPage.SetDirty(ReadyToApply)
        End Sub

#Region "Control event handlers for derived form"
        ''' <summary>
        ''' Updates the dirty state when a text change has been made.  
        ''' ImmediateApply will only be done if the textbox has lost focus.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Control_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs)
            'We don't want to apply change while the user might still be typing in the textbox, therefore
            '  we use ReadyToApply:=False
            SetDirty(False)
        End Sub

        ''' <summary>
        ''' Updates the dirty state when a text change has been made by the user typing into a combobox's edit box
        ''' ImmediateApply will only be done if the combobox has lost focus.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Control_TextUpdated(ByVal sender As Object, ByVal e As System.EventArgs)
            'We don't want to apply change while the user might still be typing in the textbox, therefore
            '  we use ReadyToApply:=False
            SetDirty(False)
        End Sub

        ''' <summary>
        ''' Commits the change when the control has lost focus (including by switching to
        '''   a tool window, etc), if ImmediateApply is true.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Control_LostFocus(ByVal sender As Object, ByVal e As System.EventArgs)
            'If the user leaves the property page to, say, a tool window, we will receive the 
            '  notification here.  If the page is dirty (s/he has typed something into the 
            '  textbox previously), then go ahead and commit the changes (if in immediate apply).
            If Me.IsDirty() Then
                SetDirty(True)
            End If
        End Sub

        ''' <summary>
        ''' Causes an apply if dirtied
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub Control_Validated(ByVal sender As Object, ByVal e As System.EventArgs)
            If Me.IsDirty Then
                SetDirty(True)
            End If
        End Sub

        ''' <summary>
        ''' Updates the dirty state when user changes selection in a listbox or combobox
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub ComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs)
            SetDirty(True)
        End Sub

        ''' <summary>
        ''' Called when a combobox's list is dropped down.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub ComboBox_DropDown(ByVal sender As Object, ByVal e As EventArgs)
            'We need to make sure the drop-down list is wide enough for its contents
            AppDesCommon.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub CheckBox_CheckStateChanged(ByVal sender As Object, ByVal e As System.EventArgs)
            SetDirty(True)
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <remarks></remarks>
        Public Overridable Sub AddChangeHandlers()

            If (Me.FormControl IsNot Nothing) AndAlso Not Me.IsUserHandledEvents Then
                If TypeOf Me.FormControl Is TextBox Then
                    AddHandler Me.FormControl.TextChanged, AddressOf Control_TextChanged
                    AddHandler Me.FormControl.LostFocus, AddressOf Control_LostFocus
                ElseIf TypeOf Me.FormControl Is ComboBox Then
                    With CType(Me.FormControl, ComboBox)
                        AddHandler .SelectionChangeCommitted, AddressOf ComboBox_SelectionChangeCommitted
                        If .DropDownStyle <> ComboBoxStyle.DropDownList Then
                            AddHandler .TextUpdate, AddressOf Control_TextUpdated
                            AddHandler Me.FormControl.LostFocus, AddressOf Control_LostFocus
                        End If
                        If .DropDownStyle <> ComboBoxStyle.Simple Then
                            AddHandler .DropDown, AddressOf ComboBox_DropDown
                        End If
                    End With
                ElseIf TypeOf Me.FormControl Is CheckBox Then
                    AddHandler CType(Me.FormControl, CheckBox).CheckStateChanged, AddressOf CheckBox_CheckStateChanged
                End If
                'Monitor all Validate events - used for immediate Apply
                AddHandler CType(Me.FormControl, Control).Validated, AddressOf Control_Validated
            End If

        End Sub


#End Region

        ''' <summary>
        ''' Flushes user changes to the underlying project system properties.
        ''' </summary>
        ''' <remarks>Properties are only flushed if they are marked as dirty.</remarks>
        Public Overridable Sub ApplyChanges()
            Dim value As Object = Nothing
            Dim values() As Object = Nothing 'Set to non-Nothing if a multiple-value get was accomplished
            Dim handled As Boolean

            If Me.IsDirty Then
                Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.ApplyChanges: " & Me.PropertyName)

                If (Me.PropDesc IsNot Nothing) AndAlso (Not Me.PropDesc.IsReadOnly) AndAlso (Not Me.IsMissing) Then
                    'Get the current value from the UI
                    If Me.MultiValueGetCallback IsNot Nothing Then
                        handled = Me.MultiValueGetCallback(Me.FormControl, Me.PropDesc, values)
                        If handled Then
                            Debug.Assert(values IsNot Nothing, "MultiValueGetCallback must return a valid array")
                            Debug.Assert(values.Length = RawPropertiesObjects.Length, "MultiValueGetCallback must return an array of the same length as RawPropertiesObjects")
                        End If
                    End If

                    If Not handled Then
                        If Me.GetCallback IsNot Nothing Then 'CONSIDER: should just always call GetControlValueNative(), which will handle GetCallback
                            handled = Me.GetCallback(Me.FormControl, Me.PropDesc, value)
                        Else
                            value = Me.GetControlValueNative()
                            handled = True
                        End If
                    End If

                    If handled Then
#If DEBUG Then
                        If values IsNot Nothing Then
                            Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.ApplyChanges: " & Me.PropertyName & ": NEW VALUES (multi-value apply):")
                            For i As Integer = 0 To values.Length - 1
                                Common.Switches.TracePDProperties(TraceLevel.Info, "  New Value #" & i & ": " & Common.DebugToString(values(i)))
                            Next
                        Else
                            Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.ApplyChanges: " & Me.PropertyName & ": NEW VALUE = " & Common.DebugToString(value))
                        End If
#End If

                        OnPropertyChanging() 'Required for Undo support

                        'Set the value into the project
                        If values IsNot Nothing Then
                            Me.SetPropertyValueNativeMultipleValues(RawPropertiesObjects, values)
                        Else
                            Me.SetPropertyValueNative(value)
                        End If

                        If m_PropPage.ProjectReloadedDuringCheckout Then
                            'If the project was reloaded (e.g. while setting the
                            '  target framework property), we need to exit ASAP.
                            '  Many operations we try to do after this point will fail badly.
                            Throw New ProjectReloadedException()
                        End If

                        OnPropertyChanged(Me.InitialValue, value) 'Required for Undo support

                        'Change the current stored baseline value of the property
                        If values IsNot Nothing Then
                            Me.SetInitialValues(values)
                        Else
                            Me.SetInitialValues(value)
                        End If
                    Else
                        Debug.Fail(Me.PropertyName & " not handled")
                    End If
                    Me.IsDirty = False

                ElseIf Me.PropDesc Is Nothing AndAlso Me.GetCallback IsNot Nothing Then
                    handled = Me.GetCallback(Me.FormControl, Nothing, value)
                    'ignore 'handled' and 'value'

                ElseIf Me.PropDesc Is Nothing Then
                    If Me.FormControl IsNot Nothing Then
                        Me.FormControl.Enabled = False
                    End If
                    Debug.Fail("PropertyDescriptor for '" & Me.PropertyName & "' cannot be found.")
                Else
                    Common.Switches.TracePDProperties(TraceLevel.Info, "  " & Me.PropertyName & " is ReadOnly or IsMissing and cannot be changed")
                End If
            Else
                'Common.Switches.TracePDProperties(TraceLevel.Info, "PropertyControlData.ApplyChanges: " & Me.PropertyName & " has not been changed - skipping")
            End If

        End Sub


        ''' <summary>
        ''' Calls OnPropertyChanging on the PropPageUserControlBase for this property.  This is required for Undo/Redo
        '''   support.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overridable Sub OnPropertyChanging()
            m_PropPage.OnPropertyChanging(Me.PropertyName, Me.PropDesc)
        End Sub


        ''' <summary>
        ''' Calls OnPropertyChanged on the PropPageUserControlBase for this property.  This is required for Undo/Redo
        '''   support.
        ''' </summary>
        ''' <param name="OldValue">The property's previous value.</param>
        ''' <param name="NewValue">The property's new value.</param>
        ''' <remarks></remarks>
        Protected Overridable Sub OnPropertyChanged(ByVal OldValue As Object, ByVal NewValue As Object)
            m_PropPage.OnPropertyChanged(Me.PropertyName, Me.PropDesc, OldValue, NewValue)
        End Sub


        ''' <summary>
        ''' Returns a list of files which should be checked out before trying to change this property's
        '''   value.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function FilesToCheckOut() As String()
            Const PERUSER_EXTENSION As String = ".user" 'Project .user file extension

            'Most properties are stored in the project file, so that's what we return by default.
            If m_PropPage Is Nothing OrElse m_PropPage.DTEProject Is Nothing OrElse m_PropPage.m_ObjectsPropertyDescriptorsArray Is Nothing OrElse m_PropPage.m_ExtendedObjects Is Nothing Then
                Debug.Fail("PropertyControlData.FilesToCheckOut: can't determine files to check out because of an uninitialized field")
            Else
                'CONSIDER: we should be allowing multiple of these flags to be set at once
                If (Me.Flags And ControlDataFlags.PersistedInProjectUserFile) <> 0 Then
                    '.user file
                    Return New String() {m_PropPage.DTEProject.FullName & PERUSER_EXTENSION}

                ElseIf (Me.Flags And ControlDataFlags.PersistedInVBMyAppFile) <> 0 Then
                    Try
                        Dim MyAppProperties As MyApplication.MyApplicationPropertiesBase = _
                            DirectCast(m_PropPage.m_ObjectsPropertyDescriptorsArray(0)("MyApplication").GetValue(m_PropPage.m_ExtendedObjects(0)), MyApplication.MyApplicationPropertiesBase)
                        Debug.Assert(MyAppProperties IsNot Nothing)
                        If MyAppProperties IsNot Nothing Then
                            Return MyAppProperties.FilesToCheckOut(True)
                        End If
                    Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                        Debug.Fail("Unable to retrieve MyApplicationProperties to figure out set of files to check out")
                    End Try
                ElseIf (Me.Flags And ControlDataFlags.PersistedInAppManifestFile) <> 0 Then
                    Dim AppManifest As String = GetSpecialFile(__PSFFILEID2.PSFFILEID_AppManifest, True)
                    If AppManifest <> "" Then
                        Return New String() {AppManifest}
                    End If
                ElseIf (Me.Flags And ControlDataFlags.PersistedInAssemblyInfoFile) <> 0 Then
                    Dim AssemblyInfo As String = GetSpecialFile(__PSFFILEID2.PSFFILEID_AssemblyInfo, True)
                    If AssemblyInfo <> "" Then
                        Return New String() {AssemblyInfo}
                    End If
                ElseIf (Me.Flags And ControlDataFlags.PersistedInApplicationDefinitionFile) <> 0 Then
                    Const PSFFILEID_AppXaml As Integer = -1008
                    Dim applicationDefinition As String = GetSpecialFile(PSFFILEID_AppXaml, True)
                    If applicationDefinition <> "" Then
                        Return New String() {applicationDefinition}
                    End If
                ElseIf (Me.Flags And ControlDataFlags.NoOptimisticFileCheckout) <> 0 Then
                    Return New String() {}
                Else
                    'Default - Changing the property requires checking out the project file
                    Return New String() {m_PropPage.DTEProject.FullName}
                End If
            End If

            Return New String() {}
        End Function


        ''' <summary>
        ''' Retrieves the name of the given special file, and creates it if requested
        ''' </summary>
        ''' <param name="PSFFILEID"></param>
        ''' <param name="CreateIfNotExist"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetSpecialFile(ByVal psfFileId As Integer, ByVal CreateIfNotExist As Boolean) As String
            If m_PropPage Is Nothing OrElse m_PropPage.ProjectHierarchy Is Nothing Then
                Debug.Fail("Unexpected null")
                Return Nothing
            End If

            Dim SpecialFiles As IVsProjectSpecialFiles = TryCast(m_PropPage.ProjectHierarchy, IVsProjectSpecialFiles)
            If SpecialFiles Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                Return Nothing
            End If
            Dim fileName As String = Nothing
            Dim itemId As UInteger
            Dim hr As Integer
            Dim flags As UInteger = CUInt(__PSFFLAGS.PSFF_FullPath)
            If CreateIfNotExist Then
                flags = flags Or CUInt(__PSFFLAGS.PSFF_CreateIfNotExist)
            End If

            hr = SpecialFiles.GetFile(psfFileId, flags, itemId, fileName)
            VSErrorHandler.ThrowOnFailure(hr)
            If itemId = VSITEMID.NIL OrElse fileName = "" Then
                Debug.Assert(fileName IsNot Nothing, "Why is filename returned as nothing?")
                Return Nothing
            End If

            Return fileName
        End Function


        ''' <summary>
        ''' Given two object pointers, returns true if the two objects are identical either by reference or value.
        ''' </summary>
        ''' <param name="Object1"></param>
        ''' <param name="Object2"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function ObjectsAreEqual(ByVal Object1 As Object, ByVal Object2 As Object) As Boolean
            If Object1 Is Nothing AndAlso TypeOf Object2 Is String Then
                Object1 = String.Empty
            ElseIf Object2 Is Nothing AndAlso TypeOf Object1 Is String Then
                Object2 = String.Empty
            End If

            If IsSpecialValue(Object1) OrElse IsSpecialValue(Object2) Then
                Return False
            End If

            If Object1 Is Object2 Then 'Handles reference identity and also Nothing = Nothing
                Return True
            ElseIf Object1 IsNot Nothing AndAlso Object2 IsNot Nothing AndAlso Object1.Equals(Object2) Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Function GetFlags() As ControlDataFlags
            Return Me.Flags
        End Function

    End Class

End Namespace
