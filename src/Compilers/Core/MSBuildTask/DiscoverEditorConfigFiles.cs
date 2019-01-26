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

        /// <summary>
        /// The set of applicable .editorconfig files.
        /// </summary>
        [Output]
        public ITaskItem[] EditorConfigFiles { get; private set; }

        /// <summary>
        /// Paths we considered that did *not* have a existing .editorconfig file.
        /// </summary>
        [Output]
        public ITaskItem[] PotentialEditorConfigFiles { get; private set; }

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
            var potentialEditorConfigFiles = new List<ITaskItem>();

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
                    else
                    {
                        potentialEditorConfigFiles.Add(new TaskItem(editorConfigFilePath));
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
            PotentialEditorConfigFiles = potentialEditorConfigFiles.ToArray();
        }

        internal static bool FileIsRootEditorConfig(string editorConfigFilePath)
        {
            using (FileStream fileStream = OpenFile(editorConfigFilePath))
            using (var reader = new StreamReader(fileStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int charIndex = skipSpaces(line, 0);
                    if (charIndex < line.Length)
                    {
                        switch (line[charIndex])
                        {
                            case '[':
                                // Once we're in a section, we don't need to go looking for root = true anymore
                                return false;

                            case 'r':
                            case 'R':
                                // Check for case insensitive 'root = true'
                                const string root = "root";
                                const string trueLiteral = "true";
                                if (matchesCaseInsensitiveAscii(line, charIndex, root))
                                {
                                    charIndex += root.Length;
                                    charIndex = skipSpaces(line, charIndex);
                                    if (charIndex < line.Length)
                                    {
                                        switch (line[charIndex])
                                        {
                                            case '=':
                                            case ':':
                                                charIndex = skipSpaces(line, charIndex + 1);
                                                if (matchesCaseInsensitiveAscii(line, charIndex, trueLiteral))
                                                {
                                                    charIndex += trueLiteral.Length;
                                                    charIndex = skipSpaces(line, charIndex);
                                                    // Is the rest of the line empty or a comment?
                                                    if (charIndex == line.Length ||
                                                        line[charIndex] == '#' ||
                                                        line[charIndex] == ';')
                                                    {
                                                        return true;
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }

            return false;

            int skipSpaces(string str, int startIndex)
            {
                for (int cur = startIndex; cur < str.Length; cur++)
                {
                    if (!char.IsWhiteSpace(str[cur]))
                    {
                        return cur;
                    }
                }

                return str.Length;
            }

            bool matchesCaseInsensitiveAscii(string str, int strIndex, string targetLower)
            {
                if (strIndex + targetLower.Length > str.Length)
                {
                    return false;
                }

                for (int targetIndex = 0; targetIndex < targetLower.Length; targetIndex++)
                {
                    char curChar = str[strIndex + targetIndex];
                    char targetChar = targetLower[targetIndex];
                    const int asciiDiff = 'a' - 'A';
                    if (curChar != targetChar && curChar != (targetChar - asciiDiff))
                    {
                        return false;
                    }
                }

                return true;
            }
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
