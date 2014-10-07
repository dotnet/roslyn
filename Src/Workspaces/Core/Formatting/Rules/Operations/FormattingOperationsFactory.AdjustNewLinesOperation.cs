using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal class AdjustNewLinesOperation : IAdjustNewLinesOperation
        {
            public AdjustNewLinesOperation(int line, AdjustNewLinesOption option)
            {
                Contract.ThrowIfFalse(option != AdjustNewLinesOption.ForceLines || line > 0);
                Contract.ThrowIfFalse(option != AdjustNewLinesOption.PreserveLines || line >= 0);

                this.Line = line;
                this.Option = option;
            }

            public int Line { get; private set; }
            public AdjustNewLinesOption Option { get; private set; }
        }
    }
}
