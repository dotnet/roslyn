// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class RuntimeInspectionContext : InspectionContext
    {
        internal static readonly InspectionContext Empty = new EmptyInspectionContext();

        internal static InspectionContext Create(DkmInspectionContext inspectionContextOpt)
        {
            // InspectionContext will be null when compiling breakpoint conditions.
            return (inspectionContextOpt == null) ? Empty : new RuntimeInspectionContext(inspectionContextOpt);
        }

        private readonly DkmInspectionContext _inspectionContext;

        private RuntimeInspectionContext(DkmInspectionContext inspectionContext)
        {
            Debug.Assert(inspectionContext != null);
            _inspectionContext = inspectionContext;
        }

        internal override string GetExceptionTypeName()
        {
            return _inspectionContext.GetClrExceptionType(_inspectionContext.Thread);
        }

        internal override string GetStowedExceptionTypeName()
        {
            return _inspectionContext.GetClrStowedExceptionType();
        }

        internal override string GetReturnValueTypeName(int index)
        {
            return _inspectionContext.GetClrReturnValueType(index);
        }

        internal override string GetObjectTypeNameById(string id)
        {
            var runtime = (DkmClrRuntimeInstance)_inspectionContext.RuntimeInstance;
            return runtime.GetObjectTypeByAlias(id);
        }

        private sealed class EmptyInspectionContext : InspectionContext
        {
            internal override string GetExceptionTypeName()
            {
                return null;
            }

            internal override string GetStowedExceptionTypeName()
            {
                return null;
            }

            internal override string GetReturnValueTypeName(int index)
            {
                return null;
            }

            internal override string GetObjectTypeNameById(string id)
            {
                return null;
            }
        }
    }
}
