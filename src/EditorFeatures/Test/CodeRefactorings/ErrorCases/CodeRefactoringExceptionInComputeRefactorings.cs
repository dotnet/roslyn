﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService.ErrorCases
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Test")]
    [Shared]
    [PartNotDiscoverable]
    internal class ExceptionInCodeActions : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExceptionInCodeActions()
        {
        }

        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            => throw new Exception($"Exception thrown from ComputeRefactoringsAsync in {nameof(ExceptionInCodeActions)}");
    }
}
