// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicDebuggerIntellisenseNetCore : BasicDebuggerIntellisenseCommon
    {
        public BasicDebuggerIntellisenseNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.VisualBasicNetCoreConsoleApplication)
        {
        }

         [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionAfterDot()
        {
            base.CompletionAfterDot();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionAfterOpenParenInMethodCall()
        {
            base.CompletionAfterOpenParenInMethodCall();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionAfterQuestionMark()
        {
            base.CompletionAfterQuestionMark();
        }

         [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionInAnExpression()
        {
            base.CompletionInAnExpression();
        }

         [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionOnFirstCharacter()
        {
            base.CompletionOnFirstCharacter();
        }

         [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void LocalsFromPreviousBlockAreNotVisibleInTheCurrentBlock()
        {
            base.LocalsFromPreviousBlockAreNotVisibleInTheCurrentBlock();
        }

         [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void StartDebuggingAndVerifyBreakpoints()
        {
            base.StartDebuggingAndVerifyBreakpoints();
        }
    }
}
