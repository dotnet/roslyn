// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.Shell;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpGoToBase : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpGoToBase(VisualStudioInstanceFactory instanceFactory)
                    : base(instanceFactory, nameof(CSharpGoToBase))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToBase)]
        public void GoToBaseFromMetadataAsSource()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "C.cs");
            VisualStudio.SolutionExplorer.OpenFile(project, "C.cs");
            VisualStudio.Editor.SetText(
@"using System;

class C
{
    public override string ToString()
    {
        return ""C"";
    }
}");
            VisualStudio.Editor.PlaceCaret("ToString", charsOffset: -1);
            VisualStudio.Editor.GoToBase("Object [from metadata]");
            VisualStudio.Editor.Verify.TextContains(@"public virtual string ToString$$()", assertCaretPosition: true);
        }
    }
}
