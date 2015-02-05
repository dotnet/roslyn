// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class InspectionContextFactory
    {
        internal static readonly InspectionContextImpl Empty = new InspectionContextImpl(ImmutableDictionary.Create<string, string>());

        internal sealed class InspectionContextImpl : InspectionContext
        {
            private readonly ImmutableDictionary<string, string> _types;

            internal InspectionContextImpl(ImmutableDictionary<string, string> types)
            {
                _types = types;
            }

            internal InspectionContextImpl Add(string id, Type type)
            {
                return Add(id, type.AssemblyQualifiedName);
            }

            internal InspectionContextImpl Add(string id, string typeName)
            {
                // '$' should only be used for well-known ids.
                Debug.Assert(!id.StartsWith("$", StringComparison.Ordinal) ||
                    id.Equals("$exception", StringComparison.InvariantCultureIgnoreCase) ||
                    id.Equals("$stowedexception", StringComparison.InvariantCultureIgnoreCase) ||
                    id.StartsWith("$ReturnValue", StringComparison.InvariantCultureIgnoreCase));

                return new InspectionContextImpl(_types.Add(id, typeName));
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
                string type;
                _types.TryGetValue(id, out type);
                return type;
            }
        }
    }
}
