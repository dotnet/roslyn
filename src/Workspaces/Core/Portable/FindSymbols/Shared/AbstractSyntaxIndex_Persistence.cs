// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal partial class AbstractSyntaxIndex<TIndex>
{
    private static readonly string s_persistenceName = typeof(TIndex).Name;

    /// <summary>
    /// Increment this whenever the data format of the <see cref="AbstractSyntaxIndex{TIndex}"/> changes.  This ensures
    /// that we will not try to read previously cached data from a prior version of roslyn with a different format and
    /// will instead regenerate all the indices with the new format.
    /// </summary>
    private static readonly Checksum s_serializationFormatChecksum = CodeAnalysis.Checksum.Create("41");

    /// <summary>
    /// Cache of ParseOptions to a checksum for the <see cref="ParseOptions.PreprocessorSymbolNames"/> contained
    /// within.  Useful so we don't have to continually reenumerate and regenerate the checksum given how rarely
    /// these ever change.
    /// </summary>
    private static readonly ConditionalWeakTable<ParseOptions, StrongBox<Checksum>> s_ppDirectivesToChecksum = new();

    public readonly Checksum? Checksum;

    protected static async Task<TIndex?> LoadAsync(
        SolutionKey solutionKey,
        ProjectState project,
        DocumentState document,
        Checksum textChecksum,
        Checksum textAndDirectivesChecksum,
        IndexReader read,
        CancellationToken cancellationToken)
    {
        var storageService = project.LanguageServices.SolutionServices.GetPersistentStorageService();
        var documentKey = DocumentKey.ToDocumentKey(ProjectKey.ToProjectKey(solutionKey, project), document);
        var stringTable = SyntaxTreeIndex.GetStringTable(project);

        // Try to read from the DB using either checksum.  If the writer determined there were no pp-directives,
        // then we may match it using textChecksum.  If there were pp directives, then we may match is using
        // textAndDirectivesChecksum.  if we match neither that means that either the data is not in the persistence
        // service, or it was written against genuinely different doc/pp-directive contents than before and we have
        // to recompute and store again.
        //
        // This does mean we have to potentially do two reads here.  However, that is cheap, and still nicer than
        // trying to produce the index again in the common case where we don't have to.
        return await LoadAsync(storageService, documentKey, textChecksum, stringTable, read, cancellationToken).ConfigureAwait(false) ??
               await LoadAsync(storageService, documentKey, textAndDirectivesChecksum, stringTable, read, cancellationToken).ConfigureAwait(false);
    }

    protected static async Task<TIndex?> LoadAsync(
        IChecksummedPersistentStorageService storageService,
        DocumentKey documentKey,
        Checksum? checksum,
        StringTable stringTable,
        IndexReader read,
        CancellationToken cancellationToken)
    {
        try
        {
            var storage = await storageService.GetStorageAsync(documentKey.Project.Solution, cancellationToken).ConfigureAwait(false);

            // attempt to load from persisted state
            using var stream = await storage.ReadStreamAsync(documentKey, s_persistenceName, checksum, cancellationToken).ConfigureAwait(false);
            if (stream != null)
            {
                using var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
                using var reader = ObjectReader.TryGetReader(gzipStream);
                if (reader != null)
                    return read(stringTable, reader, checksum);
            }
        }
        catch (Exception e) when (IOUtilities.IsNormalIOException(e))
        {
            // Storage APIs can throw arbitrary exceptions.
        }

        return null;
    }

    public static async ValueTask<(Checksum textOnlyChecksum, Checksum textAndDirectivesChecksum)> GetChecksumsAsync(
        ProjectState project,
        DocumentState document,
        CancellationToken cancellationToken)
    {
        // Since we build the SyntaxTreeIndex from a SyntaxTree, we need our checksum to change any time the
        // SyntaxTree could have changed.  Right now, that can only happen if the text of the document changes, or
        // the preprocessor directives change.  So we get the checksums for both of those, and merge them together
        // to make the final checksum.
        //
        // Note: this intentionally ignores *other* ParseOption changes.  This may look like it could cause us to
        // get inaccurate results, but here's why it's ok.  The other ParseOption changes include:
        //
        //  1. LanguageVersion changes.  It's ok to ignore that as for practically all language versions we don't
        //     produce different trees.  And, while there are some lang versions that produce different trees (for
        //     example around how records are parsed), it's not realistic that the user would somehow have a
        //     document that did *not* include pp directives, which somehow had one of those constructs *and*
        //     somehow had the code produce different trees across different language versions.  e.g. no code out
        //     there is realistically depending on `record` parsing as a method in C# X and as an actual record in
        //     C# X+1.  If code is using constructs that are actually parsing differently downlevel, they will have
        //     pp directives to avoid even using that construct downlevel.
        //
        //  2. DocComment parsing mode changes. However, in the IDE we always at least parse doc comments (though we
        //     have options to control if we report errors in it or not).  Since we're always parsing, we're always
        //     at least getting the same syntax tree shape at the end of the day.
        //
        // We also want the checksum to change any time our serialization format changes.  If the format has
        // changed, all previous versions should be invalidated.
        var documentChecksumState = await document.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

        var directivesChecksum = s_ppDirectivesToChecksum.GetValue(
            project.ParseOptions!,
            static parseOptions =>
                new StrongBox<Checksum>(CodeAnalysis.Checksum.Create(parseOptions.PreprocessorSymbolNames)));

        var textChecksum = CodeAnalysis.Checksum.Create(documentChecksumState.Text, s_serializationFormatChecksum);
        var textAndDirectivesChecksum = CodeAnalysis.Checksum.Create(textChecksum, directivesChecksum.Value);

        return (textChecksum, textAndDirectivesChecksum);
    }

    private Task<bool> SaveAsync(
        SolutionKey solutionKey,
        ProjectState project,
        DocumentState document,
        CancellationToken cancellationToken)
    {
        var persistentStorageService = project.LanguageServices.SolutionServices.GetPersistentStorageService();
        return SaveAsync(solutionKey, project, document, persistentStorageService, cancellationToken);
    }

    public Task<bool> SaveAsync(
        Document document, IChecksummedPersistentStorageService persistentStorageService)
    {
        return SaveAsync(
            SolutionKey.ToSolutionKey(document.Project.Solution),
            document.Project.State,
            (DocumentState)document.State,
            persistentStorageService,
            CancellationToken.None);
    }

    private async Task<bool> SaveAsync(
        SolutionKey solutionKey,
        ProjectState project,
        DocumentState document,
        IChecksummedPersistentStorageService persistentStorageService,
        CancellationToken cancellationToken)
    {
        try
        {
            var storage = await persistentStorageService.GetStorageAsync(solutionKey, cancellationToken).ConfigureAwait(false);

            using var stream = SerializableBytes.CreateWritableStream();
            using (var gzipStream = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true))
            using (var writer = new ObjectWriter(gzipStream, leaveOpen: true))
            {
                WriteTo(writer);
                gzipStream.Flush();
            }

            stream.Position = 0;

            var documentKey = DocumentKey.ToDocumentKey(ProjectKey.ToProjectKey(solutionKey, project), document);
            return await storage.WriteStreamAsync(documentKey, s_persistenceName, stream, this.Checksum, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (IOUtilities.IsNormalIOException(e))
        {
            // Storage APIs can throw arbitrary exceptions.
        }

        return false;
    }

    public abstract void WriteTo(ObjectWriter writer);
}
