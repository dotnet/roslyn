// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    public class PossibleTupleContextTests : AbstractContextTests
    {
        protected override void CheckResult(bool validLocation, int position, SyntaxTree syntaxTree)
        {
            var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, CancellationToken.None);
            var isPossibleTupleContext = syntaxTree.IsPossibleTupleContext(leftToken, position);

            Assert.Equal(validLocation, isPossibleTupleContext);
        }

        private void VerifyMultipleContexts(string x)
        {
            VerifyTrue(x);
            VerifyTrue(AddInsideClass(x));
            VerifyTrue(AddInsideMethod(x));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test1()
        {
            VerifyMultipleContexts(@"((a, b) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test2()
        {
            VerifyMultipleContexts(@"(xyz, (a, b) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test3()
        {
            VerifyMultipleContexts(@"(a $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test4()
        {
            VerifyMultipleContexts(@"(a, b $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test5()
        {
            VerifyMultipleContexts(@"($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test6()
        {
            VerifyMultipleContexts(@"(a, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test7()
        {
            VerifyMultipleContexts(@"(a.b $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test8()
        {
            VerifyMultipleContexts(@"(a, a.b $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test9()
        {
            VerifyTrue(@"class C : I<($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test10()
        {
            VerifyTrue(@"class C : I<(a, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test11()
        {
            VerifyTrue(AddInsideMethod(@"(var $$)"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test12()
        {
            VerifyTrue(AddInsideMethod(@"(var a, var $$)"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Test13()
        {
            VerifyTrue(AddInsideMethod(@"var str = (($$)items) as string;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void False1()
        {
            VerifyFalse(@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void False2()
        {
            VerifyFalse(AddInsideMethod(@"(int) $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void False3()
        {
            VerifyFalse(AddInsideMethod(@"(Goo()) $$"));
        }
    }
}
