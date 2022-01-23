// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmSuccessEvaluationResult : DkmEvaluationResult
    {
        public readonly string Value;
        public readonly string EditableValue;
        public readonly DkmEvaluationResultCategory Category;
        public readonly DkmEvaluationResultAccessType Access;
        public readonly DkmEvaluationResultStorageType StorageType;
        public readonly DkmEvaluationResultTypeModifierFlags TypeModifierFlags;
        public readonly ReadOnlyCollection<DkmCustomUIVisualizerInfo> CustomUIVisualizers;

        private DkmSuccessEvaluationResult(
            DkmInspectionContext inspectionContext,
            DkmStackWalkFrame stackFrame,
            string name,
            string fullName,
            DkmEvaluationResultFlags flags,
            string value,
            string editableValue,
            string type,
            DkmEvaluationResultCategory category,
            DkmEvaluationResultAccessType access,
            DkmEvaluationResultStorageType storageType,
            DkmEvaluationResultTypeModifierFlags typeModifierFlags,
            DkmDataAddress address,
            ReadOnlyCollection<DkmCustomUIVisualizerInfo> customUIVisualizers,
            ReadOnlyCollection<DkmModuleInstance> externalModules,
            DkmDataItem dataItem) :
            base(inspectionContext, stackFrame, name, fullName, flags, type, dataItem)
        {
            this.Value = value;
            this.EditableValue = editableValue;
            this.Category = category;
            this.Access = access;
            this.StorageType = storageType;
            this.TypeModifierFlags = typeModifierFlags;
            this.CustomUIVisualizers = customUIVisualizers;
        }

        public static DkmSuccessEvaluationResult Create(
            DkmInspectionContext InspectionContext,
            DkmStackWalkFrame StackFrame,
            string Name,
            string FullName,
            DkmEvaluationResultFlags Flags,
            string Value,
            string EditableValue,
            string Type,
            DkmEvaluationResultCategory Category,
            DkmEvaluationResultAccessType Access,
            DkmEvaluationResultStorageType StorageType,
            DkmEvaluationResultTypeModifierFlags TypeModifierFlags,
            DkmDataAddress Address,
            ReadOnlyCollection<DkmCustomUIVisualizerInfo> CustomUIVisualizers,
            ReadOnlyCollection<DkmModuleInstance> ExternalModules,
            DkmDataItem DataItem)
        {
            return new DkmSuccessEvaluationResult(
                InspectionContext,
                StackFrame,
                Name,
                FullName,
                Flags,
                Value,
                EditableValue,
                Type,
                Category,
                Access,
                StorageType,
                TypeModifierFlags,
                Address,
                CustomUIVisualizers,
                ExternalModules,
                DataItem);
        }

        public DkmClrValue GetClrValue()
        {
            return InspectionContext.InspectionSession.InvokeResultProvider(this, MethodId.GetClrValue, r => r.GetClrValue(this));
        }
    }
}
