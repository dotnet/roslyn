// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UpgradeProject;

namespace Microsoft.CodeAnalysis.CSharp.UnsafeProject
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUpdateProjectToAllowUnsafeCodeFixProvider : CodeFixProvider
    {
        private const string CS0227 = nameof(CS0227); // error CS0227: Unsafe code may only appear if compiling with /unsafe

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0227);

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix all could be implemented, but doesn't seem as important because for this to be useful,
            // the user would have to have erroneous unsafe blocks in several projects, which sounds really unlikely.
            // And after all, unsafe code itself is and should be an edge case - do we want to make it easier
            // to convert to it on a larger scale?
            // If we do this, we should create a custom FixAllProvider that only supports FixAllScope.Solution
            // since Document and Project don't really make sense - the action would always be the same as for fix one
            // and having these extra options would only be confusing.
            return null;
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new ProjectOptionsChangeAction(CSharpFeaturesResources.Allow_unsafe_code_in_this_project,
                _ => Task.FromResult(AllowUnsafeOnProject(context.Document.Project))), context.Diagnostics);
            return Task.CompletedTask;
        }

        private static Solution AllowUnsafeOnProject(Project project)
        {
            var compilationOptions = (CSharpCompilationOptions)project.CompilationOptions;
            return project.Solution.WithProjectCompilationOptions(project.Id, compilationOptions.WithAllowUnsafe(true));
        }
    }
}
