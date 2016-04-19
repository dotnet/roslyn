' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Shell
Imports System.CodeDom.Compiler
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Resources
Imports System.Runtime.Serialization
Imports System.Text
Imports VB = Microsoft.VisualBasic


Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    '''  Represents a resource in the .resx file.
    ''' </summary>
    ''' <remarks> 
    '''  - TypeDescriptionProvider points the shell to ask our ResourceTypeDescriptionProvider for the properties we want to expose. 
    '''  - Implements IComponent to be able to push the resource through SelectionService, so that the name of the resource 
    '''      appears on the Property Window's drop down list.
    ''' </remarks>
    <Serializable()> _
    <TypeDescriptionProvider(GetType(ResourceTypeDescriptionProvider))> _
    Friend NotInheritable Class Resource
        Implements IComponent
        Implements ISerializable 'This allows us to fully control the serialization process
        Implements IDisposable
        Implements FileWatcher.IFileWatcherListener
        Implements ResourceTypeEditor.IResource


#Region "Interface ITypeResolutionContextProvider"


        ''' <summary>
        ''' This is a very simple interface used to communicate a TypeResolutionService
        '''   pointer to a Resource so that it can correctly hook up to either the project's
        '''   resources.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Interface ITypeResolutionContextProvider

            ''' <summary>
            ''' Retrieve a type resolution service for the project that the .resx file is
            '''   opened in.
            ''' </summary>
            ''' <returns>The ITypeResolutionService for the project the .resx file was opened in, or else Nothing if it was opened outside the context of a project.</returns>
            ''' <remarks></remarks>
            Function GetTypeResolutionService() As ITypeResolutionService

        End Interface

#End Region


#Region "Private enum - ResourcePersistenceMode"

        ''' <summary>
        '''  The persistence mode of the resource to show in PropertyWindow.
        ''' </summary>
        ''' <remarks></remarks>
        <TypeConverterAttribute(GetType(ResourcePersistenceModeEnumConverter))> _
        Public Enum ResourcePersistenceMode
            'The resource is a link to a file on disk which is read and compiled into the manifest resources at compile time.
            Linked

            'The resource value is embedded directly into the resx file.
            Embedded
        End Enum

        ''' <summary>
        '''  A Enum Converter class to covert ResourcePersistenceMode to localizable strings
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class ResourcePersistenceModeEnumConverter
            Inherits EnumConverter

            Private _linkedDisplayValue As String = SR.GetString(SR.RES_PersistenceMode_Linked)
            Private _embeddedDisplayValue As String = SR.GetString(SR.RES_PersistenceMode_Embeded)

            ''' <summary>
            ''' </summary>
            Public Sub New(ByVal enumType As Type)
                MyBase.New(enumType)
            End Sub

            ''' <summary>
            '''      Converts the given object to the converter's native type.
            ''' </summary>
            ''' <param name='context'>
            '''      A formatter context.  This object can be used to extract additional information
            '''      about the environment this converter is being invoked from.  This may be null,
            '''      so you should always check.  Also, properties on the context object may also
            '''      return null.
            ''' </param>
		    ''' <param name='culture'>
            '''      Culture object for this thread.
            ''' </param>
            ''' <param name='value'>
            '''      The object to convert.
            ''' </param>
            ''' <returns>
            '''      The converted object.  This will throw an excetpion if the converson
            '''      could not be performed.
            ''' </returns>
            ''' <seealso cref='System.ComponentModel.TypeConverter' />
            Public Overrides Function ConvertFrom(ByVal context As ITypeDescriptorContext, ByVal culture As CultureInfo, ByVal value As Object) As Object
                If TypeOf (value) Is String Then
                    Dim strValue As String = CStr(value)
                    If String.Compare(strValue, _linkedDisplayValue, StringComparison.OrdinalIgnoreCase) = 0 Then
                        Return ResourcePersistenceMode.Linked
                    ElseIf String.Compare(strValue, _embeddedDisplayValue, StringComparison.OrdinalIgnoreCase) = 0 Then
                        Return ResourcePersistenceMode.Embedded
                    End If
                End If
                Return MyBase.ConvertFrom(context, culture, value)
            End Function

            ''' <summary>
            '''      Converts the given object to another type.  The most common types to convert
            '''      are to and from a string object.  The default implementation will make a call
            '''      to ToString on the object if the object is valid and if the destination
            '''      type is string.  If this cannot convert to the desitnation type, this will
            '''      throw a NotSupportedException.
            ''' </summary>
            ''' <param name='context'>
            '''      A formatter context.  This object can be used to extract additional information
            '''      about the environment this converter is being invoked from.  This may be null,
            '''      so you should always check.  Also, properties on the context object may also
            '''      return null.
            ''' </param>
            ''' <param name='culture'>
            '''      Culture object for this thread.
            ''' </param>
            ''' <param name='value'>
            '''      The object to convert.
            ''' </param>
            ''' <param name='destinationType'>
            '''      The type to convert the object to.
            ''' </param>
            ''' <returns>
            '''      The converted object.
            ''' </returns>
            Public Overrides Function ConvertTo(ByVal context As ITypeDescriptorContext, ByVal culture As CultureInfo, ByVal value As Object, ByVal destinationType As Type) As Object
                If destinationType.Equals(GetType(String)) Then
                    If value IsNot Nothing AndAlso TypeOf (value) Is ResourcePersistenceMode Then
                        Select Case CType(value, ResourcePersistenceMode)
                            Case ResourcePersistenceMode.Linked
                                Return _linkedDisplayValue
                            Case ResourcePersistenceMode.Embedded
                                Return _embeddedDisplayValue
                            Case Else
                                Debug.Fail("Unexpected persistence mode")
                        End Select
                    End If
                End If
                Return MyBase.ConvertTo(context, culture, value, destinationType)
            End Function

        End Class


        ''' <summary>
        ''' Indicates the possible types for a file-based resource in the resource editor.
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum FileTypes
            Text
            Binary
        End Enum

#End Region


#Region "Private class - ImagePropertiesCache"

        ''' <summary>
        ''' This is a simple class which contains cached information about a resource.  They are cached
        '''   because retrieving their values requires having the current value of the Resource, which 
        '''   which don't want to keep longer than necessary (e.g., it might be a large bitmap)
        ''' </summary>
        ''' <remarks></remarks>
        Private Class ImagePropertiesCache
            'Cached return value from ResourceTypeEditor.GetResourceFriendlyTypeDescription()
            Public FriendlyTypeDescription As String

            'Cached return value from ResourceTypeEditor.GetResourceFriendlySize()
            Public FriendlySize As String
        End Class

#End Region


#Region "Non-shared fields"

        ' The ResXDataNode that represents the heart of this resource (name, comment, value, fileref, etc.)
        ' NOTE: After initialization, this value may never be Nothing.
        Private _resXDataNode As ResXDataNode

        'The currently cached value of the instantiated resource (linked or non-linked).  May be Nothing 
        '  (or may be a weak reference that has been flushed and therefore doesn't have a value).
        'IMPORTANT: Do *not* use this field directory, but rather use CachedValue(), GetValue() or SetValue()
        Private _cachedValue As WeakReference

        'The parent resource file in which this Resource is contained.  Will be Nothing until the resource is
        '  actually added to a ResourceFile.
        Private _parentResourceFile As ResourceFile
        Private _typeNameConverter As Func(Of Type, String)

        'Reference to an instance of a ResourceTypeEditor clas that can handle the resource type in this cell.
        'Never use this field directly, but rather use the ResourceTypeEditor property, because this
        '  field will be figured out the first time it's needed (and depends on getting a type resolution
        '  context before it cant be figured out).
        Private _resourceTypeEditor As ResourceTypeEditor

        'The category that this resource belongs to.  Cached because it's expensive to calculate.
        Private _categoryCache As Category

        'Cached values for certain information about a resource.  This info is cached
        '  because retrieving their values requires having the current value of the Resource, which 
        '  which don't want to keep longer than necessary (e.g., it might be a large bitmap).
        Private _cachedImageProperties As ImagePropertiesCache

        'ISite reference (needed for IComponent implementation, see Site property)
        Private _site As ISite = Nothing

        'This is either an ITypeResolutionService instance (if the .resx file was opened inside the context
        '  of a project), or an array of AssemblyName's (if the .resx file was opened outside of 
        '  a project in the current solution).  The ResXDataNode has some properties which require one
        '  or the other.  This stores the value we use for calling those properties.
        'Don't use this field directly, use the TypeResolutionContext property instead.
        '  Lazily initialized.
        Private _typeResolutionContext As Object

        'Saved value of the link file/path.  This is used for Undo/Redo purposes when changing
        '   the Persistence mode.
        Private _savedFileName As String

        ' Original Timestamp of the external file. We use this to check whether the file has been updated after we imported the data.
        Private _originalFileTimeStamp As DateTime

        ' Save the original order of the resource item, so we can preserve the original order.
        Private _orderID As Integer

        'For debugging purposes only - True if this resource has been disposed.
        Private _isDisposed As Boolean

#End Region

#Region "Shared fields"

        'Hash of ValueTypeName to PropertyDescriptionCollection
        '  This is the set of properties that we expose in the property sheet for each type of 
        '  Resource.  The set of properties shown is based solely on the value type of the
        '  resource and the type of ResourceTypeEditor that it uses.  Therefore we only need to 
        '  create a unique properties collection for each distinct pairing of these values.
        Private Shared s_propertyDescriptorCollectionHash As New Hashtable '(Of PropertyDescriptorCollection), key = fully-qualified type names of resource value + resource type editor

        'A list of names which are not recommended for use by the end user (because they cause
        '  compiler errors or other problems).
        'Use the UnrecommendedResourceNamesHash property to access these so that they are properly
        '  initialized.
        Private Shared s_unrecommendedResourceNamesHash As Hashtable  'Of Boolean (key = member name [string])

#End Region

#Region "Events"

        ' Represents the method that handles the Disposed event of a Component.
        Private Event Disposed(ByVal sender As Object, ByVal e As System.EventArgs) Implements IComponent.Disposed

#End Region

#Region "Constants/Read-only Fields"

        'The category to put our properties under in the Property Window.
        '   Currently, we just putting them all under a category called "Resource" (non-localized)
        Private Shared ReadOnly s_categoryAttribute As New CategoryAttribute(ResourcePropertyDescriptor.CATEGORY_RESOURCE)

        'The attribute to prevent propertyGrid showing the property when we multi-select resources
        Private Shared ReadOnly s_notMergablePropertyAttribute As New MergablePropertyAttribute(False)

        'The type name used to indicate that a ResXDataNode is a ResXNullRef
        Private Shared ReadOnly s_resXNullRefValueTypeName As String = GetType(Object).AssemblyQualifiedName

        'Most of the ResourcePropertyDescriptors that we create for individual properties are the same no matter what the
        '  type of the resource (the exception here is the Value property typed as the actual type of the Resource value).
        '  So we create them once and cache them here for all resource editor instances.

        Private Shared ReadOnly s_valueDescriptionAttribute As DescriptionAttribute = New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Value))

        'PropertyDescriptor for "Name" property
        Private Shared ReadOnly s_propertyDescriptor_Name As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_NAME, GetType(String), IsReadOnly:=False, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, s_notMergablePropertyAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Name))})

        'PropertyDescriptor for "Comment" property
        Private Shared ReadOnly s_propertyDescriptor_Comment As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_COMMENT, GetType(String), IsReadOnly:=False, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Comment))})

        'PropertyDescriptor for "Encoding" property
        'Note that we have an attribute to associate our encoding converter with this property so we get a dropdown list to show up
        '  in the properties window
        Private Shared ReadOnly s_propertyDescriptor_Encoding As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_ENCODING, GetType(SerializableEncoding), IsReadOnly:=False, _
                    CanReset:=True, _
                    Attributes:=New Attribute() { _
                        s_categoryAttribute, _
                        New TypeConverterAttribute(GetType(SerializableEncodingConverter)), _
                        New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Encoding))})

        'PropertyDescriptor for "Filename" property (read-only property)
        Private Shared ReadOnly s_propertyDescriptor_Filename_ReadOnly As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_FILENAME, GetType(String), IsReadOnly:=True, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Filename))})

        'PropertyDescriptor for "Filename" property (read/write - not currently visible publicly - used when changing Persistence mode)
        Private Shared ReadOnly s_propertyDescriptor_Filename_ReadWrite As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_FILENAME, GetType(String), IsReadOnly:=False, Attributes:=New Attribute() {s_categoryAttribute})

        'PropertyDescriptor for "FileType" property
        Private Shared ReadOnly s_propertyDescriptor_FileType As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_FILETYPE, GetType(FileTypes), IsReadOnly:=False, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_FileType))})

        'PropertyDescriptor for "Persistence" property
        Private Shared ReadOnly s_propertyDescriptor_Persistence As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_PERSISTENCE, GetType(ResourcePersistenceMode), IsReadOnly:=False, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Persistence))})

        'PropertyDescriptor for "Persistence" property (read/only)
        Private Shared ReadOnly s_propertyDescriptor_Persistence_ReadOnly As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_PERSISTENCE, GetType(ResourcePersistenceMode), IsReadOnly:=True, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Persistence))})

        'PropertyDescriptor for "Type" property (read-only property)
        Private Shared ReadOnly s_propertyDescriptor_Type As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_TYPE, GetType(String), IsReadOnly:=True, _
                Attributes:=New Attribute() { _
                    s_categoryAttribute, _
                    New DescriptionAttribute(SR.GetString(SR.RSE_PropDesc_Type))})

        'PropertyDescriptor for "Value" property, typed as Object
        Private Shared ReadOnly s_propertyDescriptor_ValueAsObject As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_VALUE, GetType(Object), IsReadOnly:=False, _
                Attributes:=New Attribute() {s_categoryAttribute, s_valueDescriptionAttribute})

        'PropertyDescriptor for "Value" property, typed as String
        Private Shared ReadOnly s_propertyDescriptor_ValueAsString As _
            New ResourcePropertyDescriptor(ResourcePropertyDescriptor.PROPERTY_VALUE, GetType(String), IsReadOnly:=False, _
                Attributes:=New Attribute() {s_categoryAttribute, s_valueDescriptionAttribute})

#End Region

#Region "Constructors/Destructors"


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ResXDataNode">A ResXDataNode that represents this resource.</param>
        ''' <param name="TypeResolutionContextProvider">An interface from which this resource can query for an ITypeResolutionService for resolving types inside the .resx file.</param>
        ''' <remarks>
        ''' Creates a new linked or non-linked resource.  Generally used when reading ResXDataNodes from the
        '''   .resx directly.
        ''' </remarks>
        Public Sub New(ByVal resourceFile As ResourceFile, ByVal ResXDataNode As ResXDataNode, ByVal Order As Integer, ByVal TypeResolutionContextProvider As ITypeResolutionContextProvider)
            Debug.Assert(TypeResolutionContextProvider IsNot Nothing, "TypeResolutionContextProvider should have been provided - only general exception is deserialization, which does not go through this constructor")
            SetTypeNameConverter(resourceFile)
            Init(ResXDataNode, Order, TypeResolutionContextProvider)

            ' ResXNullRef is stored without an assembly version and the correct version is deduced
            ' during deserialization. As such, we don't need to recreate the m_ResXDataNode which
            ' is problematic in the case when the object represented is null
            If Not IsResXNullRef Then
                ' BUGFIX: Dev11#31931: Create a new ResXDataNode with the TypeNameConverter function.
                If ResXDataNode.FileRef Is Nothing Then
                    _resXDataNode = NewResXDataNode(ResXDataNode.Name, ResXDataNode.Comment, Me.TryGetValue())
                Else
                    _resXDataNode = NewResXDataNode(ResXDataNode.Name, ResXDataNode.Comment, ResXDataNode.FileRef.FileName, ResXDataNode.FileRef.TypeName, ResXDataNode.FileRef.TextFileEncoding)
                End If
            End If

            'This is the case used when reading from a .resx file.  We leave the TextFileEncoding alone, even if
            '  it's Nothing, so that we don't alter the .resx file.
        End Sub


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="Name">The name of the new resource</param>
        ''' <param name="Comment">The comment for the new resource</param>
        ''' <param name="Value">The (non-linked) value for the new resource.</param>
        ''' <param name="TypeResolutionContextProvider">An interface from which this resource can query for an ITypeResolutionService for resolving types inside the .resx file.</param>
        ''' <remarks>
        ''' Creates a new non-linked resource.
        ''' </remarks>
        Public Sub New(ByVal resourceFile As ResourceFile, ByVal Name As String, ByVal Comment As String, ByVal Value As Object, ByVal TypeResolutionContextProvider As ITypeResolutionContextProvider)
            If Value Is Nothing Then
                Debug.Fail("Can't create non-linked resource with value of Nothing")
                Value = "" 'defensive
            End If
            SetTypeNameConverter(resourceFile)
            Debug.Assert(TypeResolutionContextProvider IsNot Nothing, "TypeResolutionContextProvider should have been provided - only general exception is deserialization, which does not go through this constructor")
            Init(NewResXDataNode(Name, Comment, Value), Int32.MaxValue, TypeResolutionContextProvider)
            TryGuessFileEncoding()
        End Sub


        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="Name">The name of the new resource</param>
        ''' <param name="Comment">The comment for the new resource</param>
        ''' <param name="FileNameAndPath">The path/filename that points to a file containing the the new linked resource's value.</param>
        ''' <param name="ValueTypeName">The expected type of class contained in the linked file.</param>
        ''' <param name="TypeResolutionContextProvider">An interface from which this resource can query for an ITypeResolutionService for resolving types inside the .resx file.</param>
        ''' <remarks>
        ''' Creates a new linked resource.
        ''' </remarks>
        Public Sub New(ByVal resourceFile As ResourceFile, ByVal Name As String, ByVal Comment As String, ByVal FileNameAndPath As String, ByVal ValueTypeName As String, ByVal TypeResolutionContextProvider As ITypeResolutionContextProvider)
            Debug.Assert(TypeResolutionContextProvider IsNot Nothing, "TypeResolutionContextProvider should have been provided - only general exception is deserialization, which does not go through this constructor")

            SetTypeNameConverter(resourceFile)
            'Leave TextFileEncoding as Nothing so that it will be guessed automatically.
            Init(NewResXDataNode(Name, Comment, FileNameAndPath, ValueTypeName, Nothing), Int32.MaxValue, TypeResolutionContextProvider)
            TryGuessFileEncoding()
        End Sub

        Public Sub SetTypeNameConverter(ByVal resourceFile As ResourceFile)
            If _typeNameConverter Is Nothing AndAlso resourceFile IsNot Nothing Then
                _typeNameConverter = AddressOf resourceFile.TypeNameConverter
            End If
        End Sub

        Private Function TypeNameConverter(ByVal type As Type) As String
            SetTypeNameConverter(_parentResourceFile)

            If _typeNameConverter IsNot Nothing Then
                Return _typeNameConverter(type)
            Else
                Return type.AssemblyQualifiedName
            End If
        End Function

        ''' <summary>
        ''' Initializes the Resource.
        ''' </summary>
        ''' <param name="ResXDataNode">The ResXDataNode which will hold the important information about the resource.</param>
        ''' <param name="TypeResolutionContextProvider">An interface from which this resource can query for an ITypeResolutionService for resolving types inside the .resx file.
        '''   May be Nothing, but then it must be provided as soon as possible via SetTypeResolutionContext().</param>
        ''' <remarks></remarks>
        Private Sub Init(ByVal ResXDataNode As ResXDataNode, Optional ByVal Order As Integer = Int32.MaxValue, Optional ByVal TypeResolutionContextProvider As ITypeResolutionContextProvider = Nothing)
            _resXDataNode = ResXDataNode
            _orderID = Order

            SetTypeResolutionContext(TypeResolutionContextProvider)
            AddFileWatcherEntry()
        End Sub


        ''' <summary>
        ''' IDisposable.Dispose()
        ''' </summary>
        ''' <remarks>
        ''' Note: The designer host calls this on each sited Resource (and its designer, which in our case is fabricated
        '''   for us) when it gets disposed.  Then it disposes the root component and root designer.
        ''' </remarks>
        Public Overloads Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
        End Sub


        ''' <summary>
        ''' Dispose.
        ''' </summary>
        ''' <param name="Disposing">If True, we're disposing.  If false, we're finalizing.</param>
        ''' <remarks>
        ''' It's acceptable to call Dispose() on this object multiple times.
        ''' </remarks>
        Protected Overloads Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                'Raise the Disposed event (required for IComponent)
                RaiseEvent Disposed(Me, EventArgs.Empty)

                'Must be done while we still have a pointer to m_ParentResourceFile
                RemoveFileWatcherEntry()

                _parentResourceFile = Nothing

                'Dispose of the actual resource value (linked case only)
                If _cachedValue IsNot Nothing Then
                    Dim CurrentCachedValue As Object = _cachedValue.Target
                    If _cachedValue.IsAlive AndAlso CurrentCachedValue IsNot Nothing AndAlso TypeOf CurrentCachedValue Is IDisposable Then
                        Try
                            CType(CurrentCachedValue, IDisposable).Dispose()
                        Catch ex As Exception
                            RethrowIfUnrecoverable(ex)
                            Debug.Fail("Disposing a resource value threw an exception: " & ex.ToString())
                        End Try
                    End If
                    _cachedValue = Nothing
                End If

                _isDisposed = True
            End If
        End Sub

#End Region


#Region "Properties"

#Region "Miscellaneous properties"

        ''' <summary>
        ''' The parent resource file in which this Resource is contained.  Will be Nothing until the resource is
        '''  actually added to a ResourceFile.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property ParentResourceFile() As ResourceFile
            Get
                Return _parentResourceFile
            End Get
            Set(ByVal Value As ResourceFile)
                Debug.Assert(Value Is Nothing OrElse _parentResourceFile Is Nothing, "ParentResourceFile already set!")
                _parentResourceFile = Value

                AddFileWatcherEntry()

                'Now that we have a parent ResourceFile, we are allowed to add tasks to the task
                '  list.  Go ahead and check for basic problems now, but delay checking for
                '  Value problems, because we don't want to cause Value instatantion during loading
                '  (it can be expensive, especially for linked resources).  Instead, we'll handle
                '  that task later during idle (see ResourceFile.OnDelayCheckForErrors()).
                CheckForErrors(FastChecksOnly:=True)
            End Set
        End Property

        ''' <summary>
        ''' Returns whether this object has been disposed
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsDisposed() As Boolean
            Get
                Return _isDisposed
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the ResXDataNode associated with this resource
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property ResXDataNode() As ResXDataNode
            Get
                Return _resXDataNode
            End Get
        End Property


#End Region

#Region "Properties visible publicly to the user via the Properties Window (for the Value property, see GetValue/SetValue/TryGetValue"

        ''' <summary>
        ''' Sets/Gets the Name property of the Resource component.  This is the member which should be
        '''   used normally (including changing the Name in response to user manipulation), since it
        '''   enables the undo engine to undo this change.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property Name() As String
            Get
                Return _resXDataNode.Name
            End Get
            Set(ByVal Value As String)
                'To enable Undo, we must go through a property descriptor...

                If _parentResourceFile IsNot Nothing Then
                    'Theoretically, creating a transaction for any single change through a property
                    '  descriptor shouldn't be necessary.  However, it appears that the UndoEngine doesn't
                    '  have an opportunity to commit a change caused via ComponentChangeService.ComponentRename
                    '  because there's only the one event (not a Renaming/Renamed).  So, we need to wrap this 
                    '  in a transaction ourselves for this to work properly.
                    Using Transaction As ComponentModel.Design.DesignerTransaction = _parentResourceFile.View.RootDesigner.DesignerHost.CreateTransaction(SR.GetString(SR.RSE_Undo_ChangeName))
                        s_propertyDescriptor_Name.SetValue(Me, Value)
                        Transaction.Commit()
                    End Using
                Else
                    'We haven't been sited in a parent ResourceFile yet.  We don't want undo (there's no context for
                    '  supporting it).
                    NameWithoutUndo = Value
                End If
            End Set
        End Property


        ''' <summary>
        ''' Sets/Gets the Name property of the Resource component, without undo support, and without causing the 
        '''  designer to be dirtied (because ComponentChangeService notifications won't get sent)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public WriteOnly Property NameWithoutUndo() As String
            Set(ByVal Value As String)
                If Value = "" Then
                    Throw NewException(SR.GetString(SR.RSE_Err_NameBlank), HelpIDs.Err_NameBlank)
                End If

                'Validate the value first...
                Dim NewParsedName As String = Nothing
                Dim Exception As Exception = Nothing
                If Not ValidateName(Me.ParentResourceFile, CStr(Value), Name, NewParsedName, Exception) Then
                    Throw Exception
                Else
                    If _parentResourceFile IsNot Nothing Then
                        'This will eventually get back to us via NameRawWithoutUndo.  This round-about
                        '  is necessary in order for the parent ResourceFile to update its hashtable, and
                        '  for the property ISite to get updated as well (not to mention for Undo to
                        '  work properly).
                        _parentResourceFile.RenameResource(Me, NewParsedName)
                    Else
                        'This resource has been placed into a ResourceFile yet.  Just go ahead and
                        '  do the rename without trying to notify a parent.
                        NameRawWithoutUndo = NewParsedName
                    End If
                End If
            End Set
        End Property


        ''' <summary>
        ''' Sets the Name of this resource *without* changing the name of the resource
        '''   in its parent ResourceFile hash, and without enabling Undo, and without
        '''   changing the Site's name.
        ''' WARNING: This property should only be called by ResourceFile, and never
        '''   by any other code.  Other code should go through the Name property or
        '''   the NameWithoutUndo property.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Can't be private because ResourceFileneeds access to it.</remarks>
        <EditorBrowsable(EditorBrowsableState.Never)> _
        Friend Property NameRawWithoutUndo() As String
            Get
                Return _resXDataNode.Name
            End Get
            Set(ByVal Value As String)
                Debug.Assert(Value <> "", "Shouldn't have reached here without a valid name")
                _resXDataNode.Name = Value
                If _parentResourceFile IsNot Nothing Then
                    _parentResourceFile.DelayCheckResourceForErrors(Me)
                End If
                InvalidateUI()
            End Set
        End Property


        ''' <summary>
        ''' Sets/Gets the Comment property of the Resource component.  This is the member which should be
        '''   used normally (including changing the property in response to user manipulation), since it
        '''   enables the undo engine to undo this change.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property Comment() As String
            Get
                Return _resXDataNode.Comment
            End Get
            Set(ByVal Value As String)
                'To enable Undo, we must go through a property descriptor...
                s_propertyDescriptor_Comment.SetValue(Me, Value)
            End Set
        End Property


        ''' <summary>
        ''' Sets/Gets the Comment property of the Resource component, without undo support, and without causing the 
        '''  designer to be dirtied (because ComponentChangeService notifications won't get sent)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private WriteOnly Property CommentWithoutUndo() As String
            Set(ByVal Value As String)
                _resXDataNode.Comment = Value
                CheckCommentForErrors()
                InvalidateUI()
            End Set
        End Property


        ''' <summary>
        '''  Returns the ResourcePersistenceMode property.  (This property is shown in the properties window.)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property PersistenceMode() As ResourcePersistenceMode
            Get
                If IsLink Then
                    Return ResourcePersistenceMode.Linked
                Else
                    Return ResourcePersistenceMode.Embedded
                End If
            End Get
            Set(ByVal Value As ResourcePersistenceMode)
                s_propertyDescriptor_Persistence.SetValue(Me, Value)
            End Set
        End Property


        ''' <summary>
        ''' Sets/Gets the persistence mode for this resource, without undo support, and without causing the 
        '''  designer to be dirtied (because ComponentChangeService notifications won't get sent)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private WriteOnly Property PersistenceWithoutUndo() As ResourcePersistenceMode
            Set(ByVal Value As ResourcePersistenceMode)
                If Me.PersistenceMode = Value Then
                    'Nothing changed
                    Exit Property
                End If

                If ParentResourceFile Is Nothing Then
                    Debug.Fail("ParentResourceFile shouldn't be nothing")
                    Exit Property
                End If

                Select Case Value
                    Case ResourcePersistenceMode.Linked
                        'Change from non-linked to linked

                        Dim View As ResourceEditorView = ParentResourceFile.View
                        Dim needOverwriteFile As Boolean = True

                        'Can this resource type be saved to a file?
                        If Not ResourceTypeEditor.CanSaveResourceToFile(Me) OrElse Not ResourceTypeEditor.CanChangePersistenceProperty(Me.ParentResourceFile) Then
                            Throw NewException(SR.GetString(SR.RSE_Err_CantSaveResource_1Arg, Name))
                        End If

                        'First, get the path to save the file to.
                        Dim UserCancel As Boolean
                        Dim NewFilePath As String
                        If View.IsUndoing Then
                            'We should still have a filename saved.
                            Debug.Assert(_savedFileName <> "", "No original file name?")
                            If _savedFileName <> "" Then
                                If File.Exists(_savedFileName) Then
                                    Dim modifiedTime As DateTime
                                    Try
                                        modifiedTime = File.GetLastWriteTimeUtc(_savedFileName)
                                    Catch ex As SystemException
                                        modifiedTime = DateTime.UtcNow
                                    End Try

                                    If modifiedTime <> _originalFileTimeStamp AndAlso Not View.QueryUserToReplaceFiles(New String() {_savedFileName}) Then
                                        needOverwriteFile = False
                                    End If
                                End If
                                NewFilePath = _savedFileName
                            Else
                                Return
                            End If
                        Else
                            NewFilePath = View.GetSaveLocationForNewProjectFile( _
                                Me.ResourceTypeEditor, _
                                ResourceEditorView.GetSuggestedFileNameForResource(Me), _
                                UserCancel)
                            If UserCancel Then
                                Throw New DesignerFramework.UserCanceledException()
                            End If

                            If File.Exists(NewFilePath) Then
                                'Ask permission to overwrite the file.
                                If Not View.QueryUserToReplaceFiles(New String() {NewFilePath}) Then
                                    Throw New DesignerFramework.UserCanceledException()
                                End If

                                ' Checkout the file if necessary...
                                If ParentResourceFile IsNot Nothing Then
                                    Dim filesToCheckOut As New List(Of String)
                                    filesToCheckOut.Add(NewFilePath)
                                    DesignerFramework.SourceCodeControlManager.QueryEditableFiles(ParentResourceFile.ServiceProvider, filesToCheckOut, True, False)
                                End If

                                File.Delete(NewFilePath)
                            End If
                        End If

                        '... and save it
                        If needOverwriteFile Then
                            ResourceTypeEditor.SaveResourceToFile(Me, NewFilePath)
                        End If

                        'Now add the file to the project
                        Dim FinalPathAndFileName As String = ResourcesFolderService.AddFileToProject( _
                            SR.GetString(SR.RSE_ResourceEditor), _
                            View.GetProject(), _
                            View.GetResXProjectItem(), _
                            View.GetDialogOwnerWindow(), _
                            NewFilePath, _
                            CopyFileIfExists:=False)

                        ' NOTE: We will keep old value in m_SavedFileName for undo/redo support...
                        Me.FileNameWithoutUndo = FinalPathAndFileName

                    Case ResourcePersistenceMode.Embedded
                        'Can this resource type be saved to a file?  We don't want to allow changing
                        '  to non-linked if the user couldn't change it back again.
                        If Not ResourceTypeEditor.CanSaveResourceToFile(Me) OrElse Not ResourceTypeEditor.CanChangePersistenceProperty(Me.ParentResourceFile) Then
                            Throw NewException(SR.GetString(SR.RSE_Err_CantSaveResource_1Arg, Name))
                        End If

                        'Changed linked to non-linked
                        Me.FileNameWithoutUndo = ""

                    Case Else
                        Debug.Fail("Unexpected persistence mode")
                End Select

                InvalidateCachedInfoAndThumbnail()

                'Need to be hardcore about property grid update because they changes another property as well (filename).
                ParentResourceFile.View.DelayedPropertyGridUpdate()
            End Set
        End Property


        ''' <summary>
        ''' Gets or retrieves the FileName that the link is pointing to.  This property is not (at least
        '''   not currently) publicly exposed.  However, it has a custom property descriptor so that
        '''   the Undo/Redo engine can get/set it.  It is used to implement changing the Persistence
        '''   property and not lose the original filename that the link was pointing to when changing
        '''   to a non-linked resource.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' </remarks>
        Public Property FileName() As String
            Get
                If IsLink Then
                    Return AbsoluteLinkPathAndFileName
                Else
                    Return ""
                End If
            End Get
            Set(ByVal Value As String)
                s_propertyDescriptor_Filename_ReadWrite.SetValue(Me, Value)
            End Set
        End Property


        ''' <summary>
        ''' Gets or retrieves the FileName that the link is pointing to.  This property is not (at least
        '''   not currently) publicly exposed.  However, it has a custom property descriptor so that
        '''   the Undo/Redo engine can get/set it.  It is used to implement changing the Persistence
        '''   property and not lose the original filename that the link was pointing to when changing
        '''   to a non-linked resource.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' </remarks>
        Private WriteOnly Property FileNameWithoutUndo() As String
            Set(ByVal Value As String)
                Dim PersistenceChanged As Boolean

                If IsLink Then
                    If Value = "" Then
                        'Change to embedded
                        RemoveFileWatcherEntry()
                        _savedFileName = AbsoluteLinkPathAndFileName

                        Try
                            _originalFileTimeStamp = File.GetLastWriteTimeUtc(_savedFileName)
                        Catch ex As SystemException
                            _originalFileTimeStamp = DateTime.UtcNow
                        End Try

                        _resXDataNode = NewResXDataNode(Me.Name, Me.Comment, GetValue())
                        PersistenceChanged = True
                    Else
                        'Change the file the link is pointing to
                        SetLink(Path.GetFullPath(Value))
                    End If
                Else
                    'Not currently a link.
                    If Value = "" Then
                        Exit Property 'Nothing to do
                    Else
                        'Change to a link
                        SetLink(Path.GetFullPath(Value))
                        PersistenceChanged = True
                    End If
                End If

                InvalidateUI()
                If PersistenceChanged Then
                    'Allow Persistence property to be updated in the property sheet
                    ParentResourceFile.View.DelayedPropertyGridUpdate()
                End If
            End Set
        End Property


        ''' <summary>
        ''' Gets or retrieves the Encoding used for text file resources.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Meaningless for all resources other than those using ResourceTypeEditorTextFile.
        '''   This is the member which should be used normally (including changing the property 
        '''   in response to user manipulation), since it enables the undo engine to undo this change.
        ''' </remarks>
        Public Property Encoding() As Encoding
            Get
                If IsLink Then
                    Return _resXDataNode.FileRef.TextFileEncoding
                Else
                    Return Nothing
                End If
            End Get
            Set(ByVal Value As Encoding)
                s_propertyDescriptor_Encoding.SetValue(Me, New SerializableEncoding(Value))
            End Set
        End Property


        ''' <summary>
        ''' Sets/Gets the Encoding used for text file resources, without undo support, and without causing the 
        '''  designer to be dirtied (because ComponentChangeService notifications won't get sent)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private WriteOnly Property EncodingWithoutUndo() As Encoding
            Set(ByVal Value As Encoding)
                Debug.Assert(IsLink AndAlso ResourceTypeEditor.Equals(ResourceTypeEditors.TextFile))
                If _resXDataNode.FileRef IsNot Nothing Then
                    _resXDataNode = NewResXDataNode(Me.Name, Me.Comment, Me.AbsoluteLinkPathAndFileName, Me.ValueTypeName, Value)
                End If
            End Set
        End Property


        ''' <summary>
        ''' Does a reset on the "Encoding" property (when the user chooses it with a right-click on the
        '''   property browser).  In this case, that means re-automatically 
        '''   guessing the encoding from the file's contents.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ResetEncoding()
            Encoding = Nothing 'Note that we will get Undo/redo on this operation, which is what we want.
            TryGuessFileEncoding()
        End Sub


        ''' <summary>
        ''' Gets/sets the file type of a binary or text file resource.
        '''   This is the member which should be used normally (including changing the property 
        '''   in response to user manipulation), since it enables the undo engine to undo this change.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property FileType() As FileTypes
            Get
                If TypeOf ResourceTypeEditor Is ResourceTypeEditorFileBase Then
                    If TryGetValueType().Equals(ResourceTypeEditorTextFile.TextFileValueType) Then
                        Return FileTypes.Text
                    ElseIf TryGetValueType().Equals(ResourceTypeEditorBinaryFile.BinaryFileValueType) Then
                        Return FileTypes.Binary
                    Else
                        Debug.Fail("Unexpected resource value type for a file resource")
                        Return FileTypes.Binary 'defensive
                    End If
                Else
                    Return FileTypes.Binary
                End If
            End Get
            Set(ByVal Value As FileTypes)
                s_propertyDescriptor_FileType.SetValue(Me, Value)
            End Set
        End Property


        ''' <summary>
        ''' Gets/sets the file type of a binary or text file resource, without undo support, and without causing the 
        '''  designer to be dirtied (because ComponentChangeService notifications won't get sent)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private WriteOnly Property FileTypeWithoutUndo() As FileTypes
            Set(ByVal Value As FileTypes)
                If Not IsLink OrElse Not TypeOf ResourceTypeEditor Is ResourceTypeEditorFileBase OrElse _resXDataNode.FileRef Is Nothing Then
                    Debug.Fail("")
                    Return
                End If

                'This is a very unusual property, in that it actually changes the resource type editor associated with this resource.
                '  It also changes the value type that we associate with the resource (string for text files, memory stream for binary)
                Dim NewResourceTypeEditor As ResourceTypeEditor
                Dim NewResourceValueType As Type
                Select Case Value
                    Case FileTypes.Binary
                        NewResourceTypeEditor = ResourceTypeEditors.BinaryFile
                        NewResourceValueType = ResourceTypeEditorBinaryFile.BinaryFileValueType
                    Case FileTypes.Text
                        NewResourceTypeEditor = ResourceTypeEditors.TextFile
                        NewResourceValueType = ResourceTypeEditorTextFile.TextFileValueType
                    Case Else
                        Debug.Fail("Unexpected resource type editor on a file-based resource")
                        Return
                End Select

                If NewResourceTypeEditor.Equals(ResourceTypeEditor) AndAlso NewResourceValueType.Equals(TryGetValueType) Then
                    'Nothing to do
                    Exit Property
                End If

                'Change the resource type editor
                _resourceTypeEditor = NewResourceTypeEditor

                'Change the value type.  Must be done by creating a new ResXFileRef.
                Dim NewTextFileEncoding As Encoding = Me.Encoding
                If Value <> FileTypes.Text Then
                    'Encoding doesn't make any sense for non-text files, so let's clear it here.  If they change it back to
                    '  binary again later, we will re-guess the encoding automatically.
                    NewTextFileEncoding = Nothing
                End If
                _resXDataNode = NewResXDataNode(Me.Name, Me.Comment, Me.AbsoluteLinkPathAndFileName, NewResourceValueType.AssemblyQualifiedName, NewTextFileEncoding)

                'If we changed from a binary file to a text file and the encoding isn't still set from previously, make a guess
                '  at it now.
                TryGuessFileEncoding()

                'Since we've changed the properties on this resource, we have to tell the
                '  type descriptor stuff about the change.
                System.ComponentModel.TypeDescriptor.Refresh(Me)

                'UI must be updated, too
                InvalidateCachedInfoAndThumbnail()

                'Deselect/reselect to update the property sheet changes
                If ParentResourceFile IsNot Nothing Then
                    'Just updating the property sheet doesn't seem to be enough, because we're actually changed the
                    '  properties that show up for this resource, and also we're in the middle of a transaction.  We
                    '  need to do something more hard-core, in this case DelayedPropertyGridUpdate.
                    ParentResourceFile.View.DelayedPropertyGridUpdate()
                End If

                Debug.Assert(Me.FileType = Value)
            End Set
        End Property

#End Region

#Region "Link-related properties"

        ''' <summary>
        ''' Returns True iff the resource is contained in a separate file which we have a link to.  If False,
        '''   the resource is stored as a binary blob encoded directly inside the .resx file.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsLink() As Boolean Implements ResourceTypeEditor.IResource.IsLink
            Get
                Return _resXDataNode.FileRef IsNot Nothing
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the path to the linked file (if any), relative to the resx file's path
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' Note: if you need to change the link, use SetLink()
        ''' </remarks>
        Public ReadOnly Property RelativeLinkPathAndFileName() As String
            Get
                If _resXDataNode.FileRef IsNot Nothing Then
                    If ParentResourceFile IsNot Nothing Then
                        Dim FileRefPath As String = _resXDataNode.FileRef.FileName
                        If Path.IsPathRooted(FileRefPath) AndAlso ParentResourceFile.BasePath <> "" Then
                            'Turn into a relative path
                            Return Common.Utils.GetRelativePath(ParentResourceFile.BasePath, _resXDataNode.FileRef.FileName)
                        Else
                            Return FileRefPath
                        End If
                    Else
                        Debug.Fail("Can't get to base path because parent resource file is Nothing")
                        Return _resXDataNode.FileRef.FileName
                    End If
                End If

                Return ""
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the absolute path to the linked file (if any)
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Note: if you need to change the link, use SetLink()</remarks>
        Public ReadOnly Property AbsoluteLinkPathAndFileName() As String Implements ResourceTypeEditor.IResource.LinkedFilePath
            Get
                If _resXDataNode.FileRef IsNot Nothing Then
                    Debug.Assert(ParentResourceFile Is Nothing OrElse ParentResourceFile.BasePath = "" OrElse Path.IsPathRooted(_resXDataNode.FileRef.FileName), "Shouldn't get relative paths from ResXDataNode")
                    Return _resXDataNode.FileRef.FileName
                End If

                Return ""
            End Get
        End Property

#End Region

#Region "Value- and ResourceTypeEditor-related properties"

        ''' <summary>
        ''' Retrieves the string representation of the value type of the Resource.  E.g., for strings, this would be "System.String, blah blah".
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' For NullResX values (i.e., a resource value of Nothing), returns Nothing.  In all other cases, this returns a non-empty string.
        ''' No exceptions should be thrown from this property
        ''' </remarks>
        Public ReadOnly Property ValueTypeName() As String
            Get
                Try
                    Dim TypeName As String = ""
                    If TypeOf TypeResolutionContext Is ITypeResolutionService Then
                        TypeName = _resXDataNode.GetValueTypeName(DirectCast(TypeResolutionContext, ITypeResolutionService))
                    ElseIf TypeOf TypeResolutionContext Is AssemblyName() Then
                        TypeName = _resXDataNode.GetValueTypeName(DirectCast(TypeResolutionContext, AssemblyName()))
                    Else
                        Debug.Fail("TypeResolutionContext was of an unexpected type")
                    End If

                    Debug.Assert(TypeName <> "", "ResXDataNode.GetValueTypeName() should never return an empty string or Nothing (not even for ResXNullRef)")
                    Return TypeName
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Unexpected exception - ResXDataNode.GetValueTypeName() is not supposed to throw exceptions (except unrecoverable ones), it should instead return the typename as in the original .resx file - exception: " & ex.Message)
                    Return SR.GetString(SR.RSE_UnknownType)
                End Try
            End Get
        End Property


        ''' <summary>
        ''' Returns the *currently* cached value of the resource, if any.
        '''  If the cached value is Nothing, it will *not* attempt to load 
        '''  the actual value from disk (linked) or instantiate it (non-linked).
        ''' </summary>
        ''' <value>The cached or non-linked value if possible, or else Nothing.</value>
        ''' <remarks>No exceptions are thrown (that are not swalled)</remarks>
        Public ReadOnly Property CachedValue() As Object
            Get
                If _cachedValue Is Nothing Then
                    Return Nothing
                Else
                    'We must place the target value into a variable before checking it, otherwise
                    '  there is a slight chance that the garbage collector will kill the reference after
                    '  you've checked its value and before retrieving it.
                    'By placing the value into a variable, we now have a strong reference to the object,
                    '  which will ensure it doesn't get garbage collected before we can return it.
                    Dim CurrentValue As Object = _cachedValue.Target
                    If _cachedValue.IsAlive Then
                        Return CurrentValue
                    Else
                        _cachedValue = Nothing
                        Return Nothing
                    End If
                End If
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff this resource is a ResXNullRef (a Nothing value encoded into the resx).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property IsResXNullRef() As Boolean
            Get
                Return IsResXNullRef(Me.ValueTypeName)
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff the type name given is the one used to mark a ResXDataNode as a ResXNullRef (a Nothing value encoded into the resx).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Shared ReadOnly Property IsResXNullRef(ByVal ValueTypeName As String) As Boolean
            Get
                Dim Match As Boolean = ValueTypeName.Equals(s_resXNullRefValueTypeName, StringComparison.Ordinal)
                Debug.Assert(Match = (ValueTypeName.Equals(s_resXNullRefValueTypeName, StringComparison.OrdinalIgnoreCase)), _
                    "ResXNullRef type name not should vary in case")
                Return Match
            End Get
        End Property


        ''' <summary>
        ''' Gets the resource type editor associated with this resource.  This value is calculated
        '''   during initialization and cached.  The resource value is never allowed to change in 
        '''   such a way that the associated resource type editor would have to change.
        ''' </summary>
        ''' <value>The associated resource type editor.</value>
        ''' <remarks>This property is never allowed to return Nothing after initialization is complete.</remarks>
        Public ReadOnly Property ResourceTypeEditor() As ResourceTypeEditor
            Get
                If _resourceTypeEditor Is Nothing Then
                    DetermineResourceTypeEditor()
                    Debug.Assert(_resourceTypeEditor IsNot Nothing, "DetermineResourceTypeEditor didn't set type editor")
                End If
                Return _resourceTypeEditor
            End Get
        End Property

        ''' <summary>
        '''  We keep the original order of the resource item, so we can keep the order when we save the file.
        ''' </summary>
        ''' <value></value>
        Friend ReadOnly Property OrderID() As Integer
            Get
                Return _orderID
            End Get
        End Property
#End Region

#Region "IComponent properties"

        ''' <summary>
        ''' (IComomponent) Gets or sets the ISite associated with the component.
        ''' </summary>
        ''' <value>The ISite object associated with the component, or Nothing.</value>
        ''' <remarks>
        ''' It would be good to not have a separate Name property, and just to use the
        '''   name stored in the site.  However, we plan on implementing delay-adding of
        '''   Resources to the host's container, in which case the resources won't be
        '''   sited.  Thus, we need to keep these two versions of the name separate but
        '''   in sync.
        ''' </remarks>
        Friend Property IComponent_Site() As System.ComponentModel.ISite Implements IComponent.Site
            Get
                Return _site
            End Get
            Set(ByVal Value As System.ComponentModel.ISite)
                _site = Value
                Debug.Assert(_site Is Nothing OrElse _site.Name.Equals(Name, StringComparison.Ordinal), "Name property and ISite.Name are out of sync")
            End Set
        End Property

#End Region

#Region """Friendly"" properties"

        ''' <summary>
        ''' Returns the "friendly" version of the value type name of this resource.  I.e., the 
        '''   fully-qualified type name *without* the assembly information.  E.g., "System.Drawing.Bitmap".
        '''   This is used in the "Type" column of the resource string table.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property FriendlyValueTypeName() As String
            Get
                Return ValueTypeNameWithoutAssemblyInfo
            End Get
        End Property

        ''' <summary>
        ''' Returns the fully-qualified type name *without* the assembly information.  E.g., "System.Drawing.Bitmap".
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Friend ReadOnly Property ValueTypeNameWithoutAssemblyInfo() As String
            Get
                Dim TypeName As String = Me.ValueTypeName

                If IsResXNullRef(TypeName) Then
                    'Null/Nothing value
                    Return SR.GetString(SR.RSE_NothingValue)
                End If

                'Cut off the assembly info from the type, leaving just the fully-qualified name of the type
                If TypeName IsNot Nothing Then
                    Dim IndexOfFirstComma As Integer = TypeName.IndexOf(","c)
                    If IndexOfFirstComma >= 0 Then
                        TypeName = TypeName.Substring(0, IndexOfFirstComma)
                    End If
                End If

                Return TypeName
            End Get
        End Property


        ''' <summary>
        ''' Retrieves a friendly description of the type of resource for the user (e.g., "Icon", "Windows Metafile").
        ''' Used in the type column of the resource listview.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property FriendlyTypeDescription() As String
            Get
                CacheFriendlyTypeAndSize()
                If _cachedImageProperties IsNot Nothing Then
                    Return _cachedImageProperties.FriendlyTypeDescription
                End If

                Return ""
            End Get
        End Property


        ''' <summary>
        ''' Retrieves a string representing the "size" of the resource, e.g. "240 x 128"
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property FriendlySize() As String
            Get
                CacheFriendlyTypeAndSize()
                If _cachedImageProperties IsNot Nothing Then
                    Return _cachedImageProperties.FriendlySize
                End If

                Return ""
            End Get
        End Property

#End Region

#End Region


#Region "Getting/setting the Value of the Resource"

        ''' <summary>
        ''' Attempts to retrieve or calculate the actual value of the resource.  
        ''' If it's a linked resource and has not yet been cached, it will attempt
        '''   to load the value from the linked file.
        ''' Returns Nothing on failure, or if a ResXNullRef (catches and ignores
        '''   all exceptions).
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function TryGetValue() As Object
            Try
                Return GetValue()
            Catch ex As Exception
                RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True) 'We ignore OOM - the resource may simply be big.  We're okay dealing with that.
                Return Nothing
            End Try
        End Function


        ''' <summary>
        ''' Attempts to retrieve or calculate the actual value of the resource.  
        ''' If it's a linked resource and has not yet been cached, it will attempt
        '''   to load the value from the linked file.
        ''' Throws exceptions on failure.  Returns Nothing if a ResXNullRef.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetValue() As Object Implements ResourceTypeEditor.IResource.GetValue
            Debug.Assert(ValueTypeName <> "")

            Debug.Assert(_resourceTypeEditor Is Nothing OrElse Not TypeOf _resourceTypeEditor Is ResourceTypeEditorFileBase, "Perf warning: calling GetValue() on a text/binary file resource - that shouldn't happen")

            'Is this value supposed to be Nothing?
            If IsResXNullRef Then
                Debug.Assert(_cachedValue Is Nothing)
                ClearTask(ResourceFile.ResourceTaskType.CantInstantiateResource)
                Return Nothing
            Else
                'See if we have a cached value
                Dim CachedValue As Object = Me.CachedValue
                If CachedValue IsNot Nothing Then
                    Return CachedValue
                End If
            End If

            'Try to instantiate the value.  This will throw if it fails.
            Dim Value As Object = Nothing
            Try
                If TypeOf TypeResolutionContext Is ITypeResolutionService Then
                    Value = _resXDataNode.GetValue(DirectCast(TypeResolutionContext, ITypeResolutionService))
                ElseIf TypeOf TypeResolutionContext Is AssemblyName() Then
                    Value = _resXDataNode.GetValue(DirectCast(TypeResolutionContext, AssemblyName()))
                Else
                    Debug.Fail("TypeResolutionContext was of an unexpected type")
                End If

                ' Resources can be stored as byte arrays in the resource file.  See if the resource type editor
                ' can understand the byte array.
                If TypeOf Value Is Byte() AndAlso _resourceTypeEditor IsNot Nothing Then
                    Dim ConvertedValue As Object = Me.ResourceTypeEditor.ConvertByteArrayToResourceValue(DirectCast(Value, Byte()))
                    If ConvertedValue IsNot Nothing Then
                        Value = ConvertedValue
                    End If
                End If

                Debug.Assert(Value IsNot Nothing, "Resource value shouldn't be Nothing unless it's a resxnullref")

                'We were able to successfully load the resource.  Clear the resource instantiation
                '  error for this resoucre, if there was one.
                ClearTask(ResourceFile.ResourceTaskType.CantInstantiateResource)
            Catch ex As Exception
                'Create task list entry
                SetTaskFromGetValueException(ex, ex)

                'Rethrow the exception for the caller to catch
                Throw ex
            End Try

            'Cache the newly-obtained value
            _cachedValue = New WeakReference(Value)

            CacheFriendlyTypeAndSize()
            Return Value
        End Function


        ''' <summary>
        ''' Given an exception that was thrown during GetValue or CheckValueForErrors, create a task list
        '''   entry from that exception (if one doesn't already exist).  Handles some special cases.
        ''' </summary>
        ''' <param name="ExceptionFromGetValue">The exception to create the task list entry from.</param>
        ''' <param name="ExceptionToRethrow">The exception to rethrow, if any.  May be the same exeption as ExceptionFromGetValue or different.</param>
        ''' <remarks></remarks>
        Friend Sub SetTaskFromGetValueException(ByVal ExceptionFromGetValue As Exception, ByRef ExceptionToRethrow As Exception)
            RethrowIfUnrecoverable(ExceptionFromGetValue)

            'We hit an exception.  Add a task list item for resource instantiation (if it doesn't already exist)

            If TypeOf ExceptionFromGetValue Is TargetInvocationException Then
                'Dig out and use the inner exception instead
                ExceptionToRethrow = ExceptionFromGetValue.InnerException
            Else
                ExceptionToRethrow = ExceptionFromGetValue
            End If

            If TypeOf ExceptionToRethrow Is ArgumentException Then
                ExceptionToRethrow = New ApplicationException(SR.GetString(SR.RSE_Err_BadData))
            End If

            'But first, we'd like to know whether it was because the linked file couldn't be found (we
            '  show a specialized message for that)
            Dim ErrorMessage As String
            Dim HelpLink As String
            If Not IsLink OrElse File.Exists(AbsoluteLinkPathAndFileName) Then
                'Regular message - not caused by a broken link.
                ErrorMessage = SR.GetString(SR.RSE_Task_CantInstantiate_2Args, Name, ExceptionToRethrow.Message)
                HelpLink = HelpIDs.Task_CantInstantiate
            Else
                'Broken link
                ErrorMessage = SR.GetString(SR.RSE_Task_BadLink_2Args, Name, AbsoluteLinkPathAndFileName)
                HelpLink = HelpIDs.Task_BadLink
            End If

            SetTask(ResourceFile.ResourceTaskType.CantInstantiateResource, ErrorMessage, Shell.TaskPriority.Normal, HelpLink)
        End Sub




        ''' <summary>
        ''' Sets the Value property of the Resource component.  This is the member which should be
        '''   used normally (including changing the property in response to user manipulation), since it
        '''   enables the undo engine to undo this change.
        ''' Can be used for both non-linked and linked resources.  In the linked case, setting this
        '''   only sets the current *cached* value for the resource.
        ''' </summary>
        ''' <param name="NewResourceValue"></param>
        ''' <remarks>
        ''' The reason this is not a property get/set like Name and Comment is that retrieving the Value
        '''   can cause exceptions, and using GetValue/SetValue/TryGetValue helps to clarify this fact.
        ''' </remarks>
        Public Sub SetValue(ByVal NewResourceValue As Object)
            If IsLink Then
                'Linked resources use Value only for caching the current value pulled from the disk.
                '  So setting Value on a linked resource only sets the current cached value.  It does
                '  not need to go through the undo mechanism, and does not need to dirty the designer.
                SetValueWithoutUndo(NewResourceValue)
            Else
                'To enable Undo, we must go through a property descriptor...
                s_propertyDescriptor_ValueAsObject.SetValue(Me, NewResourceValue)
            End If
        End Sub


        ''' <summary>
        ''' Sets the Value property of the Resource component, without undo support, and without causing the 
        '''  designer to be dirtied (because ComponentChangeService notifications won't get sent)
        ''' Can be used for both non-linked and linked resources.  In the linked case, setting this
        '''   only sets the current *cached* value for the resource.
        ''' </summary>
        ''' <param name="NewResourceValue">The new value to set the Value property to.</param>
        ''' <remarks></remarks>
        Private Sub SetValueWithoutUndo(ByVal NewResourceValue As Object)
            If IsResXNullRef Then
                Debug.Assert(NewResourceValue Is Nothing, "Can't set a non-Nothing value to a ResXNullRef resource")
                Exit Sub
            Else
                Debug.Assert(ValueTypeName <> "")
                If NewResourceValue Is Nothing Then
                    'Value is Nothing

                    If IsLink Then
                        'This is okay - we're just invalidating our cached value and forcing an update for the UI
                        _cachedValue = Nothing
                    Else
                        Debug.Fail("NewResourceValue is Nothing - trying to set bad value into resource")
                        Exit Sub
                    End If
                Else
                    'Value is not Nothing

                    'Verify that the new value is of the same type as before.
                    If Not GetValueType().Equals(NewResourceValue.GetType()) Then
                        'Exception to this rule.  We need to handle Byte() <-> MemoryStream conversion automatically
                        If NewResourceValue.GetType().Equals(GetType(Byte())) AndAlso GetValueType().Equals(GetType(MemoryStream)) Then
                            NewResourceValue = New MemoryStream(DirectCast(NewResourceValue, Byte()))
                        ElseIf NewResourceValue.GetType().Equals(GetType(MemoryStream)) AndAlso GetValueType().Equals(GetType(Byte())) Then
                            NewResourceValue = DirectCast(NewResourceValue, MemoryStream).ToArray()
                        Else
                            Throw NewException(SR.GetString(SR.RSE_Err_UnexpectedResourceType), HelpIDs.Err_UnexpectedResourceType)
                        End If
                    End If

                    If IsLink Then
                        'This is a linked resource - just change the cached value
                        Debug.Assert(NewResourceValue IsNot Nothing, "Should have already checked for this above")
                        _cachedValue = New WeakReference(NewResourceValue)
                    Else
                        'Not a link.  Change the actual persisted value.  Current cache will be invalidated below in InvalidateCachedInfoAndThumbnail()
                        _resXDataNode = NewResXDataNode(Me.Name, Me.Comment, NewResourceValue)

                        'If the user just assigned (through import) a new value into the resource that has an extension that
                        '  doesn't match our saved filename, it doesn't make sense to remember the old saved name, because if we
                        '  go back to it, it will no longer make much sense.
                        InvalidateCachedInfoAndThumbnail() 'Must invalidate cache before calling into ResourceTypeEditor or they'll get the wrong result calling GetValue()
                        If _savedFileName <> "" AndAlso ResourceTypeEditor.GetResourceFileExtension(Me) <> Path.GetExtension(_savedFileName) Then
                            _savedFileName = Nothing
                        End If
                    End If
                End If

                'In all cases, invalidate ourselves to allow the UI to be updated, including the
                '  thumbnail.
                InvalidateCachedInfoAndThumbnail()

                '... and finally, check for errors (note that this call depends on the values
                '  we have just set).
                CheckValueForErrors(DelayValueInstantiation:=False)
            End If
        End Sub

#End Region

#Region "Getting the ValueType of the Resource"

        'NOTE: could return Nothing in these cases:
        '  1) The resource is Nothing (ResXNullRef)
        '  2) There was an error in the resx file
        '  3) There's an exception trying to load the type
        Public Function GetValueType() As Type Implements ResourceTypeEditor.IResource.GetValueType
            Return GetValueTypeHelper(Me.ValueTypeName, ThrowOnError:=True)
        End Function

        Public Shared Function GetValueType(ByVal ValueTypeName As String) As Type
            Return GetValueTypeHelper(ValueTypeName, ThrowOnError:=True)
        End Function

        Private Shared Function GetValueTypeHelper(ByVal ValueTypeName As String, ByVal ThrowOnError As Boolean) As Type
            If IsResXNullRef(ValueTypeName) Then
                'It's a ResXNullRef node
                Return Nothing
            Else
                Return Type.GetType(ValueTypeName, ThrowOnError, ignoreCase:=True)
            End If
        End Function

        Public Function TryGetValueType() As Type
            Return GetValueTypeHelper(Me.ValueTypeName, ThrowOnError:=False)
        End Function

        Public Shared Function TryGetValueType(ByVal ValueTypeName As String) As Type
            Return GetValueTypeHelper(ValueTypeName, ThrowOnError:=False)
        End Function

#End Region

#Region "UI caching and invalidation"

        ''' <summary>
        ''' Calculate and cache certain information about a resource.  This info is cached
        '''   because retrieving their values requires having the current value of the Resource, which 
        '''   which don't want to keep longer than necessary (e.g., it might be a large bitmap).
        ''' </summary>
        ''' <remarks>
        ''' Note that we cache the values even if the calls fail, so that we do not keep retrying on
        '''   every virtual listview paint.
        ''' </remarks>
        Private Sub CacheFriendlyTypeAndSize()
            If _cachedImageProperties IsNot Nothing Then
                'Already cached.
                Exit Sub
            End If

            _cachedImageProperties = New ImagePropertiesCache

            'Calculate the values.
            With _cachedImageProperties
                Try
                    .FriendlySize = ResourceTypeEditor.GetResourceFriendlySize(Me)
                Catch ex As Exception
                    'Ignore exceptions, including out of memory - just use empty string
                    RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True)
                End Try

                Try
                    .FriendlyTypeDescription = ResourceTypeEditor.GetResourceFriendlyTypeDescription(Me)
                Catch ex As Exception
                    'Ignore exceptions, including out of memory - just use empty string
                    RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True)
                End Try
            End With
        End Sub


        ''' <summary>
        ''' Invalidates any currently stored cached thumbnail and image properties, as well as any
        '''   UI view, so that the resource may be fully updated, including thumbnail.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub InvalidateCachedInfoAndThumbnail()
            If ParentResourceFile IsNot Nothing Then
                _cachedValue = Nothing
                _cachedImageProperties = Nothing
                _parentResourceFile.InvalidateResourceInView(Me, InvalidateThumbnail:=True)
            End If
        End Sub


        ''' <summary>
        ''' Invalidates any current UI view of this resource, so that it may be updated.  If this resource
        '''   has not yet been parented, this is a NOOP.
        ''' Does *not* update the current thumbnail or image properties (for that, use InvalidateCachedInfoAndThumbnail)
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub InvalidateUI()
            If _parentResourceFile IsNot Nothing Then
                _parentResourceFile.InvalidateResourceInView(Me, InvalidateThumbnail:=False)
            End If
        End Sub

#End Region

#Region "Determine the resource type editor"

        ''' <summary>
        ''' Determines the correct ResourceTypeEditor instance to use for a Resource with the given
        '''   properties.  The ResourceTypeEditor is the resource editor's (possibly extensible) way
        '''   of providing a different editor experience for each type of resource.  This function
        '''   is *not* guaranteed to return the same instance of a resource type editor for each
        '''   type of resource.  I.e., different calls to this function may return different
        '''   instances of a ResourceTypeEditorString class.  Therefore, to compare ResourceTypeEditor
        '''   instances returned from this function, use ResourceTypeEditor.Equals().
        ''' </summary>
        ''' <remarks>
        ''' This never throws exceptions and never returns Nothing.
        ''' </remarks>
        Private Sub DetermineResourceTypeEditor()
            Dim TypeEditor As ResourceTypeEditor = Nothing
            Dim ValueTypeName As String = Me.ValueTypeName 'This is expensive, only call it once

            If IsResXNullRef Then
                TypeEditor = ResourceTypeEditors.Nothing
            ElseIf ValueTypeName = "" Then
                Debug.Fail("ValueTypeName should only be empty if this is a ResXNullRef reference")
                TypeEditor = ResourceTypeEditors.NonStringConvertible 'defensive                    
            Else
                Dim ValueType As Type = TryGetValueType(ValueTypeName)
                If ValueType Is Nothing Then
                    'Apparently we couldn't load the type (assembly not found, etc.).  This item will have to show
                    '  up in the "Others" category.
                    TypeEditor = ResourceTypeEditors.NonStringConvertible
                ElseIf IsLink AndAlso ValueType.Equals(ResourceTypeEditorTextFile.TextFileValueType) Then
                    'It's a link to a text file
                    TypeEditor = ResourceTypeEditors.TextFile
                ElseIf ValueType.Equals(GetType(Byte())) OrElse ValueType.Equals(GetType(MemoryStream)) Then
                    'It's a link to a binary or audio file or it might be an embedded audio file

                    If IsLink AndAlso ValueType.Equals(GetType(Byte())) Then
                        'Unless we decide specifically otherwise, we will normally treat binary files as
                        '  binary data.
                        TypeEditor = ResourceTypeEditors.BinaryFile
                    End If

                    'Exception - we may need to treat it as an audio file.  Since there's no good typed
                    '  story for an audio file in the resx file, its type can be either byte array or memory stream.
                    If IsLink AndAlso ResourceTypeEditors.Audio.GetExtensionPriority(Path.GetExtension(AbsoluteLinkPathAndFileName)) > 0 Then
                        'Audio type editor says it can handle this extension - let it handle it.
                        TypeEditor = ResourceTypeEditors.Audio
                    ElseIf IsLink AndAlso ValueType.Equals(GetType(Byte())) Then

                        ' Bitmap and icons may be stored as a linked resource with byte array as the type.
                        ' Select the resource type editor using the extension of the file name in the link.
                        Dim Extension As String = Path.GetExtension(Me.AbsoluteLinkPathAndFileName)

                        If ResourceTypeEditors.Bitmap.GetExtensionPriority(Extension) > 0 Then
                            TypeEditor = ResourceTypeEditors.Bitmap
                        ElseIf ResourceTypeEditors.Icon.GetExtensionPriority(Extension) > 0 Then
                            TypeEditor = ResourceTypeEditors.Icon
                        End If

                    ElseIf Not IsLink Then
                        'Normally we don't want to instantiate the resource during initialization (it slows down
                        '  startup), but here we have no choice.  We have to crack the binary data to determine if
                        '  this is a .wav file disguised as a memorystream.
                        Dim Value As Object = TryGetValue()
                        If Value IsNot Nothing Then
                            Try
                                If TypeOf Value Is Byte() AndAlso Utility.IsWavSoundFile(DirectCast(Value, Byte())) Then
                                    'Okay, it's a non-linked memory blob of a .wav file.  Treat it as such.
                                    TypeEditor = ResourceTypeEditors.Audio
                                ElseIf TypeOf Value Is MemoryStream AndAlso Utility.IsWavSoundFile(DirectCast(Value, MemoryStream)) Then
                                    'Okay, it's a non-linked memory blob of a .wav file.  Treat it as such.
                                    TypeEditor = ResourceTypeEditors.Audio
                                End If
                            Catch ex As Exception
                                RethrowIfUnrecoverable(ex)
                                'Ignore any problems (there shouldn't be) - better to handle this incorrect as a binary 
                                '  blob than to throw an exception during the loading of the resx.
                            End Try
                        End If
                    End If
                End If

                If TypeEditor Is Nothing Then
                    'This isn't a type that we recognized via special case.  Ask the type system if it knows what editor
                    '  we should be using.  (Most of our intrinsic editors have this hooked up in the shared constructor
                    '  of ResourceTypeEditor.)

                    'NOTE: we should not assume that this returns a separate instance each time - however, currently it 
                    '  caches based on type, so we do not need to do any caching of our own.
                    'If the type system doesn't know, it will return Nothing.
                    'See comments in ResourceTypeEditor for how this gets hooked up.

                    TypeEditor = DirectCast(System.ComponentModel.TypeDescriptor.GetEditor(ValueType, GetType(ResourceTypeEditor)), ResourceTypeEditor)
                End If
            End If

            If TypeEditor Is Nothing Then
                'Okay, we still haven't come up with a satisfactory resource type editor.  We have two choices left:  Either we
                '  use the string-convertible type editor (which means we can treat its value as a string and use
                '  converters to convert back and forth), or the non-string-convertible type editor (which means we
                '  show only the type of the resource and don't let the user edit anything)
                If IsConvertibleFromToString(ValueTypeName, IsResXNullRef) Then
                    TypeEditor = ResourceTypeEditors.StringConvertible
                Else
                    TypeEditor = ResourceTypeEditors.NonStringConvertible
                End If
            End If

            Debug.Assert(TypeEditor IsNot Nothing, "Huh?  We should have a type editor by now - can't have a ResourceTypeEditor of Nothing")
            _resourceTypeEditor = TypeEditor
        End Sub

#End Region

#Region "Links and file watching"

        ''' <summary>
        ''' Sets the filename for a linked resource.  Also automatically adds a filewatcher entry.
        ''' </summary>
        ''' <param name="PathAndFileName">File and path to the linked file</param>
        ''' <remarks></remarks>
        Public Sub SetLink(ByVal PathAndFileName As String)
            RemoveFileWatcherEntry()

            PathAndFileName = Common.Utils.GetFullPathTolerant(PathAndFileName)
            _resXDataNode = NewResXDataNode(Name, Comment, PathAndFileName, ValueTypeName, Encoding)

            'Set a file watch on the new filename/path
            AddFileWatcherEntry()
        End Sub


        ''' <summary>
        ''' Sets up a FileWatcher entry for this resource if it is a linked resource.
        '''   times.
        ''' </summary>
        ''' <remarks>It's okay to call this multiple times.</remarks>
        Public Sub AddFileWatcherEntry(ByVal Watcher As FileWatcher)
            If Watcher IsNot Nothing Then
                If AbsoluteLinkPathAndFileName <> "" Then
                    Watcher.WatchFile(AbsoluteLinkPathAndFileName, Me)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Sets up a FileWatcher entry for this resource if it is a linked resource, and if the resource is contained
        '''   in a resource file.
        '''   times.
        ''' </summary>
        ''' <remarks>It's okay to call this multiple times.</remarks>
        Private Sub AddFileWatcherEntry()
            If AbsoluteLinkPathAndFileName <> "" AndAlso _parentResourceFile IsNot Nothing Then
                AddFileWatcherEntry(_parentResourceFile.RootComponent.RootDesigner.GetView().FileWatcher)
            End If
        End Sub


        ''' <summary>
        ''' Removes the file watcher entry for the linked filename, if this resource is a link, and if the resource
        '''   is contained in a ResourceFile.
        ''' <param name="Watcher">The FileWatcher instance from which to remove the entry</param>
        ''' </summary>
        ''' <remarks>It's okay to call this multiple times.</remarks>
        Public Sub RemoveFileWatcherEntry(ByVal Watcher As FileWatcher)
            If AbsoluteLinkPathAndFileName <> "" Then
                If Watcher IsNot Nothing Then
                    Watcher.UnwatchFile(AbsoluteLinkPathAndFileName, Me)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Removes the file watcher entry for the linked filename, if this resource is a link.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RemoveFileWatcherEntry()
            If AbsoluteLinkPathAndFileName <> "" Then
                If _parentResourceFile IsNot Nothing Then
                    RemoveFileWatcherEntry(_parentResourceFile.RootComponent.RootDesigner.GetView().FileWatcher)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Called when the file watcher notices a change, deletion, creation, rename, etc. of the file
        '''   pointed to by this resource (if it's a linked resource).  We use this to update the
        '''   currently cached value of the resource, and to update the thumbnail and other UI.
        ''' </summary>
        ''' <param name="FileNameAndPath">The file name and path of the file that was changed.</param>
        ''' <remarks></remarks>
        Private Sub IFileWatcherListener_OnFileChanged(ByVal FileNameAndPath As String) Implements FileWatcher.IFileWatcherListener.OnFileChanged
            If Not IsLink Then
                Debug.Fail("FileWatcherEntry.FileChanged fired on a Resource, but the resource is not a link")
                Exit Sub
            End If
            Debug.Assert(Common.GetFullPathTolerant(FileNameAndPath).Equals(Common.GetFullPathTolerant(AbsoluteLinkPathAndFileName), StringComparison.OrdinalIgnoreCase))

            If ResourceTypeEditor Is Nothing Then
                Debug.Fail("Resource.OnFileChanged - there's no ResourceTypeEditor - ignoring")
                Exit Sub
            End If

            If _parentResourceFile.RootComponent.RootDesigner.IsInReloading Then
                ' We will be reloaded soon, ignore those events...
                Exit Sub
            End If

            'Make sure the UI gets updated
            If IsLink Then
                'For linked resources, we simply set the cache to Nothing.  This will invalidate the cache,
                '  ensure that we update the thumbnail, and also check for errors and therefore update
                '  the task list.
                SetValueWithoutUndo(Nothing)
            Else
                Debug.Fail("A non-linked resource had a file listener?")
            End If
        End Sub

#End Region

#Region "Task List integration"

        ''' <summary>
        ''' Clears a particular type of task list entry from this resource, if it exists.
        ''' </summary>
        ''' <param name="TaskType">The type of task list entry/error to clear.</param>
        ''' <remarks></remarks>
        Public Sub ClearTask(ByVal TaskType As ResourceFile.ResourceTaskType)
            If _parentResourceFile IsNot Nothing Then
                _parentResourceFile.ClearResourceTask(Me, TaskType)
            End If
        End Sub


        ''' <summary>
        ''' Adds a task list entry of a particular type for this Resource.  If there is already an old
        '''   one of this same type for this resource, it is removed (if it's different).
        ''' </summary>
        ''' <param name="TaskType">The type of task list entry to add</param>
        ''' <param name="Text">The text of the task list entry/error message.</param>
        ''' <param name="Priority">The priority.</param>
        ''' <param name="HelpLink">The help link of the new task list entry.</param>
        ''' <param name="ErrorCategory">The ErrorCategory of the new task list entry. It is an Error or Warning.</param>
        ''' <remarks></remarks>
        Public Sub SetTask(ByVal TaskType As ResourceFile.ResourceTaskType, ByVal Text As String, ByVal Priority As TaskPriority, ByVal HelpLink As String, Optional ByVal ErrorCategory As TaskErrorCategory = Shell.TaskErrorCategory.Error)
            If _parentResourceFile IsNot Nothing Then
                _parentResourceFile.SetResourceTask(Me, TaskType, Text, Priority, HelpLink, ErrorCategory)
            End If
        End Sub


        ''' <summary>
        ''' Lazy-initializes and gets a list of names which are not recommended for use by 
        '''  the end user (because they cause compiler errors or other problems).
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private Shared ReadOnly Property UnrecommendedResourceNamesHash() As Hashtable
            Get
                If s_unrecommendedResourceNamesHash Is Nothing Then
                    s_unrecommendedResourceNamesHash = New Hashtable(StringComparer.OrdinalIgnoreCase)

                    'Names which conflict with members of Object will cause compile
                    '  errors, so add those to the unrecommended list.
                    Dim ObjectMembers As MemberInfo() = GetType(Object).GetMembers()
                    For Each Member As MemberInfo In ObjectMembers
                        If Not s_unrecommendedResourceNamesHash.ContainsKey(Member.Name) Then
                            s_unrecommendedResourceNamesHash.Add(Member.Name, True)
                        End If
                    Next
                End If

                Return s_unrecommendedResourceNamesHash
            End Get
        End Property


        ''' <summary>
        ''' Checks this resource for any errors.  If any are found (and we're currently
        '''   part of a ResourceFile), then they are automatically added to the task list.
        ''' </summary>
        ''' <param name="FastChecksOnly">If true, then we'll only do quick checks (i.e., we're loading, etc.).</param>
        ''' <remarks></remarks>
        Public Sub CheckForErrors(ByVal FastChecksOnly As Boolean)
            If _parentResourceFile Is Nothing Then
                Exit Sub
            End If

            CheckNameForErrors(FastChecksOnly)
            CheckValueForErrors(FastChecksOnly)
            CheckCommentForErrors()
        End Sub


        ''' <summary>
        ''' Checks this resource's Name property for any errors.  If any are found (and we're currently
        '''   part of a ResourceFile), then they are automatically added to the task list.
        ''' <param name="FastChecksOnly">If true, then we'll only do quick checks (i.e., we're loading, etc.).</param>
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub CheckNameForErrors(ByVal FastChecksOnly As Boolean)
            If _parentResourceFile Is Nothing Then
                'If there's no parent resource file, we can't set any tasks, so don't bother
                '  with the checks
                Exit Sub
            End If

            'Is it a legal identifier?  We only check this if strongly-typed resource generation is set up for this
            '  resx file.
            If Not FastChecksOnly AndAlso _parentResourceFile IsNot Nothing AndAlso _parentResourceFile.IsGeneratedToCode() Then
                Dim CodeDomProvider As CodeDomProvider = _parentResourceFile.GetCodeDomProvider
                If CodeDomProvider IsNot Nothing Then
                    If Not CodeDomProvider.IsValidIdentifier(Name) Then
                        SetTask(ResourceFile.ResourceTaskType.BadName, SR.GetString(SR.RSE_Task_InvalidName_1Arg, Name), Shell.TaskPriority.Low, "", Shell.TaskErrorCategory.Warning)
                        Exit Sub
                    End If
                Else
                    Debug.Fail("Couldn't get CodeDomProvider to validate resource name")
                End If
            End If

            'Is it in our list of unrecommended names?
            If UnrecommendedResourceNamesHash.ContainsKey(Name) Then
                SetTask(ResourceFile.ResourceTaskType.BadName, SR.GetString(SR.RSE_Task_NonrecommendedName_1Arg, Name), Shell.TaskPriority.Low, HelpIDs.Task_NonrecommendedName, Shell.TaskErrorCategory.Warning)
                Exit Sub
            End If

            'No problems found.
            ClearTask(ResourceFile.ResourceTaskType.BadName)
        End Sub


        ''' <summary>
        ''' Checks this resource's Value property for any errors.  If any are found (and we're currently
        '''   part of a ResourceFile), then they are automatically added to the task list.
        ''' </summary>
        ''' <param name="DelayValueInstantiation">If true, then we won't check for anything but obvious errors (otherwise it can be expensive)</param>
        ''' <remarks></remarks>
        Private Sub CheckValueForErrors(ByVal DelayValueInstantiation As Boolean)
            If _parentResourceFile Is Nothing Then
                'If there's no parent resource file, we can't set any tasks, so don't bother
                '  with the checks
                Exit Sub
            End If

            If IsResXNullRef Then
                'No problems possible for a value of Nothing.
                ClearTask(ResourceFile.ResourceTaskType.CantInstantiateResource)
            Else
                If CachedValue IsNot Nothing Then
                    'No current problems - we already have a cached value for this resource.
                    ClearTask(ResourceFile.ResourceTaskType.CantInstantiateResource)
                    Exit Sub
                End If

                If DelayValueInstantiation Then
                    'We don't want to try instantiating the resource yet (we're loading),
                    '  so we'll delay this check for later.
                    'Don't clear the task list entry, 'cause it might still apply, we don't know...
                    Exit Sub
                End If

                'Check if we can instantiate the resource at all, by simply trying it.
                'This call will automatically update the task list according to the results.
                Try
                    ResourceTypeEditor.CheckValueForErrors(Me)

                    'No exceptions were thrown, so no task list entries should be associated with this.
                    ClearTask(ResourceFile.ResourceTaskType.CantInstantiateResource)
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True) 'We want task list entries for out of memory loading a resource

                    'Create task list entry
                    SetTaskFromGetValueException(ex, ex)
                End Try

                'Nothing else to do

            End If
        End Sub


        ''' <summary>
        ''' Checks this resource's Comment property for any errors.  If any are found (and we're currently
        '''   part of a ResourceFile), then they are automatically added to the task list.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub CheckCommentForErrors()
            If _parentResourceFile Is Nothing Then
                'If there's no parent resource file, we can't set any tasks, so don't bother
                '  with the checks
                Exit Sub
            End If

            '
            'Currently no checks are necessary for the comment field
            '

            ClearTask(ResourceFile.ResourceTaskType.CommentsNotSupportedInThisFile)
        End Sub

#End Region

#Region "Type conversions"

        ''' <summary>
        ''' Gets a type converter that handles the type of values stored in a resource with the specified properties.
        ''' </summary>
        ''' <param name="ValueTypeName">The type name of the value handled by this resource.  May be empty only for ResXNullRef's.</param>
        ''' <param name="IsResXNullRef">True iff the resource is a ResXNullRef reference.</param>
        ''' <returns>The property type converter for this type of resource.  If none is found, returns Nothing.</returns>
        ''' <remarks></remarks>
        Private Shared Function GetTypeConverter(ByVal ValueTypeName As String, ByVal IsResXNullRef As Boolean) As TypeConverter
            If IsResXNullRef Then
                Return Nothing
            Else
                Debug.Assert(ValueTypeName <> "")
                Dim ValueType As Type = TryGetValueType(ValueTypeName)
                If ValueType Is Nothing Then
                    Return Nothing
                Else
                    Return TypeDescriptor.GetConverter(ValueType)
                End If
            End If
        End Function


        ''' <summary>
        ''' Gets a type converter that handles the type of values stored in a resource with the specified properties.
        ''' </summary>
        ''' <param name="Value">The current Value property of the resource.  May be Nothing.</param>
        ''' <param name="ValueTypeName">The type name of the value handled by this resource.  May be empty only for ResXNullRef's.</param>
        ''' <param name="IsResXNullRef">True iff the resource is a ResXNullRef reference.</param>
        ''' <returns>The property type converter for this type of resource.  If none is found, returns Nothing.</returns>
        ''' <remarks></remarks>
        Private Shared Function GetTypeConverter(ByVal Value As Object, ByVal ValueTypeName As String, ByVal IsResXNullRef As Boolean) As TypeConverter
            If Value Is Nothing Then
                Return GetTypeConverter(ValueTypeName, IsResXNullRef)
            Else
                Return TypeDescriptor.GetConverter(Value)
            End If
        End Function


        ''' <summary>
        ''' Gets a type converter that handles the type of values stored in this resource.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetTypeConverter() As TypeConverter
            Return GetTypeConverter(CachedValue, ValueTypeName, IsResXNullRef)
        End Function


        ''' <summary>
        ''' Determines if a resource with the given properties is convertible both to and from string values.
        ''' </summary>
        ''' <param name="ValueTypeName">The type name of the value handled by this resource.  May be empty only for ResXNullRef's.</param>
        ''' <param name="IsResXNullRef">True iff the resource is a ResXNullRef reference.</param>
        ''' <returns>True if it is convertible both to and from string values.</returns>
        ''' <remarks></remarks>
        Private Shared Function IsConvertibleFromToString(ByVal ValueTypeName As String, ByVal IsResXNullRef As Boolean) As Boolean
            Return IsConvertibleFromToString(Nothing, ValueTypeName, IsResXNullRef)
        End Function


        ''' <summary>
        ''' Determines if a resource with the given properties is convertible both to and from string values.
        ''' </summary>
        ''' <param name="Value">The current Value property of the resource.  May be Nothing.</param>
        ''' <param name="ValueTypeName">The type name of the value handled by this resource.  May be empty only for ResXNullRef's.</param>
        ''' <param name="IsResXNullRef">True iff the resource is a ResXNullRef reference.</param>
        ''' <returns>True if it is convertible both to and from string values.</returns>
        ''' <remarks></remarks>
        Private Shared Function IsConvertibleFromToString(ByVal Value As Object, ByVal ValueTypeName As String, ByVal IsResXNullRef As Boolean) As Boolean
            Dim TC As TypeConverter = GetTypeConverter(Value, ValueTypeName, IsResXNullRef)
            If TC IsNot Nothing Then
                If TC.GetType.Equals(GetType(System.Windows.Forms.CursorConverter)) Then
                    'The CursorConverter lies to us - says it's convertible from/to string.  But this is only true from the
                    '  property sheet for the standard cursors that they support from the Windows Forms designer.  If we happen
                    '  upon a custom cursor in the resx file (possible but not likely), this would get us into trouble.
                    Return False
                End If

                Return TC.CanConvertFrom(GetType(String)) AndAlso TC.CanConvertTo(GetType(String))
            Else
                Return False
            End If
        End Function


        ''' <summary>
        ''' Determines if this resource is convertible both to and from string values.
        ''' </summary>
        ''' <returns>True if it is convertible both to and from string values.</returns>
        ''' <remarks></remarks>
        Public Function IsConvertibleFromToString() As Boolean
            Return IsConvertibleFromToString(CachedValue, ValueTypeName, IsResXNullRef)
        End Function

#End Region

#Region "Integration with Visual Studio's Properties Window"

        ''' <summary>
        '''  Returns a collection of property descriptors to describle this resource's properties for the Property Window.
        ''' </summary>
        ''' <returns>A PropertyDescriptorCollection containing the properties we want to show.</returns>
        ''' <remarks>
        '''   The Resource class registers ResourceTypeDescriptionProvider (via a TypeDescriptorProvider attribute on Class
        '''   Resource) as the class that will provide a description for the Resource instead of itself.
        '''   The Property Window will ask ResourceTypeDescriptionProvider about the Resource.
        '''   ResourceTypeDescriptionProvider will return a ResourceTypeDescriptor, and ResourceTypeDescriptor
        '''   will call Resource.GetProperties to get what it needs.
        '''
        '''   Another option would be to implement ICustomTypeDescriptor on Resource itself, but going this way saves
        '''   us the hassle of implementing everything on ICustomDescriptor. Instead, we only override what we need.
        ''' </remarks>
        Friend Function GetProperties() As PropertyDescriptorCollection
            Dim HashKey As Object = ValueTypeName & "|" & ResourceTypeEditor.GetType.AssemblyQualifiedName

            If Not s_propertyDescriptorCollectionHash.ContainsKey(HashKey) Then
                Dim PropertyDescriptorArrayList As New System.Collections.ArrayList

                'Register properties: Name, Comment, Filename, Type, Persistence
                'These are all the same no matter what kind of resource value we're looking at
                PropertyDescriptorArrayList.Add(s_propertyDescriptor_Name)
                PropertyDescriptorArrayList.Add(s_propertyDescriptor_Comment)
                PropertyDescriptorArrayList.Add(s_propertyDescriptor_Filename_ReadOnly)
                PropertyDescriptorArrayList.Add(s_propertyDescriptor_Type)

                '"Encoding" property
                If ResourceTypeEditor.Equals(ResourceTypeEditors.TextFile) Then
                    PropertyDescriptorArrayList.Add(s_propertyDescriptor_Encoding)
                End If

                '"FileType" property
                If TypeOf ResourceTypeEditor Is ResourceTypeEditorFileBase Then
                    PropertyDescriptorArrayList.Add(s_propertyDescriptor_FileType)
                End If

                '"Persistence" property -  read/write or read/only, depending on the resource type
                If ResourceTypeEditor.CanChangePersistenceProperty(Me.ParentResourceFile) Then
                    PropertyDescriptorArrayList.Add(s_propertyDescriptor_Persistence)
                Else
                    PropertyDescriptorArrayList.Add(s_propertyDescriptor_Persistence_ReadOnly)
                End If

                '"Value" property.  We only show this property for strings (we could do it for other types, but there
                '  turn out to be lots of special exceptions and issues with various types).
                If ResourceTypeEditor.Equals(ResourceTypeEditors.String) Then
                    PropertyDescriptorArrayList.Add(s_propertyDescriptor_ValueAsString)
                End If

                'Create the properties collection
                Dim PropertyDescriptorArray(PropertyDescriptorArrayList.Count - 1) As PropertyDescriptor
                PropertyDescriptorArrayList.CopyTo(PropertyDescriptorArray, 0)
                Dim Properties As New PropertyDescriptorCollection(PropertyDescriptorArray)

                '... and add it to our hash table
                s_propertyDescriptorCollectionHash.Add(HashKey, Properties)
                Debug.Assert(s_propertyDescriptorCollectionHash.ContainsKey(HashKey))
            End If

            Return DirectCast(s_propertyDescriptorCollectionHash(HashKey), PropertyDescriptorCollection)
        End Function


        ''' <summary>
        '''  Returns the value of the specified property for the property grid.
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to get the value for.</param>
        ''' <returns>An Object containing the value.</returns>
        ''' <remarks>ResourceTypeDescriptor will call this to ask for each property's value</remarks>
        Friend Function GetPropertyValue(ByVal PropertyName As String) As Object
            Select Case PropertyName
                Case ResourcePropertyDescriptor.PROPERTY_COMMENT
                    Return Me.Comment
                Case ResourcePropertyDescriptor.PROPERTY_ENCODING
                    Debug.Assert(ResourceTypeEditor.Equals(ResourceTypeEditors.TextFile))
                    'Have to use SerializableEncoding as the public type of this property because the Undo engine requires
                    '  it to be serializable (and Encoding is not)
                    Return New SerializableEncoding(Me.Encoding)
                Case ResourcePropertyDescriptor.PROPERTY_FILENAME
                    If Me.AbsoluteLinkPathAndFileName <> "" Then
                        Return Common.Utils.GetFullPathTolerant(Me.AbsoluteLinkPathAndFileName)
                    Else
                        Return ""
                    End If
                Case ResourcePropertyDescriptor.PROPERTY_FILETYPE
                    Return Me.FileType
                Case ResourcePropertyDescriptor.PROPERTY_NAME
                    Return Me.Name
                Case ResourcePropertyDescriptor.PROPERTY_PERSISTENCE
                    Return Me.PersistenceMode
                Case ResourcePropertyDescriptor.PROPERTY_TYPE
                    Return Me.ValueTypeName
                Case ResourcePropertyDescriptor.PROPERTY_VALUE
                    'If this throws an exception, the exception text will be displayed in the properties window
                    Return GetValue()
                Case Else
                    Debug.Fail("Unknown property!!!")
                    Return Nothing
            End Select
        End Function


        ''' <summary>
        '''  Sets the specified property's value to the specified value (after validating that it's valid)
        ''' </summary>
        ''' <param name="PropertyName">A property name that we registered.</param>
        ''' <param name="Value">The value to set that property to.</param>
        ''' <remarks>
        ''' </remarks>
        Friend Sub SetPropertyValue(ByVal PropertyName As String, ByVal Value As Object)
            Select Case PropertyName
                Case ResourcePropertyDescriptor.PROPERTY_COMMENT
                    Comment = CStr(Value)
                Case ResourcePropertyDescriptor.PROPERTY_ENCODING
                    'Have to use SerializableEncoding as the public type of this property because the Undo engine requires
                    '  it to be serializable (and Encoding is not)
                    Encoding = DirectCast(Value, SerializableEncoding).Encoding
                Case ResourcePropertyDescriptor.PROPERTY_FILENAME
                    FileName = DirectCast(Value, String)
                Case ResourcePropertyDescriptor.PROPERTY_FILETYPE
                    FileType = DirectCast(Value, FileTypes)
                Case ResourcePropertyDescriptor.PROPERTY_NAME
                    Name = CStr(Value)
                Case ResourcePropertyDescriptor.PROPERTY_PERSISTENCE
                    PersistenceMode = DirectCast(Value, ResourcePersistenceMode)
                Case ResourcePropertyDescriptor.PROPERTY_VALUE
                    Debug.Assert(Not IsLink, "Shouldn't be setting Value for linked resources (Value = cached current value in this case, could be considered read only) through property descriptors for linked files")

                    'The Value that is passed in to us has already been converted from a String (if from the properties
                    '   window) to the correct type, or else it's already of the correct type, so we do not need to do 
                    '   any validation here.  Just try to set the new value.
                    SetValue(Value)
                Case Else
                    Debug.Fail("Unknown or read-only property!!!")
            End Select
        End Sub

        ''' <summary>
        '''  Sets the specified property's value to the specified value (after validating that it's valid)
        ''' </summary>
        ''' <param name="PropertyName">A property name that we registered.</param>
        ''' <param name="Value">The value to set that property to.</param>
        ''' <remarks>ResourceTypeDescriptor will call this to set the value of the property.
        '''   Dirtying is done by the individual property sets themselves.
        '''   Any exceptions thrown from this subroutine will be displayed in a messagebox by the properties
        '''   window (which is what we want if there are validation errors).
        ''' </remarks>
        Friend Sub SetPropertyValueWithoutUndo(ByVal PropertyName As String, ByVal Value As Object)
            Select Case PropertyName
                Case ResourcePropertyDescriptor.PROPERTY_COMMENT
                    CommentWithoutUndo = CStr(Value)
                Case ResourcePropertyDescriptor.PROPERTY_ENCODING
                    'Have to use SerializableEncoding as the public type of this property because the Undo engine requires
                    '  it to be serializable (and Encoding is not)
                    EncodingWithoutUndo = DirectCast(Value, SerializableEncoding).Encoding
                Case ResourcePropertyDescriptor.PROPERTY_FILENAME
                    FileNameWithoutUndo = DirectCast(Value, String)
                Case ResourcePropertyDescriptor.PROPERTY_FILETYPE
                    FileTypeWithoutUndo = DirectCast(Value, FileTypes)
                Case ResourcePropertyDescriptor.PROPERTY_NAME
                    NameWithoutUndo = CStr(Value)
                Case ResourcePropertyDescriptor.PROPERTY_PERSISTENCE
                    PersistenceWithoutUndo = DirectCast(Value, ResourcePersistenceMode)
                Case ResourcePropertyDescriptor.PROPERTY_VALUE
                    Debug.Assert(Not IsLink, "Shouldn't be setting Value for linked resources (Value = cached current value in this case, could be considered read only) through property descriptors for linked files")

                    'The Value that is passed in to us has already been converted from a String (if from the properties
                    '   window) to the correct type, or else it's already of the correct type, so we do not need to do 
                    '   any validation here.  Just try to set the new value.
                    SetValueWithoutUndo(Value)
                Case Else
                    Debug.Fail("Unknown or read-only property!!!")
            End Select
        End Sub


        ''' <summary>
        ''' Called by the custom property descriptors when a value should be reset.
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <remarks>
        ''' Note: Undo *is* hooked up for this method.
        ''' </remarks>
        Friend Sub ResetPropertyValue(ByVal PropertyName As String)
            Select Case PropertyName
                Case ResourcePropertyDescriptor.PROPERTY_ENCODING
                    ResetEncoding()
                Case Else
                    Debug.Fail("No Reset hooked up in Resource for this property")
            End Select
        End Sub

#End Region

#Region "ISerialization implementation"

        'Our key for the ResXDataNode in the serialization info.
        Private Const s_SERIALIZATIONKEY_RESXDATANODE As String = "ResXDataNode"
        Private Const s_SERIALIZATIONKEY_SAVEDFILENAME As String = "SavedFileName"
        Private Const s_SERIALIZATIONKEY_ORIGINALFILETIMESTAMP As String = "OriginalFileTimeStamp"


        ''' <summary>
        ''' Deserialization constructor.
        ''' </summary>
        ''' <param name="Info"></param>
        ''' <param name="Context"></param>
        ''' <remarks>
        '''See .NET Framework Developer's Guide, "Custom Serialization" for more information
        ''' </remarks>
        Private Sub New(ByVal Info As SerializationInfo, ByVal Context As StreamingContext)
            Dim ResXDataNode As ResXDataNode = DirectCast(Info.GetValue(s_SERIALIZATIONKEY_RESXDATANODE, GetType(ResXDataNode)), ResXDataNode)
            _savedFileName = Info.GetString(s_SERIALIZATIONKEY_SAVEDFILENAME)
            _originalFileTimeStamp = Info.GetDateTime(s_SERIALIZATIONKEY_ORIGINALFILETIMESTAMP)

            'Hook up TypeNameConverter for fileref object from deserializer
            If ResXDataNode.FileRef IsNot Nothing Then
                ResXDataNode = NewResXDataNode(ResXDataNode.Name, ResXDataNode.Comment, ResXDataNode.FileRef)
            End If

            'We don't have a type resolution context, so we'll have to depend upon it getting set up when it's added
            '  to the components list.
            Init(ResXDataNode)
        End Sub

        ''' <summary>
        ''' Sets the SerializationInfo with information serialization information for this class.
        ''' </summary>
        ''' <param name="Info">The SerializationInfo to which to add serialized object data.</param>
        ''' <param name="Context">The StreamingContext that contains contextual information about the source or destination.</param>
        ''' <remarks>
        '''See .NET Framework Developer's Guide, "Custom Serialization" for more information
        ''' </remarks>
        Private Sub GetObjectData(ByVal Info As SerializationInfo, ByVal Context As StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
            Info.AddValue(s_SERIALIZATIONKEY_RESXDATANODE, _resXDataNode)
            Info.AddValue(s_SERIALIZATIONKEY_SAVEDFILENAME, VB.IIf(_savedFileName Is Nothing, "", _savedFileName))
            Info.AddValue(s_SERIALIZATIONKEY_ORIGINALFILETIMESTAMP, _originalFileTimeStamp)
        End Sub

#End Region

#Region "Validation"


        ''' <summary>
        ''' Validates a resource's Name
        ''' </summary>
        ''' <param name="NewName">The new to be validated</param>
        ''' <param name="OldName">The current name of the resource.</param>
        ''' <param name="NewFormattedName">The name that should be used instead (stripped of spaces)</param>
        ''' <param name="Exception">The exception to use if there's a problem with the validation.</param>
        ''' <returns>True if the name is okay.</returns>
        ''' <remarks></remarks>
        Public Function ValidateName(ByVal NewName As String, ByVal OldName As String, Optional ByRef NewFormattedName As String = Nothing, Optional ByRef Exception As Exception = Nothing) As Boolean
            Return ValidateName(Me.ParentResourceFile, NewName, OldName, NewFormattedName, Exception)
        End Function


        ''' <summary>
        ''' Validates a resource's Name
        ''' </summary>
        ''' <param name="ResourceFile">The ResourceFile to check for duplicate IDs from.  May be Nothing (in which case the duplicate name detection is skipped).</param>
        ''' <param name="NewName">The new to be validated</param>
        ''' <param name="OldName">The current name of the resource.</param>
        ''' <param name="NewFormattedName">The name that should be used instead (stripped of spaces)</param>
        ''' <param name="Exception">The exception to use if there's a problem with the validation.</param>
        ''' <param name="FixInvalidIDs">If true, then NewFormattedName will contain the name fixed up to be a legal identifier the way StronglyTypedResourceGenerator would do it (but only if the resx file is set up for strongly-typed code generation.</param>
        ''' <param name="CheckForDuplicateNames">If true, then the new names are checked to make sure they don't duplicate anything currently in the ResourceFile</param>
        ''' <returns>True if the name is okay.</returns>
        ''' <remarks></remarks>
        Public Shared Function ValidateName(ByVal ResourceFile As ResourceFile, ByVal NewName As String, Optional ByVal OldName As String = Nothing, Optional ByRef NewFormattedName As String = Nothing, Optional ByRef Exception As Exception = Nothing, Optional ByVal FixInvalidIDs As Boolean = False, Optional ByVal CheckForDuplicateNames As Boolean = True) As Boolean
            Dim NewNames(0), OldNames(0), NewFormattedNames(0) As String
            NewNames(0) = NewName
            OldNames(0) = OldName
            Dim Result As Boolean = ValidateNames(ResourceFile, NewNames, OldNames, NewFormattedNames, Exception, FixInvalidIDs, CheckForDuplicateNames)
            NewFormattedName = NewFormattedNames(0)
            Return Result
        End Function


        ''' <summary>
        ''' Validates a resource's Name
        ''' </summary>
        ''' <param name="ResourceFile">The ResourceFile to check for duplicate IDs from.  May be Nothing (in which case the duplicate name detection is skipped).</param>
        ''' <param name="NewNames">The list of new names to be validated</param>
        ''' <param name="OldNames">The list of current names of the resource.</param>
        ''' <param name="NewFormattedNames">[Out] The list of name that should be used instead (stripped of spaces)</param>
        ''' <param name="Exception">[Out] The exception to use if there's a problem with the validation.</param>
        ''' <param name="FixInvalidIDs">If true, then NewFormattedNames will contain names fixed up to be legal identifiers the way StronglyTypedResourceGenerator would do it (but only if the resx file is set up for strongly-typed code generation.</param>
        ''' <param name="CheckForDuplicateNames">If true, then the new names are checked to make sure they don't duplicate anything currently in the ResourceFile</param>
        ''' <returns>True if the name is okay.</returns>
        ''' <remarks></remarks>
        Private Shared Function ValidateNames(ByVal ResourceFile As ResourceFile, ByVal NewNames() As String, Optional ByVal OldNames() As String = Nothing, Optional ByRef NewFormattedNames() As String = Nothing, Optional ByRef Exception As Exception = Nothing, Optional ByVal FixInvalidIDs As Boolean = False, Optional ByVal CheckForDuplicateNames As Boolean = True) As Boolean
            Dim CodeDomProvider As CodeDomProvider = Nothing
            Dim CodeGenerator As ICodeGenerator = Nothing
            Dim CheckForInvalidIdentifiers As Boolean = True

            If CheckForInvalidIdentifiers Then
                If ResourceFile IsNot Nothing Then
                    CodeDomProvider = ResourceFile.GetCodeDomProvider()
                End If
                If CodeDomProvider Is Nothing Then
                    'If there's no ResourceFile, or no CodeDomProvider is available (e.g., when in C++), there's no need 
                    '  to check anything yet (the resource hasn't been added to a ResourceFile yet)
                    CheckForInvalidIdentifiers = False
                    FixInvalidIDs = False
                End If
            End If

            If (CheckForInvalidIdentifiers OrElse FixInvalidIDs) AndAlso ResourceFile IsNot Nothing Then
                If Not ResourceFile.IsGeneratedToCode() Then
                    'Strongly-typed code generation is not set up for this resx file, so we don't need to check for invalid identifiers - we can
                    '  just let the user enter whatever names their little heart desires.
                    CheckForInvalidIdentifiers = False
                    FixInvalidIDs = False
                End If
            End If

            If OldNames Is Nothing Then
                ReDim OldNames(NewNames.Length - 1)
            End If
            If NewFormattedNames Is Nothing Then
                ReDim NewFormattedNames(NewNames.Length - 1)
            End If
            Debug.Assert(NewNames.Length = OldNames.Length)
            Debug.Assert(NewNames.Length = NewFormattedNames.Length)

            For i As Integer = 0 To NewNames.Length - 1
                If Not ValidateNameHelper(ResourceFile, NewNames(i), OldNames(i), NewFormattedNames(i), CheckForInvalidIdentifiers, FixInvalidIDs, CodeDomProvider, CheckForDuplicateNames, Exception) Then
                    Debug.Assert(Exception IsNot Nothing)
                    Return False
                End If
            Next

            Return True
        End Function


        ''' <summary>
        ''' Checks the given list of resource names for validation, and fixes them up the way the strongly-typed 
        '''   resource generator would.  If any can't be fixed (e.g. "123"), throws an exception.
        ''' </summary>
        ''' <param name="ResourceFile">The ResourceFile where the resources will eventually be added.</param>
        ''' <param name="Resources">The list of resources whose names should be validated</param>
        ''' <param name="Fix">If true, the resource names are fixed to be legal identifiers (if not possible, will throw)</param>
        ''' <param name="CheckForDuplicateNames">If true, then the new names are checked to make sure they don't duplicate anything currently in the ResourceFile</param>
        ''' <remarks></remarks>
        Public Shared Sub CheckResourceIdentifiers(ByVal ResourceFile As ResourceFile, ByVal Resources As ICollection, ByVal Fix As Boolean, ByVal CheckForDuplicateNames As Boolean)
            Dim NewNames(Resources.Count - 1), NewFormattedNames(Resources.Count - 1) As String
            Dim i As Integer = 0
            For Each Resource As Resource In Resources
                NewNames(i) = Resource.Name
                i += 1
            Next

            Dim ex As Exception = Nothing
            If Not ValidateNames(ResourceFile, NewNames, , NewFormattedNames, ex, FixInvalidIDs:=Fix, CheckForDuplicateNames:=CheckForDuplicateNames) Then
                Throw ex
            End If

            i = 0
            If Fix Then
                For Each resource As Resource In Resources
                    If Not resource.Name.Equals(NewFormattedNames(i), StringComparison.Ordinal) Then
                        resource.Name = NewFormattedNames(i)
                    End If
                    i = i + 1
                Next
            End If
        End Sub


        ''' <summary>
        ''' Validates a resource's Name
        ''' </summary>
        ''' <param name="ResourceFile">The ResourceFile to check for duplicate IDs from.  May be Nothing (in which case the duplicate name detection is skipped).</param>
        ''' <param name="NewName">The new to be validated</param>
        ''' <param name="OldName">The current name of the resource.</param>
        ''' <param name="NewFormattedName">The name that should be used instead (stripped of spaces)</param>
        ''' <param name="CheckForFatallyInvalidIDs">If true, then identifiers which cannot be munged by ResGen to be valid (e.g. "1") will be considered an error.  Names which are invalid identifiers but which will be munged by ResGen into legal IDs are not affected.</param>
        ''' <param name="FixInvalidIDs">If true, then NewFormattedNames will contain names fixed up to be legal identifiers the way ResGen would do it (but only if the resx file is set up for strongly-typed code generation (e.g., "a b" would be changed to "a_b").</param>
        ''' <param name="CodeDomProvider">The code provider to check for invalid identifiers.</param>
        ''' <param name="Exception">[Out] The exception to use if there's a problem with the validation.</param>
        ''' <param name="CheckForDuplicateNames">If true, then the new names are checked to make sure they don't duplicate anything currently in the ResourceFile</param>
        ''' <returns>True if the name is okay.</returns>
        ''' <remarks></remarks>
        Private Shared Function ValidateNameHelper(ByVal ResourceFile As ResourceFile, _
                ByVal NewName As String, ByVal OldName As String, _
                ByRef NewFormattedName As String, _
                ByVal CheckForFatallyInvalidIDs As Boolean, ByVal FixInvalidIDs As Boolean, ByVal CodeDomProvider As CodeDomProvider, _
                ByVal CheckForDuplicateNames As Boolean, _
                ByRef Exception As Exception) As Boolean
            Debug.Assert(Implies(CheckForFatallyInvalidIDs, CodeDomProvider IsNot Nothing), "Must pass in CodeDomProvider if CheckForInvalidIdentifiers=true")

            If NewName Is Nothing Then
                NewName = ""
            End If
            If OldName Is Nothing Then
                OldName = ""
            End If

            'Has the Name actually changed?  If not, no need to do further testing...
            'Note that we compare against the original NewName, not the trimmed version (so that if there are actually
            '  blanks in the Name in the file, just moving through the cells will not cause validation to occur, and so that
            '  we will notice if the original Name had blanks and the user now removes them).
            If OldName <> "" AndAlso 0 = String.Compare(NewName, OldName, StringComparison.OrdinalIgnoreCase) Then
                'No change in Name - nothing to do
                NewFormattedName = NewName
                Return True
            End If

            'Now we want to trim all blanks from the Name's beginning and end only, in case the user typed them in accidently
            Dim TrimmedName As String
            If NewName <> "" Then
                TrimmedName = NewName.Trim()
            Else
                TrimmedName = ""
            End If

            'Verify that the Name is not blank
            If TrimmedName = "" Then
                Exception = NewException(SR.GetString(SR.RSE_Err_NameBlank), HelpIDs.Err_NameBlank)
                Return False
            End If

            'Verify the name is a valid identifier, if requested
            If (CheckForFatallyInvalidIDs OrElse FixInvalidIDs) AndAlso CodeDomProvider IsNot Nothing Then
                Dim FixedName As String = System.Resources.Tools.StronglyTypedResourceBuilder.VerifyResourceName(TrimmedName, CodeDomProvider)
                If FixedName = "" Then
                    'ResGen wasn't able to create a valid identifier out of the ID (e.g. something like "$")
                    If CheckForFatallyInvalidIDs Then
                        Exception = NewException(SR.GetString(SR.RSE_Err_BadIdentifier_2Arg, TrimmedName, FindInvalidCharactersInIdentifier(TrimmedName, CodeDomProvider)), HelpIDs.Err_InvalidName)
                        Return False
                    End If
                Else
                    'ResGen returned us the original name (if valid) or else a munged, valid name.  Use it if we're fixing up.
                    If FixInvalidIDs Then
                        TrimmedName = FixedName
                    End If
                End If
            End If

            'Finally, we have to make sure that the new Name is unique.  Since we already tested above that the Name
            '  typed in by the user is different than the current value in the Resource, and the value in the
            '  Resource (in the ResourceFile) has not yet been modified, we are guaranteed not to have
            '  NameExists find the current Resource based on the new Name.  Therefore, if it finds the new Name, then
            '  it means the new Name already exists and would be a duplicate.
            If CheckForDuplicateNames Then
                If ResourceFile IsNot Nothing Then
                    If ResourceFile.Contains(TrimmedName) Then
                        Exception = NewException(SR.GetString(SR.RSE_Err_DuplicateName_1Arg, TrimmedName), HelpIDs.Err_DuplicateName)
                        Return False
                    End If
                End If
            End If

            NewFormattedName = TrimmedName
            Return True
        End Function

        ''' <summary>
        '''  Find Invalid characters in the name, which blocks it to be an identifier.
        ''' </summary>
        ''' <param name="Name">The resource name</param>
        ''' <param name="CodeDomProvider"></param>
        ''' <returns>Returns a string of invalid characters</returns>
        ''' <remarks></remarks>
        Private Shared Function FindInvalidCharactersInIdentifier(ByVal Name As String, ByVal CodeDomProvider As CodeDomProvider) As String
            Dim unsupportedChars As String = String.Empty

            Debug.Assert(Name IsNot Nothing, "Invalid Name")
            Debug.Assert(CodeDomProvider IsNot Nothing, "Why we don't have a CodeDomProvider")

            For i As Integer = 0 To Name.Length - 1
                Dim ch As Char = Name.Chars(i)
                If Not ((ch >= "0"c AndAlso ch <= "9"c) OrElse (ch >= "a"c AndAlso ch <= "z"c) OrElse (ch >= "A"c AndAlso ch <= "Z"c)) Then
                    ' we expect all languages supports those characters in the identifier
                    ' Do not use Char.IsLetter, which accept other Unicode characters

                    Dim chstr As String = CStr(ch)
                    Dim unsupported As Boolean = False

                    If Char.IsSurrogate(ch) Then
                        ' We should treat Surrogate characters specially. Do not split them into two characters
                        If i < Name.Length - 1 AndAlso IsHighSurrogate(ch) AndAlso IsLowSurrogate(Name.Chars(i + 1)) Then
                            chstr = CStr(ch) + CStr(Name.Chars(i + 1))
                            i = i + 1
                        Else
                            ' broken surrogates are always invalid.
                            unsupported = True
                        End If
                    End If

                    If Not unsupported Then
                        If Name.IndexOf(chstr, 0, i) < 0 Then
                            If Not CodeDomProvider.IsValidIdentifier(String.Concat("a", chstr)) Then
                                unsupported = True
                            End If
                        End If
                    End If

                    If unsupported Then
                        unsupportedChars = String.Concat(unsupportedChars, chstr)
                    End If
                End If
            Next
            Return unsupportedChars
        End Function

        ''' <summary>
        ''' Validates a value as input as a string.
        ''' </summary>
        ''' <param name="NewFormattedValue">The value as entered by the user in string form.</param>
        ''' <param name="NewParsedValue">[Out] The actual value that was parsed from the string.</param>
        ''' <param name="Exception">[Out] The exception to use if the return was false.</param>
        ''' <returns>True if the validation succeeds, otherwise false.</returns>
        ''' <remarks></remarks>
        Public Function ValidateValueAsString(ByVal NewFormattedValue As String, Optional ByRef NewParsedValue As Object = Nothing, Optional ByRef Exception As Exception = Nothing) As Boolean
            'Use the Resource Type Editor (specifically, the methods derived inherited from ResourceTypeEditorStringBase)
            '  to try and get the formatted value for the cell.
            Dim StringResourceEditor As ResourceTypeEditorStringBase = DirectCast(Me.ResourceTypeEditor, ResourceTypeEditorStringBase)
            Dim FailureException As Exception = Nothing 'If we failed in the conversion, this will be set to the exception at the failure
            Dim ConvertedValue As Object = Nothing
            Try
                ConvertedValue = StringResourceEditor.StringParseFormattedCellValue(Me, CStr(NewFormattedValue))
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                FailureException = ex
            End Try

            If FailureException Is Nothing Then
                'So far, so good.  We were able to convert the string that the user typed in to the grid
                '  into a value.  However, we also need to make sure that the new value is convertible back to
                '  a string before accept it (for some unknown reason, you can convert any integer value into
                '  an enum, but trying to convert that enum value back into a string fails if the integer value
                '  was not valid for that enum.  Let's catch that up front before setting the value into the
                '  resource, or it will be too late and cause problems.
                Try
                    Dim DummyString As String = StringResourceEditor.StringGetFormattedCellValue(Me, ConvertedValue)
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    FailureException = ex
                End Try
            End If

            If FailureException IsNot Nothing Then
                'We failed the validation somewhere along the way.  Return a friendly error message, in case
                '  it's needed, and return failure.
                Exception = NewException(SR.GetString(SR.RSE_Err_CantConvertFromString_2Args, Me.FriendlyValueTypeName, FailureException.Message), HelpIDs.Err_CantConvertFromString)
                NewParsedValue = Nothing
                Return False
            End If

            If ConvertedValue Is Nothing Then
                Exception = NewException(SR.GetString(SR.RSE_Err_CantUseEmptyValue, Me.FriendlyValueTypeName), HelpIDs.Err_CantConvertFromString)
                NewParsedValue = Nothing
                Return False
            End If

            'Okay, we succeeded in the conversion.  Pass the information back and return success
            NewParsedValue = ConvertedValue
            Exception = Nothing
            Return True
        End Function

#End Region

#Region "Creating ResXDataNode instances"

        ''' <summary>
        ''' Creates a new ResXDataNode.
        ''' </summary>
        ''' <param name="Name">The name for the ResXDataNode</param>
        ''' <param name="Comment">The comment for the ResXDataNode</param>
        ''' <param name="Value">The non-linked value for the ResXDataNode</param>
        ''' <returns>The new ResXDataNode instance</returns>
        ''' <remarks></remarks>
        Private Function NewResXDataNode(ByVal Name As String, ByVal Comment As String, ByVal Value As Object) As ResXDataNode
            Dim Node As ResXDataNode
            If Value IsNot Nothing AndAlso Value.GetType().Equals(GetType(ResXFileRef)) Then
                Node = New ResXDataNode(Name, DirectCast(Value, ResXFileRef), AddressOf TypeNameConverter)
            Else
                Node = New ResXDataNode(Name, Value, AddressOf TypeNameConverter)
            End If

            Node.Comment = Comment
            Return Node
        End Function


        ''' <summary>
        ''' Creates a new ResXDataNode.
        ''' </summary>
        ''' <param name="Name">The name for the ResXDataNode</param>
        ''' <param name="Comment">The comment for the ResXDataNode</param>
        ''' <param name="FileName">The full path and file name to the file which contains the linked resource's value.</param>
        ''' <param name="ValueTypeName">The expected type of the resource in the file.</param>
        ''' <param name="TextFileEncoding">The encoding to be used if this is a text file resource.  May be Nothing.  Irrevelant for any other type of resource.</param>
        ''' <returns>The new ResXDataNode instance.</returns>
        ''' <remarks></remarks>
        Private Function NewResXDataNode(ByVal Name As String, ByVal Comment As String, ByVal FileName As String, ByVal ValueTypeName As String, ByVal TextFileEncoding As Encoding) As ResXDataNode
            Dim FileRef As New ResXFileRef(FileName, ValueTypeName, TextFileEncoding)
            Return NewResXDataNode(Name, Comment, FileRef)
        End Function

#End Region

#Region "Type resolution"

        ''' <summary>
        ''' Provides the resource with an object from which it can determine its type resolution context
        '''   (either an ITypeResolutionService from the project or else a default set of common
        '''   assemblies).
        ''' </summary>
        ''' <param name="TypeResolutionContextProvider"></param>
        ''' <remarks>
        ''' A ITypeResolutionContextProvider must be provided either when the resource is constructed
        '''   or as soon as possible thereafter (for the cases of copy/paste and Undo, i.e. serialization).
        ''' </remarks>
        Public Sub SetTypeResolutionContext(ByVal TypeResolutionContextProvider As ITypeResolutionContextProvider)
            If TypeResolutionContextProvider IsNot Nothing Then
                'We've been given a new context to query for a type resolution context.
                Dim TypeResolutionContext As Object = TypeResolutionContextProvider.GetTypeResolutionService()
                If TypeResolutionContext Is Nothing Then
                    'There was no type resolution service available (our .resx file must not be opened
                    '  inside a project).  So use a default set of assemblies instead.
                    TypeResolutionContext = ResourceEditorView.GetDefaultAssemblyReferences()
                End If

                Debug.Assert(_typeResolutionContext Is Nothing OrElse TypeResolutionContext Is _typeResolutionContext, "Why did we get a different type resolution context than before?")
                _typeResolutionContext = TypeResolutionContext
            End If
        End Sub


        ''' <summary>
        ''' Returns the type resolution context for this resource.  This is either an ITypeResolutionService instance 
        '''  (if the .resx file was opened inside the context of a project), or an array of AssemblyName
        '''  instances (if the .resx file was opened outside of a project in the current solution).  The 
        '''  ResXDataNode has some properties which require one or the other.  This stores the value we 
        '''  use for calling those properties.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' This value is set either at construction time or upon the first call to SetTypeResolutionContext
        ''' </remarks>
        Private ReadOnly Property TypeResolutionContext() As Object
            Get
                If _typeResolutionContext Is Nothing Then
                    Debug.Fail("Should have called SetTypeResolutionContext() by now.  Falling back to default set of assemblies.")
                    Return ResourceEditorView.GetDefaultAssemblyReferences()
                End If

                Return _typeResolutionContext
            End Get
        End Property

#End Region

#Region "Miscellaneous"

        ''' <summary>
        ''' Retrieves the category that this resource belongs to.  Cached because it's expensive to
        '''   calculate, and it needs to be reasonably fast.
        ''' </summary>
        ''' <param name="Categories">The list of categories used by the ResourceEditorView</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        ''' CONSIDER: consider performance tuning with a hash table type editor -> category hash
        Public Function GetCategory(ByVal Categories As CategoryCollection) As Category
            Debug.Assert(Categories IsNot Nothing)
            If _categoryCache Is Nothing Then
                For Each CategoryItem As Category In Categories
                    For Each TypeEditor As ResourceTypeEditor In CategoryItem.AssociatedResourceTypeEditors
                        If TypeEditor.Equals(ResourceTypeEditor) Then
                            _categoryCache = CategoryItem
                            Return _categoryCache
                        End If
                    Next
                Next
            End If

            Return _categoryCache
        End Function


        ''' <summary>
        ''' Debug override for ToString
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function ToString() As String
#If DEBUG Then
            Dim Message As New StringBuilder("[Resource """ & Name & """")

            'Type or type name
            Message.Append(", Type=")
            If ParentResourceFile IsNot Nothing Then
                If TryGetValueType() IsNot Nothing Then
                    Message.Append(TryGetValueType.Name)
                Else
                    Message.Append(ValueTypeName)
                End If
            Else
                Message.Append("<Unknown>")
            End If

            'Comment
            Message.Append(", Comment=""")
            Message.Append(Comment)
            Message.Append("""")

            'Value
            If TypeOf ResourceTypeEditor Is ResourceTypeEditorStringBase Then
                Message.Append(", Value=""")
                Dim Value As String
                If ParentResourceFile IsNot Nothing Then
                    Try
                        Value = DirectCast(ResourceTypeEditor, ResourceTypeEditorStringBase).StringGetFormattedCellValue(Me, Me.GetValue)
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        Value = ex.Message
                    End Try
                Else
                    Value = ""
                End If

                Const MaxValueLength As Integer = 100
                If VB.Len(Value) > MaxValueLength Then
                    Value = Value.Substring(0, MaxValueLength) & "..."
                End If
                Message.Append(Value)
            End If

            Message.Append("]")
            Return Message.ToString()
#Else
            Return MyBase.ToString()
#End If
        End Function


        ''' <summary>
        ''' If this is a text file, and the user hasn't already specified an encoding, then guess it by analyzing the file
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub TryGuessFileEncoding()
            If IsLink AndAlso _resXDataNode.FileRef.TextFileEncoding Is Nothing AndAlso ResourceTypeEditor.Equals(ResourceTypeEditors.TextFile) Then
                Try
                    Me.EncodingWithoutUndo = Utility.GuessFileEncoding(Me.AbsoluteLinkPathAndFileName)
                Catch ex As IOException
                    'Ignore
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex)
                    Debug.Fail("Unexpected failure in GuessFileEncoding - ignoring")
                End Try
            End If
        End Sub


#End Region

    End Class

End Namespace
