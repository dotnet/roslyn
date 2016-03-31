Imports System.ComponentModel
Imports System.ComponentModel.Design.Serialization
Imports System.io
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    <Serializable> _
    Friend NotInheritable Class GenericComponentSerializationStore
        Inherits SerializationStore
        Implements System.Runtime.Serialization.ISerializable


        'The set of objects (IComponent instances or properties) that we wish to
        '  "serialize" into this store.  The actual values won't be serialized
        '  until we're Close:d, until then this just keeps track of what we
        '  want to serialize.  It will be cleared out when we Close.
        'Private m_HashedObjectsToSerialize As New Dictionary(Of Object, ObjectData)
        Private m_HashedObjectsToSerialize As New Hashtable
        'The actual "serialized" data (binary serialized objects and
        '  property values) which is available after Close().  This data drives
        '  the deserialization process.
        Private m_SerializedState As ArrayList

        ''' <summary>
        ''' Public empty constructor
        ''' </summary>
        ''' <remarks>
        ''' 'cause this is a serializable, there is also a private Sub new - if we want to be able to 
        ''' create instances of this class without using serialization, we better define this guy!
        '''</remarks>
        Public Sub New()
        End Sub


        ' default impl of abstract base member.  see serialization store for details.
        '	
        public readonly Overrides property Errors() as ICollection
            get
                return new object(-1) {}
            end get
        end property


        ''' <summary>
        ''' The Close method closes this store and prevents any further objects 
        '''   from being serialized into it.  Once closed, the serialization store 
        '''   may be saved (or deserialized).
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub Close()
            If m_SerializedState Is Nothing Then
                Dim SerializedState As New ArrayList(m_HashedObjectsToSerialize.Count)
                'Go through each object that we wanted to save anything from...
                For Each Data As ObjectData In m_HashedObjectsToSerialize.Values
                    If Data.IsEntireObject Then
                        'We're saving the entire object.
                        '  The constructor for SerializedObjectData will do the
                        '  actual binary serialization for us.
                        SerializedState.Add(New SerializedObjectData(Data))
                    Else
                        'We're saving individual property values.  Go through each...
                        For Each Prop As PropertyDescriptor In Data.Members
                            '... and serialize it.
                            '  The constructor for SerializedObjectData will do the
                            '  actual binary serialization for us.
                            SerializedState.Add(New SerializedObjectData(Data, Prop))
                        Next
                    End If
                Next

                'Save what we've serialized, and clear out the old data - it's no longer
                '  needed.
                m_SerializedState = SerializedState
                m_HashedObjectsToSerialize = Nothing
            End If
        End Sub



#Region "ISerialization implementation"

        'Serialization keys for ISerializable
        Const KEY_STATE As String = "State"

        ''' <summary>
        '''     Implements the save part of ISerializable.
        '''   Only needed if you're using the store for copy/paste implementation.
        ''' </summary>
        ''' <param name="info">Serialization info</param>
        ''' <param name="context">Serialization context</param>
        ''' <remarks></remarks>
        <System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand, SerializationFormatter:=True)> _
        Public Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
            info.AddValue(KEY_STATE, m_SerializedState)
        End Sub


        ''' <summary>
        ''' Constructor used to deserialize ourselves from binary serialization.
        '''   Only needed if you're using the store for copy/paste implementation.
        ''' </summary>
        ''' <param name="Info">Serialization info</param>
        ''' <param name="Context">Serialization context</param>
        ''' <remarks></remarks>
        Private Sub New(ByVal Info As SerializationInfo, ByVal Context As StreamingContext)
            m_SerializedState = DirectCast(Info.GetValue(KEY_STATE, GetType(ArrayList)), ArrayList)
        End Sub

#End Region

#Region "Load/Save the store from/to a stream"

        ''' <summary>
        ''' Loads our state from a stream.
        ''' </summary>
        ''' <param name="Stream">The stream to load from</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function Load(ByVal Stream As IO.Stream) As GenericComponentSerializationStore
            Dim f As New BinaryFormatter
            Return DirectCast(f.Deserialize(Stream), GenericComponentSerializationStore)
        End Function

        ''' <summary>
        '''     The Save method saves the store to the given stream.  If the store 
        '''     is open, Save will automatically close it for you.  You 
        '''     can call save as many times as you wish to save the store 
        '''     to different streams.
        ''' </summary>
        ''' <param name="stream">The stream to save to</param>
        ''' <remarks></remarks>
        Public Overrides Sub Save(ByVal Stream As System.IO.Stream)
            Close()

            Dim f As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            f.Serialize(Stream, Me)
        End Sub

#End Region

#Region "Add objects and properties to be serialized at Close"

        ''' <summary>
        ''' Adds a new object serialization to our list of things to serialize.
        ''' </summary>
        ''' <param name="Component">The component to be serialized as an entire object.</param>
        ''' <remarks>
        ''' This is used by UndoEngine when an object is added or removed.
        ''' Again, the object isn't actually serialized until Close(), it's just put in
        '''   our list of things to be serialized.
        ''' </remarks>
        Public Sub AddObject(ByVal Component As Object)
            If m_SerializedState IsNot Nothing Then
                Debug.Fail("State already serialization, shouldn't be adding new stuff")
                Throw New Package.InternalException
            End If

            'Get the current object (or create a new one) that stores all info to
            '  be saved from this instance.
            With GetSerializationData(Component)
                '... and tell it to store the entire object
                .IsEntireObject = True
            End With
        End Sub


        ''' <summary>
        ''' Adds a new property serialization to our list of things to serialize.
        ''' </summary>
        ''' <param name="Component">The object whose property needs to be serialized.</param>
        ''' <param name="Member">The property descriptor which should be serialized.</param>
        ''' <remarks>
        ''' This is used by UndoEngine when a objects property is changed.
        ''' Again, the property isn't actually serialized until Close(), it's just put in
        '''   our list of things to be serialized.
        ''' </remarks>
        Friend Sub AddMember(ByVal Component As Object, ByVal Member As PropertyDescriptor)
            If m_SerializedState IsNot Nothing Then
                Debug.Fail("State already serialization, shouldn't be adding new stuff")
                Throw New Package.InternalException
            End If

            'Get the current object (or create a new one) that stores all info to
            '  be saved from this instance.
            With GetSerializationData(Component)
                '... and add this property to the list of properties that we want serialized from this object
                .Members.Add(Member)
            End With
        End Sub


        ''' <summary>
        ''' Gets the current data for the given object that is contained in
        '''   m_HashedObjectsToSerialize.  Or, if there isn't one already, creates a
        '''   new one.
        ''' </summary>
        ''' <param name="Component">The component from which we want to serialize something.</param>
        ''' <returns>The DataToSerialize object associated with this component.</returns>
        ''' <remarks></remarks>
        Private Function GetSerializationData(ByVal Component As Object) As ObjectData
            Dim Data As ObjectData
            If m_HashedObjectsToSerialize.ContainsKey(Component) Then
                Data = CType(m_HashedObjectsToSerialize(Component), ObjectData)
            Else
                'No object created for this object yet.  Create one now.
                Data = New ObjectData(Component)
                m_HashedObjectsToSerialize(Component) = Data
            End If

            Return Data
        End Function

#End Region

#Region "Deserialization of the saved objects/properties (used at Undo/Redo time)"


        ''' <summary>
        ''' Deserializes the saved bits.
        '''     This method deserializes the store, but rather than produce 
        '''     new objects object, the data in the store is applied to an existing 
        '''     set of objects that are taken from the provided container.  This 
        '''     allows the caller to pre-create an object however it sees fit.  If
        '''     an object has deserialization state and the object is not named in 
        '''     the set of existing objects, a new object will be created.  If that 
        '''     object also implements IComponent, it will be added to the given 
        '''     container.  Objects in the container must have names and types that 
        '''     match objects in the serialization store in order for an existing 
        '''     object to be used.
        ''' </summary>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <remarks></remarks>
        Friend Sub DeserializeTo(ByVal Container As IContainer)
            DeserializeHelper(Container, True)
        End Sub


        ''' <summary>
        ''' Deserializes the saved bits.
        '''     This method deserializes the store to produce a collection of 
        '''     objects contained within it.  If a container is provided, objects 
        '''     that are created that implement IComponent will be added to the container. 
        ''' </summary>
        ''' <returns>The set of components that were deserialized.</returns>
        ''' <remarks></remarks>
        Friend Function Deserialize() As ICollection
            Return DeserializeHelper(Nothing, False)
        End Function


        ''' <summary>
        ''' Deserializes the saved bits.
        '''     This method deserializes the store to produce a collection of 
        '''     objects contained within it.  If a container is provided, objects 
        '''     that are created that implement IComponent will be added to the container. 
        ''' </summary>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <returns>The list of objects that were deserialized.</returns>
        ''' <remarks></remarks>
        Friend Function Deserialize(ByVal Container As IContainer) As ICollection
            Return DeserializeHelper(Container, False)
        End Function


        ''' <summary>
        ''' This method does the actual deserialization work, based on the given
        '''   arguments.
        ''' </summary>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <param name="RecycleInstances">If True, we are applying property changes to existing
        '''   instances of components (this is always the case for Undo/Redo).</param>
        ''' <returns>The objects which have been serialized.</returns>
        ''' <remarks></remarks>
        Private Function DeserializeHelper(ByVal Container As IContainer, ByVal RecycleInstances As Boolean) As ICollection
            Dim NewObjects As New ArrayList(m_SerializedState.Count)

            'Handle each individual component or property at a time...
            For Each SerializedObject As SerializedObjectData In m_SerializedState
                If SerializedObject.IsEntireObject Then
                    '... we have an entire object.  Go ahead and create it from
                    '  the stored binary serialization.
                    '
                    'For entire objects, we ignore the value of RecycleInstances (the Undo engine
                    '  calls us with RecycleInstances=True for the delete/redo case - I would have expected
                    '  False, but either way, we need to create a new instance, so we'll just ignore that 
                    '  flag).

                    Dim NewComponent As Object = SerializedObject.DeserializeObject()

                    '... and add it to the store and list.
                    If Container IsNot Nothing AndAlso TypeOf NewComponent Is IComponent Then
                        Container.Add(DirectCast(NewComponent, IComponent), SerializedObject.ObjectName)
                    End If
                    NewObjects.Add(NewComponent)
                Else
                    'We have just a property to deserialize
                    Dim ComponentToSerializeTo As IComponent = Nothing
                    If RecycleInstances AndAlso Container IsNot Nothing Then
                        'We're applying this property to an existing object.  Need to
                        '  find it in the container's list of components.
                        ComponentToSerializeTo = Container.Components(SerializedObject.ObjectName)
                        If ComponentToSerializeTo Is Nothing Then
                            'Whoops, didn't find it.
                            ' CONSIDER: should we expose a "CreateComponent" method that you can override
                            ' in order to create specific objects?
                            Debug.Fail("Couldn't find component in the container - hard to recycle an unknown component!")
                        End If
                    End If

                    If ComponentToSerializeTo Is Nothing Then
                        Debug.Fail("We didn't find the component to serialize to, and we haven't provided a mechanism to create a new component - this will be a NOOP!")
                    Else
                        'Deserialize the property value and apply it to the object
                        Dim pd As PropertyDescriptor = TypeDescriptor.GetProperties(ComponentToSerializeTo).Item(SerializedObject.PropertyName)
                        If pd Is Nothing Then
                            Debug.Fail("Failed to find named property descriptor on object!")
                        Else
                            pd.SetValue(ComponentToSerializeTo, SerializedObject.DeserializeObject())
                        End If

                        '... and add the component to our list
                        If Not NewObjects.Contains(ComponentToSerializeTo) Then
                            NewObjects.Add(ComponentToSerializeTo)
                        End If
                    End If
                End If
            Next

            'Return all Resources that were affected by the deserialization.
            Return NewObjects
        End Function

#End Region

#Region "Private class - ObjectData"

        ''' <summary>
        ''' Keeps track of everything that we want to serialized about a single
        '''   object instance. (either the entire object itself, or a set of
        '''   its properties)
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Protected Class ObjectData

            'Backing for public properties
            Private m_IsEntireObject As Boolean
            Private m_Members As ArrayList
            Private m_Value As Object
            Private m_ObjectName As String

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Value">The component from which we want to serialize stuff.</param>
            ''' <remarks></remarks>
            Public Sub New(ByVal Value As Object)
                If Value Is Nothing Then
                    Throw New ArgumentNullException("Value")
                End If

                ' If it is an IComponent, we'll try to get its name from 
                ' its site
                If TypeOf Value Is IComponent Then
                    Dim comp As IComponent = DirectCast(Value, IComponent)
                    If comp.Site IsNot Nothing Then
                        m_ObjectName = comp.Site.Name
                    End If
                End If

                If m_ObjectName = "" Then
                    ' We better create a unique name for this guy...
                    m_ObjectName = Guid.NewGuid.ToString().Replace("-", "_")
                End If

                ' Store the value for later
                m_Value = Value
            End Sub

            ''' <summary>
            ''' Get tha name of this object
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Name() As String
                Get
                    Return m_ObjectName
                End Get
            End Property

            ''' <summary>
            ''' The object from which we want to serialize stuff.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Value() As Object
                Get
                    Return m_Value
                End Get
            End Property


            ''' <summary>
            ''' If True, the entire Resource instance should be serialized.  If false,
            '''   then only the properties in PropertiesToSerialize should be serialized.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public Property IsEntireObject() As Boolean
                Get
                    Return m_IsEntireObject
                End Get
                Set(ByVal Value As Boolean)
                    If Value AndAlso m_Members IsNot Nothing Then
                        m_Members.Clear()
                    End If
                    m_IsEntireObject = Value
                End Set
            End Property


            ''' <summary>
            ''' A list of PropertyDescriptors representing the properties on
            '''   the Resource which should be serialized.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Public ReadOnly Property Members() As ArrayList
                Get
                    If m_Members Is Nothing Then
                        m_Members = New ArrayList
                    End If
                    Return m_Members
                End Get
            End Property

        End Class 'ObjectData

#End Region

#Region "Private class - SerializedObjectData"

        ''' <summary>
        ''' Keeps track of everything that we want to serialized about a single
        '''   Resource instance (either the entire Resoure itself, or a set of
        '''   its properties)
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Private Class SerializedObjectData

            'Backing for public properties
            Private m_ObjectName As String
            Private m_PropertyName As String
            Private m_SerializedValue As Byte()

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Value">The component from which we want to serialize stuff.</param>
            ''' <remarks></remarks>
            Friend Sub New(ByVal Value As ObjectData)
                If Value Is Nothing Then
                    Throw New ArgumentNullException("Value")
                End If
                m_ObjectName = Value.Name
                m_SerializedValue = SerializeObject(Value.Value)
            End Sub

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Value">The component from which we want to serialize stuff.</param>
            ''' <remarks></remarks>
            Public Sub New(ByVal Value As ObjectData, ByVal [Property] As PropertyDescriptor)
                If Value Is Nothing Then
                    Throw New ArgumentNullException("Value")
                End If
                If [Property] Is Nothing Then
                    Throw New ArgumentNullException("Property")
                End If

                m_ObjectName = Value.Name
                m_PropertyName = [Property].Name
                m_SerializedValue = SerializeObject([Property].GetValue(Value.Value))
            End Sub

            ''' <summary>
            ''' If True, the entire Resource instance should be serialized.  If false,
            '''   then only the properties in PropertiesToSerialize should be serialized.
            ''' </summary>
            ''' <value></value>
            ''' <remarks></remarks>
            Friend ReadOnly Property IsEntireObject() As Boolean
                Get
                    Return m_PropertyName = ""
                End Get
            End Property

            Friend ReadOnly Property ObjectName() As String
                Get
                    Return m_ObjectName
                End Get
            End Property

            Friend ReadOnly Property PropertyName() As String
                Get
                    Return m_PropertyName
                End Get
            End Property

            Friend Shared Function SerializeObject(ByVal [Object] As Object) As Byte()
                If [Object] Is Nothing Then
                    Return New Byte() {}
                Else
                    Dim MemoryStream As New MemoryStream
                    Call (New BinaryFormatter()).Serialize(MemoryStream, [Object])
                    Return MemoryStream.ToArray()
                End If
            End Function

            Public Function DeserializeObject() As Object
                If m_SerializedValue.Length = 0 Then
                    Return Nothing
                Else
                    Dim MemoryStream As New MemoryStream(m_SerializedValue)
                    Return (New BinaryFormatter).Deserialize(MemoryStream)
                End If
            End Function

        End Class
#End Region

    End Class

End Namespace
