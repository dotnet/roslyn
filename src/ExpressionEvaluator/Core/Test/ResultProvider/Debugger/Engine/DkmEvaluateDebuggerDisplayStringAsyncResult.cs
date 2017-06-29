// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// D:\Roslyn\Main\Open\Binaries\Debug\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public struct DkmEvaluateDebuggerDisplayStringAsyncResult
    {
        private readonly string _result;

        public DkmEvaluateDebuggerDisplayStringAsyncResult(string result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            _result = result;
        }

        public string Result { get { return _result; } }
    }
}
