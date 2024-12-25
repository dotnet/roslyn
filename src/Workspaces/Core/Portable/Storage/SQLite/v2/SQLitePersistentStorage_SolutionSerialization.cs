// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.SQLite.v2;

using static SQLitePersistentStorageConstants;

internal sealed partial class SQLitePersistentStorage
{
    public override Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
        => _solutionAccessor.ChecksumMatchesAsync(this.SolutionKey, name, checksum, cancellationToken);

    public override Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken)
        => _solutionAccessor.ReadStreamAsync(this.SolutionKey, name, checksum, cancellationToken);

    public override Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => _solutionAccessor.WriteStreamAsync(this.SolutionKey, name, stream, checksum, cancellationToken);

    private readonly record struct SolutionPrimaryKey();

    /// <summary>
    /// <see cref="Accessor{TKey, TDatabaseId}"/> responsible for storing and 
    /// retrieving data from <see cref="SolutionDataTableName"/>.  Note that with the Solution 
    /// table there is no need for key->id translation.  i.e. the key acts as the ID itself.
    /// </summary>
    private sealed class SolutionAccessor(SQLitePersistentStorage storage) : Accessor<SolutionKey, SolutionPrimaryKey>(Table.Solution,
              storage)
    {

        // For the SolutionDataTable the key itself acts as the data-id.
        protected override SolutionPrimaryKey? TryGetDatabaseKey(SqlConnection connection, SolutionKey key, bool allowWrite)
            => new SolutionPrimaryKey();

        protected override void BindAccessorSpecificPrimaryKeyParameters(SqlStatement statement, SolutionPrimaryKey primaryKey)
        {
            // nothing to do.  A solution row just needs the id of the data-name (which the caller handles).
        }
    }
}
