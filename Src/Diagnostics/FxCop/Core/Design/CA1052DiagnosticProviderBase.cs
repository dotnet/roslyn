// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FxCopRules.DiagnosticProviders.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopRules.DiagnosticProviders.Design
{
    /// <summary>
    /// CA1025: Static holder types should be sealed
    /// </summary>
    public abstract class CA1052DiagnosticProviderBase : DiagnosticProviderBase
    {
        internal const string RuleName = "CA1052";
        internal static DiagnosticMetadata Rule = new DiagnosticMetadata(RuleName,
                                                                         DiagnosticKind.Compiler,
                                                                         FxCopRulesResources.StaticHolderTypesShouldBeSealed,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning);

        public override IEnumerable<DiagnosticMetadata> GetSupportedDiagnostics()
        {
            return SpecializedCollections.SingletonEnumerable(Rule);
        }

        protected sealed override IEnumerable<Diagnostic> GetDiagnosticsForNode(SyntaxNode node, SemanticModel model)
        {
            var typeSymbol = model.GetDeclaredSymbol(node);
            var namedType = typeSymbol as INamedTypeSymbol;

            // static holder types are not already static/sealed and must be public or protected
            if (namedType != null && !namedType.IsStatic && !namedType.IsSealed
                && (namedType.DeclaredAccessibility == Accessibility.Public || namedType.DeclaredAccessibility == Accessibility.Protected))
            {
                // only get the explicitly declared members
                var allMembers = namedType.GetMembers().Where(member => !member.IsImplicitlyDeclared);
                if (!allMembers.Any())
                {
                    return null;
                }

                // to be a static holder type, all members must be static and not operator overloads
                if (allMembers.All(member => member.IsStatic && !IsUserdefinedOperator(member)))
                {
                    var diagnostic = typeSymbol.CreateDiagnostic(Rule, string.Format(FxCopRulesResources.TypeIsStaticHolderButNotSealed, namedType.Name));
                    return SpecializedCollections.SingletonEnumerable(diagnostic);
                }
            }

            return null;
        }

        private static bool IsUserdefinedOperator(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.UserDefinedOperator;
        }
    }
}
