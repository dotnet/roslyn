// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Contains well known implementations of <see cref="FixAllProvider"/>.
    /// </summary>
    public static class WellKnownFixAllProviders
    {
        /// <summary>
        /// Default batch fix all provider.
        /// This provider batches all the individual diagnostic fixes across the scope of fix all action,
        /// computes fixes in parallel and then merges all the non-conflicting fixes into a single fix all code action.
        /// This fixer supports fixes for the following fix all scopes:
        /// <see cref="FixAllScope.Document"/>, <see cref="FixAllScope.Project"/> and <see cref="FixAllScope.Solution"/>.
        /// </summary>
        public static readonly FixAllProvider BatchFixer = BatchFixAllProvider.Instance;

        /// <summary>
        /// Default batch fix all provider for simplification fixers which only add Simplifier annotations to documents.
        /// This provider batches all the simplifier annotation actions within a document into a single code action,
        /// instead of creating separate code actions for each added annotation.
        /// This fixer supports fixes for the following fix all scopes:
        /// <see cref="FixAllScope.Document"/>, <see cref="FixAllScope.Project"/> and <see cref="FixAllScope.Solution"/>.
        /// </summary>
        internal static readonly FixAllProvider BatchSimplificationFixer = BatchSimplificationFixAllProvider.Instance;
    }
}
