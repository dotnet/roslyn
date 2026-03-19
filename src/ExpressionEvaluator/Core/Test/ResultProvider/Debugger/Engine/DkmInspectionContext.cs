// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    //
    // Summary:
    //     Options and target context to use while performing the inspection operation.
    [Guid("0807c826-3338-dd99-2f3a-202ba8fb9da7")]
    public class DkmInspectionContext
    {
        public static DkmInspectionContext Create(
            DkmInspectionSession InspectionSession,
            DkmRuntimeInstance RuntimeInstance,
            DkmThread Thread,
            uint Timeout,
            DkmEvaluationFlags EvaluationFlags,
            DkmFuncEvalFlags FuncEvalFlags,
            uint Radix,
            DkmLanguage Language,
            DkmRawReturnValue ReturnValue,
            DkmCompiledVisualizationData AdditionalVisualizationData,
            DkmCompiledVisualizationDataPriority AdditionalVisualizationDataPriority,
            ReadOnlyCollection<DkmRawReturnValueContainer> ReturnValues)
        {
            return new DkmInspectionContext(InspectionSession, EvaluationFlags, Radix, RuntimeInstance);
        }

        internal DkmInspectionContext(DkmInspectionSession inspectionSession, DkmEvaluationFlags evaluationFlags, uint radix, DkmRuntimeInstance runtimeInstance)
        {
            this.InspectionSession = inspectionSession;
            this.EvaluationFlags = evaluationFlags;
            this.Radix = radix;
            this.RuntimeInstance = runtimeInstance ?? DkmClrRuntimeInstance.DefaultRuntime;
        }

        public readonly DkmInspectionSession InspectionSession;
        public readonly DkmRuntimeInstance RuntimeInstance;
        public readonly DkmThread Thread;
        public readonly uint Timeout;
        public readonly DkmEvaluationFlags EvaluationFlags;
        public readonly DkmFuncEvalFlags FuncEvalFlags;
        public readonly uint Radix;
        public readonly DkmLanguage Language;
        public readonly DkmRawReturnValue ReturnValue;
        public readonly DkmCompiledVisualizationData AdditionalVisualizationData;
        public readonly DkmCompiledVisualizationDataPriority AdditionalVisualizationDataPriority;
        public readonly ReadOnlyCollection<DkmRawReturnValueContainer> ReturnValues;

        public string GetTypeName(DkmClrType ClrType, DkmClrCustomTypeInfo CustomTypeInfo, ReadOnlyCollection<string> FormatSpecifiers)
        {
            return InspectionSession.InvokeFormatter(this, MethodId.GetTypeName, f => f.GetTypeName(this, ClrType, CustomTypeInfo, FormatSpecifiers));
        }

        public DkmInspectionContext WithProperties(uint Timeout, DkmEvaluationFlags EvaluationFlags, DkmFuncEvalFlags FuncEvalFlags, uint Radix)
        {
            return new DkmInspectionContext(
                this.InspectionSession,
                EvaluationFlags,
                Radix,
                this.RuntimeInstance);
        }
    }

    public enum DkmFuncEvalFlags
    {
    }

    public class DkmRawReturnValue
    {
    }

    public class DkmCompiledVisualizationData
    {
    }

    public class DkmCompiledVisualizationDataPriority
    {
    }

    public class DkmRawReturnValueContainer
    {
    }
}
