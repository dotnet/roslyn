// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Describes a group of effective <see cref="SymbolVisibility"/> for symbols.
    /// </summary>
    public enum DisposeAnalysisKind
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
            return disposeAnalysisKind switch
            {
                DisposeAnalysisKind.NonExceptionPaths
                or DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed => false,
                _ => true,
            };
        }

        public static bool AreMayBeNotDisposedViolationsEnabled(this DisposeAnalysisKind disposeAnalysisKind)
        {
            return disposeAnalysisKind switch
            {
                DisposeAnalysisKind.AllPathsOnlyNotDisposed
                or DisposeAnalysisKind.NonExceptionPathsOnlyNotDisposed => false,
                _ => true,
            };
        }
    }
}
