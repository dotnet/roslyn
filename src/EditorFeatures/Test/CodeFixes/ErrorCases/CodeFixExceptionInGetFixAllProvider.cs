// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            throw new Exception($"Exception thrown in GetFixAllProvider of {nameof(ExceptionInGetFixAllProvider)}");
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
#pragma warning disable RS0005 // Do not use generic CodeAction.Create to create CodeAction
            context.RegisterCodeFix(CodeAction.Create("Do Nothing", token => Task.FromResult(context.Document)), context.Diagnostics[0]);
#pragma warning restore RS0005 // Do not use generic CodeAction.Create to create CodeAction
            return Task.FromResult(true);
        }
    }
}
