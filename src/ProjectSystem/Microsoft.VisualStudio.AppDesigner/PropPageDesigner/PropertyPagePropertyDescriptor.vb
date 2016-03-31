Imports Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner
Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' This property wraps a PropertyDescriptor that was received from the project system or a user-defined property descriptor for 
    '''   user-persisted properties (UserPropertyDescriptor).
    '''   We wrap the real property descriptor that we are given before passing it to the IComponentChangeService so that we can intercept 
    '''   GetValue and SetValue and redirect them through the property page's IVsProjectDesignerPage.GetProperty/SetProperty instead of going 
    '''   directly to the propdescriptor (and hence the project).  This allows us exact control of undo and redo and how properties
    '''   are set and got.
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class PropertyPagePropertyDescriptor
        Inherits PropertyDescriptor

        'The property descriptor for the Project or Config property we wrap
        Private m_PropDesc As PropertyDescriptor
        Private m_TypeConverter As TypeConverter
        Private m_DisplayName As String
        Private m_Name As String
        Private m_PropertyType As System.Type
        Private m_IsReadOnly As Boolean

        ''' <summary>
        ''' Constructs a PropertyDescriptor using the wrapped properties property descriptor
        ''' </summary>
        ''' <param name="PropDesc">The property descriptor that is being wrapped.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal PropDesc As PropertyDescriptor, ByVal PropertyName As String)
            MyBase.New(PropDesc)

            Debug.Assert(PropDesc IsNot Nothing)

            m_PropDesc = PropDesc

            If m_PropDesc.PropertyType.IsEnum Then
                m_PropertyType = m_PropDesc.PropertyType.UnderlyingSystemType
                m_TypeConverter = New TypeConverter()
            Else
                m_PropertyType = m_PropDesc.PropertyType
                m_TypeConverter = m_PropDesc.Converter
            End If
            m_DisplayName = m_PropDesc.DisplayName
            m_Name = PropertyName
            m_IsReadOnly = m_PropDesc.IsReadOnly
        End Sub


        ''' <summary>
        ''' Returns the type of the instance this property is bound to, which is PropPageDesignerRootComponent.
        ''' </summary>
        ''' <value>The component type.</value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property ComponentType() As System.Type
            Get
                Return GetType(PropPageDesignerRootComponent)
            End Get
        End Property


        ''' <summary>
        '''  Returns a value indicating whether this property is read-only.
        ''' </summary>
        ''' <value>True if the property is read-only, False otherwise.</value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property IsReadOnly() As Boolean
            Get
                Return m_IsReadOnly
            End Get
        End Property


        ''' <summary>
        ''' Returns the type of the property.
        ''' </summary>
        ''' <value>A Type that represents the type of the property.</value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property PropertyType() As System.Type
            Get
                Return m_PropertyType
            End Get
        End Property



        ''' <summary>
        '''  Gets the current value of the property on the specified PropPageDesignerRootComponent instance.
        ''' </summary>
        ''' <param name="Component">The component instance with the property to retrieve the value.</param>
        ''' <returns>The value of the property on the specified component instance.</returns>
        Public Overrides Function GetValue(ByVal Component As Object) As Object
            Debug.Assert(Component IsNot Nothing, "component is Nothing!!!")
            If TypeOf Component Is PropPageDesignerRootComponent Then
                Dim View As PropPageDesignerView = DirectCast(Component, PropPageDesignerRootComponent).RootDesigner.GetView()
                Debug.Assert(View IsNot Nothing)
                Return View.GetProperty(Me.Name)
            Else
                Debug.Fail("PropertyPagePropertyDescriptor.GetValue() called with unexpected Component type.  Expected that this is also set up through the PropPageDesignerView (implementing IProjectDesignerPropertyPageUndoSite)")
                Throw AppDesCommon.CreateArgumentException("Component")
            End If
        End Function


        ''' <summary>
        '''  Sets the value of the property on the specified component instance to a different value.
        ''' </summary>
        ''' <param name="Component">The component instance with the property to set the value.</param>
        ''' <param name="Value">The new value to set the property to.</param>
        Public Overrides Sub SetValue(ByVal Component As Object, ByVal Value As Object)
            Debug.Fail("This shouldn't get called directly - instead the serialization store should be calling in to the prop page designer view")
            If TypeOf Component Is PropPageDesignerRootComponent Then
                Dim View As PropPageDesignerView
                Dim PropPageUndo As IVsProjectDesignerPage
                View = DirectCast(Component, PropPageDesignerRootComponent).RootDesigner.GetView()
                PropPageUndo = TryCast(View.PropPage, IVsProjectDesignerPage)
                Debug.Assert(PropPageUndo IsNot Nothing, "How could this happen?")
                View.SetProperty(Me.Name, Value)
            Else
                Debug.Fail("PropertyPagePropertyDescriptor.SetValue() called with unexpected Component type.  Expected that this is also set up through the PropPageDesignerView (implementing IProjectDesignerPropertyPageUndoSite)")
                Throw AppDesCommon.CreateArgumentException("Component")
            End If
        End Sub


        ''' <summary>
        '''  Indicates whether the value of this property needs to be persisted.
        ''' </summary>
        ''' <param name="Component">The component instance with the property to be examined for persistence.</param>
        ''' <returns>TRUE if the property should be persisted. Otherwise, FALSE.</returns>
        ''' <remarks>Since these properties are for the shell's Property Window only, none needs to be persisted.</remarks>
        Public Overrides Function ShouldSerializeValue(ByVal Component As Object) As Boolean
            Return False
        End Function


        ''' <summary>
        '''  Indicates whether resetting an object changes its value.
        ''' </summary>
        ''' <param name="Component">The component instance to test for reset capability.</param>
        Public Overrides Function CanResetValue(ByVal Component As Object) As Boolean
            Return False
        End Function

        ''' <summary>
        '''  Resets the value of this property of the component instance to the default value.
        ''' </summary>
        ''' <param name="Component">The component instance with the property value that is to be reset.</param>
        ''' <remarks>Not implemented since there is no 'reset'.</remarks>
        Public Overrides Sub ResetValue(ByVal Component As Object)
            Debug.Fail("No ResetValue implementation!!!  Shouldn't have been enabled in the properties window because CanResetValue always returns False.")
        End Sub

        ''' <summary>
        ''' Returns the converter for the contained property
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Converter() As TypeConverter
            Get
                Return m_TypeConverter
            End Get
        End Property

        ''' <summary>
        ''' Returns the name of the property.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Name() As String
            Get
                If m_Name = "" Then
                    Return MyBase.Name
                Else
                    Return m_Name
                End If
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the display name of the property.  This is the name that will
        ''' be displayed in a properties window.  This will be the same as the property
        ''' name for most properties.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property DisplayName() As String
            Get
                Return m_DisplayName
            End Get
        End Property

        Public Overrides Function GetChildProperties(ByVal instance As Object, ByVal filter As Attribute()) As PropertyDescriptorCollection
            If m_PropDesc IsNot Nothing Then
                Return m_PropDesc.GetChildProperties(instance, filter)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets an editor of the specified type.
        ''' </summary>
        ''' <param name="editorBaseType"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function GetEditor(ByVal editorBaseType As Type) As Object
            If m_PropDesc IsNot Nothing Then
                Return m_PropDesc.GetEditor(editorBaseType)
            End If
            Return Nothing
        End Function


    End Class

End Namespace

