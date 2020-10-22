// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
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
