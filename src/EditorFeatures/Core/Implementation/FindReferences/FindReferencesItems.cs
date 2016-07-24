// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract class DefinitionLocation
    {
        /// <summary>
        /// Where the location originally came from (for example, the containing assembly or
        /// project name).  May be used in the presentation of a definition.
        /// </summary>
        public abstract ImmutableArray<TaggedText> OriginationParts { get; }

        protected DefinitionLocation()
        {
        }

        public abstract bool CanNavigateTo();
        public abstract bool TryNavigateTo();

        public static DefinitionLocation CreateDocumentLocation(DocumentLocation location)
        {
            return new DocumentDefinitionLocation(location);
        }

        public static DefinitionLocation CreateSymbolLocation(ISymbol symbol, Project referencingProject)
        {
            return new SymbolDefinitionLocation(symbol, referencingProject);
        }

        public static DefinitionLocation CreateNonNavigatingLocation(
            ImmutableArray<TaggedText> originationParts)
        {
            return new NonNavigatingDefinitionLocation(originationParts);
        }

        private sealed class DocumentDefinitionLocation : DefinitionLocation
        {
            private readonly DocumentLocation _location;

            public DocumentDefinitionLocation(DocumentLocation location)
            {
                _location = location;
            }

            public override ImmutableArray<TaggedText> OriginationParts =>
                ImmutableArray.Create(new TaggedText(TextTags.Text, _location.Document.Project.Name));

            public override bool CanNavigateTo()
            {
                return _location.CanNavigateTo();
            }

            public override bool TryNavigateTo()
            {
                return _location.TryNavigateTo();
            }
        }

        internal static ImmutableArray<TaggedText> GetOriginationParts(ISymbol symbol)
        {
            var assemblyName = symbol.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            return string.IsNullOrWhiteSpace(assemblyName)
                ? ImmutableArray<TaggedText>.Empty
                : ImmutableArray.Create(new TaggedText(TextTags.Assembly, assemblyName));
        }

        private sealed class SymbolDefinitionLocation : DefinitionLocation
        {
            private readonly Workspace _workspace;
            private readonly ProjectId _referencingProjectId;
            private readonly SymbolKey _symbolKey;
            private readonly ImmutableArray<TaggedText> _originationParts;

            public SymbolDefinitionLocation(ISymbol definition, Project project)
            {
                _workspace = project.Solution.Workspace;
                _referencingProjectId = project.Id;
                _symbolKey = definition.GetSymbolKey();
                _originationParts = GetOriginationParts(definition);
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo()
            {
                return TryNavigateTo((symbol, project, service) => true);
            }

            public override bool TryNavigateTo()
            {
                return TryNavigateTo((symbol, project, service) =>
                    service.TryNavigateToSymbol(symbol, project));
            }

            private bool TryNavigateTo(Func<ISymbol, Project, ISymbolNavigationService, bool> action)
            {
                var symbol = ResolveSymbolInCurrentSolution();
                var referencingProject = _workspace.CurrentSolution.GetProject(_referencingProjectId);
                if (symbol == null || referencingProject == null)
                {
                    return false;
                }

                var navigationService = _workspace.Services.GetService<ISymbolNavigationService>();
                return action(symbol, referencingProject, navigationService);
            }

            private ISymbol ResolveSymbolInCurrentSolution()
            {
                var compilation = _workspace.CurrentSolution.GetProject(_referencingProjectId)
                                                            .GetCompilationAsync(CancellationToken.None)
                                                            .WaitAndGetResult(CancellationToken.None);
                return _symbolKey.Resolve(compilation).Symbol;
            }
        }

        private sealed class NonNavigatingDefinitionLocation : DefinitionLocation
        {
            private readonly ImmutableArray<TaggedText> _originationParts;

            public NonNavigatingDefinitionLocation(ImmutableArray<TaggedText> originationParts)
            {
                _originationParts = originationParts;
            }

            public override ImmutableArray<TaggedText> OriginationParts => _originationParts;

            public override bool CanNavigateTo()
            {
                return false;
            }

            public override bool TryNavigateTo()
            {
                return false;
            }
        }
    }

    internal struct DocumentLocation : IEquatable<DocumentLocation>, IComparable<DocumentLocation>
    {
        public Document Document { get; }
        public TextSpan SourceSpan { get; }

        public DocumentLocation(Document document, TextSpan sourceSpan)
        {
            Document = document;
            SourceSpan = sourceSpan;
        }

        public override bool Equals(object obj)
        {
            return Equals((DocumentLocation)obj);
        }

        public bool Equals(DocumentLocation obj)
        {
            return this.CompareTo(obj) == 0;
        }

        public static bool operator ==(DocumentLocation d1, DocumentLocation d2)
        {
            return d1.Equals(d2);
        }

        public static bool operator !=(DocumentLocation d1, DocumentLocation d2)
        {
            return !(d1 == d2);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Document.FilePath, this.SourceSpan.GetHashCode());
        }

        public int CompareTo(DocumentLocation other)
        {
            int compare;

            var thisPath = this.Document.FilePath;
            var otherPath = other.Document.FilePath;

            if ((compare = StringComparer.OrdinalIgnoreCase.Compare(thisPath, otherPath)) != 0 ||
                (compare = this.SourceSpan.CompareTo(other.SourceSpan)) != 0)
            {
                return compare;
            }

            return 0;
        }

        public bool CanNavigateTo()
        {
            var workspace = Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToPosition(workspace, Document.Id, SourceSpan.Start);
        }

        public bool TryNavigateTo()
        {
            var workspace = Document.Project.Solution.Workspace;
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToPosition(workspace, Document.Id, SourceSpan.Start);
        }
    }

    internal sealed class DefinitionItem
    {
        public ImmutableArray<string> Tags { get; }

        public ImmutableArray<TaggedText> DisplayParts { get; }

        /// <summary>
        /// The locations to present in the UI.  A definition may have multiple locations
        /// for things like partial types/members.
        /// </summary>
        public ImmutableArray<DefinitionLocation> Locations { get; }

        /// <summary>
        /// Whether or not this definition should be presented if we never found any 
        /// references to it.  For example, when searching for a property, the Find
        /// References enginer will cascade to the accessors in case any code specifically
        /// called those accessors (can happen in cross language cases).  However, in the 
        /// normal case where there were no calls specifically to the accessor, we would
        /// not want to display them in the UI.  
        /// 
        /// For most definitions we will want to display them, even if no references were
        /// found.  This property allows for this customization in behavior.
        /// </summary>
        public bool DisplayIfNoReferences { get; }

        public DefinitionItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<DefinitionLocation> locations,
            bool displayIfNoReferences)
        {
            Tags = tags;
            DisplayParts = displayParts;
            Locations = locations;
            DisplayIfNoReferences = displayIfNoReferences;
        }
    }

    internal sealed class SourceReferenceItem : IComparable<SourceReferenceItem>
    {
        /// <summary>
        /// The definition this reference corresponds to.
        /// </summary>
        public DefinitionItem Definition { get; }

        /// <summary>
        /// The location of the source item.
        /// </summary>
        public DocumentLocation Location { get; }

        public SourceReferenceItem(DefinitionItem definition, DocumentLocation location)
        {
            Definition = definition;
            Location = location;
        }

        public int CompareTo(SourceReferenceItem other)
        {
            return this == other ? 0 : this.Location.CompareTo(other.Location);
        }
    }

    internal struct DefinitionsAndReferences
    {
        public static readonly DefinitionsAndReferences Empty =
            new DefinitionsAndReferences(ImmutableArray<DefinitionItem>.Empty, ImmutableArray<SourceReferenceItem>.Empty);

        /// <summary>
        /// All the definitions to show.  Note: not all definitions may have references.
        /// </summary>
        public ImmutableArray<DefinitionItem> Definitions { get; }

        /// <summary>
        /// All the references to show.  Note: every <see cref="SourceReferenceItem.Definition"/> 
        /// should be in <see cref="Definitions"/> 
        /// </summary>
        public ImmutableArray<SourceReferenceItem> References { get; }

        public DefinitionsAndReferences(
            ImmutableArray<DefinitionItem> definitions,
            ImmutableArray<SourceReferenceItem> references)
        {
            Definitions = definitions;
            References = references;
        }
    }
}
