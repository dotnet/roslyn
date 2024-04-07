// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Data about a navigate to match.  Only intended for use by C# and VB.  Carries enough rich information to
/// rehydrate everything needed quickly on either the host or remote side.
/// </summary>
[DataContract]
internal readonly struct RoslynNavigateToItem(
    bool isStale,
    DocumentKey documentKey,
    ImmutableArray<ProjectId> additionalMatchingProjects,
    DeclaredSymbolInfo declaredSymbolInfo,
    string kind,
    NavigateToMatchKind matchKind,
    bool isCaseSensitive,
    ImmutableArray<TextSpan> nameMatchSpans,
    ImmutableArray<PatternMatch> matches)
{
    [DataMember(Order = 0)]
    public readonly bool IsStale = isStale;

    [DataMember(Order = 1)]
    public readonly DocumentKey DocumentKey = documentKey;

    [DataMember(Order = 2)]
    public readonly ImmutableArray<ProjectId> AdditionalMatchingProjects = additionalMatchingProjects;

    [DataMember(Order = 3)]
    public readonly DeclaredSymbolInfo DeclaredSymbolInfo = declaredSymbolInfo;

    /// <summary>
    /// Will be one of the values from <see cref="NavigateToItemKind"/>.
    /// </summary>
    [DataMember(Order = 4)]
    public readonly string Kind = kind;

    [DataMember(Order = 5)]
    public readonly NavigateToMatchKind MatchKind = matchKind;

    [DataMember(Order = 6)]
    public readonly bool IsCaseSensitive = isCaseSensitive;

    [DataMember(Order = 7)]
    public readonly ImmutableArray<TextSpan> NameMatchSpans = nameMatchSpans;

    [DataMember(Order = 8)]
    public readonly ImmutableArray<PatternMatch> Matches = matches;

    public DocumentId DocumentId => this.DocumentKey.Id;

    public async Task<INavigateToSearchResult?> TryCreateSearchResultAsync(
        Solution solution, Document? activeDocument, CancellationToken cancellationToken)
    {
        if (IsStale)
        {
            // may refer to a document that doesn't exist anymore.  Bail out gracefully in that case.
            var document = solution.GetDocument(DocumentId);
            if (document == null)
                return null;

            return new NavigateToSearchResult(this, document, activeDocument);
        }
        else
        {
            var document = await solution.GetRequiredDocumentAsync(
                DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            return new NavigateToSearchResult(this, document, activeDocument);
        }
    }

    private class NavigateToSearchResult : INavigateToSearchResult, INavigableItem
    {
        private static readonly char[] s_dotArray = ['.'];

        private readonly RoslynNavigateToItem _item;

        /// <summary>
        /// The <see cref="Document"/> that <see cref="_item"/> is contained within.
        /// </summary>
        private readonly INavigableItem.NavigableDocument _itemDocument;

        /// <summary>
        /// The document the user was editing when they invoked the navigate-to operation.
        /// </summary>
        private readonly (DocumentId id, IReadOnlyList<string> folders)? _activeDocument;

        private readonly string _additionalInformation;
        private readonly Lazy<string> _secondarySort;

        public NavigateToSearchResult(
            RoslynNavigateToItem item,
            Document itemDocument,
            Document? activeDocument)
        {
            _item = item;
            _itemDocument = INavigableItem.NavigableDocument.FromDocument(itemDocument);
            if (activeDocument is not null)
                _activeDocument = (activeDocument.Id, activeDocument.Folders);

            _additionalInformation = ComputeAdditionalInformation(in item, itemDocument);
            _secondarySort = new Lazy<string>(ComputeSecondarySort);
        }

        private static string ComputeAdditionalInformation(in RoslynNavigateToItem item, Document itemDocument)
        {
            // For partial types, state what file they're in so the user can disambiguate the results.
            var combinedProjectName = ComputeCombinedProjectName(in item, itemDocument);
            return (item.DeclaredSymbolInfo.IsPartial, IsNonNestedNamedType(in item)) switch
            {
                (true, true) => string.Format(FeaturesResources._0_dash_1, itemDocument.Name, combinedProjectName),
                (true, false) => string.Format(FeaturesResources.in_0_1_2, item.DeclaredSymbolInfo.ContainerDisplayName, itemDocument.Name, combinedProjectName),
                (false, true) => string.Format(FeaturesResources.project_0, combinedProjectName),
                (false, false) => string.Format(FeaturesResources.in_0_project_1, item.DeclaredSymbolInfo.ContainerDisplayName, combinedProjectName),
            };
        }

        private static string ComputeCombinedProjectName(in RoslynNavigateToItem item, Document itemDocument)
        {
            // If there aren't any additional matches in other projects, we don't need to merge anything.
            if (item.AdditionalMatchingProjects.Length > 0)
            {
                // First get the simple project name and flavor for the actual project we got a hit in.  If we can't
                // figure this out, we can't create a merged name.
                var firstProject = itemDocument.Project;
                var (firstProjectName, firstProjectFlavor) = firstProject.State.NameAndFlavor;

                if (firstProjectName != null)
                {
                    var solution = firstProject.Solution;

                    using var _ = ArrayBuilder<string>.GetInstance(out var flavors);
                    flavors.Add(firstProjectFlavor!);

                    // Now, do the same for the other projects where we had a match. As above, if we can't figure out the
                    // simple name/flavor, or if the simple project name doesn't match the simple project name we started
                    // with then we can't merge these.
                    foreach (var additionalProjectId in item.AdditionalMatchingProjects)
                    {
                        var additionalProject = solution.GetRequiredProject(additionalProjectId);
                        var (projectName, projectFlavor) = additionalProject.State.NameAndFlavor;
                        if (projectName == firstProjectName)
                            flavors.Add(projectFlavor!);
                    }

                    flavors.RemoveDuplicates();
                    flavors.Sort();

                    return $"{firstProjectName} ({string.Join(", ", flavors)})";
                }
            }

            // Couldn't compute a merged project name (or only had one project).  Just return the name of hte project itself.
            return itemDocument.Project.Name;
        }

        string INavigateToSearchResult.AdditionalInformation => _additionalInformation;

        private static bool IsNonNestedNamedType(in RoslynNavigateToItem item)
            => !item.DeclaredSymbolInfo.IsNestedType && IsNamedType(in item);

        private static bool IsNamedType(in RoslynNavigateToItem item)
        {
            switch (item.DeclaredSymbolInfo.Kind)
            {
                case DeclaredSymbolInfoKind.Class:
                case DeclaredSymbolInfoKind.Record:
                case DeclaredSymbolInfoKind.Enum:
                case DeclaredSymbolInfoKind.Interface:
                case DeclaredSymbolInfoKind.Module:
                case DeclaredSymbolInfoKind.Struct:
                case DeclaredSymbolInfoKind.RecordStruct:
                    return true;
                default:
                    return false;
            }
        }

        string INavigateToSearchResult.Kind => _item.Kind;

        NavigateToMatchKind INavigateToSearchResult.MatchKind => _item.MatchKind;

        bool INavigateToSearchResult.IsCaseSensitive => _item.IsCaseSensitive;

        string INavigateToSearchResult.Name => _item.DeclaredSymbolInfo.Name;

        ImmutableArray<TextSpan> INavigateToSearchResult.NameMatchSpans => _item.NameMatchSpans;

        string INavigateToSearchResult.SecondarySort => _secondarySort.Value;

        private string ComputeSecondarySort()
        {
            using var _ = ArrayBuilder<string>.GetInstance(out var parts);

            // Ensure if all else is equal, that high-pri items (e.g. from the user's current file) come first
            // before low pri items.  This only applies if things like the MatchKind are the same.  So we'll
            // still show an exact match from another file before a substring match from the current file.
            parts.Add(ComputeFolderDistance().ToString("X4"));

            parts.Add(_item.DeclaredSymbolInfo.ParameterCount.ToString("X4"));
            parts.Add(_item.DeclaredSymbolInfo.TypeParameterCount.ToString("X4"));
            parts.Add(_item.DeclaredSymbolInfo.Name);

            // For partial types, we break up the file name into pieces.  i.e. If we have
            // Outer.cs and Outer.Inner.cs  then we add "Outer" and "Outer Inner" to 
            // the secondary sort string.  That way "Outer.cs" will be weighted above
            // "Outer.Inner.cs"
            var fileName = Path.GetFileNameWithoutExtension(_itemDocument.FilePath ?? "");
            parts.AddRange(fileName.Split(s_dotArray));

            return string.Join(" ", parts);

            // How close these files are in terms of file system path.  Identical files will have distance 0. Files
            // in the same folder will have distance 1.  Files in different folders will have increasing values here
            // depending on how many folder elements they share/differ on.
            int ComputeFolderDistance()
            {
                // No need to compute anything if there is no active document.  Consider all documents equal.
                if (_activeDocument is not { } activeDocument)
                    return 0;

                // The result was in the active document, this get highest priority.
                if (activeDocument.id == _itemDocument.Id)
                    return 0;

                var activeFolders = activeDocument.folders;
                var itemFolders = _itemDocument.Folders;

                // see how many folder they have in common.
                var commonCount = GetCommonFolderCount();

                // from this, we can see how many folders then differ between them.
                var activeDiff = activeFolders.Count - commonCount;
                var itemDiff = itemFolders.Count - commonCount;

                // Add one more to the result.  This way if they share all the same folders that we still return
                // '1', indicating that this close to, but not as good a match as an exact file match.
                return activeDiff + itemDiff + 1;

                int GetCommonFolderCount()
                {
                    var activeFolders = activeDocument.folders;
                    var itemFolders = _itemDocument.Folders;

                    var maxCommon = Math.Min(activeFolders.Count, itemFolders.Count);
                    for (var i = 0; i < maxCommon; i++)
                    {
                        if (activeFolders[i] != itemFolders[i])
                            return i;
                    }

                    return maxCommon;
                }
            }
        }

        string? INavigateToSearchResult.Summary => null;

        INavigableItem INavigateToSearchResult.NavigableItem => this;

        ImmutableArray<PatternMatch> INavigateToSearchResult.Matches => _item.Matches;

        #region INavigableItem

        Glyph INavigableItem.Glyph => GetGlyph(_item.DeclaredSymbolInfo.Kind, _item.DeclaredSymbolInfo.Accessibility);

        private static Glyph GetPublicGlyph(DeclaredSymbolInfoKind kind)
            => kind switch
            {
                DeclaredSymbolInfoKind.Class => Glyph.ClassPublic,
                DeclaredSymbolInfoKind.Constant => Glyph.ConstantPublic,
                DeclaredSymbolInfoKind.Constructor => Glyph.MethodPublic,
                DeclaredSymbolInfoKind.Delegate => Glyph.DelegatePublic,
                DeclaredSymbolInfoKind.Enum => Glyph.EnumPublic,
                DeclaredSymbolInfoKind.EnumMember => Glyph.EnumMemberPublic,
                DeclaredSymbolInfoKind.Event => Glyph.EventPublic,
                DeclaredSymbolInfoKind.ExtensionMethod => Glyph.ExtensionMethodPublic,
                DeclaredSymbolInfoKind.Field => Glyph.FieldPublic,
                DeclaredSymbolInfoKind.Indexer => Glyph.PropertyPublic,
                DeclaredSymbolInfoKind.Interface => Glyph.InterfacePublic,
                DeclaredSymbolInfoKind.Method => Glyph.MethodPublic,
                DeclaredSymbolInfoKind.Module => Glyph.ModulePublic,
                DeclaredSymbolInfoKind.Property => Glyph.PropertyPublic,
                DeclaredSymbolInfoKind.Struct => Glyph.StructurePublic,
                DeclaredSymbolInfoKind.RecordStruct => Glyph.StructurePublic,
                _ => Glyph.ClassPublic,
            };

        private static Glyph GetGlyph(DeclaredSymbolInfoKind kind, Accessibility accessibility)
        {
            // Glyphs are stored in this order:
            //  ClassPublic,
            //  ClassProtected,
            //  ClassPrivate,
            //  ClassInternal,

            var rawGlyph = GetPublicGlyph(kind);

            switch (accessibility)
            {
                case Accessibility.Private:
                    rawGlyph += (Glyph.ClassPrivate - Glyph.ClassPublic);
                    break;
                case Accessibility.Internal:
                    rawGlyph += (Glyph.ClassInternal - Glyph.ClassPublic);
                    break;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.ProtectedAndInternal:
                    rawGlyph += (Glyph.ClassProtected - Glyph.ClassPublic);
                    break;
            }

            return rawGlyph;
        }

        ImmutableArray<TaggedText> INavigableItem.DisplayTaggedParts
            => [new TaggedText(
                TextTags.Text, _item.DeclaredSymbolInfo.Name + _item.DeclaredSymbolInfo.NameSuffix)];

        bool INavigableItem.DisplayFileLocation => false;

        /// <summary>
        /// DeclaredSymbolInfos always come from some actual declaration in source.  So they're
        /// never implicitly declared.
        /// </summary>
        bool INavigableItem.IsImplicitlyDeclared => false;

        INavigableItem.NavigableDocument INavigableItem.Document => _itemDocument;

        TextSpan INavigableItem.SourceSpan => _item.DeclaredSymbolInfo.Span;

        bool INavigableItem.IsStale => _item.IsStale;

        ImmutableArray<INavigableItem> INavigableItem.ChildItems => [];

        #endregion
    }
}
