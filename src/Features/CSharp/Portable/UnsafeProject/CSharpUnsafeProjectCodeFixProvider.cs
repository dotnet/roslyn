// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UpgradeProject;

namespace Microsoft.CodeAnalysis.CSharp.UnsafeProject
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUnsafeProjectCodeFixProvider : CodeFixProvider
    {
        private const string CS0227 = nameof(CS0227); // error CS0227: Unsafe code may only appear if compiling with /unsafe

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0227);

        private string UnsafeThisProjectResource => "Allow unsafe code in this project";

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new ProjectOptionsChangeAction(UnsafeThisProjectResource, _ =>
                Task.FromResult(AllowUnsafeOnProject(context.Document.Project))), context.Diagnostics);
            return Task.CompletedTask;
        }

        private static Solution AllowUnsafeOnProject(Project project)
        {
            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions;
            return project.Solution.WithProjectCompilationOptions(project.Id, compilationOptions.WithAllowUnsafe(true));
        }
    }
}
