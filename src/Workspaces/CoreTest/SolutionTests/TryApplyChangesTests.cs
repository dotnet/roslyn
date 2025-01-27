// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class TryApplyChangesTests
    {
        private class CustomizedCanApplyWorkspace : Workspace
        {
            private readonly ImmutableArray<ApplyChangesKind> _allowedKinds;
            private readonly Func<ParseOptions, ParseOptions, bool>? _canApplyParseOptions;
            private readonly Func<CompilationOptions, CompilationOptions, bool>? _canApplyCompilationOptions;

            public CustomizedCanApplyWorkspace(params ApplyChangesKind[] allowedKinds)
                : this(allowedKinds, canApplyParseOptions: null)
            {
            }

            public CustomizedCanApplyWorkspace(ApplyChangesKind[] allowedKinds,
                Func<ParseOptions, ParseOptions, bool>? canApplyParseOptions = null,
                Func<CompilationOptions, CompilationOptions, bool>? canApplyCompilationOptions = null)
                : base(Host.Mef.MefHostServices.DefaultHost, workspaceKind: nameof(CustomizedCanApplyWorkspace))
            {
                _allowedKinds = [.. allowedKinds];
                _canApplyParseOptions = canApplyParseOptions;
                _canApplyCompilationOptions = canApplyCompilationOptions;

                // Add a C# project automatically so each test has something to try mutating
                OnProjectAdded(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "TestProject", "TestProject", LanguageNames.CSharp));
            }

            public override bool CanApplyChange(ApplyChangesKind feature)
            {
                return _allowedKinds.Contains(feature);
            }

            public override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
            {
                if (_canApplyParseOptions != null)
                {
                    return _canApplyParseOptions(oldOptions, newOptions);
                }

                return base.CanApplyParseOptionChange(oldOptions, newOptions, project);
            }

            public override bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project)
            {
                if (_canApplyCompilationOptions != null)
                {
                    return _canApplyCompilationOptions(oldOptions, newOptions);
                }

                return base.CanApplyCompilationOptionChange(oldOptions, newOptions, project);
            }
        }

        [Fact]
        public void TryApplyWorksIfAllowingAnyCompilationOption()
        {
            // If we simply support the main change kind, then any type of change should be supported and we should not get called
            // to the other method
            using var workspace = new CustomizedCanApplyWorkspace(
                allowedKinds: [ApplyChangesKind.ChangeCompilationOptions],
                canApplyCompilationOptions: (_, __) => throw new Exception("This should not have been called."));

            var project = workspace.CurrentSolution.Projects.Single();

            Assert.True(workspace.TryApplyChanges(project.WithCompilationOptions(project.CompilationOptions!.WithMainTypeName("Test")).Solution));
        }

        [Fact]
        public void TryApplyWorksSpecificChangeIsAllowedForCompilationOption()
        {
            // If we don't support the main change kind, then the other method should be called
            using var workspace = new CustomizedCanApplyWorkspace(
                allowedKinds: [],
                canApplyCompilationOptions: (_, newCompilationOptions) => newCompilationOptions.MainTypeName == "Test");

            var project = workspace.CurrentSolution.Projects.Single();

            Assert.True(workspace.TryApplyChanges(project.WithCompilationOptions(project.CompilationOptions!.WithMainTypeName("Test")).Solution));
        }

        [Fact]
        public void TryApplyWorksThrowsIfChangeIsDisallowedForCompilationOption()
        {
            // If we don't support the main change kind, then the other method should be called
            using var workspace = new CustomizedCanApplyWorkspace(
                allowedKinds: [],
                canApplyCompilationOptions: (_, newCompilationOptions) => newCompilationOptions.MainTypeName == "Expected");

            var project = workspace.CurrentSolution.Projects.Single();

            var exception = Assert.Throws<NotSupportedException>(
                () => workspace.TryApplyChanges(project.WithCompilationOptions(project.CompilationOptions!.WithMainTypeName("WrongThing")).Solution));

            Assert.Equal(WorkspacesResources.Changing_compilation_options_is_not_supported, exception.Message);
        }

        [Fact]
        public void TryApplyWorksIfAllowingAnyParseOption()
        {
            // If we simply support the main change kind, then any type of change should be supported and we should not get called
            // to the other method
            using var workspace = new CustomizedCanApplyWorkspace(
                allowedKinds: [ApplyChangesKind.ChangeParseOptions],
                canApplyParseOptions: (_, __) => throw new Exception("This should not have been called."));

            var project = workspace.CurrentSolution.Projects.Single();

            Assert.True(workspace.TryApplyChanges(
                project.WithParseOptions(
                    project.ParseOptions!.WithFeatures([KeyValuePairUtil.Create("Feature", "")])).Solution));
        }

        [Fact]
        public void TryApplyWorksSpecificChangeIsAllowedForParseOption()
        {
            // If we don't support the main change kind, then the other method should be called
            using var workspace = new CustomizedCanApplyWorkspace(
                allowedKinds: [],
                canApplyParseOptions: (_, newParseOptions) => newParseOptions.Features["Feature"] == "ExpectedValue");

            var project = workspace.CurrentSolution.Projects.Single();

            Assert.True(
                workspace.TryApplyChanges(
                    project.WithParseOptions(project.ParseOptions!.WithFeatures([KeyValuePairUtil.Create("Feature", "ExpectedValue")])).Solution));
        }

        [Fact]
        public void TryApplyWorksThrowsIfChangeIsDisallowedForParseOption()
        {
            // If we don't support the main change kind, then the other method should be called
            using var workspace = new CustomizedCanApplyWorkspace(
                allowedKinds: [],
                canApplyParseOptions: (_, newParseOptions) => newParseOptions.Features["Feature"] == "ExpectedValue");

            var project = workspace.CurrentSolution.Projects.Single();

            var exception = Assert.Throws<NotSupportedException>(
                () => workspace.TryApplyChanges(
                    project.WithParseOptions(project.ParseOptions!.WithFeatures([KeyValuePairUtil.Create("Feature", "WrongThing")])).Solution));

            Assert.Equal(WorkspacesResources.Changing_parse_options_is_not_supported, exception.Message);
        }

        [Fact]
        public void TryApplyWorksWhenAddingEditorConfigWithoutSupportingCompilationOptionsChanging()
        {
            using var workspace = new CustomizedCanApplyWorkspace(allowedKinds: ApplyChangesKind.AddAnalyzerConfigDocument);

            var project = workspace.CurrentSolution.Projects.Single();

            Assert.True(workspace.TryApplyChanges(project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From("")).Project.Solution));
        }
    }
}
