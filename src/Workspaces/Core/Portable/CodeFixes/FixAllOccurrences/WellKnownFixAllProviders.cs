// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeActions;

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
        /// <remarks>
        /// The batch fix all provider only batches operations (i.e. <see cref="CodeActionOperation"/>) of type
        /// <see cref="ApplyChangesOperation"/> present within the individual diagnostic fixes. Other types of
        /// operations present within these fixes are ignored.
        /// </remarks>
        public static FixAllProvider BatchFixer => BatchFixAllProvider.Instance;
    }
}
