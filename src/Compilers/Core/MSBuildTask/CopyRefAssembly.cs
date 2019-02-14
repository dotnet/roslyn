// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public sealed class CopyRefAssembly : Task
    {
        [Required]
        public string SourcePath { get; set; }

        [Output]
        [Required]
        public string DestinationPath { get; set; }

        public CopyRefAssembly()
        {
            TaskResources = ErrorString.ResourceManager;
        }

        public override bool Execute()
        {
            if (!File.Exists(SourcePath))
            {
                Log.LogErrorWithCodeFromResources("General_ExpectedFileMissing", SourcePath);
                return false;
            }

            if (File.Exists(DestinationPath))
            {
                var source = Guid.Empty;
                try
                {
                    source = ExtractMvid(SourcePath);
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
                        Guid destination = ExtractMvid(DestinationPath);

                        if (!source.Equals(Guid.Empty) && source.Equals(destination))
                        {
                            Log.LogMessageFromResources(MessageImportance.Low, "CopyRefAssembly_SkippingCopy1", DestinationPath);
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        Log.LogMessageFromResources(MessageImportance.High, "CopyRefAssembly_BadDestination1", DestinationPath);
                    }
                }
            }

            return Copy();
        }

        private bool Copy()
        {
            try
            {
                File.Copy(SourcePath, DestinationPath, overwrite: true);
            }
            catch (Exception e)
            {
                Log.LogErrorWithCodeFromResources("Compiler_UnexpectedException");
                ManagedCompiler.LogErrorOutput(e.ToString(), Log);
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
