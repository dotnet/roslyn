' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Public Class MockHierarchyItemIdentity
        Implements IVsHierarchyItemIdentity

        Public Property Hierarchy As IVsHierarchy Implements IVsHierarchyItemIdentity.Hierarchy

        Public Property IsNestedItem As Boolean Implements IVsHierarchyItemIdentity.IsNestedItem

        Public Property IsRoot As Boolean Implements IVsHierarchyItemIdentity.IsRoot

        Public Property ItemID As UInteger Implements IVsHierarchyItemIdentity.ItemID

        Public Property NestedHierarchy As IVsHierarchy Implements IVsHierarchyItemIdentity.NestedHierarchy

        Public Property NestedItemID As UInteger Implements IVsHierarchyItemIdentity.NestedItemID

    End Class
End Namespace
