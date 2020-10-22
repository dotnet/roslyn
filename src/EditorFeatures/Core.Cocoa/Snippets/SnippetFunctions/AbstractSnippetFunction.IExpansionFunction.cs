using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor.Expansion;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract partial class AbstractSnippetFunction : IExpansionFunction
    {
        void IExpansionFunction.GetDefaultValue(out string value, out bool hasDefaultValue)
        {
            GetDefaultValue(CancellationToken.None, out value, out hasDefaultValue);
        }

        void IExpansionFunction.GetCurrentValue(out string value, out bool hasCurrentValue)
        {
            GetCurrentValue(CancellationToken.None, out value, out hasCurrentValue);
        }

        bool IExpansionFunction.FieldChanged(string field)
        {
            return FieldChanged(field);
        }

        ExpansionFunctionType IExpansionFunction.GetFunctionType()
        {
            return ExpansionFunctionType.Value;
        }

        int IExpansionFunction.GetListCount()
        {
            return 0;
        }

        string IExpansionFunction.GetListText(int index)
        {
            return string.Empty;
        }

        void IExpansionFunction.ReleaseFunction()
        {
        }
    }
}
