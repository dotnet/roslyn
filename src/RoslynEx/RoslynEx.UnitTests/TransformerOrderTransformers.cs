using System;
using Microsoft.CodeAnalysis;

[assembly: RoslynEx.TransformerOrder("RoslynEx.UnitTests.TransformerOrderTransformer2", "RoslynEx.UnitTests.TransformerOrderTransformer1")]

namespace RoslynEx.UnitTests
{
    abstract class TransformerOrderTransformer : ISourceTransformer
    {
        public Compilation Execute(TransformerContext context) => throw new Exception();
    }

    [Transformer]
    class TransformerOrderTransformer1 : TransformerOrderTransformer { }

    [Transformer]
    class TransformerOrderTransformer2 : TransformerOrderTransformer { }
}
