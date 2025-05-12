// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UpgradeProject;

namespace Microsoft.CodeAnalysis.CSharp.UpdateProjectToAllowUnsafe;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UpdateProjectToAllowUnsafe), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpUpdateProjectToAllowUnsafeCodeFixProvider() : CodeFixProvider
{
    private const string CS0227 = nameof(CS0227); // error CS0227: Unsafe code may only appear if compiling with /unsafe

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [CS0227];

    public override FixAllProvider? GetFixAllProvider()
    {
        // We're OK with users having to explicitly opt in each project. Unsafe code is itself an edge case and we don't
        // need to make it easier to convert to it on a larger scale. It's also unlikely that anyone will need this.
        return null;
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(ProjectOptionsChangeAction.Create(CSharpCodeFixesResources.Allow_unsafe_code_in_this_project,
            _ => Task.FromResult(AllowUnsafeOnProject(context.Document.Project))), context.Diagnostics);
        return Task.CompletedTask;
    }

    private static Solution AllowUnsafeOnProject(Project project)
    {
        var compilationOptions = (CSharpCompilationOptions?)project.CompilationOptions;
        Contract.ThrowIfNull(compilationOptions);
        return project.Solution.WithProjectCompilationOptions(project.Id, compilationOptions.WithAllowUnsafe(true));
    }
}
