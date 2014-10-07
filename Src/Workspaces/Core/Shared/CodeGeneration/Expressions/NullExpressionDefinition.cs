#if false
namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class NullExpressionDefinition : ConstantExpressionDefinition
    {
        public NullExpressionDefinition()
            : base(null)
        {
        }

        protected override CodeDefinition Clone()
        {
            return new NullExpressionDefinition();
        }
    }
}
#endif