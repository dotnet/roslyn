// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.VisualStudio.Debugger.ComponentInterfaces
{
    public interface IDkmClrFormatter2
    {
        string GetValueString(DkmClrValue clrValue, DkmClrCustomTypeInfo customTypeInfo, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers);
        string GetEditableValueString(DkmClrValue value, DkmInspectionContext inspectionContext, DkmClrCustomTypeInfo customTypeInfo);
    }
}
