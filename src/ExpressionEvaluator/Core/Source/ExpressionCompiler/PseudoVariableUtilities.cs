// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal enum AliasKind
    {
        None,
        Exception,
        StowedException,
        ReturnValue,
        ObjectId,
        DeclaredLocal,
    }

    internal struct Alias
    {
        internal Alias(AliasKind kind, string name, string fullName, string type, CustomTypeInfo customTypeInfo)
        {
            Debug.Assert(kind != AliasKind.None);
            Debug.Assert(!string.IsNullOrEmpty(fullName));
            Debug.Assert(!string.IsNullOrEmpty(type));

            this.Kind = kind;
            this.Name = name;
            this.FullName = fullName;
            this.Type = type;
            this.CustomTypeInfo = customTypeInfo;
        }

        internal readonly AliasKind Kind;
        internal readonly string Name;
        internal readonly string FullName;
        internal readonly string Type;
        internal readonly CustomTypeInfo CustomTypeInfo;
    }

    internal static class PseudoVariableUtilities
    {
        internal static bool TryParseVariableName(string name, bool caseSensitive, out AliasKind kind, out string id, out int index)
        {
            if (!name.StartsWith("$", StringComparison.Ordinal))
            {
                kind = AliasKind.DeclaredLocal;
                id = name;
                index = -1;
                return true;
            }

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (string.Equals(name, "$exception", comparison))
            {
                kind = AliasKind.Exception;
                id = name;
                index = 0;
                return true;
            }
            else if (string.Equals(name, "$stowedexception", comparison))
            {
                kind = AliasKind.StowedException;
                id = name;
                index = 0;
                return true;
            }
            // Allow lowercase version of $ReturnValue, even with case-sensitive match.
            else if (name.StartsWith("$ReturnValue", comparison) ||
                (caseSensitive && name.StartsWith("$returnvalue", comparison)))
            {
                if (TryParseReturnValueIndex(name, out index))
                {
                    Debug.Assert(index >= 0);
                    kind = AliasKind.ReturnValue;
                    id = name;
                    return true;
                }
            }
            else
            {
                // Check for object id: "[$][1-9][0-9]*"
                var suffix = name.Substring(1);
                // Leading zeros are not supported.
                if (!suffix.StartsWith("0", comparison) && int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out index))
                {
                    Debug.Assert(index >= 0);
                    kind = AliasKind.ObjectId;
                    id = suffix;
                    return true;
                }
            }

            kind = AliasKind.None;
            id = null;
            index = -1;
            return false;
        }

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
                case AliasKind.Exception:
                case AliasKind.StowedException:
                case AliasKind.ReturnValue:
                    return DkmClrCompilationResultFlags.ReadOnlyResult;
                default:
                    return DkmClrCompilationResultFlags.None;
            }
        }

        internal static string GetTypeName(InspectionContext context, AliasKind kind, string id, int index)
        {
            switch (kind)
            {
                case AliasKind.Exception:
                    return context.GetExceptionTypeName();
                case AliasKind.StowedException:
                    return context.GetStowedExceptionTypeName();
                case AliasKind.ReturnValue:
                    return context.GetReturnValueTypeName(index);
                case AliasKind.ObjectId:
                case AliasKind.DeclaredLocal:
                    return context.GetObjectTypeNameById(id);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
