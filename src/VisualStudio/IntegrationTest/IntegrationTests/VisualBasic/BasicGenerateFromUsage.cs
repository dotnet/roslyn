// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicGenerateFromUsage : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicGenerateFromUsage(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicGenerateFromUsage))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateLocal)]
        public void GenerateLocal()
        {
            SetUpEditor(
@"Module Program
    Sub Main(args As String())
        Dim x As String = $$xyz
    End Sub
End Module");
            VisualStudio.Editor.Verify.CodeAction("Generate local 'xyz'", applyFix: true);
            VisualStudio.Editor.Verify.TextContains(
@"Module Program
    Sub Main(args As String())
        Dim xyz As String = Nothing
        Dim x As String = xyz
    End Sub
End Module");
        }
    }
}
