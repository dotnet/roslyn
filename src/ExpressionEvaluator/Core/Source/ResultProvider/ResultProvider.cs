// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#pragma warning disable CA1825 // Avoid zero-length array allocations.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
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
    public abstract class ResultProvider : IDkmClrResultProvider
    {
        static ResultProvider()
        {
            FatalError.Handler = FailFast.OnFatalException;
        }

        // Fields should be removed and replaced with calls through DkmInspectionContext.
        // (see https://github.com/dotnet/roslyn/issues/6899).
        internal readonly IDkmClrFormatter2 Formatter2;
        internal readonly IDkmClrFullNameProvider FullNameProvider;

        internal ResultProvider(IDkmClrFormatter2 formatter2, IDkmClrFullNameProvider fullNameProvider)
        {
            Formatter2 = formatter2;
            FullNameProvider = fullNameProvider;
        }

        internal abstract string StaticMembersString { get; }

        internal abstract bool IsPrimitiveType(Type type);

        void IDkmClrResultProvider.GetResult(DkmClrValue value, DkmWorkList workList, DkmClrType declaredType, DkmClrCustomTypeInfo declaredTypeInfo, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers, string resultName, string resultFullName, DkmCompletionRoutine<DkmEvaluationAsyncResult> completionRoutine)
        {
            if (formatSpecifiers == null)
            {
                formatSpecifiers = Formatter.NoFormatSpecifiers;
            }
            if (resultFullName != null)
            {
                ReadOnlyCollection<string> otherSpecifiers;
                resultFullName = FullNameProvider.GetClrExpressionAndFormatSpecifiers(inspectionContext, resultFullName, out otherSpecifiers);
                foreach (var formatSpecifier in otherSpecifiers)
                {
                    formatSpecifiers = Formatter.AddFormatSpecifier(formatSpecifiers, formatSpecifier);
                }
            }
            var wl = new WorkList(workList, e => completionRoutine(DkmEvaluationAsyncResult.CreateErrorResult(e)));
            wl.ContinueWith(
                () => GetRootResultAndContinue(
                    value,
                    wl,
                    declaredType,
                    declaredTypeInfo,
                    inspectionContext,
                    resultName,
                    resultFullName,
                    formatSpecifiers,
                    result => wl.ContinueWith(() => completionRoutine(new DkmEvaluationAsyncResult(result)))));
        }

        DkmClrValue IDkmClrResultProvider.GetClrValue(DkmSuccessEvaluationResult evaluationResult)
        {
            try
            {
                var dataItem = evaluationResult.GetDataItem<EvalResultDataItem>();
                if (dataItem == null)
                {
                    // We don't know about this result.  Call next implementation
                    return evaluationResult.GetClrValue();
                }

                return dataItem.Value;
            }
            catch (Exception e) when (ExpressionEvaluatorFatalError.CrashIfFailFastEnabled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal const DkmEvaluationFlags NotRoot = (DkmEvaluationFlags)0x20000;
        internal const DkmEvaluationFlags NoResults = (DkmEvaluationFlags)0x40000;

        void IDkmClrResultProvider.GetChildren(DkmEvaluationResult evaluationResult, DkmWorkList workList, int initialRequestSize, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmGetChildrenAsyncResult> completionRoutine)
        {
            var dataItem = evaluationResult.GetDataItem<EvalResultDataItem>();
            if (dataItem == null)
            {
                // We don't know about this result.  Call next implementation
                evaluationResult.GetChildren(workList, initialRequestSize, inspectionContext, completionRoutine);
                return;
            }

            var expansion = dataItem.Expansion;
            if (expansion == null)
            {
                var enumContext = DkmEvaluationResultEnumContext.Create(0, evaluationResult.StackFrame, inspectionContext, new EnumContextDataItem(evaluationResult));
                completionRoutine(new DkmGetChildrenAsyncResult(new DkmEvaluationResult[0], enumContext));
                return;
            }

            // Evaluate children with InspectionContext that is not the root.
            inspectionContext = inspectionContext.With(NotRoot);

            var rows = ArrayBuilder<EvalResult>.GetInstance();
            int index = 0;
            expansion.GetRows(this, rows, inspectionContext, dataItem, dataItem.Value, 0, initialRequestSize, visitAll: true, index: ref index);
            var numRows = rows.Count;
            Debug.Assert(index >= numRows);
            Debug.Assert(initialRequestSize >= numRows);
            var initialChildren = new DkmEvaluationResult[numRows];
            void onException(Exception e) => completionRoutine(DkmGetChildrenAsyncResult.CreateErrorResult(e));
            var wl = new WorkList(workList, onException);
            wl.ContinueWith(() =>
                GetEvaluationResultsAndContinue(evaluationResult, rows, initialChildren, 0, numRows, wl, inspectionContext,
                    () =>
                    wl.ContinueWith(
                        () =>
                        {
                            var enumContext = DkmEvaluationResultEnumContext.Create(index, evaluationResult.StackFrame, inspectionContext, new EnumContextDataItem(evaluationResult));
                            completionRoutine(new DkmGetChildrenAsyncResult(initialChildren, enumContext));
                            rows.Free();
                        }),
                    onException));
        }

        void IDkmClrResultProvider.GetItems(DkmEvaluationResultEnumContext enumContext, DkmWorkList workList, int startIndex, int count, DkmCompletionRoutine<DkmEvaluationEnumAsyncResult> completionRoutine)
        {
            var enumContextDataItem = enumContext.GetDataItem<EnumContextDataItem>();
            if (enumContextDataItem == null)
            {
                // We don't know about this result.  Call next implementation
                enumContext.GetItems(workList, startIndex, count, completionRoutine);
                return;
            }

            var evaluationResult = enumContextDataItem.Result;
            var dataItem = evaluationResult.GetDataItem<EvalResultDataItem>();
            var expansion = dataItem.Expansion;
            if (expansion == null)
            {
                completionRoutine(new DkmEvaluationEnumAsyncResult(new DkmEvaluationResult[0]));
                return;
            }

            var inspectionContext = enumContext.InspectionContext;

            var rows = ArrayBuilder<EvalResult>.GetInstance();
            int index = 0;
            expansion.GetRows(this, rows, inspectionContext, dataItem, dataItem.Value, startIndex, count, visitAll: false, index: ref index);
            var numRows = rows.Count;
            Debug.Assert(count >= numRows);
            var results = new DkmEvaluationResult[numRows];
            void onException(Exception e) => completionRoutine(DkmEvaluationEnumAsyncResult.CreateErrorResult(e));
            var wl = new WorkList(workList, onException);
            wl.ContinueWith(() =>
                GetEvaluationResultsAndContinue(evaluationResult, rows, results, 0, numRows, wl, inspectionContext,
                    () =>
                    wl.ContinueWith(
                        () =>
                        {
                            completionRoutine(new DkmEvaluationEnumAsyncResult(results));
                            rows.Free();
                        }),
                    onException));
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

        private void GetChild(
            DkmEvaluationResult parent,
            WorkList workList,
            EvalResult row,
            DkmCompletionRoutine<DkmEvaluationAsyncResult> completionRoutine)
        {
            var inspectionContext = row.InspectionContext;
            if ((row.Kind != ExpansionKind.Default) || (row.Value == null))
            {
                CreateEvaluationResultAndContinue(
                    row,
                    workList,
                    row.InspectionContext,
                    parent.StackFrame,
                    child => completionRoutine(new DkmEvaluationAsyncResult(child)));
            }
            else
            {
                var typeDeclaringMember = row.TypeDeclaringMemberAndInfo;
                var name = (typeDeclaringMember.Type == null) ?
                    row.Name :
                    GetQualifiedMemberName(row.InspectionContext, typeDeclaringMember, row.Name, FullNameProvider);
                row.Value.SetDataItem(DkmDataCreationDisposition.CreateAlways, new FavoritesDataItem(row.CanFavorite, row.IsFavorite));
                row.Value.GetResult(
                    workList.InnerWorkList,
                    row.DeclaredTypeAndInfo.ClrType,
                    row.DeclaredTypeAndInfo.Info,
                    row.InspectionContext,
                    Formatter.NoFormatSpecifiers,
                    name,
                    row.FullName,
                    result => workList.ContinueWith(() => completionRoutine(result)));
            }
        }

        private void CreateEvaluationResultAndContinue(EvalResult result, WorkList workList, DkmInspectionContext inspectionContext, DkmStackWalkFrame stackFrame, CompletionRoutine<DkmEvaluationResult> completionRoutine)
        {
            switch (result.Kind)
            {
                case ExpansionKind.Explicit:
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        Name: result.DisplayName,
                        FullName: result.FullName,
                        Flags: result.Flags,
                        Value: result.DisplayValue,
                        EditableValue: result.EditableValue,
                        Type: result.DisplayType,
                        Category: DkmEvaluationResultCategory.Data,
                        Access: DkmEvaluationResultAccessType.None,
                        StorageType: DkmEvaluationResultStorageType.None,
                        TypeModifierFlags: DkmEvaluationResultTypeModifierFlags.None,
                        Address: result.Value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: result.ToDataItem()));
                    break;
                case ExpansionKind.Error:
                    completionRoutine(DkmFailedEvaluationResult.Create(
                        inspectionContext,
                        StackFrame: stackFrame,
                        Name: result.Name,
                        FullName: result.FullName,
                        ErrorMessage: result.DisplayValue,
                        Flags: DkmEvaluationResultFlags.None,
                        Type: null,
                        DataItem: null));
                    break;
                case ExpansionKind.NativeView:
                    {
                        var value = result.Value;
                        var name = Resources.NativeView;
                        var fullName = result.FullName;
                        var display = result.Name;
                        DkmEvaluationResult evalResult;
                        if (value.IsError())
                        {
                            evalResult = DkmFailedEvaluationResult.Create(
                                inspectionContext,
                                stackFrame,
                                Name: name,
                                FullName: fullName,
                                ErrorMessage: display,
                                Flags: result.Flags,
                                Type: null,
                                DataItem: result.ToDataItem());
                        }
                        else
                        {
                            // For Native View, create a DkmIntermediateEvaluationResult.
                            // This will allow the C++ EE to take over expansion.
                            var process = inspectionContext.RuntimeInstance.Process;
                            var cpp = process.EngineSettings.GetLanguage(new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.Cpp));
                            evalResult = DkmIntermediateEvaluationResult.Create(
                                inspectionContext,
                                stackFrame,
                                Name: name,
                                FullName: fullName,
                                Expression: display,
                                IntermediateLanguage: cpp,
                                TargetRuntime: process.GetNativeRuntimeInstance(),
                                DataItem: result.ToDataItem());
                        }
                        completionRoutine(evalResult);
                    }
                    break;
                case ExpansionKind.NonPublicMembers:
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        Name: Resources.NonPublicMembers,
                        FullName: result.FullName,
                        Flags: result.Flags,
                        Value: null,
                        EditableValue: null,
                        Type: string.Empty,
                        Category: DkmEvaluationResultCategory.Data,
                        Access: DkmEvaluationResultAccessType.None,
                        StorageType: DkmEvaluationResultStorageType.None,
                        TypeModifierFlags: DkmEvaluationResultTypeModifierFlags.None,
                        Address: result.Value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: result.ToDataItem()));
                    break;
                case ExpansionKind.StaticMembers:
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        Name: StaticMembersString,
                        FullName: result.FullName,
                        Flags: result.Flags,
                        Value: null,
                        EditableValue: null,
                        Type: string.Empty,
                        Category: DkmEvaluationResultCategory.Class,
                        Access: DkmEvaluationResultAccessType.None,
                        StorageType: DkmEvaluationResultStorageType.None,
                        TypeModifierFlags: DkmEvaluationResultTypeModifierFlags.None,
                        Address: result.Value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: result.ToDataItem()));
                    break;
                case ExpansionKind.RawView:
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        Name: Resources.RawView,
                        FullName: result.FullName,
                        Flags: result.Flags,
                        Value: null,
                        EditableValue: result.EditableValue,
                        Type: string.Empty,
                        Category: DkmEvaluationResultCategory.Data,
                        Access: DkmEvaluationResultAccessType.None,
                        StorageType: DkmEvaluationResultStorageType.None,
                        TypeModifierFlags: DkmEvaluationResultTypeModifierFlags.None,
                        Address: result.Value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: result.ToDataItem()));
                    break;
                case ExpansionKind.DynamicView:
                case ExpansionKind.ResultsView:
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        result.Name,
                        result.FullName,
                        result.Flags,
                        result.DisplayValue,
                        EditableValue: null,
                        Type: string.Empty,
                        Category: DkmEvaluationResultCategory.Method,
                        Access: DkmEvaluationResultAccessType.None,
                        StorageType: DkmEvaluationResultStorageType.None,
                        TypeModifierFlags: DkmEvaluationResultTypeModifierFlags.None,
                        Address: result.Value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: result.ToDataItem()));
                    break;
                case ExpansionKind.TypeVariable:
                    completionRoutine(DkmSuccessEvaluationResult.Create(
                        inspectionContext,
                        stackFrame,
                        result.Name,
                        result.FullName,
                        result.Flags,
                        result.DisplayValue,
                        EditableValue: null,
                        Type: result.DisplayValue,
                        Category: DkmEvaluationResultCategory.Data,
                        Access: DkmEvaluationResultAccessType.None,
                        StorageType: DkmEvaluationResultStorageType.None,
                        TypeModifierFlags: DkmEvaluationResultTypeModifierFlags.None,
                        Address: result.Value.Address,
                        CustomUIVisualizers: null,
                        ExternalModules: null,
                        DataItem: result.ToDataItem()));
                    break;
                case ExpansionKind.PointerDereference:
                case ExpansionKind.Default:
                    // This call will evaluate DebuggerDisplayAttributes.
                    GetResultAndContinue(
                        result,
                        workList,
                        declaredType: result.DeclaredTypeAndInfo.ClrType,
                        declaredTypeInfo: result.DeclaredTypeAndInfo.Info,
                        inspectionContext: inspectionContext,
                        useDebuggerDisplay: result.UseDebuggerDisplay,
                        completionRoutine: completionRoutine);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(result.Kind);
            }
        }

        private static DkmEvaluationResult CreateEvaluationResult(
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            string name,
            string typeName,
            string display,
            EvalResult result)
        {
            if (value.IsError())
            {
                // Evaluation failed
                return DkmFailedEvaluationResult.Create(
                    InspectionContext: inspectionContext,
                    StackFrame: value.StackFrame,
                    Name: name,
                    FullName: result.FullName,
                    ErrorMessage: display,
                    Flags: result.Flags,
                    Type: typeName,
                    DataItem: result.ToDataItem());
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

                // If the EvalResultDataItem doesn't specify a particular category, we'll just propagate DkmClrValue.Category,
                // which typically appears to be set to the default value ("Other").
                var category = (result.Category != DkmEvaluationResultCategory.Other) ? result.Category : value.Category;

                var nullableMemberInfo = value.GetDataItem<NullableMemberInfo>();

                // Valid value
                return DkmSuccessEvaluationResult.Create(
                    InspectionContext: inspectionContext,
                    StackFrame: value.StackFrame,
                    Name: name,
                    FullName: result.FullName,
                    Flags: result.Flags,
                    Value: display,
                    EditableValue: result.EditableValue,
                    Type: typeName,
                    Category: nullableMemberInfo?.Category ?? category,
                    Access: nullableMemberInfo?.Access ?? value.Access,
                    StorageType: nullableMemberInfo?.StorageType ?? value.StorageType,
                    TypeModifierFlags: nullableMemberInfo?.TypeModifierFlags ?? value.TypeModifierFlags,
                    Address: value.Address,
                    CustomUIVisualizers: customUIVisualizers,
                    ExternalModules: null,
                    DataItem: result.ToDataItem());
            }
        }

        /// <returns>
        /// The qualified name (i.e. including containing types and namespaces) of a named, pointer,
        /// or array type followed by the qualified name of the actual runtime type, if provided.
        /// </returns>
        internal static string GetTypeName(
            DkmInspectionContext inspectionContext,
            DkmClrValue value,
            DkmClrType declaredType,
            DkmClrCustomTypeInfo declaredTypeInfo,
            bool isPointerDereference)
        {
            var declaredLmrType = declaredType.GetLmrType();
            var runtimeType = value.Type;
            var declaredTypeName = inspectionContext.GetTypeName(declaredType, declaredTypeInfo, Formatter.NoFormatSpecifiers);
            // Include the runtime type if distinct.
            if (!declaredLmrType.IsPointer &&
                !isPointerDereference &&
                (!declaredLmrType.IsNullable() || value.EvalFlags.Includes(DkmEvaluationResultFlags.ExceptionThrown)))
            {
                // Generate the declared type name without tuple element names.
                var declaredTypeInfoNoTupleElementNames = declaredTypeInfo.WithNoTupleElementNames();
                var declaredTypeNameNoTupleElementNames = (declaredTypeInfo == declaredTypeInfoNoTupleElementNames) ?
                    declaredTypeName :
                    inspectionContext.GetTypeName(declaredType, declaredTypeInfoNoTupleElementNames, Formatter.NoFormatSpecifiers);
                // Generate the runtime type name with no tuple element names and no dynamic.
                var runtimeTypeName = inspectionContext.GetTypeName(runtimeType, null, FormatSpecifiers: Formatter.NoFormatSpecifiers);
                // If the two names are distinct, include both.
                if (!string.Equals(declaredTypeNameNoTupleElementNames, runtimeTypeName, StringComparison.Ordinal)) // Names will reflect "dynamic", types will not.
                {
                    return string.Format("{0} {{{1}}}", declaredTypeName, runtimeTypeName);
                }
            }
            return declaredTypeName;
        }

        internal EvalResult CreateDataItem(
            DkmInspectionContext inspectionContext,
            string name,
            TypeAndCustomInfo typeDeclaringMemberAndInfo,
            TypeAndCustomInfo declaredTypeAndInfo,
            DkmClrValue value,
            bool useDebuggerDisplay,
            ExpansionFlags expansionFlags,
            bool childShouldParenthesize,
            string fullName,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmEvaluationResultCategory category,
            DkmEvaluationResultFlags flags,
            DkmEvaluationFlags evalFlags,
            bool canFavorite,
            bool isFavorite,
            bool supportsFavorites)
        {
            if ((evalFlags & DkmEvaluationFlags.ShowValueRaw) != 0)
            {
                formatSpecifiers = Formatter.AddFormatSpecifier(formatSpecifiers, "raw");
            }

            Expansion expansion;
            // If the declared type is Nullable<T>, the value should
            // have no expansion if null, or be expanded as a T.
            var declaredType = declaredTypeAndInfo.Type;
            var lmrNullableTypeArg = declaredType.GetNullableTypeArgument();
            if (lmrNullableTypeArg != null && !value.HasExceptionThrown())
            {
                Debug.Assert(value.Type.GetProxyType() == null);

                DkmClrValue nullableValue;
                if (value.IsError())
                {
                    expansion = null;
                }
                else if ((nullableValue = value.GetNullableValue(lmrNullableTypeArg, inspectionContext)) == null)
                {
                    Debug.Assert(declaredType.Equals(value.Type.GetLmrType()));
                    // No expansion of "null".
                    expansion = null;
                }
                else
                {
                    // nullableValue is taken from an internal field.
                    // It may have different category, access, etc comparing the original member.
                    // For example, the orignal member can be a property not a field.
                    // Save original member values to restore them later.
                    if (value != nullableValue)
                    {
                        var nullableMemberInfo = new NullableMemberInfo(value.Category, value.Access, value.StorageType, value.TypeModifierFlags);
                        nullableValue.SetDataItem(DkmDataCreationDisposition.CreateAlways, nullableMemberInfo);
                    }

                    value = nullableValue;
                    Debug.Assert(lmrNullableTypeArg.Equals(value.Type.GetLmrType())); // If this is not the case, add a test for includeRuntimeTypeIfNecessary.
                    // CONSIDER: The DynamicAttribute for the type argument should just be Skip(1) of the original flag array.
                    expansion = this.GetTypeExpansion(inspectionContext, new TypeAndCustomInfo(DkmClrType.Create(declaredTypeAndInfo.ClrType.AppDomain, lmrNullableTypeArg)), value, ExpansionFlags.IncludeResultsView, supportsFavorites: supportsFavorites);
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
                    typeDeclaringMemberAndInfo,
                    declaredTypeAndInfo,
                    value,
                    childShouldParenthesize,
                    fullName,
                    flags.Includes(DkmEvaluationResultFlags.ExceptionThrown) ? null : fullName,
                    formatSpecifiers,
                    flags,
                    Formatter2.GetEditableValueString(value, inspectionContext, declaredTypeAndInfo.Info));
                if (expansion == null)
                {
                    expansion = value.HasExceptionThrown()
                        ? this.GetTypeExpansion(inspectionContext, new TypeAndCustomInfo(value.Type), value, expansionFlags, supportsFavorites: false)
                        : this.GetTypeExpansion(inspectionContext, declaredTypeAndInfo, value, expansionFlags, supportsFavorites: supportsFavorites);
                }
            }

            return new EvalResult(
                ExpansionKind.Default,
                name,
                typeDeclaringMemberAndInfo,
                declaredTypeAndInfo,
                useDebuggerDisplay: useDebuggerDisplay,
                value: value,
                displayValue: null,
                expansion: expansion,
                childShouldParenthesize: childShouldParenthesize,
                fullName: fullName,
                childFullNamePrefixOpt: flags.Includes(DkmEvaluationResultFlags.ExceptionThrown) ? null : fullName,
                formatSpecifiers: formatSpecifiers,
                category: category,
                flags: flags,
                editableValue: Formatter2.GetEditableValueString(value, inspectionContext, declaredTypeAndInfo.Info),
                inspectionContext: inspectionContext,
                canFavorite: canFavorite,
                isFavorite: isFavorite);
        }

        private void GetRootResultAndContinue(
            DkmClrValue value,
            WorkList workList,
            DkmClrType declaredType,
            DkmClrCustomTypeInfo declaredTypeInfo,
            DkmInspectionContext inspectionContext,
            string name,
            string fullName,
            ReadOnlyCollection<string> formatSpecifiers,
            CompletionRoutine<DkmEvaluationResult> completionRoutine)
        {
            Debug.Assert(formatSpecifiers != null);

            var type = value.Type.GetLmrType();
            if (type.IsTypeVariables())
            {
                Debug.Assert(type.Equals(declaredType.GetLmrType()));
                var declaredTypeAndInfo = new TypeAndCustomInfo(declaredType, declaredTypeInfo);
                var expansion = new TypeVariablesExpansion(declaredTypeAndInfo);
                var dataItem = new EvalResult(
                    ExpansionKind.Default,
                    name,
                    typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                    declaredTypeAndInfo: declaredTypeAndInfo,
                    useDebuggerDisplay: false,
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
                    DataItem: dataItem.ToDataItem()));
            }
            else if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.ResultsOnly) != 0)
            {
                var dataItem = ResultsViewExpansion.CreateResultsOnlyRow(
                    inspectionContext,
                    name,
                    fullName,
                    formatSpecifiers,
                    declaredType,
                    declaredTypeInfo,
                    value,
                    this);
                CreateEvaluationResultAndContinue(
                    dataItem,
                    workList,
                    inspectionContext,
                    value.StackFrame,
                    completionRoutine);
            }
            else if ((inspectionContext.EvaluationFlags & DkmEvaluationFlags.DynamicView) != 0)
            {
                var dataItem = DynamicViewExpansion.CreateMembersOnlyRow(
                    inspectionContext,
                    name,
                    value,
                    this);
                CreateEvaluationResultAndContinue(
                    dataItem,
                    workList,
                    inspectionContext,
                    value.StackFrame,
                    completionRoutine);
            }
            else
            {
                var dataItem = ResultsViewExpansion.CreateResultsOnlyRowIfSynthesizedEnumerable(
                    inspectionContext,
                    name,
                    fullName,
                    formatSpecifiers,
                    declaredType,
                    declaredTypeInfo,
                    value,
                    this);
                if (dataItem != null)
                {
                    CreateEvaluationResultAndContinue(
                        dataItem,
                        workList,
                        inspectionContext,
                        value.StackFrame,
                        completionRoutine);
                }
                else
                {
                    var useDebuggerDisplay = (inspectionContext.EvaluationFlags & NotRoot) != 0;
                    var expansionFlags = (inspectionContext.EvaluationFlags & NoResults) != 0 ?
                        ExpansionFlags.IncludeBaseMembers :
                        ExpansionFlags.All;
                    var favortiesDataItem = value.GetDataItem<FavoritesDataItem>();
                    dataItem = CreateDataItem(
                        inspectionContext,
                        name,
                        typeDeclaringMemberAndInfo: default(TypeAndCustomInfo),
                        declaredTypeAndInfo: new TypeAndCustomInfo(declaredType, declaredTypeInfo),
                        value: value,
                        useDebuggerDisplay: useDebuggerDisplay,
                        expansionFlags: expansionFlags,
                        childShouldParenthesize: (fullName == null) ? false : FullNameProvider.ClrExpressionMayRequireParentheses(inspectionContext, fullName),
                        fullName: fullName,
                        formatSpecifiers: formatSpecifiers,
                        category: DkmEvaluationResultCategory.Other,
                        flags: value.EvalFlags,
                        evalFlags: inspectionContext.EvaluationFlags,
                        canFavorite: favortiesDataItem?.CanFavorite ?? false,
                        isFavorite: favortiesDataItem?.IsFavorite ?? false,
                        supportsFavorites: true);
                    GetResultAndContinue(dataItem, workList, declaredType, declaredTypeInfo, inspectionContext, useDebuggerDisplay, completionRoutine);
                }
            }
        }

        private void GetResultAndContinue(
            EvalResult result,
            WorkList workList,
            DkmClrType declaredType,
            DkmClrCustomTypeInfo declaredTypeInfo,
            DkmInspectionContext inspectionContext,
            bool useDebuggerDisplay,
            CompletionRoutine<DkmEvaluationResult> completionRoutine)
        {
            var value = result.Value; // Value may have been replaced (specifically, for Nullable<T>).

            if (value.TryGetDebuggerDisplayInfo(out DebuggerDisplayInfo displayInfo))
            {
                void onException(Exception e) => completionRoutine(CreateEvaluationResultFromException(e, result, inspectionContext));

                if (displayInfo.Name != null)
                {
                    // Favorites currently dependes on the name matching the member name
                    result = result.WithDisableCanAddFavorite();
                }

                var innerWorkList = workList.InnerWorkList;
                EvaluateDebuggerDisplayStringAndContinue(value, innerWorkList, inspectionContext, displayInfo.Name,
                    displayName => EvaluateDebuggerDisplayStringAndContinue(value, innerWorkList, inspectionContext, displayInfo.GetValue(inspectionContext),
                        displayValue => EvaluateDebuggerDisplayStringAndContinue(value, innerWorkList, inspectionContext, displayInfo.TypeName,
                            displayType =>
                                workList.ContinueWith(() =>
                                    completionRoutine(GetResult(inspectionContext, result, declaredType, declaredTypeInfo, displayName.Result, displayValue.Result, displayType.Result, useDebuggerDisplay))),
                            onException),
                        onException),
                    onException);
            }
            else
            {
                completionRoutine(GetResult(inspectionContext, result, declaredType, declaredTypeInfo, displayName: null, displayValue: null, displayType: null, useDebuggerDisplay: false));
            }
        }

        private static void EvaluateDebuggerDisplayStringAndContinue(
            DkmClrValue value,
            DkmWorkList workList,
            DkmInspectionContext inspectionContext,
            DebuggerDisplayItemInfo displayInfo,
            CompletionRoutine<DkmEvaluateDebuggerDisplayStringAsyncResult> onCompleted,
            CompletionRoutine<Exception> onException)
        {
            void completionRoutine(DkmEvaluateDebuggerDisplayStringAsyncResult result)
            {
                try
                {
                    onCompleted(result);
                }
                catch (Exception e)
                {
                    onException(e);
                }
            }
            if (displayInfo == null)
            {
                completionRoutine(default(DkmEvaluateDebuggerDisplayStringAsyncResult));
            }
            else
            {
                value.EvaluateDebuggerDisplayString(workList, inspectionContext, displayInfo.TargetType, displayInfo.Value, completionRoutine);
            }
        }

        private DkmEvaluationResult GetResult(
            DkmInspectionContext inspectionContext,
            EvalResult result,
            DkmClrType declaredType,
            DkmClrCustomTypeInfo declaredTypeInfo,
            string displayName,
            string displayValue,
            string displayType,
            bool useDebuggerDisplay)
        {
            var name = result.Name;
            Debug.Assert(name != null);
            var typeDeclaringMemberAndInfo = result.TypeDeclaringMemberAndInfo;

            // Note: Don't respect the debugger display name on the root element:
            //   1) In the Watch window, that's where the user's text goes.
            //   2) In the Locals window, that's where the local name goes.
            // Note: Dev12 respects the debugger display name in the Locals window,
            // but not in the Watch window, but we can't distinguish and this 
            // behavior seems reasonable.
            if (displayName != null && useDebuggerDisplay)
            {
                name = displayName;
            }
            else if (typeDeclaringMemberAndInfo.Type != null)
            {
                name = GetQualifiedMemberName(inspectionContext, typeDeclaringMemberAndInfo, name, FullNameProvider);
            }

            var value = result.Value;
            string display;
            if (value.HasExceptionThrown())
            {
                display = result.DisplayValue ?? value.GetExceptionMessage(inspectionContext, result.FullNameWithoutFormatSpecifiers ?? result.Name);
            }
            else if (displayValue != null)
            {
                display = value.IncludeObjectId(displayValue);
            }
            else
            {
                display = value.GetValueString(inspectionContext, Formatter.NoFormatSpecifiers);
            }

            var typeName = displayType ?? GetTypeName(inspectionContext, value, declaredType, declaredTypeInfo, result.Kind == ExpansionKind.PointerDereference);

            return CreateEvaluationResult(inspectionContext, value, name, typeName, display, result);
        }

        private void GetEvaluationResultsAndContinue(
            DkmEvaluationResult parent,
            ArrayBuilder<EvalResult> rows,
            DkmEvaluationResult[] results,
            int index,
            int numRows,
            WorkList workList,
            DkmInspectionContext inspectionContext,
            CompletionRoutine onCompleted,
            CompletionRoutine<Exception> onException)
        {
            void completionRoutine(DkmEvaluationAsyncResult result)
            {
                try
                {
                    results[index] = result.Result;
                    GetEvaluationResultsAndContinue(parent, rows, results, index + 1, numRows, workList, inspectionContext, onCompleted, onException);
                }
                catch (Exception e)
                {
                    onException(e);
                }
            }
            if (index < numRows)
            {
                GetChild(
                    parent,
                    workList,
                    rows[index],
                    child => workList.ContinueWith(() => completionRoutine(child)));
            }
            else
            {
                onCompleted();
            }
        }

        internal Expansion GetTypeExpansion(
            DkmInspectionContext inspectionContext,
            TypeAndCustomInfo declaredTypeAndInfo,
            DkmClrValue value,
            ExpansionFlags flags,
            bool supportsFavorites)
        {
            var declaredType = declaredTypeAndInfo.Type;
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

                Type elementType;
                DkmClrCustomTypeInfo elementTypeInfo;
                if (declaredType.IsArray)
                {
                    elementType = declaredType.GetElementType();
                    elementTypeInfo = CustomTypeInfo.SkipOne(declaredTypeAndInfo.Info);
                }
                else
                {
                    elementType = runtimeType.GetElementType();
                    elementTypeInfo = null;
                }

                return ArrayExpansion.CreateExpansion(new TypeAndCustomInfo(DkmClrType.Create(declaredTypeAndInfo.ClrType.AppDomain, elementType), elementTypeInfo), sizes, lowerBounds);
            }

            if (this.IsPrimitiveType(runtimeType))
            {
                return null;
            }

            if (declaredType.IsFunctionPointer())
            {
                // Function pointers have no expansion
                return null;
            }

            if (declaredType.IsPointer)
            {
                // If this assert fails, the element type info is just .SkipOne().
                Debug.Assert(declaredTypeAndInfo.Info?.PayloadTypeId != CustomTypeInfo.PayloadTypeId);
                var elementType = declaredType.GetElementType();
                return value.IsNull || elementType.IsVoid()
                    ? null
                    : new PointerDereferenceExpansion(new TypeAndCustomInfo(DkmClrType.Create(declaredTypeAndInfo.ClrType.AppDomain, elementType)));
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

            int cardinality;
            if (runtimeType.IsTupleCompatible(out cardinality))
            {
                return TupleExpansion.CreateExpansion(inspectionContext, declaredTypeAndInfo, value, cardinality);
            }

            return MemberExpansion.CreateExpansion(inspectionContext, declaredTypeAndInfo, value, flags, TypeHelpers.IsVisibleMember, this, isProxyType: false, supportsFavorites);
        }

        private static DkmEvaluationResult CreateEvaluationResultFromException(Exception e, EvalResult result, DkmInspectionContext inspectionContext)
        {
            return DkmFailedEvaluationResult.Create(
                inspectionContext,
                result.Value.StackFrame,
                Name: result.Name,
                FullName: null,
                ErrorMessage: e.Message,
                Flags: DkmEvaluationResultFlags.None,
                Type: null,
                DataItem: null);
        }

        private static string GetQualifiedMemberName(
            DkmInspectionContext inspectionContext,
            TypeAndCustomInfo typeDeclaringMember,
            string memberName,
            IDkmClrFullNameProvider fullNameProvider)
        {
            var typeName = fullNameProvider.GetClrTypeName(inspectionContext, typeDeclaringMember.ClrType, typeDeclaringMember.Info) ??
                inspectionContext.GetTypeName(typeDeclaringMember.ClrType, typeDeclaringMember.Info, Formatter.NoFormatSpecifiers);
            return typeDeclaringMember.Type.IsInterface ?
                $"{typeName}.{memberName}" :
                $"{memberName} ({typeName})";
        }

        // Track remaining evaluations so that each subsequent evaluation
        // is executed at the entry point from the host rather than on the
        // callstack of the previous evaluation.
        private sealed class WorkList
        {
            private enum State { Initialized, Executing, Executed }

            internal readonly DkmWorkList InnerWorkList;
            private readonly CompletionRoutine<Exception> _onException;
            private CompletionRoutine _completionRoutine;
            private State _state;

            internal WorkList(DkmWorkList workList, CompletionRoutine<Exception> onException)
            {
                InnerWorkList = workList;
                _onException = onException;
                _state = State.Initialized;
            }

            /// <summary>
            /// Run the continuation synchronously if there is no current
            /// continuation. Otherwise hold on to the continuation for
            /// the current execution to complete.
            /// </summary>
            internal void ContinueWith(CompletionRoutine completionRoutine)
            {
                Debug.Assert(_completionRoutine == null);
                _completionRoutine = completionRoutine;
                if (_state != State.Executing)
                {
                    Execute();
                }
            }

            private void Execute()
            {
                Debug.Assert(_state != State.Executing);
                _state = State.Executing;
                while (_completionRoutine != null)
                {
                    var completionRoutine = _completionRoutine;
                    _completionRoutine = null;
                    try
                    {
                        completionRoutine();
                    }
                    catch (Exception e)
                    {
                        _onException(e);
                    }
                }
                _state = State.Executed;
            }
        }

        private class NullableMemberInfo : DkmDataItem
        {
            public readonly DkmEvaluationResultCategory Category;
            public readonly DkmEvaluationResultAccessType Access;
            public readonly DkmEvaluationResultStorageType StorageType;
            public readonly DkmEvaluationResultTypeModifierFlags TypeModifierFlags;

            public NullableMemberInfo(DkmEvaluationResultCategory category, DkmEvaluationResultAccessType access, DkmEvaluationResultStorageType storageType, DkmEvaluationResultTypeModifierFlags typeModifierFlags)
            {
                Category = category;
                Access = access;
                StorageType = storageType;
                TypeModifierFlags = typeModifierFlags;
            }
        }
    }
}
