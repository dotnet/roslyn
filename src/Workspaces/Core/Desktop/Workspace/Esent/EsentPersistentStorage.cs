// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.Isam.Esent;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Esent
{
    internal partial class EsentPersistentStorage : AbstractPersistentStorage
    {
        // cache delegates so that we don't re-create it every times
        private readonly Func<int, object, object, object, CancellationToken, Stream> _readStreamSolution;
        private readonly Func<EsentStorage.Key, int, object, object, CancellationToken, Stream> _readStream;
        private readonly Func<int, Stream, object, object, CancellationToken, bool> _writeStreamSolution;
        private readonly Func<EsentStorage.Key, int, Stream, object, CancellationToken, bool> _writeStream;

        private readonly ConcurrentDictionary<string, int> _nameTableCache;
        private readonly EsentStorage _esentStorage;

        public EsentPersistentStorage(
            IOptionService optionService,
            string workingFolderPath,
            string solutionFilePath, 
            string databaseFile,
            Action<AbstractPersistentStorage> disposer) 
            : base(optionService, workingFolderPath, solutionFilePath, databaseFile, disposer)
        {
            // cache delegates
            _readStreamSolution = ReadStreamSolution;
            _readStream = ReadStream;
            _writeStreamSolution = WriteStreamSolution;
            _writeStream = WriteStream;

            // solution must exist in disk. otherwise, we shouldn't be here at all.
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(solutionFilePath));

            _nameTableCache = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var enablePerformanceMonitor = optionService.GetOption(PersistentStorageOptions.EsentPerformanceMonitor);
            _esentStorage = new EsentStorage(DatabaseFile, enablePerformanceMonitor);
        }

        public override void Initialize(Solution solution)
        {
            _esentStorage.Initialize();
        }

        public override Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!TryGetProjectAndDocumentKey(document, out var key) ||
                !TryGetUniqueNameId(name, out var nameId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = EsentExceptionWrapper(key, nameId, _readStream, cancellationToken);
            return SpecializedTasks.DefaultOrResult(stream);
        }

        public override Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!TryGetProjectKey(project, out var key) ||
                !TryGetUniqueNameId(name, out var nameId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = EsentExceptionWrapper(key, nameId, _readStream, cancellationToken);
            return SpecializedTasks.DefaultOrResult(stream);
        }

        public override Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!TryGetUniqueNameId(name, out var nameId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = EsentExceptionWrapper(nameId, _readStreamSolution, cancellationToken);
            return SpecializedTasks.DefaultOrResult(stream);
        }

        private Stream ReadStream(EsentStorage.Key key, int nameId, object unused1, object unused2, CancellationToken cancellationToken)
        {
            using (var accessor = GetAccessor(key))
            using (var esentStream = accessor.GetReadStream(key, nameId))
            {
                if (esentStream == null)
                {
                    return null;
                }

                // this will copy over esent stream and let it go.
                return SerializableBytes.CreateReadableStream(esentStream, cancellationToken);
            }
        }

        private Stream ReadStreamSolution(int nameId, object unused1, object unused2, object unused3, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetSolutionTableAccessor())
            using (var esentStream = accessor.GetReadStream(nameId))
            {
                if (esentStream == null)
                {
                    return null;
                }

                // this will copy over esent stream and let it go.
                return SerializableBytes.CreateReadableStream(esentStream, cancellationToken);
            }
        }

        public override Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));
            Contract.ThrowIfNull(stream);

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.False;
            }

            if (!TryGetProjectAndDocumentKey(document, out var key) ||
                !TryGetUniqueNameId(name, out var nameId))
            {
                return SpecializedTasks.False;
            }

            var success = EsentExceptionWrapper(key, nameId, stream, _writeStream, cancellationToken);
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

            if (!TryGetProjectKey(project, out var key) ||
                !TryGetUniqueNameId(name, out var nameId))
            {
                return SpecializedTasks.False;
            }

            var success = EsentExceptionWrapper(key, nameId, stream, _writeStream, cancellationToken);
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

            if (!TryGetUniqueNameId(name, out var nameId))
            {
                return SpecializedTasks.False;
            }

            var success = EsentExceptionWrapper(nameId, stream, _writeStreamSolution, cancellationToken);

            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        private bool WriteStream(EsentStorage.Key key, int nameId, Stream stream, object unused1, CancellationToken cancellationToken)
        {
            using (var accessor = GetAccessor(key))
            using (var esentStream = accessor.GetWriteStream(key, nameId))
            {
                WriteToStream(stream, esentStream, cancellationToken);
                return accessor.ApplyChanges();
            }
        }

        private bool WriteStreamSolution(int nameId, Stream stream, object unused1, object unused2, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetSolutionTableAccessor())
            using (var esentStream = accessor.GetWriteStream(nameId))
            {
                WriteToStream(stream, esentStream, cancellationToken);
                return accessor.ApplyChanges();
            }
        }

        public override void Close()
        {
            _esentStorage.Close();
        }

        private bool TryGetUniqueNameId(string name, out int id)
        {
            return TryGetUniqueId(name, false, out id);
        }

        private bool TryGetUniqueFileId(string path, out int id)
        {
            return TryGetUniqueId(path, true, out id);
        }

        private bool TryGetUniqueId(string value, bool fileCheck, out int id)
        {
            id = default(int);

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // if we already know, get the id
            if (_nameTableCache.TryGetValue(value, out id))
            {
                return true;
            }

            // we only persist for things that actually exist
            if (fileCheck && !File.Exists(value))
            {
                return false;
            }

            try
            {
                var uniqueIdValue = fileCheck ? PathUtilities.GetRelativePath(Path.GetDirectoryName(SolutionFilePath), value) : value;
                id = _nameTableCache.GetOrAdd(value, _esentStorage.GetUniqueId(uniqueIdValue));

                return true;
            }
            catch (Exception ex)
            {
                // if we get fatal errors from esent such as disk out of space or log file corrupted by other process and etc
                // don't crash VS, but let VS know it can't use esent. we will gracefully recover issue by using memory.
                StorageDatabaseLogger.LogException(ex);

                return false;
            }
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

        private EsentStorage.ProjectDocumentTableAccessor GetAccessor(EsentStorage.Key key)
        {
            return key.DocumentIdOpt.HasValue ?
                _esentStorage.GetDocumentTableAccessor() :
                (EsentStorage.ProjectDocumentTableAccessor)_esentStorage.GetProjectTableAccessor();
        }

        private bool TryGetProjectAndDocumentKey(Document document, out EsentStorage.Key key)
        {
            key = default(EsentStorage.Key);
            if (!TryGetProjectId(document.Project, out var projectId, out var projectNameId) ||
                !TryGetUniqueFileId(document.FilePath, out var documentId))
            {
                return false;
            }

            key = new EsentStorage.Key(projectId, projectNameId, documentId);
            return true;
        }

        private bool TryGetProjectKey(Project project, out EsentStorage.Key key)
        {
            key = default(EsentStorage.Key);
            if (!TryGetProjectId(project, out var projectId, out var projectNameId))
            {
                return false;
            }

            key = new EsentStorage.Key(projectId, projectNameId);
            return true;
        }

        private bool TryGetProjectId(Project project, out int projectId, out int projectNameId)
        {
            projectId = default(int);
            projectNameId = default(int);

            return TryGetUniqueFileId(project.FilePath, out projectId) && TryGetUniqueNameId(project.Name, out projectNameId);
        }

        private TResult EsentExceptionWrapper<TArg1, TResult>(TArg1 arg1, Func<TArg1, object, object, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TResult>(
            TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, object, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, arg2, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TArg3, TResult>(
            TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, arg2, arg3, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TArg3, TArg4, TResult>(
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, cancellationToken);
            }
            catch (EsentInvalidSesidException)
            {
                // operation was in-fly when Esent instance was shutdown - ignore the error
            }
            catch (OperationCanceledException)
            {
            }
            catch (EsentException ex)
            {
                if (!_esentStorage.IsClosed)
                {
                    // ignore esent exception if underlying storage was closed
                    // there is not much we can do here. 
                    // internally we use it as a way to cache information between sessions anyway. 
                    // no functionality will be affected by this except perf
                    Logger.Log(FunctionId.PersistenceService_WriteAsyncFailed, "Esent Failed : " + ex.Message);
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
