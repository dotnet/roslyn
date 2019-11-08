// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder reports errors arising from resolving queries.
    /// </summary>
    internal partial class Binder
    {
        /// <summary>
        /// This is a clone of the Dev10 logic for reporting query errors.
        /// </summary>
        internal void ReportQueryLookupFailed(
            SyntaxNode queryClause,
            BoundExpression instanceArgument,
            string name,
            ImmutableArray<Symbol> symbols,
            DiagnosticBag diagnostics)
        {
            FromClauseSyntax fromClause = null;
            for (SyntaxNode node = queryClause; ; node = node.Parent)
            {
                var e = node as QueryExpressionSyntax;
                if (e != null)
                {
                    fromClause = e.FromClause;
                    break;
                }
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (instanceArgument.Type.IsDynamic())
            {
                // CS1979: Query expressions over source type 'dynamic' or with a join sequence of type 'dynamic' are not allowed
                diagnostics.Add(
                    new DiagnosticInfoWithSymbols(ErrorCode.ERR_BadDynamicQuery, Array.Empty<object>(), symbols),
                    new SourceLocation(queryClause));
            }
            else if (ImplementsStandardQueryInterface(instanceArgument.Type, name, ref useSiteDiagnostics))
            {
                // Could not find an implementation of the query pattern for source type '{0}'.  '{1}' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
                diagnostics.Add(new DiagnosticInfoWithSymbols(
                    ErrorCode.ERR_QueryNoProviderStandard,
                    new object[] { instanceArgument.Type, name },
                    symbols), new SourceLocation(fromClause != null ? fromClause.Expression : queryClause));
            }
            else if (fromClause is { Type: null } && HasCastToQueryProvider(instanceArgument.Type, ref useSiteDiagnostics))
            {
                // Could not find an implementation of the query pattern for source type '{0}'.  '{1}' not found.  Consider explicitly specifying the type of the range variable '{2}'.
                diagnostics.Add(new DiagnosticInfoWithSymbols(
                    ErrorCode.ERR_QueryNoProviderCastable,
                    new object[] { instanceArgument.Type, name, fromClause.Identifier.ValueText },
                    symbols), new SourceLocation(fromClause.Expression));
            }
            else
            {
                // Could not find an implementation of the query pattern for source type '{0}'.  '{1}' not found.
                diagnostics.Add(new DiagnosticInfoWithSymbols(
                    ErrorCode.ERR_QueryNoProvider,
                    new object[] { instanceArgument.Type, name },
                    symbols), new SourceLocation(fromClause != null ? fromClause.Expression : queryClause));
            }

            diagnostics.Add(queryClause, useSiteDiagnostics);
        }

        private bool ImplementsStandardQueryInterface(TypeSymbol instanceType, string name, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            if (instanceType.TypeKind == TypeKind.Array || name == "Cast" && HasCastToQueryProvider(instanceType, ref useSiteDiagnostics))
            {
                return true;
            }

            bool nonUnique = false;
            var originalType = instanceType.OriginalDefinition;
            var ienumerable_t = Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            var iqueryable_t = Compilation.GetWellKnownType(WellKnownType.System_Linq_IQueryable_T);
            bool isIenumerable = TypeSymbol.Equals(originalType, ienumerable_t, TypeCompareKind.ConsiderEverything2) || HasUniqueInterface(instanceType, ienumerable_t, ref nonUnique, ref useSiteDiagnostics);
            bool isQueryable = TypeSymbol.Equals(originalType, iqueryable_t, TypeCompareKind.ConsiderEverything2) || HasUniqueInterface(instanceType, iqueryable_t, ref nonUnique, ref useSiteDiagnostics);
            return isIenumerable != isQueryable && !nonUnique;
        }

        private static bool HasUniqueInterface(TypeSymbol instanceType, NamedTypeSymbol interfaceType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            bool nonUnique = false;
            return HasUniqueInterface(instanceType, interfaceType, ref nonUnique, ref useSiteDiagnostics);
        }

        private static bool HasUniqueInterface(TypeSymbol instanceType, NamedTypeSymbol interfaceType, ref bool nonUnique, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            TypeSymbol candidate = null;
            foreach (var i in instanceType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (TypeSymbol.Equals(i.OriginalDefinition, interfaceType, TypeCompareKind.ConsiderEverything2))
                {
                    if ((object)candidate == null)
                    {
                        candidate = i;
                    }
                    else if (!TypeSymbol.Equals(candidate, i, TypeCompareKind.ConsiderEverything2))
                    {
                        nonUnique = true;
                        return false; // not unique
                    }
                }
            }

            return (object)candidate != null;
        }

        private bool HasCastToQueryProvider(TypeSymbol instanceType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var originalType = instanceType.OriginalDefinition;
            var ienumerable = Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
            var iqueryable = Compilation.GetWellKnownType(WellKnownType.System_Linq_IQueryable);
            bool isIenumerable = TypeSymbol.Equals(originalType, ienumerable, TypeCompareKind.ConsiderEverything2) || HasUniqueInterface(instanceType, ienumerable, ref useSiteDiagnostics);
            bool isQueryable = TypeSymbol.Equals(originalType, iqueryable, TypeCompareKind.ConsiderEverything2) || HasUniqueInterface(instanceType, iqueryable, ref useSiteDiagnostics);
            return isIenumerable != isQueryable;
        }

        private static bool IsJoinRangeVariableInLeftKey(SimpleNameSyntax node)
        {
            for (CSharpSyntaxNode parent = node.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.Kind() == SyntaxKind.JoinClause)
                {
                    var join = (JoinClauseSyntax)parent;
                    if (join.LeftExpression.Span.Contains(node.Span) && join.Identifier.ValueText == node.Identifier.ValueText) return true;
                }
            }

            return false;
        }

        private static bool IsInJoinRightKey(SimpleNameSyntax node)
        {
            // TODO: refine this test to check if the identifier is the name of a range
            // variable of the enclosing query.
            for (CSharpSyntaxNode parent = node.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.Kind() == SyntaxKind.JoinClause)
                {
                    var join = (JoinClauseSyntax)parent;
                    if (join.RightExpression.Span.Contains(node.Span)) return true;
                }
            }

            return false;
        }

        internal static void ReportQueryInferenceFailed(CSharpSyntaxNode queryClause, string methodName, BoundExpression receiver, AnalyzedArguments arguments, ImmutableArray<Symbol> symbols, DiagnosticBag diagnostics)
        {
            string clauseKind = null;
            bool multiple = false;
            switch (queryClause.Kind())
            {
                case SyntaxKind.JoinClause:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.JoinKeyword);
                    multiple = true;
                    break;
                case SyntaxKind.LetClause:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.LetKeyword);
                    break;
                case SyntaxKind.SelectClause:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.SelectKeyword);
                    break;
                case SyntaxKind.WhereClause:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.WhereKeyword);
                    break;
                case SyntaxKind.OrderByClause:
                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.OrderByKeyword);
                    multiple = true;
                    break;
                case SyntaxKind.QueryContinuation:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.IntoKeyword);
                    break;
                case SyntaxKind.GroupClause:
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.GroupKeyword) + " " + SyntaxFacts.GetText(SyntaxKind.ByKeyword);
                    multiple = true;
                    break;
                case SyntaxKind.FromClause:
                    if (ReportQueryInferenceFailedSelectMany((FromClauseSyntax)queryClause, methodName, receiver, arguments, symbols, diagnostics))
                    {
                        return;
                    }
                    clauseKind = SyntaxFacts.GetText(SyntaxKind.FromKeyword);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(queryClause.Kind());
            }

            diagnostics.Add(new DiagnosticInfoWithSymbols(
                multiple ? ErrorCode.ERR_QueryTypeInferenceFailedMulti : ErrorCode.ERR_QueryTypeInferenceFailed,
                new object[] { clauseKind, methodName },
                symbols), queryClause.GetFirstToken().GetLocation());
        }

        private static bool ReportQueryInferenceFailedSelectMany(FromClauseSyntax fromClause, string methodName, BoundExpression receiver, AnalyzedArguments arguments, ImmutableArray<Symbol> symbols, DiagnosticBag diagnostics)
        {
            Debug.Assert(methodName == "SelectMany");

            // Estimate the return type of Select's lambda argument
            BoundExpression arg = arguments.Argument(arguments.IsExtensionMethodInvocation ? 1 : 0);
            TypeSymbol type = null;
            if (arg.Kind == BoundKind.UnboundLambda)
            {
                var unbound = (UnboundLambda)arg;
                foreach (var t in unbound.Data.InferredReturnTypes())
                {
                    if (!t.IsErrorType())
                    {
                        type = t;
                        break;
                    }
                }
            }

            if ((object)type == null || type.IsErrorType())
            {
                return false;
            }

            TypeSymbol receiverType = receiver?.Type;
            diagnostics.Add(new DiagnosticInfoWithSymbols(
                ErrorCode.ERR_QueryTypeInferenceFailedSelectMany,
                new object[] { type, receiverType, methodName },
                symbols), fromClause.Expression.Location);
            return true;
        }
    }
}
