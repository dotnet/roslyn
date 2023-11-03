// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class GeneratedNames
    {
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

        internal static string MakePrimaryConstructorParameterFieldName(string parameterName)
        {
            Debug.Assert((char)GeneratedNameKind.PrimaryConstructorParameter == 'P');
            return "<" + parameterName + ">P";
        }

        internal static string MakeIteratorFinallyMethodName(StateMachineState finalizeState)
        {
            Debug.Assert((int)finalizeState < -2);

            // It is important that the name is only derived from the finalizeState, so that when 
            // editing method during EnC the Finally methods corresponding to matching states have matching names.
            Debug.Assert((char)GeneratedNameKind.IteratorFinallyMethod == 'm');
            return "<>m__Finally" + StringExtensions.GetNumeral(-((int)finalizeState + 2));
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

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LambdaDisplayClass, methodOrdinal, generation, suffix: "DisplayClass", entityOrdinal: closureOrdinal, entityGeneration: closureGeneration);
        }

        internal static string MakeAnonymousTypeOrDelegateTemplateName(int index, int submissionSlotIndex, string moduleId, bool isDelegate)
        {
            var name = "<" + moduleId + (isDelegate ? ">f__AnonymousDelegate" : ">f__AnonymousType") + StringExtensions.GetNumeral(index);
            if (submissionSlotIndex >= 0)
            {
                name += "#" + StringExtensions.GetNumeral(submissionSlotIndex);
            }

            return name;
        }

        internal static string MakeAnonymousTypeBackingFieldName(string propertyName)
        {
            return "<" + propertyName + ">i__Field";
        }

        internal static string MakeAnonymousTypeParameterName(string propertyName)
        {
            return "<" + propertyName + ">j__TPar";
        }

        internal static string MakeStateMachineTypeName(string methodName, int methodOrdinal, int generation)
        {
            Debug.Assert(generation >= 0);
            Debug.Assert(methodOrdinal >= -1);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.StateMachineType, methodOrdinal, generation, methodName);
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

        internal static string MakeLocalFunctionName(string methodName, string localFunctionName, int methodOrdinal, int methodGeneration, int lambdaOrdinal, int lambdaGeneration)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(methodGeneration >= 0);
            Debug.Assert(lambdaOrdinal >= 0);
            Debug.Assert(lambdaGeneration >= 0);

            return MakeMethodScopedSynthesizedName(GeneratedNameKind.LocalFunction, methodOrdinal, methodGeneration, methodName, localFunctionName, GeneratedNameConstants.LocalFunctionNameTerminator, lambdaOrdinal, lambdaGeneration);
        }

        private static string MakeMethodScopedSynthesizedName(
            GeneratedNameKind kind,
            int methodOrdinal,
            int methodGeneration,
            string? methodName = null,
            string? suffix = null,
            char suffixTerminator = default,
            int entityOrdinal = -1,
            int entityGeneration = -1)
        {
            Debug.Assert(methodOrdinal >= -1);
            Debug.Assert(methodGeneration >= 0 || methodGeneration == -1 && methodOrdinal == -1);
            Debug.Assert(entityOrdinal >= -1);
            Debug.Assert(entityGeneration >= 0 || entityGeneration == -1 && entityOrdinal == -1);
            Debug.Assert(entityGeneration == -1 || entityGeneration >= methodGeneration);

            var result = PooledStringBuilder.GetInstance();
            var builder = result.Builder;
            builder.Append('<');

            if (methodName != null)
            {
                builder.Append(methodName);

                // CLR generally allows names with dots, however some APIs like IMetaDataImport
                // can only return full type names combined with namespaces. 
                // see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
                // When working with such APIs, names with dots become ambiguous since metadata 
                // consumer cannot figure where namespace ends and actual type name starts.
                // Therefore it is a good practice to avoid type names with dots.
                // As a replacement use a character not allowed in C# identifier to avoid conflicts.
                if (kind.IsTypeName())
                {
                    builder.Replace('.', GeneratedNameConstants.DotReplacementInTypeNames);
                }
            }

            builder.Append('>');
            builder.Append((char)kind);

            if (suffix != null || methodOrdinal >= 0 || entityOrdinal >= 0)
            {
                builder.Append(GeneratedNameConstants.SuffixSeparator);
                builder.Append(suffix);

                if (suffixTerminator != default)
                {
                    builder.Append(suffixTerminator);
                }

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

        internal static string MakeHoistedLocalFieldName(SynthesizedLocalKind kind, int slotIndex, string? localName = null)
        {
            Debug.Assert((localName != null) == (kind == SynthesizedLocalKind.UserDefined));
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
            if (localName != null)
            {
                Debug.Assert(localName.IndexOf('.') == -1);
                builder.Append(localName);
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

        internal static string AsyncAwaiterFieldName(int slotIndex)
        {
            Debug.Assert((char)GeneratedNameKind.AwaiterField == 'u');
            return "<>u__" + StringExtensions.GetNumeral(slotIndex + 1);
        }

        internal static string MakeCachedFrameInstanceFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.LambdaCacheField == '9');
            return "<>9";
        }

        internal static string? MakeSynthesizedLocalName(SynthesizedLocalKind kind, ref int uniqueId)
        {
            Debug.Assert(kind.IsLongLived());

            // Lambda display class local has to be named. EE depends on the name format. 
            if (kind == SynthesizedLocalKind.LambdaDisplayClass)
            {
                return MakeLambdaDisplayLocalName(uniqueId++);
            }

            return null;
        }

        internal static string MakeSynthesizedInstrumentationPayloadLocalFieldName(int uniqueId)
        {
            return GeneratedNameConstants.SynthesizedLocalNamePrefix + "InstrumentationPayload" + StringExtensions.GetNumeral(uniqueId);
        }

        internal static string MakeLambdaDisplayLocalName(int uniqueId)
        {
            Debug.Assert((char)GeneratedNameKind.DisplayClassLocalOrField == '8');
            return GeneratedNameConstants.SynthesizedLocalNamePrefix + "<>8__locals" + StringExtensions.GetNumeral(uniqueId);
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

        internal static string MakeAsyncIteratorPromiseOfValueOrEndFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.AsyncIteratorPromiseOfValueOrEndBackingField == 'v');
            return "<>v__promiseOfValueOrEnd";
        }

        internal static string MakeAsyncIteratorCombinedTokensFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.CombinedTokensField == 'x');
            return "<>x__combinedTokens";
        }

        internal static string MakeIteratorCurrentFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.IteratorCurrentBackingField == '2');
            return "<>2__current";
        }

        internal static string MakeDisposeModeFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.DisposeModeField == 'w');
            return "<>w__disposeMode";
        }

        internal static string MakeIteratorCurrentThreadIdFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.IteratorCurrentThreadIdField == 'l');
            return "<>l__initialThreadId";
        }

        internal static string MakeStateMachineStateIdFieldName()
        {
            Debug.Assert((char)GeneratedNameKind.StateMachineStateIdField == 'I');
            return "<>I";
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

        internal static string MakeDynamicCallSiteContainerName(int methodOrdinal, int localFunctionOrdinal, int generation)
        {
            return MakeMethodScopedSynthesizedName(GeneratedNameKind.DynamicCallSiteContainerType, methodOrdinal, generation,
                                                   suffix: localFunctionOrdinal != -1 ? localFunctionOrdinal.ToString() : null,
                                                   suffixTerminator: localFunctionOrdinal != -1 ? '_' : default);
        }

        internal static string MakeDynamicCallSiteFieldName(int uniqueId)
        {
            Debug.Assert((char)GeneratedNameKind.DynamicCallSiteField == 'p');
            return "<>p__" + StringExtensions.GetNumeral(uniqueId);
        }

        internal const string AnonymousTypeNameWithoutModulePrefix = "<>f__AnonymousType";
        internal const string AnonymousDelegateNameWithoutModulePrefix = "<>f__AnonymousDelegate";
        internal const string ActionDelegateNamePrefix = "<>A";
        internal const string FuncDelegateNamePrefix = "<>F";
        private const int DelegateNamePrefixLength = 3;
        private const int DelegateNamePrefixLengthWithOpenBrace = 4;

        /// <summary>
        /// Produces name of the synthesized delegate symbol that encodes the parameter byref-ness and return type of the delegate.
        /// The arity is appended via `N suffix in MetadataName calculation since the delegate is generic.
        /// </summary>
        /// <remarks>
        /// Logic here should match <see cref="TryParseSynthesizedDelegateName" />.
        /// </remarks>
        internal static string MakeSynthesizedDelegateName(RefKindVector byRefs, bool returnsVoid, int generation)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            builder.Append(returnsVoid ? ActionDelegateNamePrefix : FuncDelegateNamePrefix);

            if (!byRefs.IsNull)
            {
                builder.Append(byRefs.ToRefKindString());
            }

            AppendOptionalGeneration(builder, generation);
            return pooledBuilder.ToStringAndFree();
        }

        /// <summary>
        /// Parses the name of a synthesized delegate out into the things it represents.
        /// </summary>
        /// <remarks>
        /// Logic here should match <see cref="MakeSynthesizedDelegateName" />.
        /// </remarks>
        internal static bool TryParseSynthesizedDelegateName(string name, out RefKindVector byRefs, out bool returnsVoid, out int generation, out int parameterCount)
        {
            byRefs = default;
            parameterCount = 0;
            generation = 0;

            name = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(name, out var arity);

            returnsVoid = name.StartsWith(ActionDelegateNamePrefix);

            if (!returnsVoid && !name.StartsWith(FuncDelegateNamePrefix))
            {
                return false;
            }

            parameterCount = arity - (returnsVoid ? 0 : 1);

            // If there are no ref kinds encoded
            // (and therefore no braces), use the end of the prefix instead.
            var nameEndIndex = name.LastIndexOf('}');
            if (nameEndIndex < 0)
            {
                nameEndIndex = DelegateNamePrefixLength - 1;
            }
            else
            {
                // There should be a character after the prefix, and it should be an open brace
                if (name.Length <= DelegateNamePrefixLength || name[DelegateNamePrefixLength] != '{')
                {
                    return false;
                }

                // If there are braces, then the ref kind string is encoded between them
                var refKindString = name[DelegateNamePrefixLengthWithOpenBrace..nameEndIndex];

                if (!RefKindVector.TryParse(refKindString, arity, out byRefs))
                {
                    return false;
                }
            }

            // If there is a generation index it will be directly after the brace, otherwise the brace
            // is the last character
            if (nameEndIndex < name.Length - 1)
            {
                // Format is a '#' followed by the generation number
                if (name[nameEndIndex + 1] != '#')
                {
                    return false;
                }

                if (!int.TryParse(name[(nameEndIndex + 2)..], out generation))
                {
                    return false;
                }
            }

            Debug.Assert(name == MakeSynthesizedDelegateName(byRefs, returnsVoid, generation));
            return true;
        }

        internal static string MakeSynthesizedInlineArrayName(int arrayLength, int generation)
        {
            Debug.Assert((char)GeneratedNameKind.InlineArrayType == 'y');
            var name = "<>y__InlineArray" + arrayLength;

            // Synthesized inline arrays need to have unique name across generations because they are not reused.
            return (generation > 0) ? name + GenerationSeparator + generation : name;
        }

        internal static string MakeSynthesizedReadOnlyListName(bool hasKnownLength, int generation)
        {
            Debug.Assert((char)GeneratedNameKind.ReadOnlyListType == 'z');
            string name = hasKnownLength ? "<>z__ReadOnlyArray" : "<>z__ReadOnlyList";

            // Synthesized list types need to have unique name across generations because they are not reused.
            return (generation > 0) ? name + GenerationSeparator + generation : name;
        }

        internal static string AsyncBuilderFieldName()
        {
            // Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on this name.
            Debug.Assert((char)GeneratedNameKind.AsyncBuilderField == 't');
            return "<>t__builder";
        }

        internal static string DelegateCacheContainerType(int generation, string? methodName = null, int methodOrdinal = -1, int ownerUniqueId = -1)
        {
            const char NameKind = (char)GeneratedNameKind.DelegateCacheContainerType;

            var result = PooledStringBuilder.GetInstance();
            var builder = result.Builder;

            builder.Append('<').Append(methodName).Append('>').Append(NameKind);

            if (methodOrdinal > -1)
            {
                builder.Append(GeneratedNameConstants.SuffixSeparator).Append(methodOrdinal);
            }

            if (ownerUniqueId > -1)
            {
                builder.Append(IdSeparator).Append(ownerUniqueId);
            }

            AppendOptionalGeneration(builder, generation);

            return result.ToStringAndFree();
        }

        internal static string DelegateCacheContainerFieldName(int id, string targetMethod)
        {
            var result = PooledStringBuilder.GetInstance();
            var builder = result.Builder;

            builder.Append('<').Append(id).Append(">__").Append(targetMethod);

            return result.ToStringAndFree();
        }

        internal static string ReusableHoistedLocalFieldName(int number)
        {
            Debug.Assert((char)GeneratedNameKind.ReusableHoistedLocalField == '7');
            return "<>7__wrap" + StringExtensions.GetNumeral(number);
        }

        internal static string LambdaCopyParameterName(int ordinal)
        {
            return "<p" + StringExtensions.GetNumeral(ordinal) + ">";
        }

        internal static string AnonymousDelegateParameterName(int index, int parameterCount)
        {
            // SPEC: parameter names arg1, ..., argn or arg if a single parameter
            if (parameterCount == 1)
            {
                return "arg";
            }
            return "arg" + StringExtensions.GetNumeral(index + 1);
        }

        internal static string MakeFileTypeMetadataNamePrefix(string filePath, ImmutableArray<byte> checksumOpt)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var sb = pooledBuilder.Builder;
            sb.Append('<');
            AppendFileName(filePath, sb);
            sb.Append('>');
            sb.Append((char)GeneratedNameKind.FileType);
            if (checksumOpt.IsDefault)
            {
                // Note: this is an error condition.
                // This is only included for clarity for users inspecting the value of 'MetadataName'.
                sb.Append("<no checksum>");
            }
            else
            {
                foreach (var b in checksumOpt)
                {
                    sb.AppendFormat("{0:X2}", b);
                }
            }
            sb.Append("__");
            return pooledBuilder.ToStringAndFree();
        }

        internal static string GetDisplayFilePath(string filePath)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            AppendFileName(filePath, pooledBuilder.Builder);
            return pooledBuilder.ToStringAndFree();
        }

        private static void AppendFileName(string filePath, StringBuilder sb)
        {
            var fileName = FileNameUtilities.GetFileName(filePath, includeExtension: false);
            if (fileName is null)
            {
                return;
            }

            foreach (var ch in fileName)
            {
                sb.Append(ch switch
                {
                    >= 'a' and <= 'z' => ch,
                    >= 'A' and <= 'Z' => ch,
                    >= '0' and <= '9' => ch,
                    _ => '_'
                });
            }
        }
    }
}
