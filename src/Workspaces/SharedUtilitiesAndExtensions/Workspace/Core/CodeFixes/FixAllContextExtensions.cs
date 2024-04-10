// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal static class FixAllContextExtensions
{
    public static string GetDefaultFixAllTitle(this FixAllContext context)
        => FixAllHelper.GetDefaultFixAllTitle(context.Scope, title: context.DiagnosticIds.First(), context.Document!, context.Project);
}
