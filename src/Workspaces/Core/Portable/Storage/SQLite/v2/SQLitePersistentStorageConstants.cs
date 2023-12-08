// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal static class SQLitePersistentStorageConstants
    {
        // Version history.
        // 1. Initial use of sqlite as the persistence layer.  Simple key->value storage tables.
        // 2. Updated to store checksums.  Tables now key->(checksum,value).  Allows for reading
        //    and validating checksums without the overhead of reading the full 'value' into
        //    memory.
        // 3. Use an in-memory DB to cache writes before flushing to disk.
        // 4. Store checksums directly inline (i.e. 20 bytes), instead of using ObjectWriter serialization (which adds
        //    more data to the checksum).
        // 5. Use individual columns for primary keys.
        // 6. Use compression in some features.  Need to move to a different table since the blob
        //    format will be different and we don't want different VS versions (that do/don't support
        //    compression constantly stomping on each other.
        // 7. Checksum size changed from 20 bytes to 16 bytes long.
        private const string Version = "7";

        /// <summary>
        /// Inside the DB we have a table dedicated to storing strings that also provides a unique
        /// integral ID per string.  This allows us to store data keyed in a much more efficient
        /// manner as we can use those IDs instead of duplicating strings all over the place.  For
        /// example, there may be many pieces of data associated with a file.  We don't want to
        /// key off the file path in all these places as that would cause a large amount of bloat.
        ///
        /// Because the string table can map from arbitrary strings to unique IDs, it can also be
        /// used to create IDs for compound objects.  For example, given the IDs for the FilePath
        /// and Name of a Project, we can get an ID that represents the project itself by just
        /// creating a compound key of those two IDs.  This ID can then be used in other compound
        /// situations.  For example, a Document's ID is creating by compounding its Project's
        /// ID, along with the IDs for the Document's FilePath and Name.
        ///
        /// The format of the table is:
        ///
        ///  StringInfo
        ///  --------------------------------------------------------------------
        ///  | StringDataId (int, primary key, auto increment) | Data (varchar) |
        ///  --------------------------------------------------------------------
        /// </summary>
        public const string StringInfoTableName = "StringInfo" + Version;

        /// <summary>
        /// Inside the DB we have a table for data corresponding to the <see cref="Solution"/>.  The
        /// data is just a blob that is keyed by a string Id.  Data with this ID can be retrieved
        /// or overwritten.
        ///
        /// The format of the table is:
        ///
        ///  <code>
        ///  SolutionData
        ///  ----------------------------------------------------
        ///  | DataNameId (int) | Checksum (blob) | Data (blob) |
        ///  ----------------------------------------------------
        ///  | Primary Key      |
        ///  --------------------
        ///  </code>
        /// </summary>
        public const string SolutionDataTableName = "SolutionData" + Version;

        /// <summary>
        /// Inside the DB we have a table for data that we want associated with a <see cref="Project"/>. The data is
        /// keyed off of the path of the project and its name.  That way different TFMs will have different keys.
        ///
        /// The format of the table is:
        ///
        ///  <code>
        ///  ProjectData
        ///  ------------------------------------------------------------------------------------------------
        ///  | ProjectPathId (int) | ProjectNameId (int) | DataNameId (int) | Checksum (blob) | Data (blob) |
        ///  ------------------------------------------------------------------------------------------------
        ///  | Primary Key                                                  |
        ///  ----------------------------------------------------------------
        ///  </code>
        /// </summary>
        public const string ProjectDataTableName = "ProjectData" + Version;

        /// <summary>
        /// Inside the DB we have a table for data that we want associated with a <see cref="Document"/>. The data is
        /// keyed off the project information, and the folder and name of the document itself.  This allows the majority
        /// of the key to be shared (project path/name, and folder name) with other documents, and only having the doc
        /// name portion be distinct.  Different TFM flavors will also share everything but the project name.
        ///
        /// The format of the table is:
        ///
        ///  <code>
        ///  DocumentData
        ///  ------------------------------------------------------------------------------------------------------------------------------------------------
        ///  | ProjectPathId (int) | ProjectNameId (int) | DocumentFolderId (int) | DocumentNameId (int) | DataNameId (int) | Checksum (blob) | Data (blob) |
        ///  ------------------------------------------------------------------------------------------------------------------------------------------------
        ///  | Primary Key                                                                                                    |
        ///  ------------------------------------------------------------------------------------------------------------------
        ///  </code>
        /// </summary>
        public const string DocumentDataTableName = "DocumentData" + Version;

        public const string StringDataIdColumnName = "StringDataId";

        public const string ProjectPathIdColumnName = "ProjectPathId";
        public const string ProjectNameIdColumnName = "ProjectNameId";
        public const string DocumentFolderIdColumnName = "DocumentFolderId";
        public const string DocumentNameIdColumnName = "DocumentNameId";

        public const string DataNameIdColumnName = "DataNameId";
        public const string ChecksumColumnName = "Checksum";
        public const string DataColumnName = "Data";

        public const string SQLiteIntegerType = "integer";
        public const string SQLiteVarCharType = "varchar";
        public const string SQLiteBlobType = "blob";
    }
}
