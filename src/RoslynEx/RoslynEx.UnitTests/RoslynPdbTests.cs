using Microsoft.CodeAnalysis.CSharp.UnitTests.PDB;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace RoslynEx.UnitTests
{
    public class RoslynExCheckSumTest : CheckSumTest
    {
        public RoslynExCheckSumTest() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbConstantTests : PDBConstantTests
    {
        public RoslynExPdbConstantTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbPdbDynamicLocalsTests : PDBDynamicLocalsTests
    {
        public RoslynExPdbPdbDynamicLocalsTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbEmbeddedSourceTests : PDBEmbeddedSourceTests
    {
        public RoslynExPdbEmbeddedSourceTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbIteratorTests : PDBIteratorTests
    {
        public RoslynExPdbIteratorTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbLambdaTests : PDBLambdaTests
    {
        public RoslynExPdbLambdaTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbLocalFunctionTests : PDBLocalFunctionTests
    {
        public RoslynExPdbLocalFunctionTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbSourceLinkTests : PDBSourceLinkTests
    {
        public RoslynExPdbSourceLinkTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbTests : PDBTests
    {
        public RoslynExPdbTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbTupleTests : PDBTupleTests
    {
        public RoslynExPdbTupleTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbUsingTests : PDBUsingTests
    {
        public RoslynExPdbUsingTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPdbWinMdExpTests : PDBWinMdExpTests
    {
        public RoslynExPdbWinMdExpTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    public class RoslynExPortablePdbTests : PortablePdbTests
    {
        public RoslynExPortablePdbTests() => PdbValidation.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            PdbValidation.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
