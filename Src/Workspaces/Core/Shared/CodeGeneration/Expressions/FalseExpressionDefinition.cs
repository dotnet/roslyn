#if false
namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class FalseExpressionDefinition : ConstantExpressionDefinition
    {
        public FalseExpressionDefinition()
            : base(false)
        {
        }

        protected override CodeDefinition Clone()
        {
            return new FalseExpressionDefinition();
        }
    }
}
#endif