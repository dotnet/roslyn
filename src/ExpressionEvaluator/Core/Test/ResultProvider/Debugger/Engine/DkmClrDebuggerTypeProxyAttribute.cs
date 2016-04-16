// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.VisualStudio.Debugger.Clr;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public class DkmClrDebuggerTypeProxyAttribute : DkmClrEvalAttribute
    {
        internal DkmClrDebuggerTypeProxyAttribute(DkmClrType proxyType) :
            base(null)
        {
            this.ProxyType = proxyType;
        }

        public readonly DkmClrType ProxyType;
    }
}
