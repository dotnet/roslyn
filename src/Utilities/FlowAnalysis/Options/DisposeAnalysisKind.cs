// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Describes a group of effective <see cref="SymbolVisibility"/> for symbols.
    /// </summary>
    internal enum DisposeAnalysisKind
    {
        // NOTE: Below fields names are used in the .editorconfig specification
        //       for DisposeAnalysisKind option. Hence the names should *not* be modified,
        //       as that would be a breaking change for .editorconfig specification.

        /// <summary>
        /// Track and report missing dispose violations on all paths (non-exception and exception paths).
        /// Additionally, also flag use of non-recommended dispose patterns that may cause
        /// potential dispose leaks.
        /// </summary>
        AllPaths,

        /// <summary>
        /// Track and report missing dispose violations on all paths (non-exception and exception paths).
        /// Do not flag use of non-recommended dispose patterns that may cause
        /// potential dispose leaks.
        /// </summary>
        AllPathsOnlyNotDisposed,

        /// <summary>
        /// Track and report missing dispose violations only on non-exception program paths.
        /// Additionally, also flag use of non-recommended dispose patterns that may cause
        /// potential dispose leaks.
        /// </summary>
        NonExceptionPaths,

        /// <summary>
        /// Track and report missing dispose violations only on non-exception program paths.
        /// Do not flag use of non-recommended dispose patterns that may cause
        /// potential dispose leaks.
        /// </summary>
        NonExceptionPathsOnlyNotDisposed,
    }

    internal static class DisposeAnalysisKindExtensions
    {
        public static bool AreExceptionPathsAndMayBeNotDisposedViolationsEnabled(this DisposeAnalysisKind disposeAnalysisKind)
            => disposeAnalysisKind.AreExceptionPathsEnabled() && disposeAnalysisKind.AreMayBeNotDisposedViolationsEnabled();

        public static bool AreExceptionPathsEnabled(this DisposeAnalysisKind disposeAnalysisKind)
        {
            switch (disposeAnalysisKind)
            {
                case DisposeAnalysisKind.NonExceptionPaths:
                case DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed:
                    return false;

                default:
                    return true;
            }
        }

        public static bool AreMayBeNotDisposedViolationsEnabled(this DisposeAnalysisKind disposeAnalysisKind)
        {
            switch (disposeAnalysisKind)
            {
                case DisposeAnalysisKind.AllPathsOnlyNotDisposed:
                case DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed:
                    return false;

                default:
                    return true;
            }
        }
    }
}
