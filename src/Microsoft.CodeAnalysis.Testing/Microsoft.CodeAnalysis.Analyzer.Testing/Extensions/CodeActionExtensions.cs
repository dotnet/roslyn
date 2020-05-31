// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class CodeActionExtensions
    {
        public static ImmutableArray<CodeAction> GetNestedActions(this CodeAction action)
        {
            var property = typeof(CodeAction).GetTypeInfo().DeclaredProperties.SingleOrDefault(property => property.Name == "NestedCodeActions");
            if (property is null)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            return (ImmutableArray<CodeAction>)property.GetValue(action);
        }
    }
}
