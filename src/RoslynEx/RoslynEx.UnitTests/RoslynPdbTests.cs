using Microsoft.CodeAnalysis.CSharp.UnitTests.PDB;
using Roslyn.Test.Utilities;

namespace RoslynEx.UnitTests
{
    public class RoslynExCheckSumTest : CheckSumTest
    {
        public RoslynExCheckSumTest() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbConstantTests : PDBConstantTests
    {
        public RoslynExPdbConstantTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbPdbDynamicLocalsTests : PDBDynamicLocalsTests
    {
        public RoslynExPdbPdbDynamicLocalsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbEmbeddedSourceTests : PDBEmbeddedSourceTests
    {
        public RoslynExPdbEmbeddedSourceTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbIteratorTests : PDBIteratorTests
    {
        public RoslynExPdbIteratorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbLambdaTests : PDBLambdaTests
    {
        public RoslynExPdbLambdaTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbLocalFunctionTests : PDBLocalFunctionTests
    {
        public RoslynExPdbLocalFunctionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbSourceLinkTests : PDBSourceLinkTests
    {
        public RoslynExPdbSourceLinkTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbTests : PDBTests
    {
        public RoslynExPdbTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbTupleTests : PDBTupleTests
    {
        public RoslynExPdbTupleTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbUsingTests : PDBUsingTests
    {
        public RoslynExPdbUsingTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbWinMdExpTests : PDBWinMdExpTests
    {
        public RoslynExPdbWinMdExpTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPortablePdbTests : PortablePdbTests
    {
        public RoslynExPortablePdbTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
