﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// Handles references to source symbols both from the current project the user is invoking
        /// 'add-import' from, as well as symbols from other viable projects.
        /// 
        /// In the case where the reference is from another project we put a glyph in the add using
        /// light bulb and we say "(from ProjectXXX)" to make it clear that this will do more than
        /// just add a using/import.
        /// </summary>
        private partial class ProjectSymbolReference : SymbolReference
        {
            private readonly Project _project;

            public ProjectSymbolReference(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                SymbolResult<INamespaceOrTypeSymbol> symbolResult,
                Project project)
                : base(provider, symbolResult)
            {
                _project = project;
            }

            protected override ImmutableArray<string> GetTags(Document document)
            {
                return document.Project.Id == _project.Id
                    ? ImmutableArray<string>.Empty
                    : WellKnownTagArrays.AddReference;
            }

            /// <summary>
            /// If we're adding a reference to another project, it's ok to still add, even if there
            /// is an existing source-import in the file.  We won't add the import, but we'll still
            /// add the project-reference.
            /// </summary>
            protected override bool ShouldAddWithExistingImport(Document document)
                => document.Project.Id != _project.Id;

            protected override CodeActionPriority GetPriority(Document document)
            {
                // The only normal priority fix we have is when we find a hit in our
                // own project and we don't need to do a rename.  Anything else (i.e.
                // we need to add a project reference, or we need to rename) is low
                // priority.

                if (document.Project.Id == _project.Id)
                {
                    if (SearchResult.DesiredNameMatchesSourceName(document))
                    {
                        // The name doesn't change.  This is a normal priority action.
                        return CodeActionPriority.Medium;
                    }
                }

                // This is a weaker match.  This should be lower than all other fixes.
                return CodeActionPriority.Low;
            }

            protected override AddImportFixData GetFixData(
                Document document, ImmutableArray<TextChange> textChanges, string description,
                ImmutableArray<string> tags, CodeActionPriority priority)
            {
                return AddImportFixData.CreateForProjectSymbol(
                    textChanges, description, tags, priority, _project.Id);
            }

            protected override (string description, bool hasExistingImport) GetDescription(
                Document document, SyntaxNode node,
                SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                var (description, hasExistingImport) = base.GetDescription(document, node, semanticModel, cancellationToken);
                if (description == null)
                {
                    return (null, false);
                }

                var project = document.Project;
                description = project.Id == _project.Id
                    ? description
                    : string.Format(FeaturesResources.Add_reference_to_0, _project.Name);

                return (description, hasExistingImport);
            }

            public override bool Equals(object obj)
            {
                var reference = obj as ProjectSymbolReference;
                return base.Equals(reference) &&
                    _project.Id == reference._project.Id;
            }

            public override int GetHashCode()
                => Hash.Combine(_project.Id, base.GetHashCode());
        }
    }
}
