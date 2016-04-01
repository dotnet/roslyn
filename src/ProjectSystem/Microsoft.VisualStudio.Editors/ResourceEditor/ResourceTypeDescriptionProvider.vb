' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Provides type information about instances of the Resource class.  This is used to fill the
    '''   properties window in Visual Studio with the property values of particular instances of the
    '''   Resource class.
    ''' </summary>
    ''' <remarks> 
    '''  Resource class is hooked up with this class using TypeDescriptionProviderAttribute. 
    '''  This class inherits TypeDescriptionProvider and only overrides GetTypeDescriptor 
    '''      to return our own ResourceTypeDescriptor.
    ''' </remarks>
    Friend NotInheritable Class ResourceTypeDescriptionProvider
        Inherits System.ComponentModel.TypeDescriptionProvider

        ''' <summary>
        '''  Returns ResourceTypeDescriptor as the ICustomTypeDescriptor for the specified Resource instance.
        ''' </summary>
        ''' <param name="ObjectType">The type of the class to return the type descriptor for. In our case, Resource.</param>
        ''' <param name="Instance">Instance of the class. In our case, a Resource instance.</param>
        ''' <returns>A new ResourceTypeDescriptor for the specified Resource instance.</returns>
        ''' <remarks></remarks>
        Public Overrides Function GetTypeDescriptor(ByVal ObjectType As Type, ByVal Instance As Object) As ICustomTypeDescriptor
            If Instance Is Nothing Then
                Return MyBase.GetTypeDescriptor(ObjectType, Instance)
            End If
            Debug.Assert(TypeOf Instance Is Resource, "Instance is not a Resource!!!")

            Return New ResourceTypeDescriptor(CType(Instance, Resource))
        End Function

    End Class

End Namespace

