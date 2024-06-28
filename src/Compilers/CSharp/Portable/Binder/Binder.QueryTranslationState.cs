// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private class QueryTranslationState
        {
            // Represents the current translation state for a query.  Consider a query of the form
            // from ID in EXPR { clauses } SELECT ...

#if DEBUG
            /// <summary>
            /// For debug assert only
            /// </summary>
            public string nextInvokedMethodName;
#endif

            // EXPR, above
            public BoundExpression fromExpression;

            // ID, above.  This may possibly be the special "transparent" identifier
            public RangeVariableSymbol rangeVariable;

            // the clauses, above.  The top of the stack is the leftmost clause
            public readonly Stack<QueryClauseSyntax> clauses = new Stack<QueryClauseSyntax>();

            // the SELECT clause above (or a groupby clause in its place)
            public SelectOrGroupClauseSyntax selectOrGroup;

            // all query variables in scope, including those visible through transparent identifiers
            // introduced in previous translation phases.  Every query variable in scope is a key in
            // this dictionary.  To compute its value, one consults the list of strings that are the
            // value in this map.  If it is empty, there is a lambda parameter for the variable.  If it
            // is nonempty, one starts with the transparent lambda identifier, and follows fields of the
            // given names in reverse order.  So, for example, if the strings are ["a", "b"], the query
            // variable is represented by the expression TRANSPARENT.b.a where TRANSPARENT is a parameter
            // of the current lambda expression.
            public readonly Dictionary<RangeVariableSymbol, ArrayBuilder<string>> allRangeVariables = new Dictionary<RangeVariableSymbol, ArrayBuilder<string>>();

            public static RangeVariableMap RangeVariableMap(params RangeVariableSymbol[] parameters)
            {
                var result = new RangeVariableMap();
                foreach (var vars in parameters)
                {
                    result.Add(vars, ImmutableArray<string>.Empty);
                }
                return result;
            }

            public RangeVariableMap RangeVariableMap()
            {
                var result = new RangeVariableMap();
                foreach (var vars in allRangeVariables.Keys)
                {
                    result.Add(vars, allRangeVariables[vars].ToImmutable());
                }
                return result;
            }

            internal RangeVariableSymbol AddRangeVariable(Binder binder, SyntaxToken identifier, BindingDiagnosticBag diagnostics)
            {
                string name = identifier.ValueText;
                var result = new RangeVariableSymbol(name, binder.ContainingMemberOrLambda, identifier.GetLocation());
                bool error = false;

                Debug.Assert(identifier.Parent is { });

                foreach (var existingRangeVariable in allRangeVariables.Keys)
                {
                    if (existingRangeVariable.Name == name)
                    {
                        diagnostics.Add(ErrorCode.ERR_QueryDuplicateRangeVariable, identifier.GetLocation(), name);
                        error = true;
                    }
                }

                if (!error && (object)diagnostics != BindingDiagnosticBag.Discarded)
                {
                    var collisionDetector = new LocalScopeBinder(binder);
                    collisionDetector.ValidateDeclarationNameConflictsInScope(result, diagnostics);
                }

                allRangeVariables.Add(result, ArrayBuilder<string>.GetInstance());
                return result;
            }

            // Add a new lambda that is a transparent identifier, by providing the name that is the
            // field of the new transparent lambda parameter that contains the old variables.
            internal void AddTransparentIdentifier(string name)
            {
                foreach (var b in allRangeVariables.Values)
                {
                    b.Add(name);
                }
            }

            private int _nextTransparentIdentifierNumber;

            internal string TransparentRangeVariableName()
            {
                return transparentIdentifierPrefix + _nextTransparentIdentifierNumber++;
            }

            internal RangeVariableSymbol TransparentRangeVariable(Binder binder)
            {
                var transparentIdentifier = TransparentRangeVariableName();
                return new RangeVariableSymbol(transparentIdentifier, binder.ContainingMemberOrLambda, null, true);
            }

            public void Clear()
            {
#if DEBUG
                Debug.Assert(nextInvokedMethodName is null);
                nextInvokedMethodName = null;
#endif

                fromExpression = null;
                rangeVariable = null;
                selectOrGroup = null;
                foreach (var b in allRangeVariables.Values) b.Free();
                allRangeVariables.Clear();
                clauses.Clear();
            }

            public void Free()
            {
                Clear();
            }
        }
    }
}
