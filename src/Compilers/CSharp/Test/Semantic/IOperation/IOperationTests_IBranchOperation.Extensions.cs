// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
        public void GetCorrespondingLoop_ForLoopWithBreak()
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
        public void GetCorrespondingLoop_WhileLoopWithContinue()
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
        public void GetCorrespondingLoop_DoWhileLoopWithBreakAndContinue()
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
        public void GetCorrespondingLoop_ForEachLoopWithBreak()
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
        public void GetCorrespondingLoop_ForEachLoopWithBreakAndContinue()
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
        public void GetCorrespondingLoop_NestedLoops()
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
        public void GetCorrespondingLoop_NestedLoops2()
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
        public void GetCorrespondingSwitch_BreakInCase()
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
        public void GetCorrespondingSwitch_NestedSwitches()
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
        public void GetCorrespondingSwitch_NestedSwitches2()
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
        public void GetCorrespondingLoop_LoopInSwitch()
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
        public void GetCorrespondingSwitch_SwitchInLoop()
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
        public void GetCorrespondingLoop_ContinueNestedInIntermediateSwitch()
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
        public void GetCorrespondingLoop_BreakButNoLoop_ReturnsNull()
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<ForStatementSyntax, ILoopOperation, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/break;/*</bind>*/
    }
}", branch => branch.GetCorrespondingLoop());

            Assert.Null(expected);
            Assert.Null(actual);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingSwitch_BreakButNoSwitch_ReturnsNull()
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<SwitchStatementSyntax, ISwitchOperation, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/break;/*</bind>*/
    }
}", branch => branch.GetCorrespondingSwitch());

            Assert.Null(expected);
            Assert.Null(actual);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingLoop_BreakButCorrespondingToSwitch_ReturnsNull()
        {
            var (outer, actual) = GetOuterOperationAndCorrespondingInnerOperation<ForStatementSyntax, ILoopOperation, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/for (;;)
        {
            switch (1)
            {
                case 1:
                /*<bind>*/break;/*</bind>*/
            }
        }/*</bind>*/
        
    }
}", branch => branch.GetCorrespondingLoop());

            Assert.NotNull(outer);
            Assert.Null(actual);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [WorkItem(28095, "https://github.com/dotnet/roslyn/issues/28095")]
        [Fact]
        public void GetCorrespondingSwitch_BreakButCorrespondingToLoop_ReturnsNull()
        {
            var (outer, actual) = GetOuterOperationAndCorrespondingInnerOperation<SwitchStatementSyntax, ISwitchOperation, BreakStatementSyntax>(@"
class C
{
    void F()
    {
        /*<bind>*/switch (1)
        {
            case 1:
                for (;;)
                {
                    /*<bind>*/break;/*</bind>*/
                }
            break;
        }/*</bind>*/
        
    }
}", branch => branch.GetCorrespondingSwitch());

            Assert.NotNull(outer);
            Assert.Null(actual);
        }

        private void AssertOuterIsCorrespondingLoopOfInner<TOuterSyntax, TInnerSyntax>(string source)
            where TOuterSyntax : SyntaxNode 
            where TInnerSyntax : SyntaxNode
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<TOuterSyntax, ILoopOperation, TInnerSyntax>(
                source, branch => branch.GetCorrespondingLoop());

            Assert.Equal(expected.Syntax, actual.Syntax);
        }

        private void AssertOuterIsCorrespondingSwitchOfInner(string source)
        {
            var (expected, actual) = GetOuterOperationAndCorrespondingInnerOperation<SwitchStatementSyntax, ISwitchOperation, BreakStatementSyntax>(
                source, branch => branch.GetCorrespondingSwitch());

            Assert.Equal(expected.Syntax, actual.Syntax);
        }

        private (IOperation outer, IOperation corresponding) GetOuterOperationAndCorrespondingInnerOperation<TOuterSyntax, TOuterOp, TInnerSyntax>(
            string source, Func<IBranchOperation, IOperation> findCorresponding)
            where TOuterSyntax : SyntaxNode
            where TOuterOp : class, IOperation
            where TInnerSyntax : SyntaxNode
        {
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            (IOperation operation, SyntaxNode node) holder;
            holder = GetOperationAndSyntaxForTest<TOuterSyntax>(compilation);
            var outer = holder.operation as TOuterOp;
            holder = GetOperationAndSyntaxForTest<TInnerSyntax>(compilation);
            var inner = holder.operation as IBranchOperation;

            var correspondingOfInner = inner != null ? findCorresponding(inner) : null;

            return (outer, correspondingOfInner);
        }
    }
}
