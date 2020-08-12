using Microsoft.CodeAnalysis;

namespace RoslynEx
{
    public interface ISourceTransformer
    {
        Compilation Execute(SourceTransformerContext context);
    }
}
