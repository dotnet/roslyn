// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class GeneratedNames
    {
        internal const string SynthesizedLocalNamePrefix = "CS$";

        internal static bool IsGeneratedName(string memberName)
        {
            return memberName.Length > 0 && memberName[0] == '<';
        }

        internal static string MakeBackingFieldName(string propertyName)
        {
            return "<" + propertyName + ">k__BackingField";
        }

        internal static string MakeLambdaMethodName(string containingMethodName, int uniqueId)
        {
            return "<" + containingMethodName + ">b__" + uniqueId;
        }

        internal static string MakeIteratorFinallyMethodName(int iteratorState)
        {
            // we can pick any name, but we will try to do
            // <>m__Finally1
            // <>m__Finally2
            // <>m__Finally3
            // . . . 
            // that will roughly match native naming scheme and may also be easier when need to debug.
            return "<>m__Finally" + Math.Abs(iteratorState + 2);
        }

        internal static string MakeLambdaDisplayClassName(int uniqueId)
        {
            Debug.Assert((char)GeneratedNameKind.LambdaDisplayClassType == 'c');
            return "<>c__DisplayClass" + uniqueId;
        }

        internal static string MakeAnonymousTypeTemplateName(int index, int submissionSlotIndex, string moduleId)
        {
            var name = "<" + moduleId + ">f__AnonymousType" + index;
            if (submissionSlotIndex >= 0)
            {
                name += "#" + submissionSlotIndex;
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

        internal static string MakeStateMachineTypeName(string methodName, int uniqueId)
        {
            methodName = EnsureNoDotsInTypeName(methodName);

            Debug.Assert((char)GeneratedNameKind.StateMachineType == 'd');
            return "<" + methodName + ">d__" + uniqueId;
        }

        private static string EnsureNoDotsInTypeName(string name)
        {
            // CLR generally allows names with dots, however some APIs like IMetaDataImport
            // can only return full type names combined with namespaces. 
            // see: http://msdn.microsoft.com/en-us/library/ms230143.aspx (IMetaDataImport::GetTypeDefProps)
            // When working with such APIs, names with dots become ambiguous since metadata 
            // consumer cannot figure where namespace ends and actual type name starts.
            // Therefore it is a good practice to avoid type names with dots.
            if (name.IndexOf('.') >= 0)
            {
                name = name.Replace('.', '_');
            }
            return name;
        }

        internal static string MakeFabricatedMethodName(int uniqueId)
        {
            return "<>n__FabricatedMethod" + uniqueId;
        }

        internal static string MakeLambdaCacheFieldName(int uniqueId)
        {
            return "CS$<>9__CachedAnonymousMethodDelegate" + uniqueId;
        }

        internal static string MakeSynthesizedLocalName(SynthesizedLocalKind kind, ref int uniqueId)
        {
            Debug.Assert(kind.IsLongLived());

            // Lambda display class local has to be named. EE depends on the name format. 
            if (kind == SynthesizedLocalKind.LambdaDisplayClass)
            {
                return MakeLambdaDisplayClassStorageName(uniqueId++);
            }

            return null;
        }

        internal static string MakeLambdaDisplayClassStorageName(int uniqueId)
        {
            Debug.Assert((char)GeneratedNameKind.DisplayClassLocalOrField == '8');
            return SynthesizedLocalNamePrefix + "<>8__locals" + uniqueId;
        }

        internal static bool IsSynthesizedLocalName(string name)
        {
            return name.StartsWith(SynthesizedLocalNamePrefix, StringComparison.Ordinal);
        }

        internal static string MakeFixedFieldImplementationName(string fieldName)
        {
            // the native compiler adds numeric digits at the end.  Roslyn does not.
            return "<" + fieldName + ">" + "e__FixedBuffer";
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

        internal static string MakeIteratorCurrentBackingFieldName()
        {
            return "<>2__current";
        }

        internal static string MakeIteratorCurrentThreadIdName()
        {
            return "<>l__initialThreadId";
        }

        internal static string MakeHoistedLocalFieldName(string localName, int localNumber)
        {
            Debug.Assert((char)GeneratedNameKind.HoistedLocalField == '5');
            return "<" + localName + ">5__" + localNumber;
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

        internal static string MakeDynamicCallSiteContainerName(string methodName, int uniqueId)
        {
            methodName = EnsureNoDotsInTypeName(methodName);

            return "<" + methodName + ">o__SiteContainer" + uniqueId;
        }

        internal static string MakeDynamicCallSiteFieldName(int uniqueId)
        {
            return "<>p__Site" + uniqueId;
        }

        internal static string AsyncBuilderFieldName()
        {
            // Microsoft.VisualStudio.VIL.VisualStudioHost.AsyncReturnStackFrame depends on this name.
            return "<>t__builder";
        }

        internal static string AsyncAwaiterFieldName(int number)
        {
            return "<>u__$awaiter" + number;
        }

        internal static string ReusableHoistedLocalFieldName(int number)
        {
            return "<>7__wrap" + number;
        }
    }
}
