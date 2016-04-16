' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Friend Class HierarchyItemMapper
        Implements IHierarchyItemToProjectIdMap

        Private ReadOnly _tracker As VisualStudioProjectTracker

        Public Sub New(projectTracker As VisualStudioProjectTracker)
            _tracker = projectTracker
        End Sub

        Public Function TryGetProjectId(hierarchyItem As IVsHierarchyItem, ByRef projectId As ProjectId) As Boolean Implements IHierarchyItemToProjectIdMap.TryGetProjectId

            Dim project = _tracker.Projects.
                Where(Function(p) p.Hierarchy Is hierarchyItem.HierarchyIdentity.NestedHierarchy).
                Where(Function(p) p.ProjectSystemName Is hierarchyItem.CanonicalName).
                SingleOrDefault()

            If project Is Nothing Then
                projectId = Nothing
                Return False
            Else
                projectId = project.Id
                Return True
            End If

        End Function
    End Class
End Namespace
