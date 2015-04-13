// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;

namespace Microsoft.VisualStudio.Debugger
{
    public delegate void DkmCompletionRoutine<TResult>(TResult result);

    public static class DkmComponentManager
    {
        public static bool ReportCurrentNonFatalException(Exception currentException, string implementationName)
        {
            return true;
        }
    }

    public enum DkmDataCreationDisposition
    {
        CreateNew,
        CreateAlways
    }
}

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public enum DkmEvaluationResultAccessType
    {
        None = 0,
        Public,
        Private,
        Protected,
        Internal,
    }

    public enum DkmEvaluationResultStorageType { None = 0 }
    public enum DkmEvaluationResultTypeModifierFlags { None = 0 }
    public class DkmDataAddress { }
}

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    public class DkmCompiledClrInspectionQuery { }
}

namespace Microsoft.VisualStudio.Debugger.CallStack
{
    public class DkmStackWalkFrame { }
}
