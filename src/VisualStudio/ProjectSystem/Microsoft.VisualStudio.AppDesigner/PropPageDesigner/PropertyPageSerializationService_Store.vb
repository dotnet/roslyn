'******************************************************************************
'* PropertyPageSerializationService.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************'-------------------------------------------------------------------------------

Imports System.Reflection
Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Resources
Imports System.Collections
Imports System.Collections.Specialized
Imports System.IO
Imports System.Diagnostics
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary


Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    'This class is private to PropertyPageSerializationService

    Partial Class PropertyPageSerializationService

        ''' <summary>
        '''  Class which provides a storage for serialization data.
        ''' </summary>
        ''' <remarks>
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
        '''</remarks>
        <Serializable()> _
        Private NotInheritable Class PropertyPageSerializationStore
            Inherits System.ComponentModel.Design.Serialization.SerializationStore
            Implements System.Runtime.Serialization.ISerializable

            'The set of properties that we wish to
            '  "serialize" into this store.  The actual values won't be serialized
            '  until we're Close'd, until then this just keeps track of what we
            '  want to serialize.  It will be cleared out when we Close.
            Private m_HashedObjectsToSerialize As Hashtable

            'The actual "serialized" data (binary serialized objects and
            '  property values) which is available after Close().  This data drives
            '  the deserialization process.
            Private m_SerializedState As ArrayList

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <remarks></remarks>
            Public Sub New()
                m_HashedObjectsToSerialize = New Hashtable
            End Sub

            ' default impl of abstract base member.  see serialization store for details
            '	
            Public Overrides ReadOnly Property Errors() As ICollection
                Get
                    Return New Object(-1) {}
                End Get
            End Property

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
                    For Each Entry As DictionaryEntry In m_HashedObjectsToSerialize
                        Dim Data As DataToSerialize = DirectCast(Entry.Value, DataToSerialize)
                        If Data.IsEntireObject Then
                            'We're saving the entire object.
                            '  The constructor for SerializedProperty will do the
                            '  actual binary serialization for us.
                            Dim SerializedData As New SerializedProperty(Data.Component)
                            SerializedState.Add(SerializedData)
                        Else
                            'We're saving individual property values.  Go through each...
                            For Each Prop As PropertyDescriptor In Data.PropertiesToSerialize
                                '... and serialize it.
                                '  The constructor for SerializedProperty will do the
                                '  actual binary serialization for us.
                                Dim SerializedData As New SerializedProperty(Data.Component, Prop)
                                SerializedState.Add(SerializedData)
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
            Const KEY_OBJECTNAMES As String = "ObjectNames"

            ''' <summary>
            '''     Implements the save part of ISerializable.
            '''   Only needed if you're using the store for copy/paste implementation.
            ''' </summary>
            ''' <param name="info">Serialization info</param>
            ''' <param name="context">Serialization context</param>
            ''' <remarks></remarks>
            Private Sub GetObjectData(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
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
            Public Shared Function Load(ByVal Stream As Stream) As PropertyPageSerializationStore
                Dim f As New BinaryFormatter
                Return DirectCast(f.Deserialize(Stream), PropertyPageSerializationStore)
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


#Region "Add properties to be serialized at Close"

            ''' <summary>
            ''' Adds a new object serialization to our list of things to serialize.
            ''' </summary>
            ''' <param name="Component">The component to be serialized as an entire object.</param>
            ''' <remarks>
            ''' </remarks>
            Public Sub AddObject(ByVal Component As PropPageDesignerRootComponent)
                Debug.Fail("Not supported")
                Debug.Assert(m_SerializedState Is Nothing, "Shouldn't be adding more components to serialization store - it's already been serialized")

                'Get the current object (or create a new one) that stores all info to
                '  be saved from this component instance.
                With GetComponentSerializationData(Component)
                    '... and tell it to store the entire object
                    .IsEntireObject = True
                End With
            End Sub


            ''' <summary>
            ''' Adds a new property serialization to our list of things to serialize.
            ''' </summary>
            ''' <param name="Component">The component whose property needs to be serialized.</param>
            ''' <param name="Member">The property descriptor which should be serialized.</param>
            ''' <remarks>
            ''' This is used by UndoEngine when a component's property is changed.
            ''' Again, the property isn't actually serialized until Close(), it's just put in
            '''   our list of things to be serialized.
            ''' </remarks>
            Public Sub AddMember(ByVal Component As PropPageDesignerRootComponent, ByVal Member As MemberDescriptor)
                Debug.Assert(m_SerializedState Is Nothing, "Shouldn't be adding more to serialization store - it's already been serialized")

                'Get the current object (or create a new one) that stores all info to
                '  be saved from this component instance.
                With GetComponentSerializationData(Component)
                    '... and add this property to the list of properties that we want serialized from this component instance
                    .AddPropertyToSerialize(Member)
                End With
            End Sub


            ''' <summary>
            ''' Gets the current data for the given component that is contained in
            '''   m_HashedObjectsToSerialize.  Or, if there isn't one already, creates a
            '''   new one.
            ''' </summary>
            ''' <param name="Component">The component from which we want to serialize something.</param>
            ''' <returns>The ComponentDataToSerialize object associated with this Component.</returns>
            ''' <remarks></remarks>
            Private Function GetComponentSerializationData(ByVal Component As PropPageDesignerRootComponent) As DataToSerialize
                Dim Data As DataToSerialize = DirectCast(m_HashedObjectsToSerialize(Component), DataToSerialize)
                If Data Is Nothing Then
                    'No object created for this Component yet.  Create one now.
                    Data = New DataToSerialize(Component)
                    m_HashedObjectsToSerialize(Component) = Data
                End If

                Return Data
            End Function

#End Region


#Region "Deserialization of the saved components/properties (used at Undo/Redo time)"


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
            Public Sub DeserializeTo(ByVal Container As IContainer)
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
            Public Function Deserialize() As ICollection
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
            Public Function Deserialize(ByVal Container As IContainer) As ICollection
                Return DeserializeHelper(Container, False)
            End Function


            ''' <summary>
            ''' This method does the actual deserialization work, based on the given
            '''   arguments.
            ''' </summary>
            ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
            ''' <param name="RecycleInstances">If True, we are applying property changes to existing
            '''   instances of Component components (this is always the case for Undo/Redo).</param>
            ''' <returns>The objects which have been serialized.</returns>
            ''' <remarks></remarks>
            Private Function DeserializeHelper(ByVal Container As IContainer, ByVal RecycleInstances As Boolean) As ICollection

                Try
                    Dim NewObjects As New ArrayList(m_SerializedState.Count)

                    'Handle each individual Component or property at a time...
                    For Each SerializedObject As SerializedProperty In m_SerializedState
                        If SerializedObject.IsEntireComponentObject Then
                            Debug.Fail("Only individual properties should be persisted for PropertyPages")
                        Else
                            'We have just a property to deserialize
                            Dim ComponentToSerializeTo As Component = Nothing
                            If Container IsNot Nothing Then
                                'We're applying this property to an existing component.  Need to
                                '  find it in the container's list of components.
                                Dim ComponentObject As Object = Container.Components(SerializedObject.ComponentName)
                                If TypeOf ComponentObject Is PropPageDesignerRootComponent Then
                                    'Found it.
                                    ComponentToSerializeTo = DirectCast(ComponentObject, Component)
                                    Debug.Assert(ComponentToSerializeTo IsNot Nothing, "Couldn't find component component to deserialize to")
                                ElseIf TypeOf ComponentObject Is PropPageDesignerView Then
                                    'Found it.
                                    ComponentToSerializeTo = DirectCast(ComponentObject, Component)
                                    Debug.Assert(ComponentToSerializeTo IsNot Nothing, "Couldn't find component component to deserialize to")
                                Else
                                    'Whoops, didn't find it.
                                    Debug.Fail("Component found in container wasn't a PropPageDesignerRootComponent!")
                                End If
                            End If

                            Debug.Assert(ComponentToSerializeTo IsNot Nothing, "Should never occur for PropertyPages")

                            'Deserialize the property value and apply it to the Component instance
                            SetProperty(ComponentToSerializeTo, SerializedObject.PropertyName, SerializedObject.GetPropertyValue())

                            '... and add the Component to our list
                            If Not NewObjects.Contains(ComponentToSerializeTo) Then
                                NewObjects.Add(ComponentToSerializeTo)
                            End If
                        End If
                    Next

                    'Return all Components that were affected by the deserialization.
                    Return NewObjects

                Catch ex As CheckoutException
                    Trace.WriteLine("*** A checkout exception was thrown during Undo/Redo - ignoring.  " & ex.ToString)

                    'There's really nothing good we can do in this case - if an undo fails (esp. if it's an
                    '  undo trying to roll back a previous change that failed), the state's simply not going
                    '  to be great.  We do what the WinForms designer does (actually ReflectPropertyDescriptor
                    '  which it uses, but we add back the checkout exception in this case)- eat checkout 
                    '  exceptions only.  If we eat these exceptions, the undo stack will not be lost, although 
                    '  the state won't be perfect.
                    Return New ArrayList(0)

                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                    Trace.WriteLine("*** An exception was thrown during Undo/Redo: " & ex.ToString)
                    Throw
                End Try

            End Function

            Const Const_Configuration As String = "{Configuration}"

            Private Sub SetProperty(ByVal component As Component, ByVal PropertyName As String, ByVal Value As Object)
                Dim View As PropPageDesignerView

                If TypeOf component Is PropPageDesignerRootComponent Then
                    View = CType(component, PropPageDesignerRootComponent).RootDesigner.GetView()
                    View.SetProperty(PropertyName, Value)
                Else
                    Throw AppDesCommon.CreateArgumentException("component")
                End If
            End Sub

#End Region



#Region "Private class - DataToSerialize"

            ''' <summary>
            ''' Keeps track of everything that we want to serialized about a single
            '''   Component instance (either the entire Resoure itself, or a set of
            '''   its properties)
            ''' </summary>
            ''' <remarks></remarks>
            Private NotInheritable Class DataToSerialize

                'Backing for public properties
                Private m_IsEntireObject As Boolean
                Private m_PropertiesToSerialize As ArrayList 'Of PropertyDescriptor
                Private m_Component As PropPageDesignerRootComponent



                ''' <summary>
                ''' Constructor
                ''' </summary>
                ''' <param name="Component">The Component from which we want to serialize stuff.</param>
                ''' <remarks></remarks>
                Public Sub New(ByVal Component As PropPageDesignerRootComponent)
                    If Component Is Nothing Then
                        Throw AppDesCommon.CreateArgumentException("Component")
                    End If

                    m_Component = Component
                End Sub



                ''' <summary>
                ''' The component from which we want to serialize stuff.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property Component() As PropPageDesignerRootComponent
                    Get
                        Return m_Component
                    End Get
                End Property


                ''' <summary>
                ''' If True, the entire Component instance should be serialized.  If false,
                '''   then only the properties in PropertiesToSerialize should be serialized.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public Property IsEntireObject() As Boolean
                    Get
                        Return m_IsEntireObject
                    End Get
                    Set(ByVal Value As Boolean)
                        Debug.Assert(Value = False, "Not supported/needed")
                        If Value AndAlso m_PropertiesToSerialize IsNot Nothing Then
                            m_PropertiesToSerialize.Clear()
                        End If
                        m_IsEntireObject = Value
                    End Set
                End Property


                ''' <summary>
                ''' A list of PropertyDescriptors representing the properties on
                '''   the Component which should be serialized.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property PropertiesToSerialize() As IList
                    Get
                        If m_PropertiesToSerialize Is Nothing Then
                            m_PropertiesToSerialize = New ArrayList
                        End If
                        Return m_PropertiesToSerialize
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

            End Class 'DataToSerialize

#End Region


#Region "Private class - SerializedProperty"

            ''' <summary>
            ''' Stores a single binary serialized Component instance or Component property value
            ''' </summary>
            ''' <remarks></remarks>
            <Serializable()> _
            Private NotInheritable Class SerializedProperty

                'The name of the component from which this was serialized.
                Private m_ComponentName As String

                'The name of the property which was serialized (if it's a property)
                Private m_PropertyName As String 'Nothing if entire object

                'The serialized property (if m_PropertyName <> "") or Component instance
                Private m_SerializedValue As Byte()



                ''' <summary>
                ''' Constructor used for serializing just one property from a Component
                ''' </summary>
                ''' <param name="OwnerComponent">The Component from which to serialize the property</param>
                ''' <param name="ComponentProperty">The name of the property to serialize</param>
                ''' <remarks>
                ''' The property value is immediately serialized in the constructor and stored away.
                ''' </remarks>
                Public Sub New(ByVal OwnerComponent As PropPageDesignerRootComponent, ByVal ComponentProperty As PropertyDescriptor)
                    m_ComponentName = OwnerComponent.Name
                    m_PropertyName = ComponentProperty.Name
                    If PropertyName = "" Then
                        Throw AppDesCommon.CreateArgumentException("ComponentProperty")
                    End If

                    Dim PropertyValue As Object = ComponentProperty.GetValue(OwnerComponent)
                    If PropertyValue Is Nothing Then
                        m_SerializedValue = Nothing
                    Else
                        m_SerializedValue = SerializeObject(PropertyValue)
                    End If
                End Sub


                ''' <summary>
                ''' Constructor used for serializing an entire Component
                ''' </summary>
                ''' <param name="ComponentAsEntireObject">The Component instance to serialize</param>
                ''' <remarks>
                ''' The Component is immediately serialized in the constructor and stored away.
                ''' </remarks>
                Public Sub New(ByVal ComponentAsEntireObject As PropPageDesignerRootComponent)
                    Debug.Fail("Not supported")
                    m_PropertyName = Nothing
                    m_ComponentName = ComponentAsEntireObject.Name

                    m_SerializedValue = SerializeObject(ComponentAsEntireObject)
                End Sub


                ''' <summary>
                ''' Gets the name of the component from which this was serialized.
                ''' </summary>
                ''' <value></value>
                ''' <remarks></remarks>
                Public ReadOnly Property ComponentName() As String
                    Get
                        Return m_ComponentName
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
                        Return m_PropertyName
                    End Get
                End Property


                ''' <summary>
                ''' Returns True iff an entire Component object has been serialized, as opposed
                '''   to just a property from it.
                ''' </summary>
                ''' <returns></returns>
                ''' <remarks></remarks>
                Public ReadOnly Property IsEntireComponentObject() As Boolean
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
                ''' Deserializes an entire Component instance which has been serialized.
                ''' </summary>
                ''' <returns></returns>
                ''' <remarks>Can only be called if IsEntireComponentObject = True</remarks>
                Public Function GetEntireComponentObject() As PropPageDesignerRootComponent
                    If Not IsEntireComponentObject() Then
                        Throw New Package.InternalException
                    End If

                    Dim MemoryStream As New MemoryStream(m_SerializedValue)
                    Return DirectCast((New BinaryFormatter).Deserialize(MemoryStream), PropPageDesignerRootComponent)
                End Function


                ''' <summary>
                ''' Deserializes a property value which has been serialized.
                ''' </summary>
                ''' <returns></returns>
                ''' <remarks>Can only be called if IsEntireComponentObject = False</remarks>
                Public Function GetPropertyValue() As Object
                    If IsEntireComponentObject() Then
                        Throw New Package.InternalException
                    End If

                    If m_SerializedValue Is Nothing Then
                        Return Nothing
                    End If

                    Dim MemoryStream As New MemoryStream(m_SerializedValue)
                    Return (New BinaryFormatter).Deserialize(MemoryStream)
                End Function

            End Class 'SerializedProperty

#End Region

        End Class

    End Class

End Namespace
