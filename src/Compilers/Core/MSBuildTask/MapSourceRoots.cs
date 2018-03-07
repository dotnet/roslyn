// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Given a list of SourceRoot items produces a list of the same items with added <c>MappedPath</c> metadata that
    /// contains calculated deterministic source path for each SourceRoot.
    /// </summary>
    /// <remarks>
    /// Does not perform any path validation.
    /// </remarks>
    public sealed class MapSourceRoots : Task
    {
        public MapSourceRoots()
        {
            TaskResources = ErrorString.ResourceManager;
        }

        /// <summary>
        /// SourceRoot items with the following optional well-known metadata:
        /// <list type="bullet">
        ///   <term>SourceControl</term><description>Indicates name of the source control system the source root is tracked by (e.g. Git, TFVC, etc.), if any.</description>
        ///   <term>NestedRoot</term><description>If a value is specified the source root is nested (e.g. git submodule). The value is a path to this root relative to the containing root.</description>
        ///   <term>ContainingRoot</term><description>Identifies another source root item that this source root is nested under.</description>
        /// </list>
        /// </summary>
        [Required]
        public ITaskItem[] SourceRoots { get; set; }

        /// <summary>
        /// True if the mapped paths should be deterministic.
        /// </summary>
        public bool Deterministic { get; set; }

        [Output]
        public ITaskItem[] MappedSourceRoots { get; private set; }

        private static class Names
        {
            public const string SourceRoot = nameof(SourceRoot);

            // Names of well-known SourceRoot metadata items:
            public const string SourceControl = nameof(SourceControl);
            public const string NestedRoot = nameof(NestedRoot);
            public const string ContainingRoot = nameof(ContainingRoot);
            public const string MappedPath = nameof(MappedPath);
        }

        public override bool Execute()
        {
            var topLevelMappedPaths = new Dictionary<string, string>();
            int i = 0;

            void SetTopLevelMappedPaths(bool sourceControl)
            {
                foreach (var root in SourceRoots)
                {
                    if (!string.IsNullOrEmpty(root.GetMetadata(Names.SourceControl)) == sourceControl)
                    {
                        string nestedRoot = root.GetMetadata(Names.NestedRoot);
                        if (string.IsNullOrEmpty(nestedRoot))
                        {
                            if (topLevelMappedPaths.ContainsKey(root.ItemSpec))
                            {
                                Log.LogErrorFromResources("MapSourceRoots.ContainsDuplicate", Names.SourceRoot, root.ItemSpec);
                            }
                            else
                            {
                                var mappedPath = "/_" + (i == 0 ? "" : i.ToString()) + "/";
                                topLevelMappedPaths.Add(root.ItemSpec, mappedPath);
                                root.SetMetadata(Names.MappedPath, mappedPath);
                                i++;
                            }
                        }
                    }
                }
            }
            
            string EndWithSlash(string path)
                => (path[path.Length - 1] == '/') ? path : path + '/';

            bool EndsWithDirectorySeparator(string path)
            {
                if (path.Length == 0)
                {
                    return false;
                }

                char c = path[path.Length - 1];
                return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
            }

            // The SourceRoot is required to have a trailing directory separator.
            // We do not append one implicitly as we do not know which separator to append on Windows.
            // The usage of SourceRoot might be sensitive to what kind of separator is used (e.g. in SourceLink where it needs
            // to match the corresponding separators used in paths given to the compiler).
            foreach (var sourceRoot in SourceRoots)
            {
                if (!EndsWithDirectorySeparator(sourceRoot.ItemSpec))
                {
                    Log.LogErrorFromResources("MapSourceRoots.PathMustEndWithSlashOrBackslash", Names.SourceRoot, sourceRoot.ItemSpec);
                }
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            // TODO: deduplication

            if (Deterministic)
            {
                // assign mapped paths to process source control roots first:
                SetTopLevelMappedPaths(sourceControl: true);

                // then assign mapped paths to other source control roots:
                SetTopLevelMappedPaths(sourceControl: false);

                // finally, calculate mapped paths of nested roots:
                foreach (var root in SourceRoots)
                {
                    string nestedRoot = root.GetMetadata(Names.NestedRoot);
                    if (!string.IsNullOrEmpty(nestedRoot))
                    {
                        string containingRoot = root.GetMetadata(Names.ContainingRoot);

                        // The value of ContainingRoot metadata is a file path that is compared with ItemSpec values of SourceRoot items.
                        // Since the paths in ItemSpec have backslashes replaced with slashes on non-Windows platforms we need to do the same for ContainingRoot.
                        if (containingRoot != null && topLevelMappedPaths.TryGetValue(Utilities.FixFilePath(containingRoot), out var mappedTopLevelPath))
                        {
                            Debug.Assert(mappedTopLevelPath.EndsWith("/", StringComparison.Ordinal));
                            root.SetMetadata(Names.MappedPath, mappedTopLevelPath + EndWithSlash(nestedRoot.Replace('\\', '/')));
                        }
                        else
                        {
                            Log.LogErrorFromResources("MapSourceRoots.ValueOfNotFoundInItems", Names.SourceRoot + "." + Names.ContainingRoot, Names.SourceRoot, containingRoot);
                        }
                    }
                }
            }
            else
            {
                foreach (var root in SourceRoots)
                {
                    root.SetMetadata(Names.MappedPath, root.ItemSpec);
                }
            }

            if (!Log.HasLoggedErrors)
            {
                MappedSourceRoots = SourceRoots;
            }

            return !Log.HasLoggedErrors;
        }
    }
}
