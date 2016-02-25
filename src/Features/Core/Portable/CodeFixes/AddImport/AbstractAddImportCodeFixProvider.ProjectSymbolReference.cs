// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        /// <summary>
        /// Handles references to source symbols both from the current project the user is invoking
        /// 'add-import' from, as well as symbols from other viable projects.
        /// 
        /// In the case where the reference is from another project we put a glyph in the add using
        /// light bulb and we say "(from ProjectXXX)" to make it clear that this will do more than
        /// just add a using/import.
        /// </summary>
        private class ProjectSymbolReference : SymbolReference
        {
            private readonly Project _project;

            public ProjectSymbolReference(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider,
                SymbolResult<INamespaceOrTypeSymbol> symbolResult,
                Project project)
                : base(provider, symbolResult)
            {
                _project = project;
            }

            protected override Glyph? GetGlyph(Document document)
            {
                return document.Project.Id == _project.Id
                    ? default(Glyph?)
                    : Glyph.AddReference;
            }

            protected override Solution UpdateSolution(Document newDocument)
            {
                if (_project.Id == newDocument.Project.Id)
                {
                    // This reference was found while searching in the project for our document.  No
                    // need to make any solution changes.
                    return newDocument.Project.Solution;
                }

                // If this reference came from searching another project, then add a project reference
                // as well.
                var newProject = newDocument.Project;
                newProject = newProject.AddProjectReference(new ProjectReference(_project.Id));

                return newProject.Solution;
            }

            protected override string GetDescription(Project project, SyntaxNode node, SemanticModel semanticModel)
            {
                var description = base.GetDescription(project, node, semanticModel);
                return project.Id == _project.Id
                    ? description
                    : $"{description} ({string.Format(FeaturesResources.from_0, _project.Name)})";
            }

            protected override Func<Workspace, bool> GetIsApplicableCheck(Project contextProject)
            {
                if (contextProject.Id == _project.Id)
                {
                    // no need to do applicability check for a reference in our own project.
                    return null;
                }

                return workspace => workspace.CanAddProjectReference(contextProject.Id, _project.Id);
            }

            protected override bool CheckForExistingImport(Project project) => project.Id == _project.Id;

            public override bool Equals(object obj)
            {
                var reference = obj as ProjectSymbolReference;
                return base.Equals(reference) &&
                    _project.Id == reference._project.Id;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_project.Id, base.GetHashCode());
            }
        }
    }
}
