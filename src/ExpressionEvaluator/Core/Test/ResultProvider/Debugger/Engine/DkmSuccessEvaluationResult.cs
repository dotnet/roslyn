// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.CallStack;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmSuccessEvaluationResult : DkmEvaluationResult
    {
        public DkmEvaluationResultCategory Category { get; private set; }
        public string EditableValue { get; private set; }
        public DkmEvaluationResultFlags Flags { get; private set; }
        public string Type { get; private set; }
        public string Value { get; private set; }
        public ReadOnlyCollection<DkmCustomUIVisualizerInfo> CustomUIVisualizers { get; private set; }

        public static DkmSuccessEvaluationResult Create(DkmInspectionContext InspectionContext, DkmStackWalkFrame StackFrame, string Name, string FullName, DkmEvaluationResultFlags Flags, string Value, string EditableValue, string Type, DkmEvaluationResultCategory Category, DkmEvaluationResultAccessType Access, DkmEvaluationResultStorageType StorageType, DkmEvaluationResultTypeModifierFlags TypeModifierFlags, DkmDataAddress Address, ReadOnlyCollection<DkmCustomUIVisualizerInfo> CustomUIVisualizers, ReadOnlyCollection<DkmModuleInstance> ExternalModules, DkmDataItem DataItem)
        {
            DkmSuccessEvaluationResult result = new DkmSuccessEvaluationResult
            {
                InspectionContext = InspectionContext,
                Name = Name,
                FullName = FullName,
                Flags = Flags,
                Value = Value,
                Type = Type,
                Category = Category,
                EditableValue = EditableValue,
                CustomUIVisualizers = CustomUIVisualizers
            };

            if (DataItem != null)
            {
                result.SetDataItem(DkmDataCreationDisposition.CreateNew, DataItem);
            }

            return result;
        }
    }
}
