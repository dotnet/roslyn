// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract class DefinitionLocation
    {
        public abstract bool CanNavigateTo();
        public abstract bool TryNavigateTo();
    }

    internal sealed class DocumentDefinitionLocation : DefinitionLocation
    {
        public DocumentLocation Location { get; }

        public DocumentDefinitionLocation(DocumentLocation location)
        {
            Location = location;
        }

        public override bool CanNavigateTo()
        {
            return Location.CanNavigateTo();
        }

        public override bool TryNavigateTo()
        {
            return Location.TryNavigateTo();
        }
    }

    internal sealed class SymbolDefinitionLocation : DefinitionLocation
    {
        private readonly Workspace _workspace;
        private readonly ProjectId _referencingProjectId;
        private readonly SymbolKey _symbolKey;

        public SymbolDefinitionLocation(ISymbol definition, Project project)
        {
            _workspace = project.Solution.Workspace;
            _referencingProjectId = project.Id;
            _symbolKey = definition.GetSymbolKey();
        }

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

    internal sealed class NonNavigableDefinitionLocation : DefinitionLocation
    {
        public static readonly DefinitionLocation Instance = new NonNavigableDefinitionLocation();

        private NonNavigableDefinitionLocation()
        {
        }

        public override bool CanNavigateTo()
        {
            return false;
        }

        public override bool TryNavigateTo()
        {
            return false;
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

    internal static class DefinitionItemExtensions
    {
        public static DefinitionsAndReferences ToDefinitionItems(
            this IEnumerable<ReferencedSymbol> referencedSymbols, Solution solution)
        {
            var definitions = ImmutableArray.CreateBuilder<DefinitionItem>();
            var references = ImmutableArray.CreateBuilder<SourceReferenceItem>();

            foreach (var referencedSymbol in referencedSymbols)
            {
                ProcessReferencedSymbol(solution, referencedSymbol, definitions, references);
            }

            return new DefinitionsAndReferences(definitions.ToImmutable(), references.ToImmutable());
        }

        private static void ProcessReferencedSymbol(
            Solution solution,
            ReferencedSymbol referencedSymbol,
            ImmutableArray<DefinitionItem>.Builder definitions,
            ImmutableArray<SourceReferenceItem>.Builder references)
        {
            if (!referencedSymbol.ShouldShow())
            {
                return;
            }

            var definitionItem = CreateDefinitionItem(solution, referencedSymbol);
            if (definitionItem == null)
            {
                return;
            }

            definitions.Add(definitionItem);
            CreateReferences(referencedSymbol, references, definitionItem);
        }

        private static DefinitionItem CreateDefinitionItem(
            Solution solution, ReferencedSymbol referencedSymbol)
        {
            var definition = referencedSymbol.Definition;

            var locations = FilterDefinitionLocations(definition);
            var definitionLocations = ConvertLocations(solution, referencedSymbol, locations);
            if (definitionLocations.IsEmpty)
            {
                return null;
            }

            var firstLocation = locations.First();
            var allParts = ImmutableArray.CreateBuilder<TaggedText>();

            // For a symbol from metadata, include the assembly name before the symbol name.
            if (firstLocation.IsInMetadata)
            {
                var assemblyName = definition.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                if (!string.IsNullOrWhiteSpace(assemblyName))
                {
                    allParts.AddPunctuation("[");
                    allParts.AddText(assemblyName);
                    allParts.AddPunctuation("]");
                    allParts.AddSpace();
                }
            }

            var symbolParts = definition.ToDisplayParts(FindReferencesUtilities.DefinitionDisplayFormat).ToTaggedText();
            allParts.AddRange(symbolParts);

            return new DefinitionItem(
                GlyphTags.GetTags(definition.GetGlyph()),
                allParts.ToImmutable(),
                definitionLocations,
                definition.ShouldShowWithNoReferenceLocations());
        }

        private static ImmutableArray<DefinitionLocation> ConvertLocations(
            Solution solution, ReferencedSymbol referencedSymbol, ImmutableArray<Location> locations)
        {
            var definition = referencedSymbol.Definition;
            var result = ImmutableArray.CreateBuilder<DefinitionLocation>();

            foreach (var location in locations)
            {
                if (location.IsInMetadata)
                {
                    var firstSourceReferenceLocation = referencedSymbol.Locations.FirstOrDefault();
                    if (firstSourceReferenceLocation != null)
                    {
                        result.Add(new SymbolDefinitionLocation(
                            definition, firstSourceReferenceLocation.Document.Project));
                    }
                    else
                    {
                        result.Add(NonNavigableDefinitionLocation.Instance);
                    }
                }
                else if (location.IsInSource)
                {
                    var document = solution.GetDocument(location.SourceTree);
                    if (document != null)
                    {
                        var documentLocation = new DocumentLocation(document, location.SourceSpan);
                        if (documentLocation.CanNavigateTo())
                        {
                            result.Add(new DocumentDefinitionLocation(documentLocation));
                        }
                    }
                }
            }

            return result.ToImmutable();
        }

        private static ImmutableArray<Location> FilterDefinitionLocations(ISymbol definition)
        {
            // When finding references of a namespace, the data provided by the ReferenceFinder
            // will include one definition location for each of its exact namespace
            // declarations and each declaration of its children namespaces that mention
            // its name (e.g. definitions of A.B will include "namespace A.B.C"). The list of
            // reference locations includes both these namespace declarations and their
            // references in usings or fully qualified names. Instead of showing many top-level
            // declaration nodes (one of which will contain the full list of references
            // including declarations, the rest of which will say "0 references" due to
            // reference deduplication and there being no meaningful way to partition them),
            // we pick a single declaration to use as the top-level definition and nest all of
            // the declarations & references underneath.
            if (definition.IsKind(SymbolKind.Namespace))
            {
                // Prefer source location over metadata.
                var firstLocation = definition.Locations.FirstOrDefault(loc => loc.IsInSource) ?? definition.Locations.First();
                return ImmutableArray.Create(firstLocation);
            }

            return definition.Locations;
        }

        private static void CreateReferences(
            ReferencedSymbol referencedSymbol,
            ImmutableArray<SourceReferenceItem>.Builder references,
            DefinitionItem definitionItem)
        {
            foreach (var referenceLocation in referencedSymbol.Locations)
            {
                var location = referenceLocation.Location;
                if (!location.IsInSource)
                {
                    continue;
                }

                var documentLocation = new DocumentLocation(
                    referenceLocation.Document, referenceLocation.Location.SourceSpan);
                if (!documentLocation.CanNavigateTo())
                {
                    continue;

                }

                var referenceItem = new SourceReferenceItem(definitionItem, documentLocation);
                references.Add(referenceItem);
            }
        }
    }
}
