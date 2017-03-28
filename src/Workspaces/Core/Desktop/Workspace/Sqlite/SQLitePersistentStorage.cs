// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage : AbstractPersistentStorage
    {
        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly Func<string, object, CancellationToken, Stream> _readStream;
        private readonly Func<string, Stream, CancellationToken, bool> _writeStream;

        private readonly SQLiteStorage _sqliteStorage;

        public SQLitePersistentStorage(
            IOptionService optionService, string workingFolderPath, string solutionFilePath, Action<AbstractPersistentStorage> disposer) :
            base(optionService, workingFolderPath, solutionFilePath, disposer)
        {
            // solution must exist in disk. otherwise, we shouldn't be here at all.
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(solutionFilePath));

            _readStream = ReadStream;
            _writeStream = WriteStream;


            var databaseFile = GetDatabaseFile(workingFolderPath);

            this.DatabaseFileDirectory = Path.GetDirectoryName(databaseFile);

            if (!Directory.Exists(this.DatabaseFileDirectory))
            {
                Directory.CreateDirectory(this.DatabaseFileDirectory);
            }

            _sqliteStorage = new SQLiteStorage(databaseFile);
        }

        public static string GetDatabaseFile(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));

            return Path.Combine(workingFolderPath, StorageExtension, PersistentStorageFileName);
        }

        internal override void Initialize()
        {
            _sqliteStorage.Initialize();
        }

        internal override string DatabaseFileDirectory { get; }

        public override Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!TryGetDocumentKey(document, name, out var key))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = SQLiteExceptionWrapper(key, _readStream, cancellationToken);
            return SpecializedTasks.DefaultOrResult(stream);
        }

        public override Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!TryGetProjectKey(project, name, out var key))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = SQLiteExceptionWrapper(key, _readStream, cancellationToken);
            return SpecializedTasks.DefaultOrResult(stream);
        }

        public override Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!TryGetSolutionKey(name, out var key))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = SQLiteExceptionWrapper(key, _readStream, cancellationToken);
            return SpecializedTasks.DefaultOrResult(stream);
        }

        private Stream ReadStream(string key, object unused, CancellationToken cancellationToken)
        {
            using (var accessor = GetAccessor(key))
            using (var esentStream = accessor.GetReadStream(key))
            {
                if (esentStream == null)
                {
                    return null;
                }

                // this will copy over esent stream and let it go.
                return SerializableBytes.CreateReadableStream(esentStream, cancellationToken);
            }
        }

        //private Stream ReadStreamSolution(int nameId, object unused1, object unused2, object unused3, CancellationToken cancellationToken)
        //{
        //    using (var accessor = _sqliteStorage.GetSolutionTableAccessor())
        //    using (var esentStream = accessor.GetReadStream(nameId))
        //    {
        //        if (esentStream == null)
        //        {
        //            return null;
        //        }

        //        // this will copy over esent stream and let it go.
        //        return SerializableBytes.CreateReadableStream(esentStream, cancellationToken);
        //    }
        //}

        public override Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));
            Contract.ThrowIfNull(stream);

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.False;
            }

            if (!TryGetDocumentKey(document, name, out var key))
            {
                return SpecializedTasks.False;
            }

            var success = SQLiteExceptionWrapper(key, stream, _writeStream, cancellationToken);
            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        public override Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));
            Contract.ThrowIfNull(stream);

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.False;
            }

            if (!TryGetProjectKey(project, name, out var key))
            {
                return SpecializedTasks.False;
            }

            var success = SQLiteExceptionWrapper(key, stream, _writeStream, cancellationToken);
            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        public override Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));
            Contract.ThrowIfNull(stream);

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.False;
            }

            if (!TryGetSolutionKey(name, out var key))
            {
                return SpecializedTasks.False;
            }

            var success = SQLiteExceptionWrapper(key, stream, _writeStream, cancellationToken);
            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        private bool WriteStream(string key, Stream stream, CancellationToken cancellationToken)
        {
            using (var accessor = GetAccessor(key))
            using (var esentStream = accessor.GetWriteStream(key))
            {
                WriteToStream(stream, esentStream, cancellationToken);
                return accessor.ApplyChanges();
            }
        }

        //private bool WriteStreamSolution(int nameId, Stream stream, object unused1, object unused2, CancellationToken cancellationToken)
        //{
        //    using (var accessor = _sqliteStorage.GetSolutionTableAccessor())
        //    using (var esentStream = accessor.GetWriteStream(nameId))
        //    {
        //        WriteToStream(stream, esentStream, cancellationToken);
        //        return accessor.ApplyChanges();
        //    }
        //}

        public override void Close()
        {
            _sqliteStorage.Close();
        }

        private static void WriteToStream(Stream inputStream, Stream esentStream, CancellationToken cancellationToken)
        {
            var buffer = SharedPools.ByteArray.Allocate();
            try
            {
                int bytesRead;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        esentStream.Write(buffer, 0, bytesRead);
                    }
                }
                while (bytesRead > 0);

                // flush the data and trim column size of necessary
                esentStream.Flush();
            }
            finally
            {
                SharedPools.ByteArray.Free(buffer);
            }
        }

        private SQLiteStorage.Accessor GetAccessor(string key)
            => _sqliteStorage.GetAccessor(key);

        private bool TryGetDocumentKey(Document document, string name, out string key)
        {
            key = $"D-{document.Project.FilePath}-{document.Project.Name}-{document.FilePath}-{name}";
            return true;
        }

        private bool TryGetProjectKey(Project project, string name, out string key)
        {
            key = $"P-{project.FilePath}-{project.Name}-{name}";
            return true;
        }

        private bool TryGetSolutionKey(string name, out string key)
        {
            key = $"S-{name}";
            return true;
        }

        private TResult SQLiteExceptionWrapper<TArg1, TResult>(TArg1 arg1, Func<TArg1, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return SQLiteExceptionWrapper(arg1, (object)null, func, cancellationToken);
        }

        private TResult SQLiteExceptionWrapper<TArg1, TArg2, TResult>(
            TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            try
            {
                return func(arg1, arg2, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (SQLiteException ex)
            {
                if (!_sqliteStorage.IsClosed)
                {
                    // ignore esent exception if underlying storage was closed
                    // there is not much we can do here. 
                    // internally we use it as a way to cache information between sessions anyway. 
                    // no functionality will be affected by this except perf
                    Logger.Log(FunctionId.PersistenceService_WriteAsyncFailed, "SQLite Failed : " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                // ignore exception
                // there is not much we can do here. 
                // internally we use it as a way to cache information between sessions anyway. 
                // no functionality will be affected by this except perf
                Logger.Log(FunctionId.PersistenceService_WriteAsyncFailed, "Failed : " + ex.Message);
            }

            return default(TResult);
        }
    }
}
