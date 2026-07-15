// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class SynthesizedCollectionExpansion : Expansion
    {
        internal static Expansion? CreateExpansion(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            TypeAndCustomInfo declaredTypeAndInfo,
            DkmClrValue value,
            SynthesizedCollectionKind kind)
        {
            Debug.Assert((inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoExpansion) == 0);
            Debug.Assert(!value.IsNull);
            Debug.Assert((inspectionContext.EvaluationFlags & DkmEvaluationFlags.ShowValueRaw) == 0);

            var elementCustomTypeInfo = GetElementCustomTypeInfo(declaredTypeAndInfo);
            var elementTypeAndInfo = new TypeAndCustomInfo(
                value.Type.GenericArguments[0],
                elementCustomTypeInfo);

            DkmClrValue? singleElementValue = null;
            EvalResult? items = null;
            int itemCount;

            switch (kind)
            {
                case SynthesizedCollectionKind.SingleElement:
                    singleElementValue = value.GetFieldValue(WellKnownGeneratedNames.SynthesizedReadOnlyList_SingleElementFieldName, inspectionContext);
                    itemCount = 1;
                    break;

                case SynthesizedCollectionKind.Array:
                    {
                        var itemsValue = value.GetFieldValue(WellKnownGeneratedNames.SynthesizedReadOnlyList_ItemsFieldName, inspectionContext);
                        var dimensions = itemsValue.ArrayDimensions;
                        if (dimensions is not { Count: 1 })
                        {
                            return null;
                        }

                        itemCount = dimensions[0];
                        items = CreateItemsDataItem(
                            resultProvider,
                            inspectionContext,
                            elementCustomTypeInfo,
                            itemsValue,
                            itemCount,
                            expansionFlags: ExpansionFlags.IncludeBaseMembers);
                        break;
                    }

                case SynthesizedCollectionKind.List:
                    {
                        var itemsValue = value.GetFieldValue(WellKnownGeneratedNames.SynthesizedReadOnlyList_ItemsFieldName, inspectionContext);
                        var countValue = itemsValue.GetPropertyValue("Count", inspectionContext);
                        if (countValue.IsError() || countValue.HasExceptionThrown() || countValue.HostObjectValue is not int count)
                        {
                            return null;
                        }

                        itemCount = count;
                        items = CreateItemsDataItem(
                            resultProvider,
                            inspectionContext,
                            elementCustomTypeInfo,
                            itemsValue,
                            itemCount,
                            expansionFlags: ExpansionFlags.All);
                        break;
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }

            if (itemCount > 0 && items is { Expansion: null })
            {
                return null;
            }

            return new SynthesizedCollectionExpansion(
                elementTypeAndInfo,
                singleElementValue,
                items,
                itemCount);
        }

        private static DkmClrCustomTypeInfo? GetElementCustomTypeInfo(TypeAndCustomInfo declaredTypeAndInfo)
        {
            if (declaredTypeAndInfo.Info is null)
            {
                return null;
            }

            var declaredType = declaredTypeAndInfo.Type;
            if (declaredType is not { IsGenericType: true })
            {
                return null;
            }

            if (declaredType.GetGenericTypeDefinition().GetGenericArguments().Length != 1)
            {
                return null;
            }

            return CustomTypeInfo.SkipOne(declaredTypeAndInfo.Info);
        }

        private static DkmClrCustomTypeInfo? GetCollectionCustomTypeInfo(DkmClrCustomTypeInfo? elementCustomTypeInfo)
        {
            if (elementCustomTypeInfo is null)
            {
                return null;
            }

            CustomTypeInfo.Decode(
                elementCustomTypeInfo.PayloadTypeId,
                elementCustomTypeInfo.Payload,
                out var elementDynamicFlags,
                out var tupleElementNames);

            if (elementDynamicFlags is null)
            {
                return CustomTypeInfo.Create(dynamicFlags: null, tupleElementNames: tupleElementNames);
            }

            var builder = ArrayBuilder<bool>.GetInstance();
            builder.Add(false);
            DynamicFlagsCustomTypeInfo.CopyTo(elementDynamicFlags, builder);
            var dynamicFlags = DynamicFlagsCustomTypeInfo.ToBytes(builder);
            builder.Free();

            return CustomTypeInfo.Create(dynamicFlags, tupleElementNames);
        }

        private static EvalResult? CreateItemsDataItem(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            DkmClrCustomTypeInfo? elementCustomTypeInfo,
            DkmClrValue itemsValue,
            int itemCount,
            ExpansionFlags expansionFlags)
        {
            if (itemCount == 0)
            {
                return null;
            }

            return resultProvider.CreateDataItem(
                inspectionContext,
                WellKnownGeneratedNames.SynthesizedReadOnlyList_ItemsFieldName,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: new TypeAndCustomInfo(itemsValue.Type, GetCollectionCustomTypeInfo(elementCustomTypeInfo)),
                value: itemsValue,
                useDebuggerDisplay: false,
                expansionFlags: expansionFlags,
                childShouldParenthesize: false,
                fullName: null,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: itemsValue.EvalFlags,
                evalFlags: DkmEvaluationFlags.None,
                canFavorite: false,
                isFavorite: false,
                supportsFavorites: false);
        }

        private readonly TypeAndCustomInfo _elementTypeAndInfo;
        private readonly DkmClrValue? _singleElementValue;
        private readonly EvalResult? _items;
        private readonly int _itemCount;

        private SynthesizedCollectionExpansion(
            TypeAndCustomInfo elementTypeAndInfo,
            DkmClrValue? singleElementValue,
            EvalResult? items,
            int itemCount)
        {
            Debug.Assert(itemCount >= 0);
            Debug.Assert(itemCount == 0 || (singleElementValue is null) != (items is null));

            _elementTypeAndInfo = elementTypeAndInfo;
            _singleElementValue = singleElementValue;
            _items = items;
            _itemCount = itemCount;
        }

        internal override void GetRows(
            ResultProvider resultProvider,
            ArrayBuilder<EvalResult> rows,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent,
            DkmClrValue value,
            int startIndex,
            int count,
            bool visitAll,
            ref int index)
        {
            GetIntersection(startIndex, count, index, _itemCount, out var firstRequestedItemIndex, out var requestedItemCount);

            if (requestedItemCount > 0)
            {
                if (_singleElementValue is not null)
                {
                    rows.Add(CreateSingleElementRow(resultProvider, inspectionContext, parent));
                }
                else
                {
                    var items = _items;
                    Debug.Assert(items is { Expansion: { } });

                    var itemsIndex = 0;
                    items.Expansion.GetRows(
                        resultProvider,
                        rows,
                        inspectionContext,
                        items.ToDataItem(),
                        items.Value,
                        firstRequestedItemIndex - index,
                        requestedItemCount,
                        visitAll: false,
                        index: ref itemsIndex);
                }
            }

            index += _itemCount;

            if (InRange(startIndex, count, index))
            {
                rows.Add(CreateRawViewRow(resultProvider, inspectionContext, parent));
            }

            index++;
        }

        private EvalResult CreateSingleElementRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent)
        {
            var singleElementValue = _singleElementValue;
            Debug.Assert(singleElementValue is not null);

            var name = resultProvider.FullNameProvider.GetClrArrayIndexExpression(inspectionContext, ["0"]);
            return resultProvider.CreateDataItem(
                inspectionContext,
                name,
                typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                declaredTypeAndInfo: _elementTypeAndInfo,
                value: singleElementValue,
                useDebuggerDisplay: parent is not null,
                expansionFlags: ExpansionFlags.IncludeBaseMembers,
                childShouldParenthesize: false,
                fullName: null,
                formatSpecifiers: Formatter.NoFormatSpecifiers,
                category: DkmEvaluationResultCategory.Other,
                flags: singleElementValue.EvalFlags,
                evalFlags: DkmEvaluationFlags.None,
                canFavorite: false,
                isFavorite: false,
                supportsFavorites: true);
        }

        private EvalResult CreateRawViewRow(
            ResultProvider resultProvider,
            DkmInspectionContext inspectionContext,
            EvalResultDataItem parent)
        {
            var rawInspectionContext = inspectionContext.With(DkmEvaluationFlags.ShowValueRaw);
            return new EvalResult(
                ExpansionKind.RawView,
                parent.Name,
                default(TypeAndCustomInfo),
                parent.DeclaredTypeAndInfo,
                useDebuggerDisplay: false,
                value: parent.Value,
                displayValue: null,
                expansion: resultProvider.GetTypeExpansion(rawInspectionContext, parent.DeclaredTypeAndInfo, parent.Value, ExpansionFlags.IncludeBaseMembers, supportsFavorites: false),
                childShouldParenthesize: parent.ChildShouldParenthesize,
                fullName: parent.FullNameWithoutFormatSpecifiers,
                childFullNamePrefixOpt: parent.ChildFullNamePrefix,
                formatSpecifiers: Formatter.AddFormatSpecifier(parent.FormatSpecifiers, "raw"),
                category: DkmEvaluationResultCategory.Data,
                flags: parent.Value.EvalFlags | DkmEvaluationResultFlags.ReadOnly,
                editableValue: resultProvider.Formatter2.GetEditableValueString(parent.Value, rawInspectionContext, parent.DeclaredTypeAndInfo.Info),
                inspectionContext: inspectionContext);
        }
    }
}
