// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

internal abstract partial class AbstractSnippetFunction : IVsExpansionFunction
{
    int IVsExpansionFunction.GetDefaultValue(out string bstrValue, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")] out int fHasDefaultValue)
        => GetDefaultValue(CancellationToken.None, out bstrValue, out fHasDefaultValue);

    int IVsExpansionFunction.GetCurrentValue(out string bstrValue, out int fHasCurrentValue)
        => GetCurrentValue(CancellationToken.None, out bstrValue, out fHasCurrentValue);

    int IVsExpansionFunction.FieldChanged(string bstrField, out int fRequeryFunction)
        => FieldChanged(bstrField, out fRequeryFunction);

    int IVsExpansionFunction.GetFunctionType(out uint funcType)
    {
        funcType = (int)_ExpansionFunctionType.eft_Value;
        return VSConstants.S_OK;
    }

    int IVsExpansionFunction.GetListCount(out int count)
    {
        count = 0;
        return VSConstants.S_OK;
    }

    int IVsExpansionFunction.GetListText(int index, out string text)
    {
        text = string.Empty;
        return VSConstants.E_NOTIMPL;
    }

    int IVsExpansionFunction.ReleaseFunction()
    {
        return VSConstants.S_OK;
    }
}
