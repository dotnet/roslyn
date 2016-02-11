// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
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

        internal static bool IsGeneratedMemberName(string memberName)
        {
            return memberName.Length > 0 && memberName[0] == '<';
        }

        internal static string MakeBackingFieldName(string propertyName)
        {
            Debug.Assert((char)GeneratedNameKind.AutoPropertyBackingField == 'k');
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
            Debug.Assert((char)GeneratedNameKind.IteratorFinallyMethod == 'm');
            return "<>m__Finally" + StringExtensions.GetNumeral(Math.Abs(iteratorState + 2));
        }

        internal static string MakeStaticLambdaDisplayClassName(int methodOrdinal, int generation)
        {
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaDisplayClass, methodOrdinal, generation);
        }

        internal static string MakeLambdaDisplayClassName(int methodOrdinal, int generation, int closureOrdinal, int closureGeneration)
        {
            // -1 for singleton static lambdas
            Debug.Assert(closureOrdinal >= -1);
            Debug.Assert(methodOrdinal >= 0);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaDisplayClass, methodOrdinal, generation, suffix: "DisplayClass", entityOrdinal: closureOrdinal, entityGeneration: closureGeneration, isTypeName: true);
        }

        internal static string MakeAnonymousTypeTemplateName(int index, int submissionSlotIndex, string moduleId)
        {
            var name = "<" + moduleId + ">f__AnonymousType" + StringExtensions.GetNumeral(index);
            if (submissionSlotIndex >= 0)
            {
                name += "#" + StringExtensions.GetNumeral(submissionSlotIndex);
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
            Debug.Assert((char)GeneratedNameKind.BaseMethodWrapper == 'n');
            return "<>n__" + StringExtensions.GetNumeral(uniqueId);
        }

        internal static string MakeLambdaMethodName(string methodName, int methodOrdinal, int methodGeneration, int lambdaOrdinal, int lambdaGeneration)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(methodGeneration >= 0);
            Debug.Assert(lambdaOrdinal >= 0);
            Debug.Assert(lambdaGeneration >= 0);

            // The EE displays the containing method name and unique id in the stack trace,
            // and uses it to find the original binding context.
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaMethod, methodOrdinal, methodGeneration, methodName, entityOrdinal: lambdaOrdinal, entityGeneration: lambdaGeneration);
        }

        internal static string MakeLambdaCacheFieldName(int methodOrdinal, int generation, int lambdaOrdinal, int lambdaGeneration)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(lambdaOrdinal >= 0);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaCacheField, methodOrdinal, generation, entityOrdinal: lambdaOrdinal, entityGeneration: lambdaGeneration);
        }

        private static string MakeMethodScopedSynthesizedName(
            GeneratedNameKind kind,
            int methodOrdinal,
            int methodGeneration,
            string methodNameOpt = null,
            string suffix = null,
            int entityOrdinal = -1,
            int entityGeneration = -1,
            bool isTypeName = false)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(methodGeneration >= 0 || methodGeneration == -1 && methodOrdinal == -1);
            Debug.Assert(entityOrdinal >= -1);
            Debug.Assert(entityGeneration >= 0 || entityGeneration == -1 && entityOrdinal == -1);
            Debug.Assert(entityGeneration == -1 || entityGeneration >= methodGeneration);

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
                if (kind.IsTypeName())
                {
                    builder.Replace('.', DotReplacementInTypeNames);
                }
            }

            builder.Append('>');
            builder.Append((char)kind);

            if (suffix != null || methodOrdinal >= 0 || entityOrdinal >= 0)
            {
                builder.Append(SuffixSeparator);
                builder.Append(suffix);

                if (methodOrdinal >= 0)
                {
                    builder.Append(methodOrdinal);
                    AppendOptionalGeneration(builder, methodGeneration);
                }

                if (entityOrdinal >= 0)
                {
                    if (methodOrdinal >= 0)
                    {
                        builder.Append(IdSeparator);
                    }

                    builder.Append(entityOrdinal);
                    AppendOptionalGeneration(builder, entityGeneration);
                }
            }

            return result.ToStringAndFree();
        }

        private static void AppendOptionalGeneration(StringBuilder builder, int generation)
        {
            if (generation > 0)
            {
                builder.Append(GenerationSeparator);
                builder.Append(generation);
            }
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
                builder.Append((char)GeneratedNameKind.HoistedSynthesizedLocalField);
            }

            builder.Append("__");
            builder.Append(slotIndex + 1);

            return result.ToStringAndFree();
        }

        // The type of generated name. See TryParseGeneratedName.
        internal static GeneratedNameKind GetKind(string name)
        {
            GeneratedNameKind kind;
            int openBracketOffset;
            int closeBracketOffset;
            return TryParseGeneratedName(name, out kind, out openBracketOffset, out closeBracketOffset) ? kind : GeneratedNameKind.None;
        }

        // Parse the generated name. Returns true for names of the form
        // [CS$]<[middle]>c[__[suffix]] where [CS$] is included for certain
        // generated names, where [middle] and [__[suffix]] are optional,
        // and where c is a single character in [1-9a-z]
        // (csharp\LanguageAnalysis\LIB\SpecialName.cpp).
        internal static bool TryParseGeneratedName(
            string name,
            out GeneratedNameKind kind,
            out int openBracketOffset,
            out int closeBracketOffset)
        {
            openBracketOffset = -1;
            if (name.StartsWith("CS$<", StringComparison.Ordinal))
            {
                openBracketOffset = 3;
            }
            else if (name.StartsWith("<", StringComparison.Ordinal))
            {
                openBracketOffset = 0;
            }

            if (openBracketOffset >= 0)
            {
                closeBracketOffset = name.IndexOfBalancedParenthesis(openBracketOffset, '>');
                if (closeBracketOffset >= 0 && closeBracketOffset + 1 < name.Length)
                {
                    int c = name[closeBracketOffset + 1];
                    if ((c >= '1' && c <= '9') || (c >= 'a' && c <= 'z')) // Note '0' is not special.
                    {
                        kind = (GeneratedNameKind)c;
                        return true;
                    }
                }
            }

            kind = GeneratedNameKind.None;
            openBracketOffset = -1;
            closeBracketOffset = -1;
            return false;
        }

        internal static bool TryParseSourceMethodNameFromGeneratedName(string generatedName, GeneratedNameKind requiredKind, out string methodName)
        {
            int openBracketOffset;
            int closeBracketOffset;
            GeneratedNameKind kind;
            if (!TryParseGeneratedName(generatedName, out kind, out openBracketOffset, out closeBracketOffset))
            {
                methodName = null;
                return false;
            }

            if (requiredKind != 0 && kind != requiredKind)
            {
                methodName = null;
                return false;
            }

            methodName = generatedName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);

            if (kind.IsTypeName())
            {
                methodName = methodName.Replace(DotReplacementInTypeNames, '.');
            }

            return true;
        }

        internal static string AsyncAwaiterFieldName(int slotIndex)
        {
            Debug.Assert((char)GeneratedNameKind.AwaiterField == 'u');
            return "<>u__" + StringExtensions.GetNumeral(slotIndex + 1);
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

            if (int.TryParse(fieldName.Substring(lastUnder + 1), NumberStyles.None, CultureInfo.InvariantCulture, out slotIndex) && slotIndex >= 1)
            {
                slotIndex--;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        internal static string MakeCachedFrameInstanceFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.LambdaCacheField == '9');
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
            return SynthesizedLocalNamePrefix + "<>8__locals" + StringExtensions.GetNumeral(uniqueId);
        }

        internal static bool IsSynthesizedLocalName(string name)
        {
            return name.StartsWith(SynthesizedLocalNamePrefix, StringComparison.Ordinal);
        }

        internal static string MakeFixedFieldImplementationName(string fieldName)
        {
            // the native compiler adds numeric digits at the end.  Roslyn does not.
            Debug.Assert((char)GeneratedNameKind.FixedBufferField == 'e');
            return "<" + fieldName + ">e__FixedBuffer";
        }

        internal static string MakeStateMachineStateFieldName()
        {
            // Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on this name.
            Debug.Assert((char)GeneratedNameKind.StateMachineStateField == '1');
            return "<>1__state";
        }

        internal static string MakeIteratorCurrentFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.IteratorCurrentBackingField == '2');
            return "<>2__current";
        }

        internal static string MakeIteratorCurrentThreadIdFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.IteratorCurrentThreadIdField == 'l');
            return "<>l__initialThreadId";
        }

        internal static string ThisProxyFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.ThisProxyField == '4');
            return "<>4__this";
        }

        internal static string StateMachineThisParameterProxyName()
        {
            return StateMachineParameterProxyFieldName(ThisProxyFieldName());
        }

        internal static string StateMachineParameterProxyFieldName(string parameterName)
        {
            Debug.Assert((char)GeneratedNameKind.StateMachineParameterProxyField == '3');
            return "<>3__" + parameterName;
        }

        internal static string MakeDynamicCallSiteContainerName(int methodOrdinal, int generation)
        {
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.DynamicCallSiteContainerType, methodOrdinal, generation, isTypeName: true);
        }

        internal static string MakeDynamicCallSiteFieldName(int uniqueId)
        {
            Debug.Assert((char)GeneratedNameKind.DynamicCallSiteField == 'p');
            return "<>p__" + StringExtensions.GetNumeral(uniqueId);
        }

        /// <summary>
        /// Produces name of the synthesized delegate symbol that encodes the parameter byref-ness and return type of the delegate.
        /// The arity is appended via `N suffix in MetadataName calculation since the delegate is generic.
        /// </summary>
        internal static string MakeDynamicCallSiteDelegateName(BitVector byRefs, bool returnsVoid, int generation)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            builder.Append(returnsVoid ? "<>A" : "<>F");

            if (!byRefs.IsNull)
            {
                builder.Append("{");

                int i = 0;
                foreach (int byRefIndex in byRefs.Words())
                {
                    if (i > 0)
                    {
                        builder.Append(",");
                    }

                    builder.AppendFormat("{0:x8}", byRefIndex);
                    i++;
                }

                builder.Append("}");
                Debug.Assert(i > 0);
            }

            AppendOptionalGeneration(builder, generation);
            return pooledBuilder.ToStringAndFree();
        }

        internal static string AsyncBuilderFieldName()
        {
            // Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on this name.
            Debug.Assert((char)GeneratedNameKind.AsyncBuilderField == 't');
            return "<>t__builder";
        }

        internal static string ReusableHoistedLocalFieldName(int number)
        {
            Debug.Assert((char)GeneratedNameKind.ReusableHoistedLocalField == '7');
            return "<>7__wrap" + StringExtensions.GetNumeral(number);
        }

        internal static string LambdaCopyParameterName(ParameterSymbol sourceParameter)
        {
            return "<" + sourceParameter.Name + ">";
        }
    }
}
