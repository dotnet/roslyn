// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct Alias
    {
        internal Alias(DkmClrAliasKind kind, string name, string fullName, string type, CustomTypeInfo customTypeInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullName));
            Debug.Assert(!string.IsNullOrEmpty(type));

            this.Kind = kind;
            this.Name = name;
            this.FullName = fullName;
            this.Type = type;
            this.CustomTypeInfo = customTypeInfo;
        }

        internal readonly DkmClrAliasKind Kind;
        internal readonly string Name;
        internal readonly string FullName;
        internal readonly string Type;
        internal readonly CustomTypeInfo CustomTypeInfo;
    }

    internal static class PseudoVariableUtilities
    {
        internal static bool TryParseReturnValueIndex(string name, out int index)
        {
            Debug.Assert(name.StartsWith("$ReturnValue", StringComparison.OrdinalIgnoreCase));
            const int prefixLength = 12; // "$ReturnValue"
            int n = name.Length;
            index = 0;
            return (n == prefixLength) ||
                ((n > prefixLength) && int.TryParse(name.Substring(prefixLength), NumberStyles.None, CultureInfo.InvariantCulture, out index));
        }

        internal static DkmClrCompilationResultFlags GetLocalResultFlags(this Alias alias)
        {
            switch (alias.Kind)
            {
                case DkmClrAliasKind.Exception:
                case DkmClrAliasKind.StowedException:
                case DkmClrAliasKind.ReturnValue:
                    return DkmClrCompilationResultFlags.ReadOnlyResult;
                default:
                    return DkmClrCompilationResultFlags.None;
            }
        }

        internal static bool IsReturnValueWithoutIndex(this Alias alias)
        {
            Debug.Assert(alias.Kind != DkmClrAliasKind.ReturnValue || 
                alias.FullName.StartsWith("$ReturnValue", StringComparison.OrdinalIgnoreCase));
            return
                alias.Kind == DkmClrAliasKind.ReturnValue &&
                alias.FullName.Length == 12; // "$ReturnValue"
        }
    }
}
