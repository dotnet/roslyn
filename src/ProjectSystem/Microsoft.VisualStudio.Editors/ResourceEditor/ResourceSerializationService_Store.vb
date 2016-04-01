' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary


Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    'This class is private to ResourceSerializationService

    Friend Partial Class ResourceSerializationService

        ''' <summary>
        ''' Comments from the SerializationStore class which this derives from:
        '''
        '''      The SerializationStore class is an implementation-specific class that stores 
        '''      serialization data for the component serialization service.  The 
        '''      service adds state to this serialization store.  Once the store is 
        '''      closed it can be saved to a stream.  A serialization store can be 
        '''      deserialized at a later date by the same type of serialization service.  
        '''      SerializationStore implements the IDisposable interface such that Dispose 
        '''      simply calls the Close method.  Dispose is implemented as a private 
        '''      interface to avoid confusion.  The IDisposable pattern is provided 
        '''      for languages that support a "using" syntax like C# and VB .NET.
        '''
        ''' The way this class works is as follows...  It does not serialize in
        '''   quite the same manner as you would normally think of for serialization.
        '''   What it does is to keep a list of all Resources (as an entire object - for 
        '''   instances, when a Resource is deleted or created) and also all individual
        '''   Resource properties (when a property value is changed) that you want to
        '''   serialize.  The serialization doesn't actually  happen until the object is
        '''   closed, because it does not have access to the current property values
        '''   until then.  Usually, a single Resource or property is added to this store
        '''   and then it's immediately closed (not clear if there's ever a scenario
        '''   where multiple objects will be added to this store in the resource editor,
        '''   but I believe if you set multiple properties during a single transaction, this might
        '''   happen).
        ''' When the store is closed, the resource objects are binary serialized and the 
        '''   property values are serialized, and they are stored in m_SerializedState.
        '''   There's no need for the actual ResourceSerializationStore class itself
        '''   to be serialized unless you're using this for Copy/Paste.  The UndoEngine
        '''   simply keeps the ResourceSerializationStore instance itself around until
        '''   it's no longer needed.
        ''' Technically the Resource objects and property values don't have to be binary
        '''   serialized or serialized at all, but they do have to be cloned (and cloned
        '''   again on deserialization), because the UndoEngine needs to get a copy of
        '''   the object at its original value, and not with any changes that might have
        '''   been made since.  Binary serialization is the easiest way to accomplish this.
        ''' When the UndoEngine performs an Undo or a Redo, it asks the serialization store
        '''   to "deserialize" itself into a container (DesignerLoader's list of 
        '''   components).  Thus, deserialization is the act of restoring old Resource
        '''   instances that no longer exist, or of re-applying old property values to
        '''   an existing Resource instance.
        ''' </summary>
        ''' <remarks></remarks>
        <Serializable()> _
        Private NotInheritable Class ResourceSerializationStore
            Inherits System.ComponentModel.Design.Serialization.SerializationStore
            Implements System.Runtime.Serialization.ISerializable



            'The set of objects (Resource instances or properties) that we wish to
            '  "serialize" into this store.  The actual values won't be serialized
            '  until we're Close'd, until then this just keeps track of what we
            '  want to serialize.  It will be cleared out when we Close.
            Private _hashedObjectsToSerialize As Hashtable 'Of ResourceSerializationData


            'The actual "serialized" data (binary serialized Resource instances and
            '  property values) which is available after Close().  This data drives
            '  the deserialization process.
            Private _serializedState As ArrayList


            ' default impl of abstract base member.  see serialization store for details.
            '	
            Public Overrides ReadOnly Property Errors() As ICollection
                Get
                    Return New Object(-1) {}
                End Get
            End Property


            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <remarks></remarks>
            Public Sub New()
                _hashedObjectsToSerialize = New Hashtable

                Trace("Created new store")
            End Sub




            ''' <summary>
            ''' The Close method closes this store and prevents any further objects 
            '''   from being serialized into it.  Once closed, the serialization store 
            '''   may be saved (or deserialized).
            ''' </summary>
            ''' <remarks></remarks>
            Public Overrides Sub Close()
                If _serializedState Is Nothing Then
                    Dim SerializedState As New ArrayList(_hashedObjectsToSerialize.Count)

                    Trace("Closing Store: serializing {0} objects", _hashedObjectsToSerialize.Count)

                    'Go through each object that we wanted to save anything from...
                    For Each Entry As DictionaryEntry In _hashedObjectsToSerialize
                        Dim Data As ResourceDataToSerialize = DirectCast(Entry.Value, ResourceDataToSerialize)
                        If Data.EntireObject Then
                            'We're saving the entire Resource object.
                            '  The constructor for SerializedResourceOrProperty will do the
                            '  actual binary serialization for us.
                            Dim SerializedData As New SerializedResourceOrProperty(Data.Resource)
                            SerializedState.Add(SerializedData)
                        Else
                            'We're saving individual property values.  Go through each...
                            For Each Prop As PropertyDescriptor In Data.PropertiesToSerialize
                                '... and serialize it.
                                '  The constructor for SerializedResourceOrProperty will do the
                                '  actual binary serialization for us.
                                Dim SerializedData As New SerializedResourceOrProperty(Data.Resource, Prop)
                                SerializedState.Add(SerializedData)
                            Next
                        End If
                    Next

                    'Save what we've serialized, and clear out the old data - it's no longer
                    '  needed.
                    _serializedState = SerializedState
                    _hashedObjectsToSerialize = Nothing
                End If
            End Sub


#Region "ISerialization implementation"

            'Serialization keys for ISerializable
            Private Const s_KEY_STATE As String = "State"
            Private Const s_KEY_OBJECTNAMES As String = "ObjectNames"

            ''' <summary>
            '''     Implements the save part of ISerializable.
            '''   Only needed if you're using the store for copy/paste implementation.
            ''' </summary>
            ''' <param name="info">Serialization info</param>
            ''' <param name="context">Serialization context</param>
            ''' <remarks></remarks>
            Private Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
                info.AddValue(s_KEY_STATE, _serializedState)

                Trace("Serialized store (GetObjectData)")
            End Sub


            ''' <summary>
            ''' Constructor used to deserialize ourselves from binary serialization.
            '''   Only needed if you're using the store for copy/paste implementation.
            ''' </summary>
            ''' <param name="Info">Serialization info</param>
            ''' <param name="Context">Serialization context</param>
            ''' <remarks></remarks>
            Private Sub New(ByVal Info As SerializationInfo, ByVal Context As StreamingContext)
                _serializedState = DirectCast(Info.GetValue(s_KEY_STATE, GetType(ArrayList)), ArrayList)

                Trace("Deserialized store from a stream (constructor)")
            End Sub

#End Region


#Region "Load/Save the store from/to a stream"

            ''' <summary>
            ''' Loads our state from a stream.
            ''' </summary>
            ''' <param name="Stream">The stream to load from</param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Shared Function Load(ByVal Stream As Stream) As ResourceSerializationStore
                Dim f As New BinaryFormatter
                Return DirectCast(f.Deserialize(Stream), ResourceSerializationStore)
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

                Trace("Saved store")
            End Sub

#End Region


#Region "Add Resources and Resource properties to be serialized at Close"

            ''' <summary>
            ''' Adds a new object serialization to our list of things to serialize.
            ''' </summary>
            ''' <param name="Resource">The Resource to be serialized as an entire object.</param>
            ''' <remarks>
            ''' This is used by UndoEngine when a resource is added or removed.
            ''' Again, the resource isn't actually serialized until Close(), it's just put in
            '''   our list of things to be serialized.
            ''' </remarks>
            Public Sub AddResource(ByVal Resource As Resource)
                Debug.Assert(_serializedState Is Nothing, "Shouldn't be adding more resources to serialization store - it's already been serialized")

                'Get the current object (or create a new one) that stores all info to
                '  be saved from this Resource instance.
                With GetResourceSerializationData(Resource)
                    '... and tell it to store the entire object
                    .EntireObject = True
                End With

                Trace("Added Resource to serialize as entire object: {0}", Resource.ToString())
            End Sub


            ''' <summary>
            ''' Adds a new property serialization to our list of things to serialize.
            ''' </summary>
            ''' <param name="Resource">The Resource whose property needs to be serialized.</param>
            ''' <param name="Member">The property descriptor which should be serialized.</param>
            ''' <remarks>
            ''' This is used by UndoEngine when a resource's property is changed.
            ''' Again, the property isn't actually serialized until Close(), it's just put in
            '''   our list of things to be serialized.
            ''' </remarks>
            Friend Sub AddMember(ByVal Resource As Resource, ByVal Member As MemberDescriptor)
                Debug.Assert(_serializedState Is Nothing, "Shouldn't be adding more to serialization store - it's already been serialized")

                'Get the current object (or create a new one) that stores all info to
                '  be saved from this Resource instance.
                With GetResourceSerializationData(Resource)
                    '... and add this property to the list of properties that we want serialized from this Resource instance
                    .AddPropertyToSerialize(Member)
                End With

                Trace("Added Resource property to serialize: {0}, prop={1}", Resource.Name, Member.Name)
            End Sub


            ''' <summary>
            ''' Gets the current data for the given Resource object that is contained in
            '''   m_HashedObjectsToSerialize.  Or, if there isn't one already, creates a
            '''   new one.
            ''' </summary>
            ''' <param name="Resource">The Resource from which we want to serialize something.</param>
            ''' <returns>The ResourceDataToSerialize object associated with this Resource.</returns>
            ''' <remarks></remarks>
            Private Function GetResourceSerializationData(ByVal Resource As Resource) As ResourceDataToSerialize
                Dim Data As ResourceDataToSerialize = DirectCast(_hashedObjectsToSerialize(Resource), ResourceDataToSerialize)
                If Data Is Nothing Then
                    'No object created for this Resource yet.  Create one now.
                    Data = New ResourceDataToSerialize(Resource)
                    _hashedObjectsToSerialize(Resource) = Data

                    Trace("ResourceSerializationService: Adding new ResourceSerializationData to hashed objects to serialize: '{0}'", Resource.Name)
                End If

                Return Data
            End Function

#End Region



#Region "Deserialization of the saved resources/properties (used at Undo/Redo time)"


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
            '''   instances of Resource components (this is always the case for Undo/Redo).</param>
            ''' <returns>The objects which have been serialized.</returns>
            ''' <remarks></remarks>
            Private Function DeserializeHelper(ByVal Container As IContainer, ByVal RecycleInstances As Boolean) As ICollection

                Trace("Deserializing store (Container Exists={0}, RecycleInstances={1}): {2} objects", Container IsNot Nothing, RecycleInstances, _serializedState.Count)

                Dim NewObjects As New ArrayList(_serializedState.Count)

                'Handle each individual Resource or property at a time...
                For Each SerializedObject As SerializedResourceOrProperty In _serializedState
                    If SerializedObject.IsEntireResourceObject Then
                        '... we have an entire Resource instance.  Go ahead and create it from
                        '  the stored binary serialization.
                        '
                        'For entire resource instances, we ignore the value of RecycleInstances (the Undo engine
                        '  calls us with RecycleInstances=True for the delete/redo case - I would have expected
                        '  False, but either way, we need to create a new instance, so we'll just ignore that 
                        '  flag).

                        Dim NewResource As Resource = SerializedObject.GetEntireResourceObject()
                        Debug.Assert(NewResource.Name = SerializedObject.ResourceName)

                        '... and add it to the store and list.
                        If Container IsNot Nothing Then
                            Container.Add(NewResource, NewResource.Name)
                        End If
                        NewObjects.Add(NewResource)

                        Trace("   ... Resource as entire object: " & NewResource.Name)
                    Else
                        'We have just a property to deserialize
                        Dim ResourceToSerializeTo As Resource = Nothing
                        If RecycleInstances AndAlso Container IsNot Nothing Then
                            'We're applying this property to an existing resource.  Need to
                            '  find it in the container's list of components.
                            Dim ResourceObject As Object = Container.Components(SerializedObject.ResourceName)
                            If TypeOf ResourceObject Is Resource Then
                                'Found it.
                                ResourceToSerializeTo = DirectCast(ResourceObject, Resource)
                                Debug.Assert(ResourceToSerializeTo IsNot Nothing, "Couldn't find resource component to deserialize to")
                            Else
                                'Whoops, didn't find it.
                                Debug.Fail("Component found in container wasn't a Resource!")
                            End If
                        End If

                        If ResourceToSerializeTo Is Nothing Then
                            'We don't have an existing resource to apply the property to.  This shouldn't happen in practice.
                            Debug.Fail("Unexpected path in undo - ResourceToSerializeTo is Nothing")
                            Throw New NotSupportedException()
                        End If

#If DEBUG Then
                        'Trace
                        Dim StringValue As String
                        Try
                            Dim PropertyValue As Object = SerializedObject.GetPropertyValue()
                            If PropertyValue Is Nothing Then
                                StringValue = ""
                            Else
                                StringValue = PropertyValue.ToString()
                            End If
                        Catch ex As Exception
                            RethrowIfUnrecoverable(ex)
                            StringValue = ex.Message
                        End Try
                        Trace("   ... Resource property: {0} = {1}", SerializedObject.PropertyName, StringValue)
#End If

                        'Deserialize the property value and apply it to the Resource instance
                        ResourceToSerializeTo.SetPropertyValue(SerializedObject.PropertyName, SerializedObject.GetPropertyValue())

                        '... and add the Resource to our list
                        If Not NewObjects.Contains(ResourceToSerializeTo) Then
                            NewObjects.Add(ResourceToSerializeTo)
                        End If
                    End If
                Next

                'Return all Resources that were affected by the deserialization.
                Return NewObjects
            End Function

#End Region



#Region "Private class - ResourceDataToSerialize"

            ''' <summary>
            ''' Keeps track of everything that we want to serialized about a single
            '''   Resource instance (either the entire Resoure itself, or a set of
            '''   its properties)
            ''' </summary>
            ''' <remarks></remarks>
            Private Class ResourceDataToSerialize

                'Backing for public properties
                Private _entireObject As Boolean
                Private _propertiesToSerialize As ArrayList 'Of PropertyDescriptor
                Private _resource As Resource



                ''' <summary>
                ''' Constructor
                ''' </summary>
                ''' <param name="Resource">The Resource from which we want to serialize stuff.</param>
                ''' <remarks></remarks>
                Public Sub New(ByVal Resource As Resource)
                    If Resource Is Nothing Then
                        Throw Common.CreateArgumentException("Resource")
                    End If

                    _resource = Resource
                End Sub



                ''' <summary>
                ''' The Resource from which we want to serialize stuff.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property Resource() As Resource
                    Get
                        Return _resource
                    End Get
                End Property


                ''' <summary>
                ''' If True, the entire Resource instance should be serialized.  If false,
                '''   then only the properties in PropertiesToSerialize should be serialized.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public Property EntireObject() As Boolean
                    Get
                        Return _entireObject
                    End Get
                    Set(ByVal Value As Boolean)
                        If Value AndAlso _propertiesToSerialize IsNot Nothing Then
                            _propertiesToSerialize.Clear()
                        End If
                        _entireObject = Value
                    End Set
                End Property


                ''' <summary>
                ''' A list of PropertyDescriptors representing the properties on
                '''   the Resource which should be serialized.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property PropertiesToSerialize() As IList
                    Get
                        If _propertiesToSerialize Is Nothing Then
                            _propertiesToSerialize = New ArrayList
                        End If
                        Return _propertiesToSerialize
                    End Get
                End Property


                ''' <summary>
                ''' Adds a property to be serialized to the list.
                ''' </summary>
                ''' <param name="Member">The property (must be a PropertyDescriptor) to be serialized.</param>
                ''' <remarks></remarks>
                Public Sub AddPropertyToSerialize(ByVal Member As MemberDescriptor)
                    If Member Is Nothing Then
                        Throw New ArgumentNullException("Member")
                    End If

                    If TypeOf Member Is PropertyDescriptor Then
                        Dim Prop As PropertyDescriptor = DirectCast(Member, PropertyDescriptor)
                        Debug.Assert(Prop.PropertyType.IsSerializable)
                        PropertiesToSerialize.Add(Prop)
                    Else
                        Debug.Fail("Member should have been a property")
                    End If
                End Sub

            End Class 'ResourceDataToSerialize

#End Region


#Region "Private class - SerializedResourceOrProperty"

            ''' <summary>
            ''' Stores a single binary serialized Resource instance or Resource property value
            ''' </summary>
            ''' <remarks></remarks>
            <Serializable()> _
            Private NotInheritable Class SerializedResourceOrProperty

                'The name of the resource from which this was serialized.
                Private _resourceName As String

                'The name of the property which was serialized (if it's a property)
                Private _propertyName As String 'Nothing if entire object

                'The name of the value type for this resource (needed to create a
                '  new resource if necessary)
                Private _resourceValueTypeName As String

                'The serialized property (if m_PropertyName <> "") or Resource instance
                Private _serializedValue As Byte()



                ''' <summary>
                ''' Constructor used for serializing just one property from a Resource
                ''' </summary>
                ''' <param name="OwnerResource">The Resource from which to serialize the property</param>
                ''' <param name="ResourceProperty">The name of the property to serialize</param>
                ''' <remarks>
                ''' The property value is immediately serialized in the constructor and stored away.
                ''' </remarks>
                Public Sub New(ByVal OwnerResource As Resource, ByVal ResourceProperty As PropertyDescriptor)
                    _resourceName = OwnerResource.Name
                    _propertyName = ResourceProperty.Name
                    If PropertyName = "" Then
                        Throw Common.CreateArgumentException("ResourceProperty")
                    End If
                    _resourceValueTypeName = OwnerResource.ValueTypeName

                    Dim PropertyValue As Object = ResourceProperty.GetValue(OwnerResource)
                    If PropertyValue Is Nothing Then
                        _serializedValue = Nothing
                    Else
                        _serializedValue = SerializeObject(PropertyValue)
                    End If
                End Sub


                ''' <summary>
                ''' Constructor used for serializing an entire Resource
                ''' </summary>
                ''' <param name="ResourceAsEntireObject">The Resource instance to serialize</param>
                ''' <remarks>
                ''' The Resource is immediately serialized in the constructor and stored away.
                ''' </remarks>
                Public Sub New(ByVal ResourceAsEntireObject As Resource)
                    _propertyName = Nothing
                    _resourceName = ResourceAsEntireObject.Name
                    _resourceValueTypeName = ResourceAsEntireObject.ValueTypeName

                    _serializedValue = SerializeObject(ResourceAsEntireObject)
                End Sub


                ''' <summary>
                ''' Gets the name of the resource from which this was serialized.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property ResourceName() As String
                    Get
                        Return _resourceName
                    End Get
                End Property


                ''' <summary>
                ''' Gets the name of the property which was serialized (or Nothing if
                '''   not a property serialization).
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property PropertyName() As String
                    Get
                        Return _propertyName
                    End Get
                End Property


                ''' <summary>
                ''' The name of the value type for this resource (needed to create a
                '''   new resource if necessary)
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property ResourceValueTypeName() As String
                    Get
                        Return _resourceValueTypeName
                    End Get
                End Property


                ''' <summary>
                ''' Returns True iff an entire Resource object has been serialized, as opposed
                '''   to just a property from it.
                ''' </summary>
                ''' <returns></returns>
                ''' <remarks></remarks>
                Public ReadOnly Property IsEntireResourceObject() As Boolean
                    Get
                        Return (PropertyName = "")
                    End Get
                End Property


                ''' <summary>
                ''' Serializes an object
                ''' </summary>
                ''' <param name="Object">The object to serialize</param>
                ''' <returns>The binary serialized object.</returns>
                ''' <remarks></remarks>
                Private Function SerializeObject(ByVal [Object] As Object) As Byte()
                    Dim MemoryStream As New MemoryStream
                    Call (New BinaryFormatter).Serialize(MemoryStream, [Object])
                    Return MemoryStream.ToArray()
                End Function


                ''' <summary>
                ''' Deserializes an entire Resource instance which has been serialized.
                ''' </summary>
                ''' <returns></returns>
                ''' <remarks>Can only be called if IsEntireResourceObject = True</remarks>
                Public Function GetEntireResourceObject() As Resource
                    If Not IsEntireResourceObject() Then
                        Throw New Package.InternalException
                    End If

                    Dim MemoryStream As New MemoryStream(_serializedValue)
                    Return DirectCast((New BinaryFormatter).Deserialize(MemoryStream), Resource)
                End Function


                ''' <summary>
                ''' Deserializes a property value which has been serialized.
                ''' </summary>
                ''' <returns></returns>
                ''' <remarks>Can only be called if IsEntireResourceObject = False</remarks>
                Public Function GetPropertyValue() As Object
                    If IsEntireResourceObject() Then
                        Throw New Package.InternalException
                    End If

                    If _serializedValue Is Nothing Then
                        Return Nothing
                    End If

                    Dim MemoryStream As New MemoryStream(_serializedValue)
                    Return (New BinaryFormatter).Deserialize(MemoryStream)
                End Function

            End Class 'SerializedResourceOrProperty

#End Region

        End Class

    End Class

End Namespace
