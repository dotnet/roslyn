// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class GeneratedNames
    {
        internal const string SynthesizedLocalNamePrefix = "CS$";
        internal const char DotReplacementInTypeNames = '-';
        private const string SuffixSeparator = "__";
        private const char IdSeparator = '_';
        private const char GenerationSeparator = '#';

        private static readonly ImmutableArray<string> numericStrings = ImmutableArray.Create("0", "1", "2", "3", "4", "5", "6", "7", "8", "9");

        private static string GetString(int number)
        {
            Debug.Assert(number >= 0);
            return (number < numericStrings.Length) ? numericStrings[number] : number.ToString();
        }

        internal static bool IsGeneratedMemberName(string memberName)
        {
            return memberName.Length > 0 && memberName[0] == '<';
        }

        internal static string MakeBackingFieldName(string propertyName)
        {
            return "<" + propertyName + ">k__BackingField";
        }

        internal static string MakeIteratorFinallyMethodName(int iteratorState)
        {
            // we can pick any name, but we will try to do
            // <>m__Finally1
            // <>m__Finally2
            // <>m__Finally3
            // . . . 
            // that will roughly match native naming scheme and may also be easier when need to debug.
            return "<>m__Finally" + GetString(Math.Abs(iteratorState + 2));
        }

        internal static string MakeStaticLambdaDisplayClassName(int methodOrdinal, int generation)
        {
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaDisplayClassType, methodOrdinal, generation);
        }

        internal static string MakeLambdaDisplayClassName(int methodOrdinal, int generation, int scopeOrdinal)
        {
            // -1 for singleton static lambdas
            Debug.Assert(scopeOrdinal >= -1);
            Debug.Assert(methodOrdinal >= 0);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaDisplayClassType, methodOrdinal, generation, suffix: "DisplayClass", uniqueId: scopeOrdinal, isTypeName: true);
        }

        internal static string MakeAnonymousTypeTemplateName(int index, int submissionSlotIndex, string moduleId)
        {
            var name = "<" + moduleId + ">f__AnonymousType" + GetString(index);
            if (submissionSlotIndex >= 0)
            {
                name += "#" + GetString(submissionSlotIndex);
            }

            return name;
        }

        internal const string AnonymousNamePrefix = "<>f__AnonymousType";

        internal static bool TryParseAnonymousTypeTemplateName(string name, out int index)
        {
            // No callers require anonymous types from net modules,
            // so names with module id are ignored.
            if (name.StartsWith(AnonymousNamePrefix, StringComparison.Ordinal))
            {
                if (int.TryParse(name.Substring(AnonymousNamePrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out index))
                {
                    return true;
                }
            }

            index = -1;
            return false;
        }

        internal static string MakeAnonymousTypeBackingFieldName(string propertyName)
        {
            return "<" + propertyName + ">i__Field";
        }

        internal static string MakeAnonymousTypeParameterName(string propertyName)
        {
            return "<" + propertyName + ">j__TPar";
        }

        internal static bool TryParseAnonymousTypeParameterName(string typeParameterName, out string propertyName)
        {
            if (typeParameterName.StartsWith("<", StringComparison.Ordinal) &&
                typeParameterName.EndsWith(">j__TPar", StringComparison.Ordinal))
            {
                propertyName = typeParameterName.Substring(1, typeParameterName.Length - 9);
                return true;
            }

            propertyName = null;
            return false;
        }

        internal static string MakeStateMachineTypeName(string methodName, int methodOrdinal, int generation)
        {
            Debug.Assert(generation >= 0);
            Debug.Assert(methodOrdinal >= -1);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.StateMachineType, methodOrdinal, generation, methodName, isTypeName: true);
        }

        internal static string MakeBaseMethodWrapperName(int uniqueId)
        {
            return "<>n__" + GetString(uniqueId);
        }

        internal static string MakeLambdaMethodName(string methodName, int methodOrdinal, int generation, int lambdaOrdinal)
        {
            Debug.Assert(generation >= 0);
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(lambdaOrdinal >= 0);

            // The EE displays the containing method name and unique id in the stack trace,
            // and uses it to find the original binding context.
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaMethod, methodOrdinal, generation, methodName, uniqueId: lambdaOrdinal);
        }

        internal static string MakeLambdaCacheFieldName(int methodOrdinal, int generation, int lambdaOrdinal)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(lambdaOrdinal >= 0);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaCacheField, methodOrdinal, generation, uniqueId: lambdaOrdinal);
        }

        private static string MakeMethodScopedSynthesizedName(GeneratedNameKind kind, int methodOrdinal, int generation, string methodNameOpt = null, string suffix = null, int uniqueId = -1, bool isTypeName = false)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(generation >= 0);
            Debug.Assert(uniqueId >= -1);

            var result = PooledStringBuilder.GetInstance();
            var builder = result.Builder;
            builder.Append('<');

            if (methodNameOpt != null)
            {
                builder.Append(methodNameOpt);

                // CLR generally allows names with dots, however some APIs like IMetaDataImport
                // can only return full type names combined with namespaces. 
                // see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                // When working with such APIs, names with dots become ambiguous since metadata 
                // consumer cannot figure where namespace ends and actual type name starts.
                // Therefore it is a good practice to avoid type names with dots.
                // As a replacement use a character not allowed in C# identifier to avoid conflicts.
                if (isTypeName)
                {
                    builder.Replace('.', DotReplacementInTypeNames);
                }
            }

            builder.Append('>');
            builder.Append((char)kind);

            if (suffix != null || methodOrdinal >= 0 || uniqueId >= 0)
            {
                builder.Append(SuffixSeparator);
                builder.Append(suffix);

                if (methodOrdinal >= 0)
                {
                    builder.Append(methodOrdinal);

                    if (generation > 0)
                    {
                        builder.Append(GenerationSeparator);
                        builder.Append(generation);
                    }
                }

                if (uniqueId >= 0)
                {
                    if (methodOrdinal >= 0)
                    {
                        builder.Append(IdSeparator);
                    }

                    builder.Append(uniqueId);
                }
            }

            return result.ToStringAndFree();
        }

        internal static string MakeHoistedLocalFieldName(SynthesizedLocalKind kind, int slotIndex, string localNameOpt = null)
        {
            Debug.Assert((localNameOpt != null) == (kind == SynthesizedLocalKind.UserDefined));
            Debug.Assert(slotIndex >= 0);
            Debug.Assert(kind.IsLongLived());

            // Lambda display class local follows a different naming pattern.
            // EE depends on the name format. 
            // There's logic in the EE to recognize locals that have been captured by a lambda
            // and would have been hoisted for the state machine.  Basically, we just hoist the local containing
            // the instance of the lambda display class and retain its original name (rather than using an
            // iterator local name).  See FUNCBRECEE::ImportIteratorMethodInheritedLocals.

            var result = PooledStringBuilder.GetInstance();
            var builder = result.Builder;
            builder.Append('<');
            if (localNameOpt != null)
            {
                Debug.Assert(localNameOpt.IndexOf('.') == -1);
                builder.Append(localNameOpt);
            }

            builder.Append('>');

            if (kind == SynthesizedLocalKind.LambdaDisplayClass)
            {
                builder.Append((char)GeneratedNameKind.DisplayClassLocalOrField);
            }
            else if (kind == SynthesizedLocalKind.UserDefined)
            {
                builder.Append((char)GeneratedNameKind.HoistedLocalField);
            }
            else
            {
                builder.Append('s');
            }

            builder.Append("__");
            builder.Append(slotIndex + 1);

            return result.ToStringAndFree();
        }

        internal static string AsyncAwaiterFieldName(int slotIndex)
        {
            return "<>u__" + GetString(slotIndex + 1);
        }

        // Extracts the slot index from a name of a field that stores hoisted variables or awaiters.
        // Such a name ends with "__{slot index + 1}". 
        // Returned slot index is >= 0.
        internal static bool TryParseSlotIndex(string fieldName, out int slotIndex)
        {
            int lastUnder = fieldName.LastIndexOf('_');
            if (lastUnder - 1 < 0 || lastUnder == fieldName.Length || fieldName[lastUnder - 1] != '_')
            {
                slotIndex = -1;
                return false;
            }

            if (int.TryParse(fieldName.Substring(lastUnder + 1), out slotIndex) && slotIndex >= 1)
            {
                slotIndex--;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        internal static string MakeCachedFrameInstanceName()
        {
            return "<>9";
        }

        internal static string MakeSynthesizedLocalName(SynthesizedLocalKind kind, ref int uniqueId)
        {
            Debug.Assert(kind.IsLongLived());

            // Lambda display class local has to be named. EE depends on the name format. 
            if (kind == SynthesizedLocalKind.LambdaDisplayClass)
            {
                return MakeLambdaDisplayLocalName(uniqueId++);
            }

            return null;
        }

        internal static string MakeLambdaDisplayLocalName(int uniqueId)
        {
            Debug.Assert((char)GeneratedNameKind.DisplayClassLocalOrField == '8');
            return SynthesizedLocalNamePrefix + "<>8__locals" + GetString(uniqueId);
        }

        internal static bool IsSynthesizedLocalName(string name)
        {
            return name.StartsWith(SynthesizedLocalNamePrefix, StringComparison.Ordinal);
        }

        internal static string MakeFixedFieldImplementationName(string fieldName)
        {
            // the native compiler adds numeric digits at the end.  Roslyn does not.
            return "<" + fieldName + ">e__FixedBuffer";
        }

        internal static string MakeStateMachineStateName()
        {
            // Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on this name.
            return "<>1__state";
        }

        internal static bool TryParseIteratorName(string mangledTypeName, out string iteratorName)
        {
            GeneratedNameKind kind;
            int openBracketOffset;
            int closeBracketOffset;
            if (TryParseGeneratedName(mangledTypeName, out kind, out openBracketOffset, out closeBracketOffset) &&
                (kind == GeneratedNameKind.StateMachineType) &&
                (openBracketOffset == 0))
            {
                iteratorName = mangledTypeName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                return true;
            }

            iteratorName = null;
            return false;
        }

        internal static bool TryParseLambdaMethodName(string mangledTypeName, out string containingMethodName)
        {
            GeneratedNameKind kind;
            int openBracketOffset;
            int closeBracketOffset;
            if (TryParseGeneratedName(mangledTypeName, out kind, out openBracketOffset, out closeBracketOffset) &&
                (kind == GeneratedNameKind.LambdaMethod) &&
                (openBracketOffset == 0))
            {
                containingMethodName = mangledTypeName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                return true;
            }

            containingMethodName = null;
            return false;
        }

        internal static string MakeIteratorCurrentBackingFieldName()
        {
            return "<>2__current";
        }

        internal static string MakeIteratorCurrentThreadIdName()
        {
            return "<>l__initialThreadId";
        }

        internal static string ThisProxyName()
        {
            Debug.Assert((char)GeneratedNameKind.ThisProxy == '4');
            return "<>4__this";
        }

        internal static string StateMachineThisParameterProxyName()
        {
            return StateMachineParameterProxyName(ThisProxyName());
        }

        internal static string StateMachineParameterProxyName(string parameterName)
        {
            return "<>3__" + parameterName;
        }

        internal static string MakeDynamicCallSiteContainerName(int methodOrdinal, int generation)
        {
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.DynamicCallSiteContainer, methodOrdinal, generation, isTypeName: true);
        }

        internal static string MakeDynamicCallSiteFieldName(int uniqueId)
        {
            return "<>p__" + GetString(uniqueId);
        }

        internal static string AsyncBuilderFieldName()
        {
            // Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on this name.
            return "<>t__builder";
        }

        internal static string ReusableHoistedLocalFieldName(int number)
        {
            return "<>7__wrap" + GetString(number);
        }
    }
}
