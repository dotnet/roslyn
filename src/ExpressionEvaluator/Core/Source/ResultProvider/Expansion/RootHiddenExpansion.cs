// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class RootHiddenExpansion : Expansion
    {
        internal static Expansion CreateExpansion(
            MemberAndDeclarationInfo members,
            DynamicFlagsMap dynamicFlagsMap)
        {
            return new RootHiddenExpansion(members, dynamicFlagsMap);
        }

        private readonly MemberAndDeclarationInfo _member;
        private readonly DynamicFlagsMap _dynamicFlagsMap;

        internal RootHiddenExpansion(MemberAndDeclarationInfo member, DynamicFlagsMap dynamicFlagsMap)
        {
            _member = member;
            _dynamicFlagsMap = dynamicFlagsMap;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResultDataItem> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            var memberValue = value.GetMemberValue(_member, inspectionContext);
            var isDynamicDebugViewEmptyException = memberValue.Type.GetLmrType().IsDynamicDebugViewEmptyException();
            if (isDynamicDebugViewEmptyException || memberValue.IsError())
            {
                if (InRange(startIndex, count, index))
                {
                    if (isDynamicDebugViewEmptyException)
                    {
                        var emptyMember = memberValue.Type.GetMemberByName("Empty");
                        memberValue = memberValue.GetMemberValue(emptyMember, inspectionContext);
                    }
                    var row = new EvalResultDataItem(Resources.ErrorName, (string)memberValue.HostObjectValue);
                    rows.Add(row);
                }
                index++;
            }
            else
            {
                parent = MemberExpansion.CreateMemberDataItem(
                    resultProvider,
                    inspectionContext,
                    _member,
                    memberValue,
                    parent,
                    _dynamicFlagsMap,
                    ExpansionFlags.IncludeBaseMembers | ExpansionFlags.IncludeResultsView);
                var expansion = parent.Expansion;
                if (expansion != null)
                {
                    expansion.GetRows(resultProvider, rows, inspectionContext, parent, parent.Value, startIndex, count, visitAll, ref index);
                }
            }
        }
    }
}
