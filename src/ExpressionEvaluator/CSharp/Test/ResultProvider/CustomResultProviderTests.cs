// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// A custom IDkmClrResultProvider implementation
    /// re-using C# implementation.
    /// </summary>
    public class CustomResultProviderTests : CSharpResultProviderTestBase
    {
        public CustomResultProviderTests() :
            base(
                new DkmInspectionSession(
                    ImmutableArray.Create<IDkmClrFormatter>(new CustomFormatter(new CSharpFormatter()), new CSharpFormatter()),
                    ImmutableArray.Create<IDkmClrResultProvider>(new CustomResultProvider(), new CSharpResultProvider())))
        {
        }

        [Fact]
        public void Root()
        {
            var source =
@".field static assembly int32 s_val
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance int32* modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) get_P()
  {
    ldsflda int32 s_val
    ret
  }
  .property instance int32* modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) P()
  {
    .get instance int32* modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) C::get_P()
  }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CommonTestBase.EmitILToArray(source, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate()).GetMemberValue("P", (int)MemberTypes.Property, "C", DefaultInspectionContext);
            var evalResult = FormatResult("P", value);
            Verify(evalResult,
                EvalResult("P", "0", "int*", "P", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Property));
        }

        [Fact]
        public void Member()
        {
            var source =
@".field static assembly int32 s_val
.class public C
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance int32* modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) get_P()
  {
    ldsflda int32 s_val
    ret
  }
  .property instance int32* modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) P()
  {
    .get instance int32* modopt([mscorlib]System.Runtime.CompilerServices.IsImplicitlyDereferenced) C::get_P()
  }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CommonTestBase.EmitILToArray(source, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate(), type);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Other));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("P", "0", "int*", "o.P", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Property));
        }

        private sealed class CustomResultProvider : IDkmClrResultProvider
        {
            void IDkmClrResultProvider.GetResult(DkmClrValue clrValue, DkmWorkList workList, DkmClrType declaredType, DkmClrCustomTypeInfo customTypeInfo, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers, string resultName, string resultFullName, DkmCompletionRoutine<DkmEvaluationAsyncResult> completionRoutine)
            {
                clrValue.GetResult(
                    workList,
                    declaredType,
                    customTypeInfo,
                    inspectionContext,
                    formatSpecifiers,
                    resultName,
                    resultFullName,
                    result =>
                    {
                        var type = declaredType.GetLmrType();
                        if (type.IsPointer)
                        {
                            var r = (DkmSuccessEvaluationResult)result.Result;
                            // TODO: Why aren't modopts for & properties included?
                            r.GetChildren(
                                workList,
                                1,
                                inspectionContext,
                                children =>
                                {
                                    var c = (DkmSuccessEvaluationResult)children.InitialChildren[0];
                                    r = DkmSuccessEvaluationResult.Create(
                                        c.InspectionContext,
                                        c.StackFrame,
                                        r.Name,
                                        r.FullName,
                                        c.Flags,
                                        c.Value,
                                        r.EditableValue,
                                        r.Type,
                                        r.Category,
                                        r.Access,
                                        r.StorageType,
                                        r.TypeModifierFlags,
                                        null,
                                        r.CustomUIVisualizers,
                                        null,
                                        null);
                                    completionRoutine(new DkmEvaluationAsyncResult(r));
                                });
                        }
                        else
                        {
                            completionRoutine(result);
                        }
                    });
            }

            DkmClrValue IDkmClrResultProvider.GetClrValue(DkmSuccessEvaluationResult successResult)
            {
                return successResult.GetClrValue();
            }

            void IDkmClrResultProvider.GetChildren(DkmEvaluationResult evaluationResult, DkmWorkList workList, int initialRequestSize, DkmInspectionContext inspectionContext, DkmCompletionRoutine<DkmGetChildrenAsyncResult> completionRoutine)
            {
                evaluationResult.GetChildren(workList, initialRequestSize, inspectionContext, completionRoutine);
            }

            void IDkmClrResultProvider.GetItems(DkmEvaluationResultEnumContext enumContext, DkmWorkList workList, int startIndex, int count, DkmCompletionRoutine<DkmEvaluationEnumAsyncResult> completionRoutine)
            {
                enumContext.GetItems(workList, startIndex, count, completionRoutine);
            }

            string IDkmClrResultProvider.GetUnderlyingString(DkmEvaluationResult result)
            {
                return result.GetUnderlyingString();
            }
        }

        private sealed class CustomFormatter : IDkmClrFormatter, IDkmClrFormatter2, IDkmClrFullNameProvider
        {
            private readonly IDkmClrFormatter _fallback; // Remove and dispatch calls through DkmInspectionContext.

            internal CustomFormatter(IDkmClrFormatter2 fallback)
            {
                _fallback = (IDkmClrFormatter)fallback;
            }

            string IDkmClrFormatter.GetTypeName(DkmInspectionContext inspectionContext, DkmClrType clrType, DkmClrCustomTypeInfo customTypeInfo, ReadOnlyCollection<string> formatSpecifiers)
            {
                return inspectionContext.GetTypeName(clrType, customTypeInfo, formatSpecifiers);
            }

            string IDkmClrFormatter.GetUnderlyingString(DkmClrValue clrValue, DkmInspectionContext inspectionContext)
            {
                return clrValue.GetUnderlyingString(inspectionContext);
            }

            string IDkmClrFormatter.GetValueString(DkmClrValue clrValue, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers)
            {
                return clrValue.GetValueString(inspectionContext, formatSpecifiers);
            }

            bool IDkmClrFormatter.HasUnderlyingString(DkmClrValue clrValue, DkmInspectionContext inspectionContext)
            {
                return clrValue.HasUnderlyingString(inspectionContext);
            }

            string IDkmClrFormatter2.GetValueString(DkmClrValue value, DkmClrCustomTypeInfo customTypeInfo, DkmInspectionContext inspectionContext, ReadOnlyCollection<string> formatSpecifiers)
            {
                return ((IDkmClrFormatter2)_fallback).GetValueString(value, customTypeInfo, inspectionContext, formatSpecifiers);
            }

            string IDkmClrFormatter2.GetEditableValueString(DkmClrValue value, DkmInspectionContext inspectionContext, DkmClrCustomTypeInfo customTypeInfo)
            {
                return ((IDkmClrFormatter2)_fallback).GetEditableValueString(value, inspectionContext, customTypeInfo);
            }

            string IDkmClrFullNameProvider.GetClrTypeName(DkmInspectionContext inspectionContext, DkmClrType clrType, DkmClrCustomTypeInfo customTypeInfo)
            {
                throw new NotImplementedException();
            }

            string IDkmClrFullNameProvider.GetClrArrayIndexExpression(DkmInspectionContext inspectionContext, int[] indices)
            {
                throw new NotImplementedException();
            }

            string IDkmClrFullNameProvider.GetClrCastExpression(DkmInspectionContext inspectionContext, string argument, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo, bool parenthesizeArgument, bool parenthesizeEntireExpression)
            {
                throw new NotImplementedException();
            }

            string IDkmClrFullNameProvider.GetClrObjectCreationExpression(DkmInspectionContext inspectionContext, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo, string arguments)
            {
                throw new NotImplementedException();
            }

            string IDkmClrFullNameProvider.GetClrValidIdentifier(DkmInspectionContext inspectionContext, string identifier)
            {
                return ((IDkmClrFullNameProvider)_fallback).GetClrValidIdentifier(inspectionContext, identifier);
            }

            string IDkmClrFullNameProvider.GetClrExpressionAndFormatSpecifiers(DkmInspectionContext inspectionContext, string expression, out ReadOnlyCollection<string> formatSpecifiers)
            {
                return ((IDkmClrFullNameProvider)_fallback).GetClrExpressionAndFormatSpecifiers(inspectionContext, expression, out formatSpecifiers);
            }

            bool IDkmClrFullNameProvider.ClrExpressionMayRequireParentheses(DkmInspectionContext inspectionContext, string expression)
            {
                return ((IDkmClrFullNameProvider)_fallback).ClrExpressionMayRequireParentheses(inspectionContext, expression);
            }

            string IDkmClrFullNameProvider.GetClrMemberName(
                DkmInspectionContext inspectionContext,
                string parentFullName,
                DkmClrType declaringType,
                DkmClrCustomTypeInfo declaringTypeInfo,
                string memberName,
                bool memberAccessRequiresExplicitCast,
                bool memberIsStatic)
            {
                return ((IDkmClrFullNameProvider)_fallback).GetClrMemberName(inspectionContext, parentFullName, declaringType, declaringTypeInfo, memberName, memberAccessRequiresExplicitCast, memberIsStatic);
            }
        }
    }
}
