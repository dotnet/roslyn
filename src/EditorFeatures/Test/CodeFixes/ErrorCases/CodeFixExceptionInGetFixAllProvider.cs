// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes.ErrorCases
{
    public class ExceptionInGetFixAllProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CodeFixServiceTests.MockFixer.Id); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
            => throw new Exception($"Exception thrown in GetFixAllProvider of {nameof(ExceptionInGetFixAllProvider)}");

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
#pragma warning disable RS0005 // Do not use generic CodeAction.Create to create CodeAction
            context.RegisterCodeFix(CodeAction.Create("Do Nothing", token => Task.FromResult(context.Document)), context.Diagnostics[0]);
#pragma warning restore RS0005 // Do not use generic CodeAction.Create to create CodeAction
            return Task.FromResult(true);
        }
    }
}
