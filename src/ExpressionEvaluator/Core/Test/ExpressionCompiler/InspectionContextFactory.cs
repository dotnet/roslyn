// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class InspectionContextFactory
    {
        internal static readonly InspectionContextImpl Empty = new InspectionContextImpl(new ReadOnlyCollection<Alias>(new Alias[0]));

        internal sealed class InspectionContextImpl : InspectionContext
        {
            private readonly ReadOnlyCollection<Alias> _variables;

            internal InspectionContextImpl(ReadOnlyCollection<Alias> variables)
            {
                _variables = variables;
            }

            internal InspectionContextImpl Add(string id, Type type, CustomTypeInfo customTypeInfo = default(CustomTypeInfo))
            {
                return Add(id, type.AssemblyQualifiedName, customTypeInfo);
            }

            internal InspectionContextImpl Add(string id, string typeName, CustomTypeInfo customTypeInfo = default(CustomTypeInfo))
            {
                var builder = ArrayBuilder<Alias>.GetInstance();
                builder.AddRange(_variables);
                builder.Add(new Alias(GetPseudoVariableKind(id), id, id, typeName, customTypeInfo));
                return new InspectionContextImpl(new ReadOnlyCollection<Alias>(builder.ToArrayAndFree()));
            }

            internal override string GetExceptionTypeName()
            {
                return this.GetType("$exception");
            }

            internal override string GetStowedExceptionTypeName()
            {
                return this.GetType("$stowedexception");
            }

            internal override string GetReturnValueTypeName(int index)
            {
                string id = "$ReturnValue";
                if (index != 0)
                {
                    id += index;
                }
                return this.GetType(id);
            }

            internal override string GetObjectTypeNameById(string id)
            {
                Debug.Assert(!id.StartsWith("$", StringComparison.Ordinal));
                return this.GetType(id);
            }

            private string GetType(string id)
            {
                var alias = _variables.FirstOrDefault(a => a.FullName == id);
                return alias.Type;
            }

            private static AliasKind GetPseudoVariableKind(string name)
            {
                if (name.StartsWith("$", StringComparison.Ordinal))
                {
                    if (name.Equals("$exception", StringComparison.OrdinalIgnoreCase))
                    {
                        return AliasKind.Exception;
                    }
                    else if (name.Equals("$stowedexception", StringComparison.OrdinalIgnoreCase))
                    {
                        return AliasKind.StowedException;
                    }
                    else if (name.StartsWith("$returnvalue", StringComparison.OrdinalIgnoreCase))
                    {
                        return AliasKind.ReturnValue;
                    }
                    else
                    {
                        // '$' should only be used for well-known names.
                        throw ExceptionUtilities.UnexpectedValue(name);
                    }
                }
                else
                {
                    return AliasKind.DeclaredLocal;
                }
            }
        }
    }
}
