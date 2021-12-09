using System;
using Microsoft.CodeAnalysis;

[assembly: Metalama.Compiler.TransformerOrder("Metalama.Compiler.UnitTests.TransformerOrderTransformer2", "Metalama.Compiler.UnitTests.TransformerOrderTransformer1")]

namespace Metalama.Compiler.UnitTests
{
    abstract class TransformerOrderTransformer : ISourceTransformer
    {
        public void Execute(TransformerContext context) => throw new Exception();
    }

    [Transformer]
    class TransformerOrderTransformer1 : TransformerOrderTransformer { }

    [Transformer]
    class TransformerOrderTransformer2 : TransformerOrderTransformer { }
}
