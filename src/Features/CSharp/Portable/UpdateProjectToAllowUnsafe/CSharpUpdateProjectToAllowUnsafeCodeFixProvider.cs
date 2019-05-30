// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UpgradeProject;

namespace Microsoft.CodeAnalysis.CSharp.UpdateProjectToAllowUnsafe
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUpdateProjectToAllowUnsafeCodeFixProvider : CodeFixProvider
    {
        private const string CS0227 = nameof(CS0227); // error CS0227: Unsafe code may only appear if compiling with /unsafe

        [ImportingConstructor]
        public CSharpUpdateProjectToAllowUnsafeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0227);

        public override FixAllProvider GetFixAllProvider()
        {
            // We're OK with users having to explicitly opt in each project. Unsafe code is itself an edge case and we don't
            // need to make it easier to convert to it on a larger scale. It's also unlikely that anyone will need this.
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
