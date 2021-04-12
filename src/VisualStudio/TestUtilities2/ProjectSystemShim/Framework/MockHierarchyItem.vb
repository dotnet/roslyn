' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Public Class MockHierarchyItem
        Implements IVsHierarchyItem

        Public ReadOnly Property AreChildrenRealized As Boolean Implements IVsHierarchyItem.AreChildrenRealized
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Property CanonicalName As String Implements IVsHierarchyItem.CanonicalName

        Public ReadOnly Property Children As IEnumerable(Of IVsHierarchyItem) Implements IVsHierarchyItem.Children
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Property HierarchyIdentity As IVsHierarchyItemIdentity Implements IVsHierarchyItem.HierarchyIdentity

        Public Property Parent As IVsHierarchyItem Implements IVsHierarchyItem.Parent

        Public Property IsBold As Boolean Implements IVsHierarchyItem.IsBold
            Get
                Throw New NotImplementedException()
            End Get
            Set(value As Boolean)
                Throw New NotImplementedException()
            End Set
        End Property

        Public Property IsCut As Boolean Implements IVsHierarchyItem.IsCut
            Get
                Throw New NotImplementedException()
            End Get
            Set(value As Boolean)
                Throw New NotImplementedException()
            End Set
        End Property

        Public ReadOnly Property IsDisposed As Boolean Implements ISupportDisposalNotification.IsDisposed
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public ReadOnly Property Text As String Implements IVsHierarchyItem.Text
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Public Event PropertyChanging As PropertyChangingEventHandler Implements INotifyPropertyChanging.PropertyChanging
    End Class
End Namespace
