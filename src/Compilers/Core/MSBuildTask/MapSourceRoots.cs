// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Given a list of SourceRoot items produces a list of the same items with added <c>MappedPath</c> metadata that
    /// contains calculated deterministic source path for each SourceRoot.
    /// </summary>
    /// <remarks>
    /// Does not perform any path validation.
    /// 
    /// The <c>MappedPath</c> is either the path (ItemSpec) itself, when <see cref="Deterministic"/> is false, 
    /// or a calculated deterministic source path (starting with prefix '/_/', '/_1/', etc.), otherwise.
    /// </remarks>
    public sealed class MapSourceRoots : Task
    {
        public MapSourceRoots()
        {
            TaskResources = ErrorString.ResourceManager;

            // These required properties will all be assigned by MSBuild. Suppress warnings about leaving them with
            // their default values.
            SourceRoots = null!;
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

        /// <summary>
        /// SourceRoot items with <term>MappedPath</term> metadata set.
        /// Items listed in <see cref="SourceRoots"/> that have the same ItemSpec will be merged into a single item in this list.
        /// </summary>
        [Output]
        public ITaskItem[]? MappedSourceRoots { get; private set; }

        private static class Names
        {
            public const string SourceRoot = nameof(SourceRoot);
            public const string DeterministicSourcePaths = nameof(DeterministicSourcePaths);

            // Names of well-known SourceRoot metadata items:
            public const string SourceControl = nameof(SourceControl);
            public const string RevisionId = nameof(RevisionId);
            public const string NestedRoot = nameof(NestedRoot);
            public const string ContainingRoot = nameof(ContainingRoot);
            public const string MappedPath = nameof(MappedPath);
            public const string SourceLinkUrl = nameof(SourceLinkUrl);

            public static readonly string[] SourceRootMetadataNames = new[] { SourceControl, RevisionId, NestedRoot, ContainingRoot, MappedPath, SourceLinkUrl };
        }

        private static string EnsureEndsWithSlash(string path)
            => (path[path.Length - 1] == '/') ? path : path + '/';

        private static bool EndsWithDirectorySeparator(string path)
        {
            if (path.Length == 0)
            {
                return false;
            }

            char c = path[path.Length - 1];
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        private static bool ContainsRelativePathComponents(string path)
        {
            // Only canonicalize paths that are already rooted (absolute paths)
            // Relative paths like "C:" on Linux should not be canonicalized
            if (!Path.IsPathRooted(path))
            {
                return false;
            }

            // Check for ".." (parent directory reference)
            if (path.Contains(".."))
            {
                return true;
            }

            // Check for "/." or "\." patterns (current directory reference)
            // But exclude paths that just end with "." like "C:." 
            int dotIndex = path.IndexOf('.');
            while (dotIndex >= 0)
            {
                // Check if this is a path separator followed by dot
                if (dotIndex > 0)
                {
                    char prevChar = path[dotIndex - 1];
                    if (prevChar == Path.DirectorySeparatorChar || prevChar == Path.AltDirectorySeparatorChar)
                    {
                        // This is a "/." or "\." pattern - it's a relative component
                        return true;
                    }
                }

                // Look for next dot
                dotIndex = path.IndexOf('.', dotIndex + 1);
            }

            return false;
        }

        public override bool Execute()
        {
            // Canonicalize SourceRoot paths to ensure path comparisons work correctly downstream.
            // This removes relative path components (e.g., ".." or ".") from SourceRoot items.
            foreach (var sourceRoot in SourceRoots)
            {
                var itemSpec = sourceRoot.ItemSpec;
                if (!string.IsNullOrEmpty(itemSpec) && ContainsRelativePathComponents(itemSpec))
                {
                    // Preserve the trailing separator type from the original path
                    char? trailingSeparator = null;
                    if (itemSpec.Length > 0)
                    {
                        char lastChar = itemSpec[itemSpec.Length - 1];
                        if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
                        {
                            trailingSeparator = lastChar;
                        }
                    }

                    // Canonicalize the path (GetFullPathNoThrow handles exceptions)
                    var canonicalPath = Utilities.GetFullPathNoThrow(itemSpec);

                    // Restore the trailing separator if it was present
                    if (trailingSeparator.HasValue && !EndsWithDirectorySeparator(canonicalPath))
                    {
                        canonicalPath += trailingSeparator.Value;
                    }

                    // Update the ItemSpec with the canonicalized path
                    sourceRoot.ItemSpec = canonicalPath;
                }

                // Also canonicalize the ContainingRoot metadata if it has relative components
                var containingRoot = sourceRoot.GetMetadata(Names.ContainingRoot);
                if (!string.IsNullOrEmpty(containingRoot) && ContainsRelativePathComponents(containingRoot))
                {
                    // Preserve the trailing separator type from the original path
                    char? trailingSeparator = null;
                    if (containingRoot.Length > 0)
                    {
                        char lastChar = containingRoot[containingRoot.Length - 1];
                        if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
                        {
                            trailingSeparator = lastChar;
                        }
                    }

                    // Canonicalize the path
                    var canonicalContainingRoot = Utilities.GetFullPathNoThrow(containingRoot);

                    // Restore the trailing separator if it was present
                    if (trailingSeparator.HasValue && !EndsWithDirectorySeparator(canonicalContainingRoot))
                    {
                        canonicalContainingRoot += trailingSeparator.Value;
                    }

                    // Update the ContainingRoot metadata
                    sourceRoot.SetMetadata(Names.ContainingRoot, canonicalContainingRoot);
                }
            }

            // Merge metadata of SourceRoot items with the same identity.
            var mappedSourceRoots = new List<ITaskItem>();
            var rootByItemSpec = new Dictionary<string, ITaskItem>();
            foreach (var sourceRoot in SourceRoots)
            {
                // The SourceRoot is required to have a trailing directory separator.
                // We do not append one implicitly as we do not know which separator to append on Windows.
                // The usage of SourceRoot might be sensitive to what kind of separator is used (e.g. in SourceLink where it needs
                // to match the corresponding separators used in paths given to the compiler).
                if (!EndsWithDirectorySeparator(sourceRoot.ItemSpec))
                {
                    Log.LogErrorFromResources("MapSourceRoots.PathMustEndWithSlashOrBackslash", Names.SourceRoot, sourceRoot.ItemSpec);
                }

                if (rootByItemSpec.TryGetValue(sourceRoot.ItemSpec, out var existingRoot))
                {
                    ReportConflictingWellKnownMetadata(existingRoot, sourceRoot);
                    sourceRoot.CopyMetadataTo(existingRoot);
                }
                else
                {
                    rootByItemSpec.Add(sourceRoot.ItemSpec, sourceRoot);
                    mappedSourceRoots.Add(sourceRoot);
                }
            }

            if (Log.HasLoggedErrors)
            {
                return false;
            }

            if (Deterministic)
            {
                var topLevelMappedPaths = new Dictionary<string, string>();
                void setTopLevelMappedPaths(bool sourceControlled)
                {
                    foreach (var root in mappedSourceRoots)
                    {
                        if (!string.IsNullOrEmpty(root.GetMetadata(Names.SourceControl)) == sourceControlled)
                        {
                            string localPath = root.ItemSpec;
                            string nestedRoot = root.GetMetadata(Names.NestedRoot);
                            if (string.IsNullOrEmpty(nestedRoot))
                            {
                                // root isn't nested

                                if (topLevelMappedPaths.ContainsKey(localPath))
                                {
                                    Log.LogErrorFromResources("MapSourceRoots.ContainsDuplicate", Names.SourceRoot, localPath);
                                }
                                else
                                {
                                    int index = topLevelMappedPaths.Count;
                                    var mappedPath = "/_" + (index == 0 ? "" : index.ToString()) + "/";
                                    topLevelMappedPaths.Add(localPath, mappedPath);
                                    root.SetMetadata(Names.MappedPath, mappedPath);
                                }
                            }
                        }
                    }
                }

                // assign mapped paths to process source-controlled top-level roots first:
                setTopLevelMappedPaths(sourceControlled: true);

                // then assign mapped paths to other source-controlled top-level roots:
                setTopLevelMappedPaths(sourceControlled: false);

                if (topLevelMappedPaths.Count == 0)
                {
                    Log.LogErrorFromResources("MapSourceRoots.NoTopLevelSourceRoot", Names.SourceRoot, Names.DeterministicSourcePaths);
                    return false;
                }

                // finally, calculate mapped paths of nested roots:
                foreach (var root in mappedSourceRoots)
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
                            root.SetMetadata(Names.MappedPath, mappedTopLevelPath + EnsureEndsWithSlash(nestedRoot.Replace('\\', '/')));
                        }
                        else
                        {
                            Log.LogErrorFromResources("MapSourceRoots.NoSuchTopLevelSourceRoot", Names.SourceRoot + "." + Names.ContainingRoot, Names.SourceRoot, containingRoot);
                        }
                    }
                }
            }
            else
            {
                foreach (var root in mappedSourceRoots)
                {
                    root.SetMetadata(Names.MappedPath, root.ItemSpec);
                }
            }

            if (!Log.HasLoggedErrors)
            {
                MappedSourceRoots = mappedSourceRoots.ToArray();
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Checks that when merging metadata of two SourceRoot items we don't have any conflicting well-known metadata values.
        /// </summary>
        private void ReportConflictingWellKnownMetadata(ITaskItem left, ITaskItem right)
        {
            foreach (var metadataName in Names.SourceRootMetadataNames)
            {
                var leftValue = left.GetMetadata(metadataName);
                var rightValue = right.GetMetadata(metadataName);

                if (!string.IsNullOrEmpty(leftValue) && !string.IsNullOrEmpty(rightValue) && leftValue != rightValue)
                {
                    Log.LogWarningFromResources("MapSourceRoots.ContainsDuplicate", Names.SourceRoot, left.ItemSpec, metadataName, leftValue, rightValue);
                }
            }
        }
    }
}
