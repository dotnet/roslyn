using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Esent;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class PersistentStorageService
    {
        private class PersistentStorage : IPersistentStorage
        {
            private const string StorageExtension = ".ide";
            private const string PersistentStorageFileName = "storage.ide";
            private const string SolutionTableName = "Solution";

            private Action<PersistentStorage> disposer;
            private int refCounter;

            private readonly EsentKeyValueStorage esentKeyValueStorage;
            private Tuple<string, string, string> doNotAccessDirectlyLastTableName;
            private Tuple<string, string, string> doNotAccessDirectlyLastKey;
            private readonly IOptionService optionService;

            public PersistentStorage(IOptionService optionService, PersistentStorageService container, Solution solution, Action<PersistentStorage> disposer)
            {
                Contract.ThrowIfNull(disposer);

                // solution must exist in disk. otherwise, we shouldn't be here at all.
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(solution.FilePath));

                // we only support persistency on primary solution not on any forked one
                Contract.ThrowIfFalse(solution.Workspace.PrimaryBranchId == solution.BranchId);

                this.optionService = optionService;
                this.disposer = disposer;

                var databaseFile = GetDatabaseFile(solution);
                Solution = solution;
                var directory = Path.GetDirectoryName(databaseFile);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                this.esentKeyValueStorage = new EsentKeyValueStorage(databaseFile);
            }

            public Solution Solution { get; private set; }

            public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                Contract.ThrowIfFalse(Solution.Workspace.PrimaryBranchId == document.Project.Solution.BranchId);
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                var projectKey = GetTableNameForProject(document.Project, name);
                var documentKey = GetKeyForDocument(document);
                if (projectKey == null || documentKey == null)
                {
                    return SpecializedTasks.Default<Stream>();
                }

                if (PersistenceEnabled)
                {
                    var stream = ReadStreamCore(projectKey, documentKey, cancellationToken);
                    return Task.FromResult(stream);
                }
                else
                {
                    return SpecializedTasks.Default<Stream>();
                }
            }

            public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                Contract.ThrowIfFalse(Solution.Workspace.PrimaryBranchId == project.Solution.BranchId);
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                var projectKey = GetTableNameForProject(project, name);
                if (projectKey == null)
                {
                    return SpecializedTasks.Default<Stream>();
                }

                if (PersistenceEnabled)
                {
                    // TODO: verify that project is in solution associated with the storage
                    var stream = ReadStreamCore(projectKey, name, cancellationToken);
                    return Task.FromResult(stream);
                }
                else
                {
                    return SpecializedTasks.Default<Stream>();
                }
            }

            public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                if (PersistenceEnabled)
                {
                    var stream = ReadStreamCore(
                        SolutionTableName,
                        name,
                        cancellationToken);

                    return Task.FromResult(stream);
                }
                else
                {
                    return SpecializedTasks.Default<Stream>();
                }
            }

            public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
            {
                Contract.ThrowIfFalse(Solution.Workspace.PrimaryBranchId == document.Project.Solution.BranchId);
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                var projectKey = GetTableNameForProject(document.Project, name);
                var documentKey = GetKeyForDocument(document);
                if (projectKey == null || documentKey == null)
                {
                    return SpecializedTasks.False;
                }

                if (PersistenceEnabled)
                {
                    var success = WriteStreamCore(projectKey, documentKey, stream, cancellationToken);
                    return success ? SpecializedTasks.True : SpecializedTasks.False;
                }

                return SpecializedTasks.False;
            }

            public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
            {
                Contract.ThrowIfFalse(Solution.Workspace.PrimaryBranchId == project.Solution.BranchId);
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                var projectKey = GetTableNameForProject(project, name);
                if (projectKey == null)
                {
                    return SpecializedTasks.False;
                }

                if (PersistenceEnabled)
                {
                    var success = WriteStreamCore(projectKey, name, stream, cancellationToken);
                    return success ? SpecializedTasks.True : SpecializedTasks.False;
                }

                return SpecializedTasks.False;
            }

            public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                if (PersistenceEnabled)
                {
                    var success = WriteStreamCore(SolutionTableName, name, stream, cancellationToken);
                    return success ? SpecializedTasks.True : SpecializedTasks.False;
                }

                return SpecializedTasks.False;
            }

            internal void AddRefUnsafe()
            {
                refCounter++;
            }

            internal bool ReleaseRefUnsafe()
            {
                refCounter--;
                return refCounter == 0;
            }

            private string GetDatabaseFile(Solution solution)
            {
                return Path.Combine(
                    Path.GetDirectoryName(solution.FilePath),
                    Path.GetFileName(solution.FilePath) + StorageExtension,
                    PersistentStorageFileName);
            }

            private string GetKeyForDocument(Document document)
            {
                var lastKey = doNotAccessDirectlyLastKey;
                if (lastKey != null && lastKey.Item1 == document.FilePath && lastKey.Item2 == document.Project.FilePath)
                {
                    return lastKey.Item3;
                }

                if (document.Project.FilePath == null || document.FilePath == null)
                {
                    return null;
                }

                var key = FilePathUtilities.GetRelativePath(Path.GetDirectoryName(document.Project.FilePath), document.FilePath);
                doNotAccessDirectlyLastKey = Tuple.Create(document.FilePath, document.Project.FilePath, key);

                return key;
            }

            private string GetTableNameForProject(Project project, string prefix = null)
            {
                var lastTableName = doNotAccessDirectlyLastTableName;
                if (lastTableName != null && lastTableName.Item1 == project.FilePath && lastTableName.Item2 == project.Solution.FilePath)
                {
                    return lastTableName.Item3;
                }

                if (project.Solution.FilePath == null || project.FilePath == null)
                {
                    return null;
                }

                var key = FilePathUtilities.GetRelativePath(Path.GetDirectoryName(project.Solution.FilePath), project.FilePath);
                var tableName = string.IsNullOrEmpty(prefix) ? key : prefix + "_" + key;

                doNotAccessDirectlyLastTableName = Tuple.Create(project.FilePath, project.Solution.FilePath, tableName);

                return tableName;
            }

            private bool WriteStreamCore(string tableName, string key, Stream stream, CancellationToken cancellationToken)
            {
                Contract.Requires(!string.IsNullOrEmpty(tableName));
                Contract.Requires(!string.IsNullOrEmpty(key));
                Contract.Requires(stream != null);

                var success = true;
                try
                {
                    using (var table = this.esentKeyValueStorage.GetTableAccessor(tableName))
                    using (var writeStream = table.GetWriteStream(key))
                    {
                        var buffer = ByteArrayPool.Instance.Allocate();
                        try
                        {
                            int bytesRead;
                            do
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                bytesRead = stream.Read(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    writeStream.Write(buffer, 0, bytesRead);
                                }
                            }
                            while (bytesRead > 0);

                            // flush the data and trim column size of necessary
                            writeStream.Flush();

                            success = table.ApplyChanges();
                        }
                        finally
                        {
                            ByteArrayPool.Instance.Free(buffer);
                        }
                    }
                }
                catch (EsentInstanceShutdownException)
                {
                    // operation was in-fly when Esent instance was shutdown - ignore the error
                }
                catch (OperationCanceledException)
                {
                }
                catch (EsentException ex)
                {
                    if (!esentKeyValueStorage.IsClosed)
                    {
                        // ignore esent exception if underlying storage was closed
                        // there is not much we can do here. 
                        // internally we use it as a way to cache information between sessions anyway. 
                        // no functionality will be affected by this except perf
                        Logger.Log(FeatureId.Persistence, FunctionId.PersistenceService_WriteAsyncFailed, "WriteStreamCore Failed : " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    // ignore exception
                    // there is not much we can do here. 
                    // internally we use it as a way to cache information between sessions anyway. 
                    // no functionality will be affected by this except perf
                    Logger.Log(FeatureId.Persistence, FunctionId.PersistenceService_WriteAsyncFailed, "WriteStreamCore Failed : " + ex.Message);
                }

                return success;
            }

            private Stream ReadStreamCore(string tableName, string key, CancellationToken cancellationToken)
            {
                Contract.Requires(!string.IsNullOrEmpty(tableName));
                Contract.Requires(!string.IsNullOrEmpty(key));

                try
                {
                    using (var table = this.esentKeyValueStorage.GetTableAccessor(tableName))
                    using (var stream = table.GetReadStream(key))
                    {
                        if (stream == null)
                        {
                            return null;
                        }
                        else
                        {
                            return SerializableBytes.CreateReadableStream(stream, cancellationToken);
                        }
                    }
                }
                catch (EsentInstanceShutdownException)
                {
                    // operation was in-fly when Esent instance was shutdown - ignore the error
                    return null;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (EsentException ex)
                {
                    if (!esentKeyValueStorage.IsClosed)
                    {
                        // ignore esent exception if underlying storage was closed
                        // there is not much we can do here. 
                        // internally we use it as a way to cache information between sessions anyway. 
                        // no functionality will be affected by this except perf
                        Logger.Log(FeatureId.Persistence, FunctionId.PersistenceService_WriteAsyncFailed, "WriteStreamCore Failed : " + ex.Message);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    // ignore exception
                    // there is not much we can do here. 
                    // internally we use it as a way to cache information between sessions anyway. 
                    // no functionality will be affected by this except perf
                    Logger.Log(FeatureId.Persistence, FunctionId.PersistenceService_ReadAsyncFailed, "ReadStreamCore Failed : " + ex.Message);

                    return null;
                }
            }

            public void Dispose()
            {
                disposer(this);
            }

            internal void Close()
            {
                esentKeyValueStorage.Close();
            }

            public virtual bool PersistenceEnabled
            {
                get { return optionService.GetOption(PersistentStorageOptions.Enabled); }
            }
        }
    }
}
