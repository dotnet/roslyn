// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public enum DkmClrDebuggerBrowsableAttributeState
    {
        Never,
        Collapsed,
        RootHidden,
    }

    public class DkmClrDebuggerBrowsableAttribute : DkmClrEvalAttribute
    {
        internal DkmClrDebuggerBrowsableAttribute(string targetMember, DkmClrDebuggerBrowsableAttributeState state) :
            base(targetMember)
        {
            this.State = state;
        }

        public readonly DkmClrDebuggerBrowsableAttributeState State;
    }
}
