using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class PersistentStorageService
    {
        internal class FileStorage : IPersistentStorage
        {
            private const string StorageExtension = ".ide";
            private readonly IOptionService optionService;
            private readonly Action<FileStorage> disposer;

            private int refCounter;

            public FileStorage(Solution solution, IOptionService optionService, Action<FileStorage> disposer)
            {
                Contract.ThrowIfNull(disposer);

                this.optionService = optionService;
                this.disposer = disposer;
                Solution = solution;
            }

            public Solution Solution { get; private set; }

            public void Dispose()
            {
                disposer(this);
            }

            public void AddRefUnsafe()
            {
                refCounter++;
            }

            public bool ReleaseRefUnsafe()
            {
                refCounter--;
                return refCounter == 0;
            }

            public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken)
            {
                if (CanStore(Solution))
                {
                    return ReadAsync(GetStorageFileName(Solution, name), cancellationToken);
                }
                else
                {
                    return SpecializedTasks.Default<Stream>();
                }
            }

            public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                if (CanStore(project))
                {
                    return ReadAsync(GetStorageFileName(project, name), cancellationToken);
                }
                else
                {
                    return SpecializedTasks.Default<Stream>();
                }
            }

            public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                if (CanStore(document))
                {
                    return ReadAsync(GetStorageFileName(document, name), cancellationToken);
                }
                else
                {
                    return SpecializedTasks.Default<Stream>();
                }
            }

            private async Task<Stream> ReadAsync(string filePath, CancellationToken cancellationToken)
            {
                try
                {
                    if (PersistenceEnabled && File.Exists(filePath))
                    {
                        using (Logger.LogBlock(FeatureId.Persistence, FunctionId.PersistenceService_ReadAsync, filePath, cancellationToken))
                        using (var stream = File.OpenRead(filePath))
                        {
                            return await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore exceptions. 
                    // there is not much we can do here. and caller should be expected to handle null.
                    Logger.Log(FeatureId.Persistence, FunctionId.PersistenceService_ReadAsyncFailed, "ReadAsync Failed : " + ex.Message);
                }

                return null;
            }

            public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
            {
                if (CanStore(Solution))
                {
                    return WriteAsync(GetStorageFileName(Solution, name), stream, cancellationToken);
                }
                else
                {
                    return SpecializedTasks.True;
                }
            }

            public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                if (CanStore(project))
                {
                    return WriteAsync(GetStorageFileName(project, name), stream, cancellationToken);
                }
                else
                {
                    return SpecializedTasks.True;
                }
            }

            public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(name));

                if (CanStore(document))
                {
                    return WriteAsync(GetStorageFileName(document, name), stream, cancellationToken);
                }
                else
                {
                    return SpecializedTasks.True;
                }
            }

            private async Task<bool> WriteAsync(string filePath, Stream stream, CancellationToken cancellationToken)
            {
                try
                {
                    if (PersistenceEnabled)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        using (Logger.LogBlock(FeatureId.Persistence, FunctionId.PersistenceService_WriteAsync, filePath, CancellationToken.None))
                        using (var writeStream = File.Create(filePath))
                        {
                            var buffer = ByteArrayPool.Instance.Allocate();
                            try
                            {
                                int bytesRead;
                                do
                                {
                                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                                    if (bytesRead > 0)
                                    {
                                        await writeStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                    }
                                }
                                while (bytesRead > 0);
                            }
                            finally
                            {
                                ByteArrayPool.Instance.Free(buffer);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore exception
                    // there is not much we can do here. 
                    // internally we use it as a way to cache information between sessions anyway. 
                    // no functionality will be affected by this except perf
                    Logger.Log(FeatureId.Persistence, FunctionId.PersistenceService_WriteAsyncFailed, "WriteAsync Failed : " + ex.Message);
                }

                // assume that write always succeeded
                return true;
            }

            private bool CanStore(Solution solution)
            {
                // we can store items relative to the solution file
                if (string.IsNullOrEmpty(solution.FilePath))
                {
                    return false;
                }

                // if the solution doesn't really exist on disk at this location then don't persist anything here.
                if (!File.Exists(solution.FilePath))
                {
                    return false;
                }

                return true;
            }

            private bool CanStore(Project project)
            {
                return CanStore(project.Solution) && !string.IsNullOrEmpty(project.FilePath) && !string.IsNullOrEmpty(project.OutputFilePath);
            }

            private bool CanStore(Document document)
            {
                return CanStore(document.Project) && !string.IsNullOrEmpty(document.FilePath);
            }

            private string GetStorageFileName(Solution solution, string name)
            {
                // put solution related storage just below the storage root
                return Path.Combine(GetSolutionStorageRoot(solution), name);
            }

            private string GetStorageFileName(Project project, string name)
            {
                // place project related items in a subdirectory below the storage root (matching the
                // project's directory relative to the solution)
                // var relativeProjectPath = FilePathUtilities.GetRelativePath(Path.GetDirectoryName(project.Solution.FilePath), project.FilePath);

                var root = this.GetProjectStorageRoot(project);

                return Path.Combine(root, name);
            }

            private string GetProjectStorageRoot(Project project)
            {
                return Path.Combine(Path.GetDirectoryName(project.OutputFilePath), Path.GetFileName(project.OutputFilePath) + StorageExtension);
            }

            private string GetStorageFileName(Document document, string name)
            {
                // place document related items in a subdirectory below the storage root (matching the
                // file's directory relative to the solution)
                var relativeDocumentPath = FilePathUtilities.GetRelativePath(Path.GetDirectoryName(document.Project.FilePath), document.FilePath);
                return Path.Combine(GetProjectStorageRoot(document.Project), name, relativeDocumentPath + StorageExtension);
            }

            private string GetSolutionStorageRoot(Solution solution)
            {
                // place the storage directory in the solution folder with the same name as the solution but with the .ide extension added.
                return Path.Combine(Path.GetDirectoryName(solution.FilePath), Path.GetFileName(solution.FilePath) + StorageExtension);
            }

            private bool PersistenceEnabled
            {
                get { return optionService.GetOption(PersistentStorageOptions.Enabled); }
            }
        }
    }
}
