// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal sealed class SolutionStateChecksums : ChecksumWithChildren
    {
        public SolutionStateChecksums(Checksum infoChecksum, ProjectChecksumCollection projectChecksums) :
            this((object)infoChecksum, projectChecksums)
        {
        }

        public SolutionStateChecksums(params object[] children) : base(nameof(SolutionStateChecksums), children)
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public ProjectChecksumCollection Projects => (ProjectChecksumCollection)Children[1];

        public SolutionStateChecksums With(Checksum infoChecksum = null, ProjectChecksumCollection projectChecksums = null)
        {
            return new SolutionStateChecksums(infoChecksum ?? Info, projectChecksums ?? Projects);
        }

        public void Find(
            SolutionState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // verify input
            SolutionStateChecksums stateChecksum;
            Contract.ThrowIfFalse(state.TryGetStateChecksums(out stateChecksum));
            Contract.ThrowIfFalse(this == stateChecksum);

            if (searchingChecksumsLeft.Remove(Checksum))
            {
                result[Checksum] = this;
            }

            if (searchingChecksumsLeft.Remove(Info))
            {
                result[Info] = state.SolutionInfo.Attributes;
            }

            if (searchingChecksumsLeft.Remove(Projects.Checksum))
            {
                result[Projects.Checksum] = Projects;
            }

            foreach (var kv in state.ProjectStates)
            {
                var projectState = kv.Value;

                // solution state checksum can't be created without project state checksums created first
                ProjectStateChecksums projectStateChecksums;
                Contract.ThrowIfFalse(projectState.TryGetStateChecksums(out projectStateChecksums));

                projectStateChecksums.Find(projectState, searchingChecksumsLeft, result, cancellationToken);
                if (searchingChecksumsLeft.Count == 0)
                {
                    return;
                }
            }
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
            TextDocumentChecksumCollection additionalDocumentChecksums) :
            this(
                (object)infoChecksum,
                compilationOptionsChecksum,
                parseOptionsChecksum,
                documentChecksums,
                projectReferenceChecksums,
                metadataReferenceChecksums,
                analyzerReferenceChecksums,
                additionalDocumentChecksums)
        {
        }

        public ProjectStateChecksums(params object[] children) : base(nameof(ProjectStateChecksums), children)
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

        public ProjectStateChecksums With(
            Checksum infoChecksum = null,
            Checksum compilationOptionsChecksum = null,
            Checksum parseOptionsChecksum = null,
            DocumentChecksumCollection documentChecksums = null,
            ProjectReferenceChecksumCollection projectReferenceChecksums = null,
            MetadataReferenceChecksumCollection metadataReferenceChecksums = null,
            AnalyzerReferenceChecksumCollection analyzerReferenceChecksums = null,
            TextDocumentChecksumCollection additionalDocumentChecksums = null)
        {
            return new ProjectStateChecksums(
                infoChecksum ?? Info,
                compilationOptionsChecksum ?? CompilationOptions,
                parseOptionsChecksum ?? ParseOptions,
                documentChecksums ?? Documents,
                projectReferenceChecksums ?? ProjectReferences,
                metadataReferenceChecksums ?? MetadataReferences,
                analyzerReferenceChecksums ?? AnalyzerReferences,
                additionalDocumentChecksums ?? AdditionalDocuments);
        }

        public void Find(
            ProjectState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // verify input
            ProjectStateChecksums stateChecksum;
            Contract.ThrowIfFalse(state.TryGetStateChecksums(out stateChecksum));
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

            Find(state.DocumentStates, searchingChecksumsLeft, result, cancellationToken);
            Find(state.ProjectReferences, ProjectReferences, searchingChecksumsLeft, result, cancellationToken);
            Find(state.MetadataReferences, MetadataReferences, searchingChecksumsLeft, result, cancellationToken);
            Find(state.AnalyzerReferences, AnalyzerReferences, searchingChecksumsLeft, result, cancellationToken);
            Find(state.AdditionalDocumentStates, searchingChecksumsLeft, result, cancellationToken);
        }

        private static void Find<T>(
            ImmutableDictionary<DocumentId, T> values,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken) where T : TextDocumentState
        {
            foreach (var kv in values)
            {
                var state = kv.Value;

                DocumentStateChecksums stateChecksums;
                Contract.ThrowIfFalse(state.TryGetStateChecksums(out stateChecksums));

                stateChecksums.Find(state, searchingChecksumsLeft, result, cancellationToken);
                if (searchingChecksumsLeft.Count == 0)
                {
                    return;
                }
            }
        }

        private static void Find<T>(
            IReadOnlyList<T> values,
            ChecksumWithChildren checksums,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(values.Count == checksums.Children.Count);

            for (var i = 0; i < checksums.Children.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (searchingChecksumsLeft.Count == 0)
                {
                    return;
                }

                var checksum = (Checksum)checksums.Children[i];
                var value = values[i];

                if (searchingChecksumsLeft.Remove(checksum))
                {
                    result[checksum] = value;
                }
            }
        }
    }

    internal class DocumentStateChecksums : ChecksumWithChildren
    {
        public DocumentStateChecksums(Checksum infoChecksum, Checksum textChecksum) :
            this((object)infoChecksum, textChecksum)
        {
        }

        public DocumentStateChecksums(params object[] children) : base(nameof(DocumentStateChecksums), children)
        {
        }

        public Checksum Info => (Checksum)Children[0];
        public Checksum Text => (Checksum)Children[1];

        public DocumentStateChecksums With(Checksum infoChecksum = null, Checksum textChecksum = null)
        {
            return new DocumentStateChecksums(infoChecksum ?? Info, textChecksum ?? Text);
        }

        public void Find(
            TextDocumentState state,
            HashSet<Checksum> searchingChecksumsLeft,
            Dictionary<Checksum, object> result,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // verify input
            DocumentStateChecksums stateChecksum;
            Contract.ThrowIfFalse(state.TryGetStateChecksums(out stateChecksum));
            Contract.ThrowIfFalse(this == stateChecksum);

            if (searchingChecksumsLeft.Remove(Checksum))
            {
                result[Checksum] = this;
            }

            if (searchingChecksumsLeft.Remove(Info))
            {
                result[Info] = state.Info.Attributes;
            }

            if (searchingChecksumsLeft.Remove(Text))
            {
                // why I can't get text synchronously when async lazy support synchronous callback?
                result[Text] = state;
            }
        }
    }
}
