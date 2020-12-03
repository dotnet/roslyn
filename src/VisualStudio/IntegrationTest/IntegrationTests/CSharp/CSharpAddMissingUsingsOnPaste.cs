// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpAddMissingUsingsOnPaste : AbstractEditorTest
    {
        public CSharpAddMissingUsingsOnPaste(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpAddMissingUsingsOnPaste))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [WpfFact, Trait(Traits.Feature, Traits.Features.AddMissingImports)]
        public void VerifyMissingByDefault()
        {
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Foo.cs", contents: @"
public class Foo
{
}
");
            SetUpEditor(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    $$
}");

            VisualStudio.Editor.Paste(@"Task DoThingAsync() => Task.CompletedTask;");

            VisualStudio.Editor.Verify.TextContains(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    Task DoThingAsync() => Task.CompletedTask;
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AddMissingImports)]
        public void VerifyAddImportsOnPaste()
        {
            var project = new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Foo.cs", contents: @"
public class Foo
{
}
");
            SetUpEditor(@"
using System;

class Program
{
    static void Main(string[] args)
    {
    }

    $$
}");

            using var telemetry = VisualStudio.EnableTestTelemetryChannel();

            VisualStudio.Workspace.SetFeatureOption(FeatureOnOffOptions.AddImportsOnPaste.Feature, FeatureOnOffOptions.AddImportsOnPaste.Name, LanguageNames.CSharp, "True");

            VisualStudio.Editor.Paste(@"Task DoThingAsync() => Task.CompletedTask;");

            VisualStudio.Editor.Verify.TextContains(@"
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
    }

    Task DoThingAsync() => Task.CompletedTask;
}");
            telemetry.VerifyFired("vs/ide/vbcs/commandhandler/paste/importsonpaste");
        }
    }
}
