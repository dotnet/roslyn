// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Locates the applicable set of .editorconfig files based on given inputs. Walks through
    /// parent directories locating the ones that exist.
    /// </summary>
    public sealed class DiscoverEditorConfigFiles : Task
    {
        /// <summary>
        /// The set of source and other files that we should find the applicable .editorconfig files to.
        /// </summary>
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        [Output]
        public ITaskItem[] EditorConfigFiles { get; private set; }

        static DiscoverEditorConfigFiles()
        {
            AssemblyResolution.Install();
        }

        public DiscoverEditorConfigFiles()
        {
            TaskResources = ErrorString.ResourceManager;
        }

        public override bool Execute()
        {
            try
            {
                ExecuteCore();
                return !Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {
                // We'll explicitly log friendly errors and then rethrow in the case of IO errors while reading the file system.
                // We'll only print the raw exception if we didn't already log something nice.
                if (!Log.HasLoggedErrors)
                {
                    Log.LogErrorFromException(ex);
                }

                return false;
            }
        }

        private void ExecuteCore()
        {
            var directoriesAddedToQueue = new HashSet<string>(StringComparer.Ordinal);
            var directoriesToVisit = new Queue<DirectoryInfo>();
            var editorConfigFiles = new List<ITaskItem>();

            void addNewDirectoryIfNotAlreadyAdded(DirectoryInfo directory)
            {
                // If we're being passed files that don't exist, we'll just skip them and let the compiler handle the actual reporting.
                if (directory.Exists)
                {
                    if (directoriesAddedToQueue.Add(directory.FullName))
                    {
                        directoriesToVisit.Enqueue(directory);
                    }
                }
            }

            foreach (var inputFile in InputFiles)
            {
                addNewDirectoryIfNotAlreadyAdded(new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(inputFile.ItemSpec))));
            }

            while (directoriesToVisit.Count > 0)
            {
                var directory = directoriesToVisit.Dequeue();
                var editorConfigFilePath = Path.Combine(directory.FullName, ".editorconfig");
                bool isRootEditorConfig = false;

                try
                {
                    if (File.Exists(editorConfigFilePath))
                    {
                        editorConfigFiles.Add(new TaskItem(editorConfigFilePath));

                        // We will attempt to determine if this is a root .editorconfig, so we can stop enumerating farther if necessary.
                        // We will do no further interpretation of the .editorconfig.

                        isRootEditorConfig = FileIsRootEditorConfig(editorConfigFilePath);
                    }
                }
                catch (IOException exception)
                {
                    // We'll log the error and continue; the "it failed" is handled in Execute().
                    Log.LogErrorFromResources(nameof(ErrorString.General_UnableToReadFile), editorConfigFilePath, exception.Message);
                }

                if (!isRootEditorConfig && directory.Parent != null)
                {
                    addNewDirectoryIfNotAlreadyAdded(directory.Parent);
                }
            }

            EditorConfigFiles = editorConfigFiles.ToArray();
        }

        // These Regexes are the regexes from Microsoft.VisualStudio.CodingConventions.EditorConfig.
        private static readonly Regex s_sectionMatcher = new Regex(@"^\s*\[(([^#;]|\\#|\\;)+)\]\s*([#;].*)?$", RegexOptions.Compiled);
        private static readonly Regex s_propertyMatcher = new Regex(@"^\s*([\w\.\-_]+)\s*[=:]\s*(.*?)\s*([#;].*)?$", RegexOptions.Compiled);

        internal static bool FileIsRootEditorConfig(string editorConfigFilePath)
        {
            using (FileStream fileStream = OpenFile(editorConfigFilePath))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Once we're in a section, we don't need to go looking for root = true anymore
                    if (s_sectionMatcher.IsMatch(line))
                    {
                        return false;
                    }

                    var propMatches = s_propertyMatcher.Matches(line);
                    if (propMatches.Count == 1 && propMatches[0].Groups.Count > 2)
                    {
                        var key = propMatches[0].Groups[1].Value;
                        var value = propMatches[0].Groups[2].Value;

                        if (key == "root")
                        {
                            return value == "true";
                        }
                    }
                }
            }

            return false;
        }

        private static FileStream OpenFile(string editorConfigFilePath)
        {
            try
            {
                return new FileStream(editorConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new IOException(e.Message, e);
            }
        }
    }
}
