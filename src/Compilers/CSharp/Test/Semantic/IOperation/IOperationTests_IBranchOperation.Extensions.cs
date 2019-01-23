// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_ForNull_ThrowsArgumentNullException()
        {
            Assert.ThrowsAny<ArgumentNullException>(() => OperationExtensions.GetCorrespondingOperation(null));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_ForGotoBranch_ReturnsNull()
        {
            var result = GetOuterOperationAndCorrespondingInnerOperation<LabeledStatementSyntax, GotoStatementSyntax>(@"
class C
{
    void F()
    {
/*<bind>*/begin:
        for (;;)
        {
            /*<bind>*/goto begin;/*</bind>*/
        }/*</bind>*/
    }
}");
            Assert.IsAssignableFrom(typeof(ILabeledOperation), result.outer);
            Assert.Null(result.corresponding);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_ForLoopWithBreak()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/for (;;)
        {
            /*<bind>*/break;/*</bind>*/
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_WhileLoopWithContinue()
        {
            AssertOuterIsCorrespondingLoopOfInner<WhileStatementSyntax, ContinueStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/while (true)
        {
            /*<bind>*/continue;/*</bind>*/
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_DoWhileLoopWithBreakAndContinue()
        {
            AssertOuterIsCorrespondingLoopOfInner<DoStatementSyntax, ContinueStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/do
        {
            if (true)
                break;
            else
                /*<bind>*/continue;/*</bind>*/
        } while (true)/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_ForEachLoopWithBreak()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForEachStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/foreach (var i in new [] {1,2,3})
        {
            if (i == 2)
                /*<bind>*/break;/*</bind>*/
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_ForEachLoopWithBreakAndContinue()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForEachStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/foreach (var i in new [] {1,2,3})
        {
            if (i == 2) 
                /*<bind>*/break;/*</bind>*/
            else
                continue;
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_NestedLoops()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/for (;;)
        {
            for (;;)
            {
            }
            /*<bind>*/break;/*</bind>*/
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_NestedLoops2()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        for (;;)
        {
            /*<bind>*/for (;;)
            {
                /*<bind>*/break;/*</bind>*/
            }/*</bind>*/
        }
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_SwitchLookup_BreakInCase()
        {
            AssertOuterIsCorrespondingSwitchOfInner(@"
class C
{
    void F()
    {
        /*<bind>*/switch (1)
        {
            case 1:
            /*<bind>*/break;/*</bind>*/
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_SwitchLookup_NestedSwitches()
        {
            AssertOuterIsCorrespondingSwitchOfInner(@"
class C
{
    void F()
    {
        /*<bind>*/switch (1)
        {
            case 1:
                switch (2)
                {
                    case 2:
                    break;
                }
            /*<bind>*/break;/*</bind>*/
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_SwitchLookup_NestedSwitches2()
        {
            AssertOuterIsCorrespondingSwitchOfInner(@"
class C
{
    void F()
    {
        switch (1)
        {
            case 1:
                /*<bind>*/switch (2)
                {
                    case 2:
                    /*<bind>*/break;/*</bind>*/
                }/*</bind>*/
            break;
        }
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_LoopInSwitch()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        switch (1)
        {
            case 1:
                /*<bind>*/for (;;)
                {
                    /*<bind>*/break;/*</bind>*/
                }/*</bind>*/
            break;
        }
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_SwitchLookup_SwitchInLoop()
        {
            AssertOuterIsCorrespondingSwitchOfInner(@"
class C
{
    void F()
    {
        for (;;)
        {
            /*<bind>*/switch (1)
            {
                case 1:
                /*<bind>*/break;/*</bind>*/
            }/*</bind>*/
        }
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_ContinueNestedInIntermediateSwitch()
        {
            AssertOuterIsCorrespondingLoopOfInner<ForStatementSyntax, ContinueStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/for (;;)
        {
            switch (1)
            {
                case 1:
                    /*<bind>*/continue;/*</bind>*/
                    break;
            }
        }/*</bind>*/
    }
}");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_LoopLookup_BreakButNoLoop_ReturnsNull()
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<ForStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/break;/*</bind>*/
    }
}");

            Assert.Null(expected);
            Assert.Null(actual);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingOperation_SwitchLookup_BreakButNoSwitch_ReturnsNull()
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<SwitchStatementSyntax, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/break;/*</bind>*/
    }
}");

            Assert.Null(expected);
            Assert.Null(actual);
        }

        private void AssertOuterIsCorrespondingLoopOfInner<TOuterSyntax, TInnerSyntax>(string source)
            where TOuterSyntax : SyntaxNode
            where TInnerSyntax : SyntaxNode
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<TOuterSyntax, TInnerSyntax>(source);

            Assert.Equal(expected.Syntax, actual.Syntax);
        }

        private void AssertOuterIsCorrespondingSwitchOfInner(string source)
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<SwitchStatementSyntax, BreakStatementSyntax>(source);

            Assert.Equal(expected.Syntax, actual.Syntax);
        }

        private (IOperation outer, IOperation corresponding) GetOuterOperationAndCorrespondingInnerOperation<TOuterSyntax, TInnerSyntax>(string source)
            where TOuterSyntax : SyntaxNode
            where TInnerSyntax : SyntaxNode
        {
            var compilation = CreateCompilation(source);

            var outer = GetOperationAndSyntaxForTest<TOuterSyntax>(compilation).operation;
            var inner = GetOperationAndSyntaxForTest<TInnerSyntax>(compilation).operation as IBranchOperation;
            var correspondingOfInner = inner?.GetCorrespondingOperation();

            return (outer, correspondingOfInner);
        }
    }
}
