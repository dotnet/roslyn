// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes.ErrorCases
{
    public class ExceptionInFixableDiagnosticIds : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                throw new Exception($"Exception thrown in FixableDiagnosticIds of {nameof(ExceptionInFixableDiagnosticIds)}");
            }
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            return Task.FromResult(true);
        }
    }
}
