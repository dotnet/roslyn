using Roslyn.Utilities;

namespace Roslyn.Services.Formatting
{
    public partial class FormattingOperationsFactory
    {
        internal class AdjustSpacesOperation : IAdjustSpacesOperation
        {
            public AdjustSpacesOperation(int space, AdjustSpacesOption option)
            {
                Contract.ThrowIfFalse(space >= 0);

                this.Space = space;
                this.Option = option;
            }

            public int Space { get; private set; }
            public AdjustSpacesOption Option { get; private set; }
        }
    }
}
