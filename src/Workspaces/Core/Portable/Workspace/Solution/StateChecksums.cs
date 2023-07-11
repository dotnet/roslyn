// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal sealed class SolutionStateChecksums(ImmutableArray<object> children) : ChecksumWithChildren(children)
    {
        public SolutionStateChecksums(
            Checksum attributesChecksum,
            ChecksumCollection projectChecksums,
            ChecksumCollection analyzerReferenceChecksums,
            Checksum frozenSourceGeneratedDocumentIdentity,
            Checksum frozenSourceGeneratedDocumentText)
            : this(ImmutableArray.Create<object>(attributesChecksum, projectChecksums, analyzerReferenceChecksums, frozenSourceGeneratedDocumentIdentity, frozenSourceGeneratedDocumentText))
        {
        }

        public Checksum Attributes => (Checksum)Children[0];
        public ChecksumCollection Projects => (ChecksumCollection)Children[1];
        public ChecksumCollection AnalyzerReferences => (ChecksumCollection)Children[2];
        public Checksum FrozenSourceGeneratedDocumentIdentity => (Checksum)Children[3];
        public Checksum FrozenSourceGeneratedDocumentText => (Checksum)Children[4];

        public async Task FindAsync(
            SolutionState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (searchingChecksumsLeft.Count == 0)
                return;

            // verify input
            if (searchingChecksumsLeft.Remove(Checksum))
                result[Checksum] = this;

            if (searchingChecksumsLeft.Remove(Attributes))
                result[Attributes] = state.SolutionAttributes;

            if (searchingChecksumsLeft.Remove(FrozenSourceGeneratedDocumentIdentity))
            {
                Contract.ThrowIfNull(state.FrozenSourceGeneratedDocumentState, "We should not have had a FrozenSourceGeneratedDocumentIdentity checksum if we didn't have a text in the first place.");
                result[FrozenSourceGeneratedDocumentIdentity] = state.FrozenSourceGeneratedDocumentState.Identity;
            }

            if (searchingChecksumsLeft.Remove(FrozenSourceGeneratedDocumentText))
            {
                Contract.ThrowIfNull(state.FrozenSourceGeneratedDocumentState, "We should not have had a FrozenSourceGeneratedDocumentState checksum if we didn't have a text in the first place.");
                result[FrozenSourceGeneratedDocumentText] = await SerializableSourceText.FromTextDocumentStateAsync(state.FrozenSourceGeneratedDocumentState, cancellationToken).ConfigureAwait(false);
            }

            if (searchingChecksumsLeft.Remove(Projects.Checksum))
            {
                result[Projects.Checksum] = Projects;
            }

            if (searchingChecksumsLeft.Remove(AnalyzerReferences.Checksum))
            {
                result[AnalyzerReferences.Checksum] = AnalyzerReferences;
            }

            foreach (var (_, projectState) in state.ProjectStates)
            {
                if (searchingChecksumsLeft.Count == 0)
                    break;

                // It's possible not all all our projects have checksums.  Specifically, we may have only been
                // asked to compute the checksum tree for a subset of projects that were all that a feature needed.
                if (projectState.TryGetStateChecksums(out var projectStateChecksums))
                    await projectStateChecksums.FindAsync(projectState, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
            }

            ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);
        }
    }

    internal class ProjectStateChecksums(ImmutableArray<object> children) : ChecksumWithChildren(children)
    {
        public ProjectStateChecksums(
            Checksum infoChecksum,
            Checksum compilationOptionsChecksum,
            Checksum parseOptionsChecksum,
            ChecksumCollection documentChecksums,
            ChecksumCollection projectReferenceChecksums,
            ChecksumCollection metadataReferenceChecksums,
            ChecksumCollection analyzerReferenceChecksums,
            ChecksumCollection additionalDocumentChecksums,
            ChecksumCollection analyzerConfigDocumentChecksums)
            : this(ImmutableArray.Create<object>(
                infoChecksum,
                compilationOptionsChecksum,
                parseOptionsChecksum,
                documentChecksums,
                projectReferenceChecksums,
                metadataReferenceChecksums,
                analyzerReferenceChecksums,
                additionalDocumentChecksums,
                analyzerConfigDocumentChecksums))
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public Checksum CompilationOptions => (Checksum)Children[1];
        public Checksum ParseOptions => (Checksum)Children[2];

        public ChecksumCollection Documents => (ChecksumCollection)Children[3];

        public ChecksumCollection ProjectReferences => (ChecksumCollection)Children[4];
        public ChecksumCollection MetadataReferences => (ChecksumCollection)Children[5];
        public ChecksumCollection AnalyzerReferences => (ChecksumCollection)Children[6];

        public ChecksumCollection AdditionalDocuments => (ChecksumCollection)Children[7];
        public ChecksumCollection AnalyzerConfigDocuments => (ChecksumCollection)Children[8];

        public async Task FindAsync(
            ProjectState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // verify input
            Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksum));
            Contract.ThrowIfFalse(this == stateChecksum);

            if (searchingChecksumsLeft.Count == 0)
                return;

            if (searchingChecksumsLeft.Remove(Checksum))
            {
                result[Checksum] = this;
            }

            if (searchingChecksumsLeft.Remove(Info))
            {
                result[Info] = state.ProjectInfo.Attributes;
            }

            if (searchingChecksumsLeft.Remove(CompilationOptions))
            {
                Contract.ThrowIfNull(state.CompilationOptions, "We should not be trying to serialize a project with no compilation options; RemoteSupportedLanguages.IsSupported should have filtered it out.");
                result[CompilationOptions] = state.CompilationOptions;
            }

            if (searchingChecksumsLeft.Remove(ParseOptions))
            {
                Contract.ThrowIfNull(state.ParseOptions, "We should not be trying to serialize a project with no compilation options; RemoteSupportedLanguages.IsSupported should have filtered it out.");
                result[ParseOptions] = state.ParseOptions;
            }

            if (searchingChecksumsLeft.Remove(Documents.Checksum))
            {
                result[Documents.Checksum] = Documents;
            }

            if (searchingChecksumsLeft.Remove(ProjectReferences.Checksum))
            {
                result[ProjectReferences.Checksum] = ProjectReferences;
            }

            if (searchingChecksumsLeft.Remove(MetadataReferences.Checksum))
            {
                result[MetadataReferences.Checksum] = MetadataReferences;
            }

            if (searchingChecksumsLeft.Remove(AnalyzerReferences.Checksum))
            {
                result[AnalyzerReferences.Checksum] = AnalyzerReferences;
            }

            if (searchingChecksumsLeft.Remove(AdditionalDocuments.Checksum))
            {
                result[AdditionalDocuments.Checksum] = AdditionalDocuments;
            }

            if (searchingChecksumsLeft.Remove(AnalyzerConfigDocuments.Checksum))
            {
                result[AnalyzerConfigDocuments.Checksum] = AnalyzerConfigDocuments;
            }

            ChecksumCollection.Find(state.ProjectReferences, ProjectReferences, searchingChecksumsLeft, result, cancellationToken);
            ChecksumCollection.Find(state.MetadataReferences, MetadataReferences, searchingChecksumsLeft, result, cancellationToken);
            ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);

            await ChecksumCollection.FindAsync(state.DocumentStates, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
            await ChecksumCollection.FindAsync(state.AdditionalDocumentStates, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
            await ChecksumCollection.FindAsync(state.AnalyzerConfigDocumentStates, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
        }
    }

    internal class DocumentStateChecksums(ImmutableArray<object> children) : ChecksumWithChildren(children)
    {
        public DocumentStateChecksums(Checksum infoChecksum, Checksum textChecksum)
            : this(ImmutableArray.Create<object>(infoChecksum, textChecksum))
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public Checksum Text => (Checksum)Children[1];

        public async Task FindAsync(
            TextDocumentState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            Debug.Assert(state.TryGetStateChecksums(out var stateChecksum) && this == stateChecksum);

            cancellationToken.ThrowIfCancellationRequested();

            if (searchingChecksumsLeft.Remove(Checksum))
            {
                result[Checksum] = this;
            }

            if (searchingChecksumsLeft.Remove(Info))
            {
                result[Info] = state.Attributes;
            }

            if (searchingChecksumsLeft.Remove(Text))
            {
                result[Text] = await SerializableSourceText.FromTextDocumentStateAsync(state, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// hold onto object checksum that currently doesn't have a place to hold onto checksum
    /// </summary>
    internal static class ChecksumCache
    {
        private static readonly ConditionalWeakTable<object, object> s_cache = new();

        public static IReadOnlyList<T> GetOrCreate<T>(IReadOnlyList<T> unorderedList, ConditionalWeakTable<object, object>.CreateValueCallback orderedListGetter)
            => (IReadOnlyList<T>)s_cache.GetValue(unorderedList, orderedListGetter);

        public static bool TryGetValue(object value, [NotNullWhen(true)] out Checksum? checksum)
        {
            // same key should always return same checksum
            if (!s_cache.TryGetValue(value, out var result))
            {
                checksum = null;
                return false;
            }

            checksum = (Checksum)result;
            return true;
        }

        public static Checksum GetOrCreate(object value, ConditionalWeakTable<object, object>.CreateValueCallback checksumCreator)
        {
            // same key should always return same checksum
            return (Checksum)s_cache.GetValue(value, checksumCreator);
        }

        public static T GetOrCreate<T>(object value, ConditionalWeakTable<object, object>.CreateValueCallback checksumCreator) where T : IChecksummedObject
        {
            // same key should always return same checksum
            return (T)s_cache.GetValue(value, checksumCreator);
        }
    }
}
