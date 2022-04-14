// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static partial class FixAllContextHelper
    {
        public static string GetDefaultFixAllTitle(FixAllContext fixAllContext)
            => GetDefaultFixAllTitle(fixAllContext.Scope, fixAllContext.DiagnosticIds, fixAllContext.Document, fixAllContext.Project);

        public static string GetDefaultFixAllTitle(
            FixAllScope fixAllScope,
            ImmutableHashSet<string> diagnosticIds,
            Document? triggerDocument,
            Project triggerProject)
        {
            var diagnosticId = diagnosticIds.First();

            return fixAllScope switch
            {
                FixAllScope.Custom => string.Format(WorkspaceExtensionsResources.Fix_all_0, diagnosticId),
                FixAllScope.Document => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, diagnosticId, triggerDocument!.Name),
                FixAllScope.Project => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, diagnosticId, triggerProject.Name),
                FixAllScope.Solution => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Solution, diagnosticId),
                FixAllScope.ContainingMember => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_member, diagnosticId),
                FixAllScope.ContainingType => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_type, diagnosticId),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllScope),
            };
        }
    }
}
