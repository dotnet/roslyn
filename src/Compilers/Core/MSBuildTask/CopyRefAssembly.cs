// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// By default, this task copies the source over to the destination. 
    /// But if we're able to check that they are identical, the destination is left untouched.
    /// </summary>
    [MSBuildMultiThreadableTask]
    public sealed class CopyRefAssembly : Task, IMultiThreadableTask
    {
        /// <inheritdoc />
        public TaskEnvironment TaskEnvironment { get; set; } = TaskEnvironment.Fallback;

        [Required]
        public string SourcePath { get; set; }

        [Output]
        [Required]
        public string DestinationPath { get; set; }

        public CopyRefAssembly()
            : base(ErrorString.ResourceManager)
        {
            // These required properties will all be assigned by MSBuild. Suppress warnings about leaving them with
            // their default values.
            SourcePath = null!;
            DestinationPath = null!;
        }

        public override bool Execute()
        {
            var absoluteSourcePath = string.IsNullOrEmpty(SourcePath) ? SourcePath : TaskEnvironment.GetAbsolutePath(SourcePath);
            var absoluteDestinationPath = string.IsNullOrEmpty(DestinationPath) ? DestinationPath : TaskEnvironment.GetAbsolutePath(DestinationPath);

            if (!File.Exists(absoluteSourcePath))
            {
                Log.LogErrorWithCodeFromResources("General_ExpectedFileMissing", SourcePath);
                return false;
            }

            if (File.Exists(absoluteDestinationPath))
            {
                var source = Guid.Empty;
                try
                {
                    source = ExtractMvid(absoluteSourcePath);
                }
                catch (Exception e)
                {
                    Log.LogMessageFromResources(MessageImportance.High, "CopyRefAssembly_BadSource3", SourcePath, e.Message, e.StackTrace);
                }

                if (source.Equals(Guid.Empty))
                {
                    Log.LogMessageFromResources(MessageImportance.High, "CopyRefAssembly_SourceNotRef1", SourcePath);
                }
                else
                {
                    try
                    {
                        Guid destination = ExtractMvid(absoluteDestinationPath);

                        if (!source.Equals(Guid.Empty) && source.Equals(destination))
                        {
                            Log.LogMessageFromResources(MessageImportance.Low, "CopyRefAssembly_SkippingCopy1", DestinationPath);
                            return true;
                        }

                        Log.LogMessageFromResources(MessageImportance.Low, "CopyRefAssembly_Changed", SourcePath, File.GetLastWriteTimeUtc(absoluteSourcePath).ToString("O"), source, DestinationPath, File.GetLastWriteTimeUtc(absoluteDestinationPath).ToString("O"), destination);
                    }
                    catch (Exception)
                    {
                        Log.LogMessageFromResources(MessageImportance.High, "CopyRefAssembly_BadDestination1", DestinationPath);
                    }
                }
            }

            return Copy(absoluteSourcePath, absoluteDestinationPath);
        }

        private bool Copy(string absoluteSourcePath, string absoluteDestinationPath)
        {
            try
            {
                Log.LogMessageFromResources(MessageImportance.Normal, "CopyRefAssembly_Copying", SourcePath, DestinationPath);
                File.Copy(absoluteSourcePath, absoluteDestinationPath, overwrite: true);
            }
            catch (Exception e)
            {
                Log.LogErrorWithCodeFromResources("Compiler_UnexpectedException");
                Log.LogErrorFromException(e, showStackTrace: true, showDetail: true, file: null);
                return false;
            }

            return true;
        }

        private Guid ExtractMvid(string path)
        {
            using (FileStream source = File.OpenRead(path))
            {
                return MvidReader.ReadAssemblyMvidOrEmpty(source);
            }
        }
    }
}
