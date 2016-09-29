// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// C:\Enlistments\Roslyn2\_Download\Concord\0.0.1+b140327.201033\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public class DkmClrDebuggerDisplayAttribute : DkmClrEvalAttribute
    {
        public string Name { get; internal set; }
        public string TypeName { get; internal set; }
        public string Value { get; internal set; }

        public DkmClrDebuggerDisplayAttribute(string targetMember)
            : base(targetMember)
        {
        }
    }
}
