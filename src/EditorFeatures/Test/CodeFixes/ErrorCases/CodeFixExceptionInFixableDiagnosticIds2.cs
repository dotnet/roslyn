// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes.ErrorCases;

public class ExceptionInFixableDiagnosticIds2 : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds => new ImmutableArray<string>();

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        => Task.FromResult(true);
}
