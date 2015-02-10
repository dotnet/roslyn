// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Computes expansion of <see cref="DkmClrValue"/> instances.
    /// </summary>
    /// <remarks>
    /// This class provides implementation for the default ResultProvider component.
    /// </remarks>
    internal abstract class ResultProvider : IDkmClrResultProvider
    {
        internal readonly Formatter Formatter;

        static ResultProvider()
        {
            FatalError.Handler = FailFast.OnFatalException;
        }

        internal ResultProvider(Formatter formatter)
        {
            this.Formatter = formatter;
        }

        DkmEvaluationResult IDkmClrResultProvider.GetResult(DkmClrValue value, ReadOnlyCollection<string> formatSpecifiers, string resultName, string resultFullName)
        {
            var declaredType = value.DeclaredType; // See #1099981
            return GetResult(value, declaredType, formatSpecifiers, resultName, resultFullName);
        }

        internal DkmEvaluationResult GetResult(DkmClrValue value, DkmClrType declaredType, ReadOnlyCollection<string> formatSpecifiers, string resultName, string resultFullName)
        {
            // TODO: Use full name
            try
            {
                return GetRootResult(value, declaredType, resultName);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        DkmClrValue IDkmClrResultProvider.GetClrValue(DkmSuccessEvaluationResult evaluationResult)
        {
            try
            {
                var dataItem = evaluationResult.GetDataItem<EvalResultDataItem>();
                if (dataItem == null)
                {
                    return null;
                }

                return dataItem.Value;
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        void IDkmClrResultProvider.GetChildren(DkmEvaluationResult evaluationResult, DkmWorkList workList, int initialRequestSize, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmGetChildrenAsyncResult> completionRoutine)
        {
            var dataItem = evaluationResult.GetDataItem<EvalResultDataItem>();
            if (dataItem == null)
            {
                // We don't know about this result.  Call next implementation
                evaluationResult.GetChildren(workList, initialRequestSize, inspectionContext, completionRoutine);
                return;
            }

            completionRoutine(GetChildren(inspectionContext, evaluationResult, dataItem, initialRequestSize));
        }

        void IDkmClrResultProvider.GetItems(DkmEvaluationResultEnumContext enumContext, DkmWorkList workList, int startIndex, int count, DkmCompletionRoutine<DkmEvaluationEnumAsyncResult> completionRoutine)
        {
            try
            {
                var dataItem = enumContext.GetDataItem<EnumContextDataItem>();
                if (dataItem == null)
                {
                    // We don't know about this result.  Call next implementation
                    enumContext.GetItems(workList, startIndex, count, completionRoutine);
                    return;
                }

                completionRoutine(GetItems(enumContext.InspectionContext, dataItem.EvalResultDataItem, startIndex, count));
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        string IDkmClrResultProvider.GetUnderlyingString(DkmEvaluationResult result)
        {
            try
            {
                var dataItem = result.GetDataItem<EvalResultDataItem>();
                if (dataItem == null)
                {
                    // We don't know about this result.  Call next implementation
                    return result.GetUnderlyingString();
                }

                return dataItem.Value?.GetUnderlyingString();
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal static DkmEvaluationResult CreateEvaluationResult(
            DkmClrValue value,
            string name, // Reflects DebuggerDisplayAttribute
            string typeName, // Reflects DebuggerDisplayAttribute
            string display, // Reflects DebuggerDisplayAttribute
            EvalResultDataItem dataItem)
        {
            if (value.IsError())
            {
                // Evaluation failed
                return DkmFailedEvaluationResult.Create(
                    InspectionContext: value.InspectionContext,
                    StackFrame: value.StackFrame,
                    Name: name,
                    FullName: dataItem.FullName,
                    ErrorMessage: display,
                    Flags: dataItem.Flags,
                    Type: typeName,
                    DataItem: dataItem);
            }
            else
            {
                ReadOnlyCollection<DkmCustomUIVisualizerInfo> customUIVisualizers = null;

                if (!value.IsNull)
                {
                    DkmCustomUIVisualizerInfo[] customUIVisualizerInfo = value.Type.GetDebuggerCustomUIVisualizerInfo();
                    if (customUIVisualizerInfo != null)
                    {
                        customUIVisualizers = new ReadOnlyCollection<DkmCustomUIVisualizerInfo>(customUIVisualizerInfo);
                    }
                }

                // If the EvalResultData item doesn't specify a particular category, we'll just propagate DkmClrValue.Category,
                // which typically appears to be set to the default value ("Other").
                var category = (dataItem.Category != DkmEvaluationResultCategory.Other) ? dataItem.Category : value.Category;

                // Valid value
                return DkmSuccessEvaluationResult.Create(
                    InspectionContext: value.InspectionContext,
                    StackFrame: value.StackFrame,
                    Name: name,
                    FullName: dataItem.FullName,
                    Flags: dataItem.Flags,
                    Value: display,
                    EditableValue: dataItem.EditableValue,
                    Type: typeName,
                    Category: category,
                    Access: value.Access,
                    StorageType: value.StorageType,
                    TypeModifierFlags: value.TypeModifierFlags,
                    Address: value.Address,
                    CustomUIVisualizers: customUIVisualizers,
                    ExternalModules: null,
                    DataItem: dataItem);
            }
        }

        /// <returns>The qualified name (i.e. including containing types and namespaces) of a named,
        /// pointer, or array type followed by the qualified name of the actual runtime type, if it
        /// differs from the declared type.</returns>
        private static string GetTypeName(DkmInspectionContext inspectionContext, DkmClrType declaredType, DkmClrType runtimeType)
        {
            var declaredTypeName = inspectionContext.GetTypeName(declaredType);
            var declaredLmrType = declaredType.GetLmrType();
            var runtimeLmrType = runtimeType.GetLmrType();
            return declaredLmrType.Equals(runtimeLmrType) || declaredLmrType.IsPointer
                ? declaredTypeName
                : string.Format("{0} {{{1}}}", declaredTypeName, inspectionContext.GetTypeName(runtimeType));
        }

        internal EvalResultDataItem CreateDataItem(
            DkmInspectionContext inspectionContext,
            string name,
            Type typeDeclaringMember,
            Type declaredType,
            DkmClrValue value,
            EvalResultDataItem parent,
            ExpansionFlags expansionFlags,
            bool childShouldParenthesize,
            string fullName,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmEvaluationResultCategory category,
            DkmEvaluationResultFlags flags,
            DkmEvaluationFlags evalFlags)
        {
            if ((evalFlags & DkmEvaluationFlags.ShowValueRaw) != 0)
            {
                formatSpecifiers = Formatter.AddFormatSpecifier(formatSpecifiers, "raw");
            }

            Expansion expansion;
            // If the declared type is Nullable<T>, the value should
            // have no expansion if null, or be expanded as a T.
            var lmrNullableTypeArg = declaredType.GetNullableTypeArgument();
            if (lmrNullableTypeArg != null && !value.HasExceptionThrown(parent))
            {
                Debug.Assert(value.Type.GetProxyType() == null);

                var nullableValue = value.GetNullableValue();
                if (nullableValue == null)
                {
                    Debug.Assert(declaredType.Equals(value.Type.GetLmrType()));
                    // No expansion of "null".
                    expansion = null;
                }
                else
                {
                    value = nullableValue;
                    Debug.Assert(lmrNullableTypeArg.Equals(value.Type.GetLmrType())); // If this is not the case, add a test for includeRuntimeTypeIfNecessary.
                    expansion = this.GetTypeExpansion(inspectionContext, lmrNullableTypeArg, value, ExpansionFlags.IncludeResultsView);
                }
            }
            else if (value.IsError() || (inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoExpansion) != 0)
            {
                expansion = null;
            }
            else
            {
                expansion = DebuggerTypeProxyExpansion.CreateExpansion(
                    this,
                    inspectionContext,
                    name,
                    typeDeclaringMember,
                    declaredType,
                    value,
                    childShouldParenthesize,
                    fullName,
                    flags.Includes(DkmEvaluationResultFlags.ExceptionThrown) ? null : fullName,
                    formatSpecifiers,
                    flags,
                    this.Formatter.GetEditableValue(value));
                if (expansion == null)
                {
                    var expansionType = value.HasExceptionThrown(parent) ? value.Type.GetLmrType() : declaredType;
                    expansion = this.GetTypeExpansion(inspectionContext, expansionType, value, expansionFlags);
                }
            }

            return new EvalResultDataItem(
                name,
                typeDeclaringMember,
                declaredType,
                value,
                expansion,
                childShouldParenthesize,
                fullName,
                flags.Includes(DkmEvaluationResultFlags.ExceptionThrown) ? null : fullName,
                formatSpecifiers,
                category,
                flags,
                this.Formatter.GetEditableValue(value));
        }

        private DkmEvaluationResult GetRootResult(DkmClrValue value, DkmClrType declaredType, string resultName)
        {
            var type = value.Type.GetLmrType();
            if (type.IsTypeVariables())
            {
                var expansion = new TypeVariablesExpansion(type);
                var dataItem = new EvalResultDataItem(
                    resultName,
                    typeDeclaringMember: null,
                    declaredType: type,
                    value: value,
                    expansion: expansion,
                    childShouldParenthesize: false,
                    fullName: null,
                    childFullNamePrefixOpt: null,
                    formatSpecifiers: Formatter.NoFormatSpecifiers,
                    category: DkmEvaluationResultCategory.Data,
                    flags: DkmEvaluationResultFlags.ReadOnly,
                    editableValue: null);

                Debug.Assert(dataItem.Flags == (DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable));

                // Note: We're not including value.EvalFlags in Flags parameter
                // below (there shouldn't be a reason to do so).
                return DkmSuccessEvaluationResult.Create(
                    InspectionContext: value.InspectionContext,
                    StackFrame: value.StackFrame,
                    Name: Resources.TypeVariablesName,
                    FullName: dataItem.FullName,
                    Flags: dataItem.Flags,
                    Value: "",
                    EditableValue: null,
                    Type: "",
                    Category: dataItem.Category,
                    Access: value.Access,
                    StorageType: value.StorageType,
                    TypeModifierFlags: value.TypeModifierFlags,
                    Address: value.Address,
                    CustomUIVisualizers: null,
                    ExternalModules: null,
                    DataItem: dataItem);
            }
            else
            {
                var inspectionContext = value.InspectionContext;
                if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.ResultsOnly) != 0)
                {
                    return ResultsViewExpansion.CreateResultsOnly(resultName, declaredType, value, null, this.Formatter);
                }

                ReadOnlyCollection<string> formatSpecifiers;
                var fullName = this.Formatter.TrimAndGetFormatSpecifiers(resultName, out formatSpecifiers);
                var dataItem = CreateDataItem(
                    inspectionContext,
                    resultName,
                    typeDeclaringMember: null,
                    declaredType: declaredType.GetLmrType(),
                    value: value,
                    parent: null,
                    expansionFlags: ExpansionFlags.All,
                    childShouldParenthesize: this.Formatter.NeedsParentheses(fullName),
                    fullName: fullName,
                    formatSpecifiers: formatSpecifiers,
                    category: DkmEvaluationResultCategory.Other,
                    flags: value.EvalFlags,
                    evalFlags: inspectionContext.EvaluationFlags);
                return GetResult(dataItem, value.Type, declaredType, parent: null);
            }
        }

        internal DkmEvaluationResult GetResult(EvalResultDataItem dataItem, DkmClrType runtimeType, DkmClrType declaredType, EvalResultDataItem parent)
        {
            var value = dataItem.Value; // Value may have replaced (specifically, for Nullable<T>).

            string debuggerDisplayName;
            string debuggerDisplayValue;
            string debuggerDisplayType;
            value.GetDebuggerDisplayStrings(out debuggerDisplayName, out debuggerDisplayValue, out debuggerDisplayType);

            var name = dataItem.NameOpt;
            Debug.Assert(name != null);
            var typeDeclaringMember = dataItem.TypeDeclaringMember;

            // Note: Don't respect the debugger display name on the root element:
            //   1) In the Watch window, that's where the user's text goes.
            //   2) In the Locals window, that's where the local name goes.
            // Note: Dev12 respects the debugger display name in the Locals window,
            // but not in the Watch window, but we can't distinguish and this 
            // behavior seems reasonable.
            if (debuggerDisplayName != null && parent != null)
            {
                name = debuggerDisplayName;
            }
            else if (typeDeclaringMember != null)
            {
                if (typeDeclaringMember.IsInterface)
                {
                    var interfaceTypeName = this.Formatter.GetTypeName(typeDeclaringMember, escapeKeywordIdentifiers: true);
                    name = string.Format("{0}.{1}", interfaceTypeName, name);
                }
                else
                {
                    var pooled = PooledStringBuilder.GetInstance();
                    var builder = pooled.Builder;
                    builder.Append(name);
                    builder.Append(" (");
                    builder.Append(this.Formatter.GetTypeName(typeDeclaringMember));
                    builder.Append(')');
                    name = pooled.ToStringAndFree();
                }
            }

            string display;
            if (value.HasExceptionThrown(parent))
            {
                display = value.GetExceptionMessage(dataItem.FullNameWithoutFormatSpecifiers, this.Formatter);
            }
            else if (debuggerDisplayValue != null)
            {
                display = value.IncludeObjectId(debuggerDisplayValue);
            }
            else
            {
                display = value.GetValueString();
            }

            var typeName = debuggerDisplayType ?? GetTypeName(value.InspectionContext, declaredType, runtimeType);
            return CreateEvaluationResult(value, name, typeName, display, dataItem);
        }

        private DkmGetChildrenAsyncResult GetChildren(DkmInspectionContext inspectionContext, DkmEvaluationResult evaluationResult, EvalResultDataItem dataItem, int initialRequestSize)
        {
            var expansion = dataItem.Expansion;
            var builder = ArrayBuilder<DkmEvaluationResult>.GetInstance();
            int index = 0;
            if (expansion != null)
            {
                expansion.GetRows(this, builder, inspectionContext, dataItem, dataItem.Value, 0, initialRequestSize, visitAll: true, index: ref index);
            }
            var rows = builder.ToArrayAndFree();
            Debug.Assert(index >= rows.Length);
            Debug.Assert(initialRequestSize >= rows.Length);
            var enumContext = DkmEvaluationResultEnumContext.Create(index, evaluationResult.StackFrame, evaluationResult.InspectionContext, new EnumContextDataItem(dataItem));
            return new DkmGetChildrenAsyncResult(rows, enumContext);
        }

        private DkmEvaluationEnumAsyncResult GetItems(DkmInspectionContext inspectionContext, EvalResultDataItem dataItem, int startIndex, int count)
        {
            var expansion = dataItem.Expansion;
            var builder = ArrayBuilder<DkmEvaluationResult>.GetInstance();
            if (expansion != null)
            {
                int index = 0;
                expansion.GetRows(this, builder, inspectionContext, dataItem, dataItem.Value, startIndex, count, visitAll: false, index: ref index);
            }
            var rows = builder.ToArrayAndFree();
            Debug.Assert(count >= rows.Length);
            return new DkmEvaluationEnumAsyncResult(rows);
        }

        internal Expansion GetTypeExpansion(DkmInspectionContext inspectionContext, Type declaredType, DkmClrValue value, ExpansionFlags flags)
        {
            Debug.Assert(!declaredType.IsTypeVariables());

            if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.NoExpansion) != 0)
            {
                return null;
            }

            var runtimeType = value.Type.GetLmrType();

            // If the value is an array, expand the array elements.
            if (runtimeType.IsArray)
            {
                var sizes = value.ArrayDimensions;
                if (sizes == null)
                {
                    // Null array. No expansion.
                    return null;
                }
                var lowerBounds = value.ArrayLowerBounds;
                return ArrayExpansion.CreateExpansion(sizes, lowerBounds);
            }

            if (this.Formatter.IsPredefinedType(runtimeType))
            {
                return null;
            }

            if (declaredType.IsPointer)
            {
                return !value.IsNull ? new PointerDereferenceExpansion(declaredType.GetElementType()) : null;
            }

            if (value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown) &&
                runtimeType.IsEmptyResultsViewException())
            {
                // The value is an exception thrown expanding an empty
                // IEnumerable. Use the runtime type of the exception and
                // skip base types. (This matches the native EE behavior
                // to expose a single property from the exception.)
                flags &= ~ExpansionFlags.IncludeBaseMembers;
            }

            return MemberExpansion.CreateExpansion(declaredType, value, flags, TypeHelpers.IsVisibleMember, this.Formatter);
        }
    }
}
