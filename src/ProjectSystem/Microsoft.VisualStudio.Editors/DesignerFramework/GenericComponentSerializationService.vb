' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.ComponentModel.Design.Serialization
Imports System.io

Namespace Microsoft.VisualStudio.Editors.DesignerFramework


    Friend Class GenericComponentSerializationService
        Inherits ComponentSerializationService


        Private _serviceProvider As IServiceProvider

        ''' <summary>
        ''' This method creates a new SerializationService.  The serialization store can 
        '''   be passed to any of the various Serialize methods to build up serialization 
        '''   state for a group of objects.
        ''' </summary>
        ''' <param name="Provider">.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal Provider As IServiceProvider)
            _serviceProvider = Provider
        End Sub

        ''' <summary>
        ''' This method creates a new SerializationStore.  The serialization store can 
        '''  be passed to any of the various Serialize methods to build up serialization 
        '''  state for a group of objects.
        ''' </summary>
        ''' <returns>An instance of a new serialization store for objects</returns>
        ''' <remarks></remarks>
        Public Overrides Function CreateStore() As SerializationStore
            Return New GenericComponentSerializationStore
        End Function


        ''' <summary>
        ''' This method loads a SerializationStore and from the given
        '''   stream.  This store can then be used to deserialize objects by passing it to 
        '''   the various Deserialize methods.
        ''' </summary>
        ''' <param name="Stream">The stream to load from.</param>
        ''' <returns>The loaded store for objects.</returns>
        ''' <remarks></remarks>
        Public Overrides Function LoadStore(ByVal Stream As Stream) As SerializationStore
            If Stream Is Nothing Then
                Throw New ArgumentNullException("Stream")
            End If

            Return GenericComponentSerializationStore.Load(Stream)
        End Function


        ''' <summary>
        ''' This method serializes the given object to the store.  The store 
        '''   can be used to serialize more than one object by calling this method 
        '''   more than once. 
        ''' </summary>
        ''' <param name="Store">The store to serialize into.</param>
        ''' <param name="Value">The object to serialize into the store.</param>
        ''' <remarks></remarks>
        Public Overrides Sub Serialize(ByVal Store As SerializationStore, ByVal Value As Object)
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            If Value Is Nothing Then
                Throw New ArgumentNullException("Value")
            End If

            Dim RFStore As GenericComponentSerializationStore = TryCast(Store, GenericComponentSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            RFStore.AddObject(Value)
        End Sub

        Public Overrides Sub SerializeAbsolute(ByVal store As SerializationStore, ByVal value As Object)
            Serialize(store, value)
        End Sub


        ''' <summary>
        ''' This method serializes the given member on the given object.  This method 
        '''   can be invoked multiple times for the same object to build up a list of 
        '''   serialized members within the serialization store.  The member generally 
        '''   has to be a property or an event.
        ''' </summary>
        ''' <param name="Store">The store to serialize into.</param>
        ''' <param name="OwningObject">The object whose property (member) you are trying to serialize into the store.</param>
        ''' <param name="Member">The property whose value needs to be serialized into the store.</param>
        ''' <remarks>
        ''' The member has to be a property.
        ''' Note that the actual value is *not* yet serialized into the store, it's just remembered that we
        '''   *want* to serialize it.  It will actually get serialized when the store is closed.
        ''' </remarks>
        Public Overrides Sub SerializeMember(ByVal Store As SerializationStore, ByVal OwningObject As Object, ByVal Member As MemberDescriptor)
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            If OwningObject Is Nothing Then
                Throw New ArgumentNullException("OwningObject")
            End If

            If Member Is Nothing Then
                Throw New ArgumentNullException("Member")
            End If

            Dim RFStore As GenericComponentSerializationStore = TryCast(Store, GenericComponentSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            If Not TypeOf Member Is PropertyDescriptor Then
                Throw Common.CreateArgumentException("Member")
            End If

            RFStore.AddMember(OwningObject, DirectCast(Member, PropertyDescriptor))
        End Sub


        ''' <summary>
        ''' This method serializes the given member on the given object, 
        '''   but attempts to do so in such a way as to serialize only the 
        '''   difference between the member's current value and oldValue.  
        '''   This type of seralization is generally only useful for properties 
        '''   that maintain a lot of state such as collections, and then only 
        '''   if the state is being deserialized into an existing object.
        ''' </summary>
        ''' <param name="Store">The store to serialize into.</param>
        ''' <param name="OwningObject">The object whose property (member) you are trying to serialize into the store.</param>
        ''' <param name="Member">The property whose value needs to be serialized into the store.</param>
        ''' <remarks></remarks>
        Public Overrides Sub SerializeMemberAbsolute(ByVal Store As SerializationStore, ByVal OwningObject As Object, ByVal Member As MemberDescriptor)
            'This method is intended for properties such as collections which might have had only some of their
            '  members changed.

            SerializeMember(Store, OwningObject, Member)
        End Sub


        ''' <summary>
        '''     This method deserializes the given store to produce a collection of 
        '''     objects contained within it.  If a container is provided, objects 
        '''     that are created that implement IComponent will be added to the container. 
        ''' </summary>
        ''' <param name="Store">The store to serialize into.</param>
        ''' <returns>The set of components that were deserialized.</returns>
        ''' <remarks></remarks>
        Public Overrides Function Deserialize(ByVal Store As SerializationStore) As ICollection
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            Dim RFStore As GenericComponentSerializationStore = TryCast(Store, GenericComponentSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            Return RFStore.Deserialize()
        End Function


        ''' <summary>
        '''     This method deserializes the given store to produce a collection of 
        '''     objects contained within it.  If a container is provided, objects 
        '''     that are created that implement IComponent will be added to the container. 
        ''' </summary>
        ''' <param name="Store">The store to serialize into.</param>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <returns>The list of objects that were deserialized.</returns>
        ''' <remarks></remarks>
        Public Overrides Function Deserialize(ByVal Store As SerializationStore, ByVal Container As IContainer) As ICollection
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            If Container Is Nothing Then
                Throw New ArgumentNullException("Container")
            End If

            Dim RFStore As GenericComponentSerializationStore = TryCast(Store, GenericComponentSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            Return RFStore.Deserialize(Container)
        End Function


        ''' <summary>
        '''     This method deserializes the given store, but rather than produce 
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
        ''' <param name="Store">The store to serialize into.</param>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <remarks></remarks>
        Public Overrides Sub DeserializeTo(ByVal Store As SerializationStore, ByVal Container As IContainer, ByVal ValidateRecycledTypes As Boolean, ByVal applyDefaults As Boolean)
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            If Container Is Nothing Then
                Throw New ArgumentNullException("Container")
            End If

            Dim RFStore As GenericComponentSerializationStore = TryCast(Store, GenericComponentSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            RFStore.DeserializeTo(Container)
        End Sub

        ''' <summary>
        ''' Get/set the service provider
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Not used by this class, may be useful for derived classes</remarks>
        Protected Property ServiceProvider() As IServiceProvider
            Get
                Return _serviceProvider
            End Get
            Set(ByVal Value As IServiceProvider)
                _serviceProvider = Value
            End Set
        End Property
    End Class

End Namespace
