' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    ''' The component class to wrap the original reference object. We will push this object to the property grid
    ''' </Summary>
    Friend Class ReferenceComponent
        Inherits ComponentWrapper
        Implements IComparable, IReferenceComponent

        Public Sub New(ByVal realObject As VSLangProj.Reference)
            MyBase.New(realObject)
        End Sub

        ''' <Summary>
        ''' The original reference object in DTE.Project
        ''' </Summary>
        Friend ReadOnly Property CodeReference() As VSLangProj.Reference
            Get
                Return CType(CurrentObject, VSLangProj.Reference)
            End Get
        End Property

        Friend ReadOnly Property Name() As String
            Get
                Return CodeReference.Name
            End Get
        End Property

        ''' <Summary>
        ''' Remove the referece from the project...
        ''' </Summary>
        Private Sub Remove() Implements IReferenceComponent.Remove
            CodeReference.Remove()
        End Sub

        Private Function GetName() As String Implements IReferenceComponent.GetName
            Return Name
        End Function

        Public Function CompareTo(ByVal obj As Object) As Integer Implements System.IComparable.CompareTo
            Dim y As ReferenceComponent = CType(obj, ReferenceComponent)
            If y IsNot Nothing Then
                Return String.Compare(Name, y.Name)
            Else
                Debug.Fail("we can not compare to an unknown object")
                Return 1
            End If
        End Function
    End Class

End Namespace

