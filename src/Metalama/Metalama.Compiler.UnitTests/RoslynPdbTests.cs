using Microsoft.CodeAnalysis.CSharp.UnitTests.PDB;
using Roslyn.Test.Utilities;

namespace Metalama.Compiler.UnitTests.Pdb
{
    public class MetalamaCompilerCheckSumTest : CheckSumTest
    {
        public MetalamaCompilerCheckSumTest() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbConstantTests : PDBConstantTests
    {
        public MetalamaCompilerPdbConstantTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbPdbDynamicLocalsTests : PDBDynamicLocalsTests
    {
        public MetalamaCompilerPdbPdbDynamicLocalsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbEmbeddedSourceTests : PDBEmbeddedSourceTests
    {
        public MetalamaCompilerPdbEmbeddedSourceTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbIteratorTests : PDBIteratorTests
    {
        public MetalamaCompilerPdbIteratorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbLambdaTests : PDBLambdaTests
    {
        public MetalamaCompilerPdbLambdaTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbLocalFunctionTests : PDBLocalFunctionTests
    {
        public MetalamaCompilerPdbLocalFunctionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbSourceLinkTests : PDBSourceLinkTests
    {
        public MetalamaCompilerPdbSourceLinkTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbTests : PDBTests
    {
        public MetalamaCompilerPdbTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbTupleTests : PDBTupleTests
    {
        public MetalamaCompilerPdbTupleTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbUsingTests : PDBUsingTests
    {
        public MetalamaCompilerPdbUsingTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPdbWinMdExpTests : PDBWinMdExpTests
    {
        public MetalamaCompilerPdbWinMdExpTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class MetalamaCompilerPortablePdbTests : PortablePdbTests
    {
        public MetalamaCompilerPortablePdbTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
