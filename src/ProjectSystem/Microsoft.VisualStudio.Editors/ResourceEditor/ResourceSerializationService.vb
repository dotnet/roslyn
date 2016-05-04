' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common
Imports System.ComponentModel
Imports System.ComponentModel.Design.Serialization
Imports System.IO


Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is a class that enables the UndoEngine to "serialize" our components (Resource instances)
    '''   so that it can save the originals before any changes, and then use our service to deserialize
    '''   those original and changed values when executing an Undo/Redo operation.
    '''
    ''' This same schema can also be used to implement copy/paste, since it's essentially just 
    '''   a serialization of a set of objects (or property changes).  In the resource editor
    '''   copy/paste was implemented another way (largely because the other way existed first).
    '''
    ''' This class itself just delegates everything to the ResourceSerializationStore, which
    '''   does the actual work.
    '''
    '''  Comments from the (abstract) ComponentSerializationService from which this derives:
    '''
    '''      This class serializes a set of components or serializable objects into a 
    '''     serialization store.  The store can then be deserialized at a later 
    '''     date.  ComponentSerializationService differs from other serialization 
    '''     schemes in that the serialization format is opaque, and it allows for 
    '''     partial serialization of objects.  For example, you can choose to 
    '''     serialize only selected properties for an object.
    '''     
    '''     This class is abstract. Typically a DesignerLoader will provide a 
    '''     concrete implementation of this class and add it as a service to 
    '''     its DesignSurface.  This allows objects to be serialized in the 
    '''     format best suited for them.
    '''
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceSerializationService
        Inherits ComponentSerializationService




        ''' <summary>
        ''' This method creates a new SerializationStore.  The serialization store can 
        '''   be passed to any of the various Serialize methods to build up serialization 
        '''   state for a group of objects.
        ''' </summary>
        ''' <param name="Provider">Ignored.</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal Provider As IServiceProvider)
            'We don't need th service provider, we ignore it.
        End Sub







        ''' <summary>
        ''' Does debug-only tracing for this class and its entire processes.
        ''' </summary>
        ''' <param name="Message">The message to displaying, including optional formatting parameters "{0}" etc.</param>
        ''' <param name="FormatArguments">Arguments for "{0}", "{1}", etc.</param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub Trace(ByVal Message As String, ByVal ParamArray FormatArguments() As Object)
            Debug.WriteLineIf(Switches.RSEResourceSerializationService.TraceVerbose, "ResourceSerializationService: " & String.Format(Message, FormatArguments))
        End Sub


        ''' <summary>
        ''' This method creates a new SerializationStore.  The serialization store can 
        '''  be passed to any of the various Serialize methods to build up serialization 
        '''  state for a group of objects.
        ''' </summary>
        ''' <returns>An instance of a new serialization store for resources</returns>
        ''' <remarks></remarks>
        Public Overrides Function CreateStore() As SerializationStore
            Return New ResourceSerializationStore()
        End Function


        ''' <summary>
        ''' This method loads a SerializationStore and from the given
        '''   stream.  This store can then be used to deserialize objects by passing it to 
        '''   the various Deserialize methods.
        ''' </summary>
        ''' <param name="Stream">The stream to load from.</param>
        ''' <returns>The loaded store for resources.</returns>
        ''' <remarks></remarks>
        Public Overrides Function LoadStore(ByVal Stream As Stream) As SerializationStore
            If Stream Is Nothing Then
                Throw New ArgumentNullException("Stream")
            End If

            Return ResourceSerializationStore.Load(Stream)
        End Function


        ''' <summary>
        ''' This method serializes the given object to the store.  The store 
        '''   can be used to serialize more than one object by calling this method 
        '''   more than once. 
        ''' </summary>
        ''' <param name="Store">The store to serialize into.</param>
        ''' <param name="Value">The object (must be a Resource instance) to serialize into the store.</param>
        ''' <remarks></remarks>
        Public Overrides Sub Serialize(ByVal Store As SerializationStore, ByVal Value As Object)
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            If Value Is Nothing Then
                Throw New ArgumentNullException("Value")
            End If

            If Not TypeOf Value Is Resource Then
                Throw Common.CreateArgumentException("Value")
            End If
            Dim Resource As Resource = DirectCast(Value, Resource)

            Dim RFStore As ResourceSerializationStore = TryCast(Store, ResourceSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            RFStore.AddResource(Resource)
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
        ''' <param name="OwningObject">The object (must be a Resource instance) whose property (member) you are trying to serialize into the store.</param>
        ''' <param name="Member">The property whose value needs to be serialized into the store.</param>
        ''' <remarks>
        ''' For the resource editor, Member has to be a property.
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

            Dim OwningResource As Resource = TryCast(OwningObject, Resource)
            If OwningObject Is Nothing Then
                Throw Common.CreateArgumentException("OwningObject")
            End If

            Dim RFStore As ResourceSerializationStore = TryCast(Store, ResourceSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            RFStore.AddMember(OwningResource, Member)
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
        ''' <param name="OwningObject">The object (must be a Resource instance) whose property (member) you are trying to serialize into the store.</param>
        ''' <param name="Member">The property whose value needs to be serialized into the store.</param>
        ''' <remarks></remarks>
        Public Overrides Sub SerializeMemberAbsolute(ByVal Store As SerializationStore, ByVal OwningObject As Object, ByVal Member As MemberDescriptor)
            'This method is intended for properties such as collections which might have had only some of their
            '  members changed.
            'The resource editor doesn't have any such properties, so we just treat this the same
            '  as simple SerializeMember (ignoring OldValue)

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

            Dim RFStore As ResourceSerializationStore = TryCast(Store, ResourceSerializationStore)
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

            Dim RFStore As ResourceSerializationStore = TryCast(Store, ResourceSerializationStore)
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
        Public Overrides Sub DeserializeTo(ByVal Store As SerializationStore, ByVal Container As IContainer, ByVal ValidateRecycledTypes As Boolean, ByVal applyDefault As Boolean)
            If Store Is Nothing Then
                Throw New ArgumentNullException("Store")
            End If

            If Container Is Nothing Then
                Throw New ArgumentNullException("Container")
            End If

            Dim RFStore As ResourceSerializationStore = TryCast(Store, ResourceSerializationStore)
            If RFStore Is Nothing Then
                Throw Common.CreateArgumentException("Store")
            End If

            RFStore.DeserializeTo(Container)
        End Sub

    End Class


End Namespace
