// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// D:\Roslyn\Main\Open\Binaries\Debug\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public readonly struct DkmEvaluateDebuggerDisplayStringAsyncResult
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
