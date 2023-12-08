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
    public struct DkmEvaluationAsyncResult
    {
        private readonly DkmEvaluationResult _result;

        public DkmEvaluationAsyncResult(DkmEvaluationResult Result)
            : this()
        {
            if (Result == null)
            {
                throw new ArgumentNullException(nameof(Result));
            }

            _result = Result;
        }

        public int ErrorCode { get { throw new NotImplementedException(); } }

        public readonly DkmEvaluationResult Result { get { return _result; } }

        internal Exception Exception { get; set; }

        public static DkmEvaluationAsyncResult CreateErrorResult(Exception exception)
        {
            return new DkmEvaluationAsyncResult() { Exception = exception };
        }
    }
}
