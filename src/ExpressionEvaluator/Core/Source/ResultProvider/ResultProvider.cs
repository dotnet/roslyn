// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal delegate void CompletionRoutine();
    internal delegate void CompletionRoutine<TResult>(TResult result);

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

        void IDkmClrResultProvider.GetResult(DkmClrValue value, DkmWorkList workList, DkmClrType declaredType, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers, string resultName, string resultFullName, DkmCompletionRoutine<DkmEvaluationAsyncResult> completionRoutine)
        {
            // TODO: Use full name
            GetRootResultAndContinue(value, workList, declaredType, inspectionContext, resultName,
                result => ContinueWithExceptionHandling(
                    () => completionRoutine(new DkmEvaluationAsyncResult(result)),
                    e => completionRoutine(DkmEvaluationAsyncResult.CreateErrorResult(e))));
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

            var stackFrame = evaluationResult.StackFrame;
            GetChildrenAndContinue(dataItem, workList, stackFrame, initialRequestSize, inspectionContext, completionRoutine);
        }

        void IDkmClrResultProvider.GetItems(DkmEvaluationResultEnumContext enumContext, DkmWorkList workList, int startIndex, int count, DkmCompletionRoutine<DkmEvaluationEnumAsyncResult> completionRoutine)
        {
            var dataItem = enumContext.GetDataItem<EnumContextDataItem>();
            if (dataItem == null)
            {
                // We don't know about this result.  Call next implementation
                enumContext.GetItems(workList, startIndex, count, completionRoutine);
                return;
            }

            GetItemsAndContinue(dataItem.EvalResultDataItem, workList, startIndex, count, enumContext.InspectionContext, completionRoutine);
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

                return dataItem.Value?.GetUnderlyingString(result.InspectionContext);
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void CreateEvaluationResultAndContinue(EvalResultDataItem dataItem, DkmWorkList workList, DkmInspectionContext inspectionContext, DkmStackWalkFrame stackFrame, CompletionRoutine<DkmEvaluationResult> completionRoutine)
        {
            switch (dataItem.Kind)
            {
                case ExpansionKind.Error:
                    completionRoutine(DkmFailedEvaluationResult.Create(
                        inspectionContext,
                        StackFrame: stackFrame,
                        Name: dataItem.Name,
                        FullName: dataItem.FullName,
                        ErrorMessage: dataItem.DisplayValue,
                        Flags: DkmEvaluationResultFlags.None,
                        Type: null,
                        DataItem: null));
                    break;
                case ExpansionKind.NonPublicMembers:
                case ExpansionKind.StaticMembers:
                    completionRoutine(CreateEvaluationResult(
                        inspectionContext,
                        dataItem.Value,
                        dataItem.Name,
                        typeName: string.Empty,
                        display: null,
                        dataItem: dataItem));
                    break;
                case ExpansionKind.RawView:
                    completionRoutine(CreateEvaluationResult(
                        inspectionContext,
                        dataItem.Value,
                        Resources.RawView,
                        typeName: string.Empty,
                        display: null,
                        dataItem: dataItem));
                    break;
                case ExpansionKind.ResultsView:
                    completionRoutine(CreateEvaluationResult(
                        inspectionContext,
                        dataItem.Value,
                        dataItem.Name,
                        typeName: string.Empty,
                        display: Resources.ResultsViewValueWarning,
                        dataItem: dataItem));
                    break;
                case ExpansionKind.TypeVariables:
                    var value = dataItem.Value;
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        dataItem.Name,
                        dataItem.FullName,
                        dataItem.Flags,
                        dataItem.DisplayValue,
                        EditableValue: null,
                        Type: dataItem.DisplayValue,
                        Category: dataItem.Category,
                        Access: value.Access,
                        StorageType: value.StorageType,
                        TypeModifierFlags: value.TypeModifierFlags,
                        Address: value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: dataItem));
                    break;
                default:
                    Debug.Assert((dataItem.Kind == ExpansionKind.Default) || (dataItem.Kind == ExpansionKind.PointerDereference));
                    // This call will evaluate DebuggerDisplayAttributes.
                    GetResultAndContinue(
                        dataItem,
                        workList,
                        declaredType: DkmClrType.Create(dataItem.Value.Type.AppDomain, dataItem.DeclaredType),
                        inspectionContext: inspectionContext,
                        parent: dataItem.Parent,
                        completionRoutine: completionRoutine);
                    break;
            }
        }

        private static DkmEvaluationResult CreateEvaluationResult(
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            string name,
            string typeName,
            string display,
            EvalResultDataItem dataItem)
        {
            if (value.IsError())
            {
                // Evaluation failed
                return DkmFailedEvaluationResult.Create(
                    InspectionContext: inspectionContext,
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
                    InspectionContext: inspectionContext,
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

        /// <returns>
        /// The qualified name (i.e. including containing types and namespaces) of a named, pointer,
        /// or array type followed by the qualified name of the actual runtime type, if provided.
        /// </returns>
        private static string GetTypeName(DkmInspectionContext inspectionContext, DkmClrValue value, DkmClrType declaredType, ExpansionKind kind)
        {
            var declaredLmrType = declaredType.GetLmrType();
            var runtimeType = value.Type;
            var runtimeLmrType = runtimeType.GetLmrType();
            var includeRuntimeTypeName =
                !declaredLmrType.Equals(runtimeLmrType) &&
                !declaredLmrType.IsPointer &&
                (kind != ExpansionKind.PointerDereference) &&
                (!declaredLmrType.IsNullable() || value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown));
            var declaredTypeName = inspectionContext.GetTypeName(declaredType, Formatter.NoFormatSpecifiers);
            return includeRuntimeTypeName ?
                string.Format("{0} {{{1}}}", declaredTypeName, inspectionContext.GetTypeName(runtimeType, Formatter.NoFormatSpecifiers)) :
                declaredTypeName;
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

                DkmClrValue nullableValue;
                if (value.IsError())
                {
                    expansion = null;
                }
                else if ((nullableValue = value.GetNullableValue(inspectionContext)) == null)
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
                    this.Formatter.GetEditableValue(value, inspectionContext));
                if (expansion == null)
                {
                    var expansionType = value.HasExceptionThrown(parent) ? value.Type.GetLmrType() : declaredType;
                    expansion = this.GetTypeExpansion(inspectionContext, expansionType, value, expansionFlags);
                }
            }

            return new EvalResultDataItem(
                ExpansionKind.Default,
                name,
                typeDeclaringMember,
                declaredType,
                parent: parent,
                value: value,
                displayValue: null,
                expansion: expansion,
                childShouldParenthesize: childShouldParenthesize,
                fullName: fullName,
                childFullNamePrefixOpt: flags.Includes(DkmEvaluationResultFlags.ExceptionThrown) ? null : fullName,
                formatSpecifiers: formatSpecifiers,
                category: category,
                flags: flags,
                editableValue: this.Formatter.GetEditableValue(value, inspectionContext),
                inspectionContext: inspectionContext);
        }

        private void GetRootResultAndContinue(DkmClrValue value, DkmWorkList workList, DkmClrType declaredType, DkmInspectionContext inspectionContext, string resultName, CompletionRoutine<DkmEvaluationResult> completionRoutine)
        {
            var type = value.Type.GetLmrType();
            if (type.IsTypeVariables())
            {
                var expansion = new TypeVariablesExpansion(type);
                var dataItem = new EvalResultDataItem(
                    ExpansionKind.Default,
                    resultName,
                    typeDeclaringMember: null,
                    declaredType: type,
                    parent: null,
                    value: value,
                    displayValue: null,
                    expansion: expansion,
                    childShouldParenthesize: false,
                    fullName: null,
                    childFullNamePrefixOpt: null,
                    formatSpecifiers: Formatter.NoFormatSpecifiers,
                    category: DkmEvaluationResultCategory.Data,
                    flags: DkmEvaluationResultFlags.ReadOnly,
                    editableValue: null,
                    inspectionContext: inspectionContext);

                Debug.Assert(dataItem.Flags == (DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.Expandable));

                // Note: We're not including value.EvalFlags in Flags parameter
                // below (there shouldn't be a reason to do so).
                completionRoutine(DkmSuccessEvaluationResult.Create(
                    InspectionContext: inspectionContext,
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
                    DataItem: dataItem));
            }
            else
            {
                if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.ResultsOnly) != 0)
                {
                    CreateEvaluationResultAndContinue(
                        ResultsViewExpansion.CreateResultsOnlyRow(inspectionContext, resultName, declaredType, value, null, this.Formatter),
                        workList,
                        inspectionContext,
                        value.StackFrame,
                        completionRoutine);
                }
                else
                {
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
                    GetResultAndContinue(dataItem, workList, declaredType, inspectionContext, parent: null, completionRoutine: completionRoutine);
                }
            }
        }

        internal void GetResultAndContinue(EvalResultDataItem dataItem, DkmWorkList workList, DkmClrType declaredType, DkmInspectionContext inspectionContext, EvalResultDataItem parent, CompletionRoutine<DkmEvaluationResult> completionRoutine)
        {
            var value = dataItem.Value; // Value may have been replaced (specifically, for Nullable<T>).
            DebuggerDisplayInfo displayInfo;
            if (value.TryGetDebuggerDisplayInfo(out displayInfo))
            {
                var targetType = displayInfo.TargetType;
                var attribute = displayInfo.Attribute;
                CompletionRoutine<Exception> onException =
                    e => completionRoutine(CreateEvaluationResultFromException(e, dataItem, inspectionContext));

                value.EvaluateDebuggerDisplayStringAndContinue(workList, inspectionContext, targetType, attribute.Name,
                    displayName => ContinueWithExceptionHandling(
                        () => value.EvaluateDebuggerDisplayStringAndContinue(workList, inspectionContext, targetType, attribute.Value,
                                displayValue => ContinueWithExceptionHandling(
                                    () => value.EvaluateDebuggerDisplayStringAndContinue(workList, inspectionContext, targetType, attribute.TypeName,
                                            displayType => ContinueWithExceptionHandling(
                                                () => completionRoutine(GetResult(inspectionContext, dataItem, declaredType, displayName.Result, displayValue.Result, displayType.Result, parent)),
                                                onException)),
                                    onException)),
                        onException));
            }
            else
            {
                completionRoutine(GetResult(inspectionContext, dataItem, declaredType, displayName: null, displayValue: null, displayType: null, parent: parent));
            }
        }

        private DkmEvaluationResult GetResult(DkmInspectionContext inspectionContext, EvalResultDataItem dataItem, DkmClrType declaredType, string displayName, string displayValue, string displayType, EvalResultDataItem parent)
        {
            var name = dataItem.Name;
            Debug.Assert(name != null);
            var typeDeclaringMember = dataItem.TypeDeclaringMember;

            // Note: Don't respect the debugger display name on the root element:
            //   1) In the Watch window, that's where the user's text goes.
            //   2) In the Locals window, that's where the local name goes.
            // Note: Dev12 respects the debugger display name in the Locals window,
            // but not in the Watch window, but we can't distinguish and this 
            // behavior seems reasonable.
            if (displayName != null && parent != null)
            {
                name = displayName;
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

            var value = dataItem.Value;
            string display;
            if (value.HasExceptionThrown(parent))
            {
                display = dataItem.DisplayValue ?? value.GetExceptionMessage(dataItem.FullNameWithoutFormatSpecifiers, this.Formatter);
            }
            else if (displayValue != null)
            {
                display = value.IncludeObjectId(displayValue);
            }
            else
            {
                display = value.GetValueString(inspectionContext, Formatter.NoFormatSpecifiers);
            }

            var typeName = displayType ?? GetTypeName(inspectionContext, value, declaredType, dataItem.Kind);

            return CreateEvaluationResult(inspectionContext, value, name, typeName, display, dataItem);
        }

        private void GetChildrenAndContinue(EvalResultDataItem dataItem, DkmWorkList workList, DkmStackWalkFrame stackFrame, int initialRequestSize, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmGetChildrenAsyncResult> completionRoutine)
        {
            var expansion = dataItem.Expansion;
            var rows = ArrayBuilder<EvalResultDataItem>.GetInstance();
            int index = 0;
            if (expansion != null)
            {
                expansion.GetRows(this, rows, inspectionContext, dataItem, dataItem.Value, 0, initialRequestSize, visitAll: true, index: ref index);
            }
            var numRows = rows.Count;
            Debug.Assert(index >= numRows);
            Debug.Assert(initialRequestSize >= numRows);
            var initialChildren = new DkmEvaluationResult[numRows];
            GetEvaluationResultsAndContinue(rows, initialChildren, 0, numRows, workList, inspectionContext, stackFrame,
                () => ContinueWithExceptionHandling(
                    () =>
                    {
                        var enumContext = DkmEvaluationResultEnumContext.Create(index, stackFrame, inspectionContext, new EnumContextDataItem(dataItem));
                        completionRoutine(new DkmGetChildrenAsyncResult(initialChildren, enumContext));
                        rows.Free();
                    },
                    e => completionRoutine(DkmGetChildrenAsyncResult.CreateErrorResult(e))));
        }

        private void GetItemsAndContinue(EvalResultDataItem dataItem, DkmWorkList workList, int startIndex, int count, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmEvaluationEnumAsyncResult> completionRoutine)
        {
            var expansion = dataItem.Expansion;
            var value = dataItem.Value;
            var rows = ArrayBuilder<EvalResultDataItem>.GetInstance();
            if (expansion != null)
            {
                int index = 0;
                expansion.GetRows(this, rows, inspectionContext, dataItem, value, startIndex, count, visitAll: false, index: ref index);
            }
            var numRows = rows.Count;
            Debug.Assert(count >= numRows);
            var results = new DkmEvaluationResult[numRows];
            GetEvaluationResultsAndContinue(rows, results, 0, numRows, workList, inspectionContext, value.StackFrame,
                () => ContinueWithExceptionHandling(
                    () =>
                    {
                        completionRoutine(new DkmEvaluationEnumAsyncResult(results));
                        rows.Free();
                    },
                    e => completionRoutine(DkmEvaluationEnumAsyncResult.CreateErrorResult(e))));
        }

        private void StoreResultAndContinue(DkmEvaluationResult result, ArrayBuilder<EvalResultDataItem> rows, DkmEvaluationResult[] results, int index, int numRows, DkmWorkList workList, DkmInspectionContext inspectionContext, DkmStackWalkFrame stackFrame, CompletionRoutine completionRoutine)
        {
            results[index] = result;
            index++;
            if (index < numRows)
            {
                GetEvaluationResultsAndContinue(rows, results, index, numRows, workList, inspectionContext, stackFrame, completionRoutine);
            }
            else
            {
                completionRoutine();
            }
        }

        private void GetEvaluationResultsAndContinue(ArrayBuilder<EvalResultDataItem> rows, DkmEvaluationResult[] results, int index, int numRows, DkmWorkList workList, DkmInspectionContext inspectionContext, DkmStackWalkFrame stackFrame, CompletionRoutine completionRoutine)
        {
            if (index >= numRows)
            {
                completionRoutine();
            }
            else
            {
                CreateEvaluationResultAndContinue(rows[index], workList, inspectionContext, stackFrame,
                    result => ContinueWithExceptionHandling(
                        () => StoreResultAndContinue(result, rows, results, index, numRows, workList, inspectionContext, stackFrame, completionRoutine),
                        e =>
                        {
                            // If we fail to store a result, just stop enumerating rows (rather than attempting to store
                            // an error message in the current row, etc).  This is because it may have been the act of
                            // indexing into the results store that threw (so it would just throw again here).  The user
                            // experience is less than ideal (there's no real indication that something went wrong),
                            // however, it seems like it's better to enumerate/display some rows than none.  We will
                            // receive a non-fatal Watson report in this case, so the problem won't go unnoticed.
                            completionRoutine();
                        }));
            }
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

            return MemberExpansion.CreateExpansion(inspectionContext, declaredType, value, flags, TypeHelpers.IsVisibleMember, this.Formatter);
        }

        private static DkmEvaluationResult CreateEvaluationResultFromException(Exception e, EvalResultDataItem dataItem, DkmInspectionContext inspectionContext)
        {
            return DkmFailedEvaluationResult.Create(
                inspectionContext,
                dataItem.Value.StackFrame,
                Name: dataItem.Name,
                FullName: null,
                ErrorMessage: e.Message,
                Flags: DkmEvaluationResultFlags.None,
                Type: null,
                DataItem: null);
        }

        internal static void ContinueWithExceptionHandling(CompletionRoutine onCompleted, CompletionRoutine<Exception> onException)
        {
            try
            {
                onCompleted();
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.ReportNonFatalException(e, DkmComponentManager.ReportCurrentNonFatalException))
            {
                onException(e);
            }
        }
    }
}
