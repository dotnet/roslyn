// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Isam.Esent;
using Microsoft.Isam.Esent.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Esent
{
    internal partial class EsentPersistentStorage : AbstractPersistentStorage
    {
        private const string StorageExtension = "vbcs.cache";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly ConcurrentDictionary<string, int> _nameTableCache;
        private readonly EsentStorage _esentStorage;

        public EsentPersistentStorage(
            IOptionService optionService, string workingFolderPath, string solutionFilePath, Action<AbstractPersistentStorage> disposer) :
            base(optionService, workingFolderPath, solutionFilePath, disposer)
        {
            // solution must exist in disk. otherwise, we shouldn't be here at all.
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(solutionFilePath));

            var databaseFile = GetDatabaseFile(workingFolderPath);

            this.EsentDirectory = Path.GetDirectoryName(databaseFile);

            if (!Directory.Exists(this.EsentDirectory))
            {
                Directory.CreateDirectory(this.EsentDirectory);
            }

            _nameTableCache = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var enablePerformanceMonitor = optionService.GetOption(InternalFeatureOnOffOptions.EsentPerformanceMonitor);
            _esentStorage = new EsentStorage(databaseFile, enablePerformanceMonitor);
        }

        public static string GetDatabaseFile(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));

            return Path.Combine(workingFolderPath, StorageExtension, PersistentStorageFileName);
        }

        public void Initialize()
        {
            _esentStorage.Initialize();
        }

        public string EsentDirectory { get; private set; }

        public override Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            int projectId;
            int documentId;
            int nameId;
            if (!TryGetProjectAndDocumentId(document, out projectId, out documentId) ||
                !TryGetUniqueNameId(name, out nameId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = EsentExceptionWrapper(projectId, documentId, nameId, ReadStream, cancellationToken);
            return Task.FromResult(stream);
        }

        private Stream ReadStream(int projectId, int documentId, int nameId, object unused1, object unused2, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetDocumentTableAccessor())
            using (var esentStream = accessor.GetReadStream(projectId, documentId, nameId))
            {
                if (esentStream == null)
                {
                    return null;
                }

                // this will copy over esent stream and let it go.
                return SerializableBytes.CreateReadableStream(esentStream, cancellationToken);
            }
        }

        public override Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            if (!IsSupported(project))
            {
                return SpecializedTasks.Default<Stream>();
            }

            int projectId;
            int nameId;
            if (!TryGetUniqueFileId(project.FilePath, out projectId) ||
                !TryGetUniqueNameId(name, out nameId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = EsentExceptionWrapper(projectId, nameId, ReadStream, cancellationToken);
            return Task.FromResult(stream);
        }

        private Stream ReadStream(int projectId, int nameId, object unused1, object unused2, object unused3, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetProjectTableAccessor())
            using (var esentStream = accessor.GetReadStream(projectId, nameId))
            {
                if (esentStream == null)
                {
                    return null;
                }

                // this will copy over esent stream and let it go.
                return SerializableBytes.CreateReadableStream(esentStream, cancellationToken);
            }
        }

        public override Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.Default<Stream>();
            }

            int nameId;
            if (!TryGetUniqueNameId(name, out nameId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            var stream = EsentExceptionWrapper(nameId, ReadStream, cancellationToken);
            return Task.FromResult(stream);
        }

        private Stream ReadStream(int nameId, object unused1, object unused2, object unused3, object unused4, CancellationToken cancellationToken)
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

            int projectId;
            int documentId;
            int nameId;
            if (!TryGetProjectAndDocumentId(document, out projectId, out documentId) ||
                !TryGetUniqueNameId(name, out nameId))
            {
                return SpecializedTasks.False;
            }

            var success = EsentExceptionWrapper(projectId, documentId, nameId, stream, WriteStream, cancellationToken);
            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        private bool WriteStream(int projectId, int documentId, int nameId, Stream stream, object unused1, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetDocumentTableAccessor())
            using (var esentStream = accessor.GetWriteStream(projectId, documentId, nameId))
            {
                WriteToStream(stream, esentStream, cancellationToken);
                return accessor.ApplyChanges();
            }
        }

        public override Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));
            Contract.ThrowIfNull(stream);

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.False;
            }

            if (!IsSupported(project))
            {
                return SpecializedTasks.False;
            }

            int projectId;
            int nameId;
            if (!TryGetUniqueFileId(project.FilePath, out projectId) ||
                !TryGetUniqueNameId(name, out nameId))
            {
                return SpecializedTasks.False;
            }

            var success = EsentExceptionWrapper(projectId, nameId, stream, WriteStream, cancellationToken);
            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        private bool WriteStream(int projectId, int nameId, Stream stream, object unused1, object unused2, CancellationToken cancellationToken)
        {
            using (var accessor = _esentStorage.GetProjectTableAccessor())
            using (var esentStream = accessor.GetWriteStream(projectId, nameId))
            {
                WriteToStream(stream, esentStream, cancellationToken);
                return accessor.ApplyChanges();
            }
        }

        public override Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));
            Contract.ThrowIfNull(stream);

            if (!PersistenceEnabled)
            {
                return SpecializedTasks.False;
            }

            int nameId;
            if (!TryGetUniqueNameId(name, out nameId))
            {
                return SpecializedTasks.False;
            }

            var success = EsentExceptionWrapper(nameId, stream, WriteStream, cancellationToken);

            return success ? SpecializedTasks.True : SpecializedTasks.False;
        }

        private bool WriteStream(int nameId, Stream stream, object unused1, object unused2, object unused3, CancellationToken cancellationToken)
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

        private bool IsSupported(Project project)
        {
            // TODO: figure out a proper way to support project K scenario where we can't use path as a unique key
            // https://github.com/dotnet/roslyn/issues/1860
            return !LinkedFileUtilities.IsProjectKProject(project);
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
                id = _nameTableCache.GetOrAdd(value, v =>
                {
                    // okay, get one from esent
                    var uniqueIdValue = fileCheck ? FilePathUtilities.GetRelativePath(Path.GetDirectoryName(SolutionFilePath), v) : v;
                    return _esentStorage.GetUniqueId(uniqueIdValue);
                });
            }
            catch (Exception ex)
            {
                // if we get fatal errors from esent such as disk out of space or log file corrupted by other process and etc
                // don't crash VS, but let VS know it can't use esent. we will gracefully recover issue by using memory.
                EsentLogger.LogException(ex);

                return false;
            }

            return true;
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

        private bool TryGetProjectAndDocumentId(Document document, out int projectId, out int documentId)
        {
            projectId = default(int);
            documentId = default(int);

            if (!IsSupported(document.Project))
            {
                return false;
            }

            return TryGetUniqueFileId(document.Project.FilePath, out projectId) && TryGetUniqueFileId(document.FilePath, out documentId);
        }

        private TResult EsentExceptionWrapper<TArg1, TResult>(TArg1 arg1, Func<TArg1, object, object, object, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TResult>(
            TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, object, object, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, arg2, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TArg3, TResult>(
            TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, object, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, arg2, arg3, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TArg3, TArg4, TResult>(
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, object, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            return EsentExceptionWrapper(arg1, arg2, arg3, arg4, (object)null, func, cancellationToken);
        }

        private TResult EsentExceptionWrapper<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(
            TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TArg1, TArg2, TArg3, TArg4, TArg5, CancellationToken, TResult> func, CancellationToken cancellationToken)
        {
            try
            {
                return func(arg1, arg2, arg3, arg4, arg5, cancellationToken);
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
