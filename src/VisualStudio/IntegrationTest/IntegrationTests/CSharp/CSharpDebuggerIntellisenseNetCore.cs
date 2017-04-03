// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpDebuggerIntellisenseNetCore : CSharpDebuggerIntellisenseCommon
    {
        public CSharpDebuggerIntellisenseNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreConsoleApplication)
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionAfterDot()
        {
            base.CompletionAfterDot();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionAfterOpenParenInMethodCall()
        {
            base.CompletionAfterOpenParenInMethodCall();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionAfterQuestionMark()
        {
            base.CompletionAfterQuestionMark();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionInAnExpression()
        {
            base.CompletionInAnExpression();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void CompletionOnFirstCharacter()
        {
            base.CompletionOnFirstCharacter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void DoNotCrashOnSemicolon()
        {
            base.DoNotCrashOnSemicolon();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void LocalsFromPreviousBlockAreNotVisibleInTheCurrentBlock()
        {
            base.LocalsFromPreviousBlockAreNotVisibleInTheCurrentBlock();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Debugger)]
        public override void StartDebuggingAndVerifyBreakpoints()
        {
            base.StartDebuggingAndVerifyBreakpoints();
        }
    }
}
