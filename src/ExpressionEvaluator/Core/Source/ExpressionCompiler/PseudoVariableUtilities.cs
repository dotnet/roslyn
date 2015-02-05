// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal enum PseudoVariableKind
    {
        None,
        Exception,
        StowedException,
        ReturnValue,
        ObjectId,
        DeclaredLocal,
    }

    internal static class PseudoVariableUtilities
    {
        internal static bool TryParseVariableName(string name, bool caseSensitive, out PseudoVariableKind kind, out string id, out int index)
        {
            if (!name.StartsWith("$", StringComparison.Ordinal))
            {
                kind = PseudoVariableKind.DeclaredLocal;
                id = name;
                index = -1;
                return true;
            }

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (string.Equals(name, "$exception", comparison))
            {
                kind = PseudoVariableKind.Exception;
                id = name;
                index = 0;
                return true;
            }
            else if (string.Equals(name, "$stowedexception", comparison))
            {
                kind = PseudoVariableKind.StowedException;
                id = name;
                index = 0;
                return true;
            }
            // Allow lowercase version of $ReturnValue, even with case-sensitive match.
            else if (name.StartsWith("$ReturnValue", comparison) ||
                (caseSensitive && name.StartsWith("$returnvalue", comparison)))
            {
                var suffix = name.Substring(12);
                index = 0;
                if ((suffix.Length == 0) || int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out index))
                {
                    Debug.Assert(index >= 0);
                    kind = PseudoVariableKind.ReturnValue;
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
                    kind = PseudoVariableKind.ObjectId;
                    id = suffix;
                    return true;
                }
            }

            kind = PseudoVariableKind.None;
            index = -1;
            id = null;
            return false;
        }

        internal static string GetTypeName(InspectionContext context, PseudoVariableKind kind, string id, int index)
        {
            switch (kind)
            {
                case PseudoVariableKind.Exception:
                    return context.GetExceptionTypeName();
                case PseudoVariableKind.StowedException:
                    return context.GetStowedExceptionTypeName();
                case PseudoVariableKind.ReturnValue:
                    return context.GetReturnValueTypeName(index);
                case PseudoVariableKind.ObjectId:
                case PseudoVariableKind.DeclaredLocal:
                    return context.GetObjectTypeNameById(id);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }
}
