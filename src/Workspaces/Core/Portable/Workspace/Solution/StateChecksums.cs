// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal sealed class SolutionStateChecksums : ChecksumWithChildren
    {
        public SolutionStateChecksums(Checksum infoChecksum, Checksum optionsChecksum, ProjectChecksumCollection projectChecksums, AnalyzerReferenceChecksumCollection analyzerReferenceChecksums)
            : this(new object[] { infoChecksum, optionsChecksum, projectChecksums, analyzerReferenceChecksums })
        {
        }

        public SolutionStateChecksums(object[] children) : base(WellKnownSynchronizationKind.SolutionStateChecksums, children)
        {
        }

        public Checksum Attributes => (Checksum)Children[0];
        public Checksum Options => (Checksum)Children[1];
        public ProjectChecksumCollection Projects => (ProjectChecksumCollection)Children[2];
        public AnalyzerReferenceChecksumCollection AnalyzerReferences => (AnalyzerReferenceChecksumCollection)Children[3];

        public async Task FindAsync(
            SolutionState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // verify input
            Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksum));
            Contract.ThrowIfFalse(this == stateChecksum);

            if (searchingChecksumsLeft.Remove(Checksum))
            {
                result[Checksum] = this;
            }

            if (searchingChecksumsLeft.Remove(Attributes))
            {
                result[Attributes] = state.SolutionAttributes;
            }

            if (searchingChecksumsLeft.Remove(Options))
            {
                result[Options] = state.Options;
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
                // solution state checksum can't be created without project state checksums created first
                // check unsupported projects
                if (!projectState.TryGetStateChecksums(out var projectStateChecksums))
                {
                    Contract.ThrowIfTrue(RemoteSupportedLanguages.IsSupported(projectState.Language));
                    continue;
                }

                await projectStateChecksums.FindAsync(projectState, searchingChecksumsLeft, result, cancellationToken).ConfigureAwait(false);
                if (searchingChecksumsLeft.Count == 0)
                {
                    break;
                }
            }

            ChecksumCollection.Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);
        }
    }

    internal class ProjectStateChecksums : ChecksumWithChildren
    {
        public ProjectStateChecksums(
            Checksum infoChecksum,
            Checksum compilationOptionsChecksum,
            Checksum parseOptionsChecksum,
            DocumentChecksumCollection documentChecksums,
            ProjectReferenceChecksumCollection projectReferenceChecksums,
            MetadataReferenceChecksumCollection metadataReferenceChecksums,
            AnalyzerReferenceChecksumCollection analyzerReferenceChecksums,
            TextDocumentChecksumCollection additionalDocumentChecksums,
            AnalyzerConfigDocumentChecksumCollection analyzerConfigDocumentChecksumCollection)
            : this(
                (object)infoChecksum,
                compilationOptionsChecksum,
                parseOptionsChecksum,
                documentChecksums,
                projectReferenceChecksums,
                metadataReferenceChecksums,
                analyzerReferenceChecksums,
                additionalDocumentChecksums,
                analyzerConfigDocumentChecksumCollection)
        {
        }

        public ProjectStateChecksums(params object[] children) : base(WellKnownSynchronizationKind.ProjectStateChecksums, children)
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public Checksum CompilationOptions => (Checksum)Children[1];
        public Checksum ParseOptions => (Checksum)Children[2];

        public DocumentChecksumCollection Documents => (DocumentChecksumCollection)Children[3];

        public ProjectReferenceChecksumCollection ProjectReferences => (ProjectReferenceChecksumCollection)Children[4];
        public MetadataReferenceChecksumCollection MetadataReferences => (MetadataReferenceChecksumCollection)Children[5];
        public AnalyzerReferenceChecksumCollection AnalyzerReferences => (AnalyzerReferenceChecksumCollection)Children[6];

        public TextDocumentChecksumCollection AdditionalDocuments => (TextDocumentChecksumCollection)Children[7];
        public AnalyzerConfigDocumentChecksumCollection AnalyzerConfigDocuments => (AnalyzerConfigDocumentChecksumCollection)Children[8];

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
                result[CompilationOptions] = state.CompilationOptions;
            }

            if (searchingChecksumsLeft.Remove(ParseOptions))
            {
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

    internal class DocumentStateChecksums : ChecksumWithChildren
    {
        public DocumentStateChecksums(Checksum infoChecksum, Checksum textChecksum)
            : this((object)infoChecksum, textChecksum)
        {
        }

        public DocumentStateChecksums(params object[] children) : base(WellKnownSynchronizationKind.DocumentStateChecksums, children)
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
            cancellationToken.ThrowIfCancellationRequested();

            // verify input
            Contract.ThrowIfFalse(state.TryGetStateChecksums(out var stateChecksum));
            Contract.ThrowIfFalse(this == stateChecksum);

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
        private static readonly ConditionalWeakTable<object, object> s_cache = new ConditionalWeakTable<object, object>();

        public static IReadOnlyList<T> GetOrCreate<T>(IReadOnlyList<T> unorderedList, ConditionalWeakTable<object, object>.CreateValueCallback orderedListGetter)
            => (IReadOnlyList<T>)s_cache.GetValue(unorderedList, orderedListGetter);

        public static bool TryGetValue(object value, out Checksum checksum)
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
