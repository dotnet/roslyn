using System;
using Microsoft.CodeAnalysis;

[assembly: Caravela.Compiler.TransformerOrder("Caravela.Compiler.UnitTests.TransformerOrderTransformer2", "Caravela.Compiler.UnitTests.TransformerOrderTransformer1")]

namespace Caravela.Compiler.UnitTests
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
