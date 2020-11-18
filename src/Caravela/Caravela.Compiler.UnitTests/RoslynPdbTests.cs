using Microsoft.CodeAnalysis.CSharp.UnitTests.PDB;
using Roslyn.Test.Utilities;

namespace Caravela.Compiler.UnitTests.Pdb
{
    public class CaravelaCompilerCheckSumTest : CheckSumTest
    {
        public CaravelaCompilerCheckSumTest() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbConstantTests : PDBConstantTests
    {
        public CaravelaCompilerPdbConstantTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbPdbDynamicLocalsTests : PDBDynamicLocalsTests
    {
        public CaravelaCompilerPdbPdbDynamicLocalsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbEmbeddedSourceTests : PDBEmbeddedSourceTests
    {
        public CaravelaCompilerPdbEmbeddedSourceTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbIteratorTests : PDBIteratorTests
    {
        public CaravelaCompilerPdbIteratorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbLambdaTests : PDBLambdaTests
    {
        public CaravelaCompilerPdbLambdaTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbLocalFunctionTests : PDBLocalFunctionTests
    {
        public CaravelaCompilerPdbLocalFunctionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbSourceLinkTests : PDBSourceLinkTests
    {
        public CaravelaCompilerPdbSourceLinkTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbTests : PDBTests
    {
        public CaravelaCompilerPdbTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbTupleTests : PDBTupleTests
    {
        public CaravelaCompilerPdbTupleTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbUsingTests : PDBUsingTests
    {
        public CaravelaCompilerPdbUsingTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPdbWinMdExpTests : PDBWinMdExpTests
    {
        public CaravelaCompilerPdbWinMdExpTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class CaravelaCompilerPortablePdbTests : PortablePdbTests
    {
        public CaravelaCompilerPortablePdbTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
