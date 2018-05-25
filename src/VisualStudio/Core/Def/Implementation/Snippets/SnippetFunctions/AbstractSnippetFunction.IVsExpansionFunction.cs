// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract partial class AbstractSnippetFunction : IVsExpansionFunction
    {
        int IVsExpansionFunction.GetDefaultValue(out string bstrValue, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.BOOL")]out int fHasDefaultValue)
        {
            return GetDefaultValue(CancellationToken.None, out bstrValue, out fHasDefaultValue);
        }

        int IVsExpansionFunction.GetCurrentValue(out string bstrValue, out int fHasCurrentValue)
        {
            return GetCurrentValue(CancellationToken.None, out bstrValue, out fHasCurrentValue);
        }

        int IVsExpansionFunction.FieldChanged(string bstrField, out int fRequeryFunction)
        {
            return FieldChanged(bstrField, out fRequeryFunction);
        }

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
            index = 0;
            text = string.Empty;
            return VSConstants.E_NOTIMPL;
        }

        int IVsExpansionFunction.ReleaseFunction()
        {
            snippetExpansionClient = null;
            return VSConstants.S_OK;
        }
    }
}
