// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Naming
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1708DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1708";
        internal const string Namespace = "Namespaces";
        internal const string Type = "Types";
        internal const string Member = "Members";
        internal const string Parameter = "Parameters of";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                                      new LocalizableResourceString(nameof(FxCopRulesResources.IdentifiersShouldDifferByMoreThanCase), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources)),
                                                                                      new LocalizableResourceString(nameof(FxCopRulesResources.IdentifierNamesShouldDifferMoreThanCase), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources)),
                                                                                      FxCopDiagnosticCategory.Naming,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: false,
                                                                                      description: new LocalizableResourceString(nameof(FxCopRulesResources.IdentifiersShouldDifferByMoreThanCaseDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources)),
                                                                                      helpLinkUri: "http://msdn.microsoft.com/library/ms182242.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            base.Initialize(analysisContext);

            analysisContext.RegisterCompilationAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            var globalNamespaces = context.Compilation.GlobalNamespace.GetNamespaceMembers()
                .Where(item => item.ContainingAssembly == context.Compilation.Assembly);

            var globalTypes = context.Compilation.GlobalNamespace.GetTypeMembers().Where(item =>
                    item.ContainingAssembly == context.Compilation.Assembly &&
                    IsExternallyVisible(item));

            CheckTypeNames(globalTypes, context.ReportDiagnostic);
            CheckNamespaceMembers(globalNamespaces, context.Compilation, context.ReportDiagnostic);
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol namedTypeSymbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            // Do not descent into non-publicly visible types
            // Note: This is the behavior of FxCop, it might be more correct to descend into internal but not private
            // types because "InternalsVisibleTo" could be set. But it might be bad for users to start seeing warnings
            // where they previously did not from FxCop.
            if (namedTypeSymbol.GetResultantVisibility() != SymbolVisibility.Public)
            {
                return;
            }

            // Get externally visible members in the given type
            var members = namedTypeSymbol.GetMembers().Where(item => !item.IsAccessorMethod() && IsExternallyVisible(item));

            if (members.Any())
            {
                // Check parameters names of externally visible members with parameters
                CheckParameterMembers(members, addDiagnostic);

                // Check names of externally visible type members and their members
                CheckTypeMembers(members, addDiagnostic);
            }
        }

        private void CheckNamespaceMembers(IEnumerable<INamespaceSymbol> namespaces, Compilation compilation, Action<Diagnostic> addDiagnostic)
        {
            HashSet<INamespaceSymbol> excludedNamespaces = new HashSet<INamespaceSymbol>();
            foreach (var @namespace in namespaces)
            {
                // Get all the potentially externally visible types in the namespace
                var typeMembers = @namespace.GetTypeMembers().Where(item =>
                    item.ContainingAssembly == compilation.Assembly &&
                    IsExternallyVisible(item));

                if (typeMembers.Any())
                {
                    CheckTypeNames(typeMembers, addDiagnostic);
                }
                else
                {
                    // If the namespace does not contain any externally visible types then exclude it from namecheck
                    excludedNamespaces.Add(@namespace);
                }

                var namespaceMembers = @namespace.GetNamespaceMembers();
                if (namespaceMembers.Any())
                {
                    CheckNamespaceMembers(namespaceMembers, compilation, addDiagnostic);

                    // If there is a child namespace that has externally visible types, then remove the parent namespace from exclusion list
                    if (namespaceMembers.Any(item => !excludedNamespaces.Contains(item)))
                    {
                        excludedNamespaces.Remove(@namespace);
                    }
                }
            }

            // Before name check, remove all namespaces that don't contain externally visible types in current scope
            namespaces = namespaces.Where(item => !excludedNamespaces.Contains(item));

            CheckNamespaceNames(namespaces, addDiagnostic);
        }

        private static void CheckTypeMembers(IEnumerable<ISymbol> members, Action<Diagnostic> addDiagnostic)
        {
            // Remove constructors, indexers, operators and destructors for namecheck
            var membersForNameCheck = members.Where(item => !item.IsConstructor() && !item.IsDestructor() && !item.IsIndexer() && !item.IsUserDefinedOperator());
            if (membersForNameCheck.Any())
            {
                CheckMemberNames(membersForNameCheck, addDiagnostic);
            }
        }

        private static void CheckParameterMembers(IEnumerable<ISymbol> members, Action<Diagnostic> addDiagnostic)
        {
            var violatingMembers = members
                .Where(item => item.ContainingType.DelegateInvokeMethod == null && HasViolatingParameters(item));

            var violatingDelegates = members.Select(item =>
            {
                var typeSymbol = item as INamedTypeSymbol;
                if (typeSymbol != null &&
                    typeSymbol.DelegateInvokeMethod != null &&
                    HasViolatingParameters(typeSymbol.DelegateInvokeMethod))
                {
                    return item;
                }
                else
                {
                    return null;
                }
            }).WhereNotNull();

            foreach (var symbol in violatingMembers.Concat(violatingDelegates))
            {
                addDiagnostic(symbol.CreateDiagnostic(Rule, Parameter, symbol.ToDisplayString()));
            }
        }

        #region NameCheck Methods

        private static void CheckParameterNames(IEnumerable<IParameterSymbol> parameters, Action<Diagnostic> addDiagnostic)
        {
            // If there is only one parameter, then return
            if (!parameters.Skip(1).Any())
            {
                return;
            }

            var parameterList = parameters.GroupBy((item) => item.Name, StringComparer.InvariantCultureIgnoreCase).Where((group) => group.Count() > 1);

            foreach (var group in parameterList)
            {
                var symbol = group.First().ContainingSymbol;
                addDiagnostic(symbol.CreateDiagnostic(Rule, Parameter, symbol.ToDisplayString()));
            }
        }

        private static bool HasViolatingParameters(ISymbol symbol)
        {
            var parameters = symbol.GetParameters();

            // If there is only one parameter, then return an empty collection
            if (!parameters.Skip(1).Any())
            {
                return false;
            }

            return parameters.GroupBy(item => item.Name, StringComparer.InvariantCultureIgnoreCase).Where((group) => group.Count() > 1).Any();
        }

        private static void CheckMemberNames(IEnumerable<ISymbol> members, Action<Diagnostic> addDiagnostic)
        {
            // If there is only one member, then return
            if (!members.Skip(1).Any())
            {
                return;
            }

            var overloadedMembers = members.Where((item) => !item.IsType()).GroupBy((item) => item.Name).Where((group) => group.Count() > 1).SelectMany((group) => group.Skip(1));
            var memberList = members.Where((item) => !overloadedMembers.Contains(item)).GroupBy((item) => DiagnosticHelpers.GetMemberName(item), StringComparer.InvariantCultureIgnoreCase).Where((group) => group.Count() > 1);

            foreach (var group in memberList)
            {
                var symbol = group.First().ContainingSymbol;
                addDiagnostic(symbol.CreateDiagnostic(Rule, Member, GetSymbolDisplayString(group)));
            }
        }

        private static void CheckTypeNames(IEnumerable<ITypeSymbol> types, Action<Diagnostic> addDiagnostic)
        {
            // If there is only one type, then return
            if (!types.Skip(1).Any())
            {
                return;
            }

            var typeList = types.GroupBy((item) => DiagnosticHelpers.GetMemberName(item), StringComparer.InvariantCultureIgnoreCase)
                .Where((group) => group.Count() > 1);

            foreach (var group in typeList)
            {
                addDiagnostic(Diagnostic.Create(Rule, Location.None, Type, GetSymbolDisplayString(group)));
            }
        }

        private static void CheckNamespaceNames(IEnumerable<INamespaceSymbol> namespaces, Action<Diagnostic> addDiagnostic)
        {
            // If there is only one namespace, then return
            if (!namespaces.Skip(1).Any())
            {
                return;
            }

            var namespaceList = namespaces.GroupBy((item) => item.ToDisplayString(), StringComparer.InvariantCultureIgnoreCase).Where((group) => group.Count() > 1);

            foreach (var group in namespaceList)
            {
                addDiagnostic(Diagnostic.Create(Rule, Location.None, Namespace, GetSymbolDisplayString(group)));
            }
        }

        #endregion

        #region Helper Methods

        private static string GetSymbolDisplayString(IGrouping<string, ISymbol> group)
        {
            return string.Join(", ", group.Select(s => s.ToDisplayString()).OrderBy(k => k, StringComparer.InvariantCulture));
        }

        public static bool IsExternallyVisible(ISymbol symbol)
        {
            var visibility = symbol.GetResultantVisibility();
            return visibility == SymbolVisibility.Public || visibility == SymbolVisibility.Internal;
        }

        #endregion
    }
}
