using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// Maintains a key-value map of text on disk.
    /// </summary>
    internal partial class TextStorage
    {
        private readonly ITextFactory textFactory;
        private readonly int MaxKeySize;
        private const int MaxChunkSize = 4096; // this is currently arbitrary 

        private readonly string databaseFileName;
        private readonly string connectionString;

        // this gate guards creating and destroying the database 
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private bool databaseExists = false;

        // manage our own connection pooling due to real performance concerns.
        // use conditional weak table for connection pool so we don't keep transient threads alive
        private readonly ConditionalWeakTable<Thread, Future<SqlCeConnection>> openConnectionPool = new ConditionalWeakTable<Thread, Future<SqlCeConnection>>();
        private readonly ConditionalWeakTable<Thread, Future<SqlCeConnection>>.CreateValueCallback createSqlCeConnection;

        // keep a separate list of open connections so we can close them later
        private readonly ConcurrentBag<SqlCeConnection> openConnectionList = new ConcurrentBag<SqlCeConnection>();

        public TextStorage(string filename, ITextFactory textFactory, int maxKeySize = 64)
        {
            this.textFactory = textFactory;
            this.databaseFileName = filename;
            this.MaxKeySize = maxKeySize;
            this.connectionString = string.Format("Data Source='{0}'; Max Database Size=4000", this.databaseFileName);
            this.createSqlCeConnection = this.CreateSqlCeConnection;
        }

        /// <summary>
        /// True if the storage exists
        /// </summary>
        public bool Exists
        {
            get
            {
                return this.databaseExists;
            }
        }

        private Future<SqlCeConnection> CreateSqlCeConnection(Thread thread)
        {
            return new Future<SqlCeConnection>(() =>
            {
                var connection = new SqlCeConnection(this.connectionString);
                connection.Open();
                this.openConnectionList.Add(connection);
                return connection;
            });
        }

        private SqlCeConnection GetOpenConnection()
        {
            return this.openConnectionPool.GetValue(Thread.CurrentThread, createSqlCeConnection).Value;
        }

        private void ReturnOpenCollectionToPool(SqlCeConnection connection)
        {
            // since this is stored by thread-id now, there is no needed to add/remove 
        }

        private void CloseAllOpenConnections()
        {
            using (this.gate.DisposableWait())
            {
                foreach (var connection in this.openConnectionList)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Creates a new storage. After the call to Create the storage is open.
        /// </summary>
        public bool Create(CancellationToken cancellationToken)
        {
            using (this.gate.DisposableWait(cancellationToken))
            {
                if (!this.databaseExists)
                {
                    if (File.Exists(this.databaseFileName))
                    {
                        File.Delete(this.databaseFileName);
                    }

                    // use SqlCeEngine to create a new empty database
                    var engine = new SqlCeEngine();
                    engine.LocalConnectionString = this.connectionString;
                    engine.CreateDatabase();

                    // issue a create table command to create the key-value table we will use
                    var connection = this.GetOpenConnection();
                    try
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = string.Format("CREATE TABLE TextData (id NVARCHAR({0}), text NTEXT, CONSTRAINT PK PRIMARY KEY (id))", MaxKeySize);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    finally
                    {
                        this.ReturnOpenCollectionToPool(connection);
                    }

                    this.databaseExists = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Destroys an existing storage. If the storage was open before the call to Destroy it will be closed.
        /// </summary>
        public void Destroy(CancellationToken cancellationToken)
        {
            this.CloseAllOpenConnections();

            using (this.gate.DisposableWait(cancellationToken))
            {
                if (File.Exists(this.databaseFileName))
                {
                    File.Delete(this.databaseFileName);
                }
            }
        }

        /// <summary>
        /// Adds text to the storage given a unique key. 
        /// </summary>
        public void Add(string key, IText text, CancellationToken cancellationToken)
        {
            // insert first chunk (or whole text if less than max chunk size)
            var firstChunk = text.GetText(new TextSpan(0, Math.Min(text.Length, MaxChunkSize)));

            var connection = this.GetOpenConnection();
            try
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.RepeatableRead))
                {
                    using (var insertCommand = connection.CreateCommand())
                    {
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = "INSERT INTO TextData(id, text) VALUES (@k, @v)";
                        insertCommand.Parameters.Add("k", SqlDbType.NVarChar).Value = key;
                        insertCommand.Parameters.Add("v", SqlDbType.NText, text.Length).Value = firstChunk;
                        var rows = insertCommand.ExecuteNonQuery();
                        System.Diagnostics.Debug.Assert(rows == 1);

                        // append rest using updatable result set
                        if (firstChunk.Length < text.Length)
                        {
                            // get result set by querying for the first chunk inserted above
                            using (var selectCommand = connection.CreateCommand())
                            {
                                selectCommand.Transaction = transaction;
                                selectCommand.CommandText = "SELECT text FROM TextData WHERE id = @k";
                                selectCommand.Parameters.Add("k", SqlDbType.NVarChar).Value = key;

                                using (var resultSet = selectCommand.ExecuteResultSet(ResultSetOptions.Updatable))
                                {
                                    // call Read to position result set on the single result row
                                    resultSet.Read();

                                    // use IText.Write to copy text into field
                                    text.Write(new FieldTextWriter(resultSet, fieldIndex: 0));

                                    // call Update to write row change back to database
                                    resultSet.Update();
                                }
                            }
                        }
                    }

                    transaction.Commit();
                }
            }
            finally
            {
                this.ReturnOpenCollectionToPool(connection);
            }
        }

        /// <summary>
        /// Removes the text from the storage associated with the specified key.
        /// </summary>
        public void Remove(string key, CancellationToken cancellationToken)
        {
            var connection = this.GetOpenConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM TextData WHERE id = @k";
                    command.Parameters.Add("k", SqlDbType.NVarChar).Value = key;
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                this.ReturnOpenCollectionToPool(connection);
            }
        }

        /// <summary>
        /// Retrieves the text from the storage associated with the specified key.
        /// </summary>
        public IText Retrieve(string key, CancellationToken cancellationToken)
        {
            var connection = this.GetOpenConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT text FROM TextData WHERE id = @k";
                    command.Parameters.Add("k", SqlDbType.NVarChar).Value = key;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var textReader = new FieldTextReader(reader, fieldIndex: 0);
                            return this.textFactory.CreateText(textReader, cancellationToken);
                        }

                        throw new InvalidOperationException("Key not found.".NeedsLocalization());
                    }
                }
            }
            finally
            {
                this.ReturnOpenCollectionToPool(connection);
            }
        }
    }
}