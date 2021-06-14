// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents symbols imported to the binding scope via using namespace, using alias, and extern alias.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class Imports
    {
        internal static readonly Imports Empty = new Imports(
            ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
            ImmutableArray<AliasAndExternAliasDirective>.Empty);

        public readonly ImmutableDictionary<string, AliasAndUsingDirective> UsingAliases;
        public readonly ImmutableArray<NamespaceOrTypeAndUsingDirective> Usings;
        public readonly ImmutableArray<AliasAndExternAliasDirective> ExternAliases;

        private Imports(
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            Debug.Assert(usingAliases != null);
            Debug.Assert(!usings.IsDefault);
            Debug.Assert(!externs.IsDefault);

            this.UsingAliases = usingAliases;
            this.Usings = usings;
            this.ExternAliases = externs;
        }

        internal string GetDebuggerDisplay()
        {
            return string.Join("; ",
                UsingAliases.OrderBy(x => x.Value.UsingDirective.Location.SourceSpan.Start).Select(ua => $"{ua.Key} = {ua.Value.Alias.Target}").Concat(
                Usings.Select(u => u.NamespaceOrType.ToString())).Concat(
                ExternAliases.Select(ea => $"extern alias {ea.Alias.Name}")));

        }

<<<<<<< HEAD
        public static Imports FromSyntax(
            CSharpSyntaxNode declarationSyntax,
            InContainerBinder binder,
            ConsList<TypeSymbol> basesBeingResolved,
            bool inUsing)
        {
            SyntaxList<UsingDirectiveSyntax> usingDirectives;
            SyntaxList<ExternAliasDirectiveSyntax> externAliasDirectives;
            if (declarationSyntax.Kind() == SyntaxKind.CompilationUnit)
            {
                var compilationUnit = (CompilationUnitSyntax)declarationSyntax;
                // using directives are not in scope within using directives
                usingDirectives = inUsing ? default(SyntaxList<UsingDirectiveSyntax>) : compilationUnit.Usings;
                externAliasDirectives = compilationUnit.Externs;
            }
            else if (declarationSyntax.Kind() is SyntaxKind.NamespaceDeclaration or SyntaxKind.SingleLineNamespaceDeclaration)
            {
                var namespaceDecl = (BaseNamespaceDeclarationSyntax)declarationSyntax;
                // using directives are not in scope within using directives
                usingDirectives = inUsing ? default(SyntaxList<UsingDirectiveSyntax>) : namespaceDecl.Usings;
                externAliasDirectives = namespaceDecl.Externs;
            }
            else
            {
                return Empty;
            }

            if (usingDirectives.Count == 0 && externAliasDirectives.Count == 0)
            {
                return Empty;
            }

            // define all of the extern aliases first. They may used by the target of a using

            // using Bar=Goo::Bar;
            // using Goo::Baz;
            // extern alias Goo;

            var diagnostics = new DiagnosticBag();

            var compilation = binder.Compilation;

            var externAliases = BuildExternAliases(externAliasDirectives, binder, diagnostics);
            var usings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            ImmutableDictionary<string, AliasAndUsingDirective>.Builder usingAliases = null;
            if (usingDirectives.Count > 0)
            {
                // A binder that contains the extern aliases but not the usings. The resolution of the target of a using directive or alias 
                // should not make use of other peer usings.
                Binder usingsBinder;
                if (declarationSyntax.SyntaxTree.Options.Kind != SourceCodeKind.Regular)
                {
                    usingsBinder = compilation.GetBinderFactory(declarationSyntax.SyntaxTree).GetImportsBinder(declarationSyntax, inUsing: true);
                }
                else
                {
                    var imports = externAliases.Length == 0
                        ? Empty
                        : new Imports(
                            compilation,
                            ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
                            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
                            externAliases,
                            diagnostics: null);
                    usingsBinder = new InContainerBinder(binder.Container, binder.Next, imports);
                }

                var uniqueUsings = SpecializedSymbolCollections.GetPooledSymbolHashSetInstance<NamespaceOrTypeSymbol>();

                foreach (var usingDirective in usingDirectives)
                {
                    compilation.RecordImport(usingDirective);

                    if (usingDirective.Alias != null)
                    {
                        SyntaxToken identifier = usingDirective.Alias.Name.Identifier;
                        Location location = usingDirective.Alias.Name.Location;

                        if (identifier.ContextualKind() == SyntaxKind.GlobalKeyword)
                        {
                            diagnostics.Add(ErrorCode.WRN_GlobalAliasDefn, location);
                        }

                        if (usingDirective.StaticKeyword != default(SyntaxToken))
                        {
                            diagnostics.Add(ErrorCode.ERR_NoAliasHere, location);
                        }

                        SourceMemberContainerTypeSymbol.ReportTypeNamedRecord(identifier.Text, compilation, diagnostics, location);

                        string identifierValueText = identifier.ValueText;
                        if (usingAliases != null && usingAliases.ContainsKey(identifierValueText))
                        {
                            // Suppress diagnostics if we're already broken.
                            if (!usingDirective.Name.IsMissing)
                            {
                                // The using alias '{0}' appeared previously in this namespace
                                diagnostics.Add(ErrorCode.ERR_DuplicateAlias, location, identifierValueText);
                            }
                        }
                        else
                        {
                            // an O(m*n) algorithm here but n (number of extern aliases) will likely be very small.
                            foreach (var externAlias in externAliases)
                            {
                                if (externAlias.Alias.Name == identifierValueText)
                                {
                                    // The using alias '{0}' appeared previously in this namespace
                                    diagnostics.Add(ErrorCode.ERR_DuplicateAlias, usingDirective.Location, identifierValueText);
                                    break;
                                }
                            }

                            if (usingAliases == null)
                            {
                                usingAliases = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
                            }

                            // construct the alias sym with the binder for which we are building imports. That
                            // way the alias target can make use of extern alias definitions.
                            usingAliases.Add(identifierValueText, new AliasAndUsingDirective(new AliasSymbol(usingsBinder, usingDirective.Name, usingDirective.Alias), usingDirective));
                        }
                    }
                    else
                    {
                        if (usingDirective.Name.IsMissing)
                        {
                            //don't try to lookup namespaces inserted by parser error recovery
                            continue;
                        }

                        var directiveDiagnostics = BindingDiagnosticBag.GetInstance();

                        var declarationBinder = usingsBinder.WithAdditionalFlags(BinderFlags.SuppressConstraintChecks);
                        var imported = declarationBinder.BindNamespaceOrTypeSymbol(usingDirective.Name, directiveDiagnostics, basesBeingResolved).NamespaceOrTypeSymbol;

                        if (imported.Kind == SymbolKind.Namespace)
                        {
                            Debug.Assert(directiveDiagnostics.DependenciesBag.IsEmpty());

                            if (usingDirective.StaticKeyword != default(SyntaxToken))
                            {
                                diagnostics.Add(ErrorCode.ERR_BadUsingType, usingDirective.Name.Location, imported);
                            }
                            else if (!uniqueUsings.Add(imported))
                            {
                                diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, imported);
                            }
                            else
                            {
                                usings.Add(new NamespaceOrTypeAndUsingDirective(imported, usingDirective, dependencies: default));
                            }
                        }
                        else if (imported.Kind == SymbolKind.NamedType)
                        {
                            if (usingDirective.StaticKeyword == default(SyntaxToken))
                            {
                                diagnostics.Add(ErrorCode.ERR_BadUsingNamespace, usingDirective.Name.Location, imported);
                            }
                            else
                            {
                                var importedType = (NamedTypeSymbol)imported;
                                if (uniqueUsings.Contains(importedType))
                                {
                                    diagnostics.Add(ErrorCode.WRN_DuplicateUsing, usingDirective.Name.Location, importedType);
                                }
                                else
                                {
                                    declarationBinder.ReportDiagnosticsIfObsolete(diagnostics, importedType, usingDirective.Name, hasBaseReceiver: false);

                                    uniqueUsings.Add(importedType);
                                    usings.Add(new NamespaceOrTypeAndUsingDirective(importedType, usingDirective, directiveDiagnostics.DependenciesBag.ToImmutableArray()));
                                }
                            }
                        }
                        else if (imported.Kind != SymbolKind.ErrorType)
                        {
                            // Do not report additional error if the symbol itself is erroneous.

                            // error: '<symbol>' is a '<symbol kind>' but is used as 'type or namespace'
                            diagnostics.Add(ErrorCode.ERR_BadSKknown, usingDirective.Name.Location,
                                usingDirective.Name,
                                imported.GetKindText(),
                                MessageID.IDS_SK_TYPE_OR_NAMESPACE.Localize());
                        }

                        diagnostics.AddRange(directiveDiagnostics.DiagnosticBag);
                        directiveDiagnostics.Free();
                    }
                }

                uniqueUsings.Free();
            }

            if (diagnostics.IsEmptyWithoutResolution)
            {
                diagnostics = null;
            }

            return new Imports(compilation, usingAliases.ToImmutableDictionaryOrEmpty(), usings.ToImmutableAndFree(), externAliases, diagnostics);
        }

        public static Imports FromGlobalUsings(CSharpCompilation compilation)
        {
            var usings = compilation.Options.Usings;

            if (usings.Length == 0 && compilation.PreviousSubmission == null)
            {
                return Empty;
            }

            var diagnostics = new DiagnosticBag();
            var usingsBinder = new InContainerBinder(compilation.GlobalNamespace, new BuckStopsHereBinder(compilation));
            var boundUsings = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();
            var uniqueUsings = PooledHashSet<NamespaceOrTypeSymbol>.GetInstance();

            foreach (string @using in usings)
            {
                if (!@using.IsValidClrNamespaceName())
                {
                    continue;
                }

                string[] identifiers = @using.Split('.');
                NameSyntax qualifiedName = SyntaxFactory.IdentifierName(identifiers[0]);

                for (int j = 1; j < identifiers.Length; j++)
                {
                    qualifiedName = SyntaxFactory.QualifiedName(left: qualifiedName, right: SyntaxFactory.IdentifierName(identifiers[j]));
                }

                var directiveDiagnostics = BindingDiagnosticBag.GetInstance();

                var imported = usingsBinder.BindNamespaceOrTypeSymbol(qualifiedName, directiveDiagnostics).NamespaceOrTypeSymbol;
                if (uniqueUsings.Add(imported))
                {
                    boundUsings.Add(new NamespaceOrTypeAndUsingDirective(imported, null, dependencies: directiveDiagnostics.DependenciesBag.ToImmutableArray()));
                }

                diagnostics.AddRange(directiveDiagnostics.DiagnosticBag);
                directiveDiagnostics.Free();
            }

            if (diagnostics.IsEmptyWithoutResolution)
            {
                diagnostics = null;
            }

            var previousSubmissionImports = compilation.PreviousSubmission?.GlobalImports;
            if (previousSubmissionImports != null)
            {
                // Currently, only usings are supported.
                Debug.Assert(previousSubmissionImports.UsingAliases.IsEmpty);
                Debug.Assert(previousSubmissionImports.ExternAliases.IsEmpty);

                var expandedImports = ExpandPreviousSubmissionImports(previousSubmissionImports, compilation);

                foreach (var previousUsing in expandedImports.Usings)
                {
                    if (uniqueUsings.Add(previousUsing.NamespaceOrType))
                    {
                        boundUsings.Add(previousUsing);
                    }
                }
            }

            uniqueUsings.Free();

            if (boundUsings.Count == 0)
            {
                boundUsings.Free();
                return Empty;
            }

            return new Imports(compilation, ImmutableDictionary<string, AliasAndUsingDirective>.Empty, boundUsings.ToImmutableAndFree(), ImmutableArray<AliasAndExternAliasDirective>.Empty, diagnostics);
        }

=======
>>>>>>> upstream/features/FileScopedNamespaces
        // TODO (https://github.com/dotnet/roslyn/issues/5517): skip namespace expansion if references haven't changed.
        internal static Imports ExpandPreviousSubmissionImports(Imports previousSubmissionImports, CSharpCompilation newSubmission)
        {
            if (previousSubmissionImports == Empty)
            {
                return Empty;
            }

            Debug.Assert(previousSubmissionImports != null);
            Debug.Assert(newSubmission.IsSubmission);

            var expandedAliases = ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            if (!previousSubmissionImports.UsingAliases.IsEmpty)
            {
                var expandedAliasesBuilder = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
                foreach (var pair in previousSubmissionImports.UsingAliases)
                {
                    var name = pair.Key;
                    var directive = pair.Value;
                    expandedAliasesBuilder.Add(name, new AliasAndUsingDirective(directive.Alias.ToNewSubmission(newSubmission), directive.UsingDirective));
                }
                expandedAliases = expandedAliasesBuilder.ToImmutable();
            }

            var expandedUsings = ExpandPreviousSubmissionImports(previousSubmissionImports.Usings, newSubmission);

            return Imports.Create(
                expandedAliases,
                expandedUsings,
                previousSubmissionImports.ExternAliases);
        }

        internal static ImmutableArray<NamespaceOrTypeAndUsingDirective> ExpandPreviousSubmissionImports(ImmutableArray<NamespaceOrTypeAndUsingDirective> previousSubmissionUsings, CSharpCompilation newSubmission)
        {
            Debug.Assert(newSubmission.IsSubmission);

            if (!previousSubmissionUsings.IsEmpty)
            {
                var expandedUsingsBuilder = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance(previousSubmissionUsings.Length);
                var expandedGlobalNamespace = newSubmission.GlobalNamespace;

                foreach (var previousUsing in previousSubmissionUsings)
                {
                    var previousTarget = previousUsing.NamespaceOrType;
                    if (previousTarget.IsType)
                    {
                        expandedUsingsBuilder.Add(previousUsing);
                    }
                    else
                    {
                        var expandedNamespace = ExpandPreviousSubmissionNamespace((NamespaceSymbol)previousTarget, expandedGlobalNamespace);
                        expandedUsingsBuilder.Add(new NamespaceOrTypeAndUsingDirective(expandedNamespace, previousUsing.UsingDirective, dependencies: default));
                    }
                }

                return expandedUsingsBuilder.ToImmutableAndFree();
            }

            return previousSubmissionUsings;
        }

        internal static NamespaceSymbol ExpandPreviousSubmissionNamespace(NamespaceSymbol originalNamespace, NamespaceSymbol expandedGlobalNamespace)
        {
            // Soft assert: we'll still do the right thing if it fails.
            Debug.Assert(!originalNamespace.IsGlobalNamespace, "Global using to global namespace");

            // Hard assert: we depend on this.
            Debug.Assert(expandedGlobalNamespace.IsGlobalNamespace, "Global namespace required");

            var nameParts = ArrayBuilder<string>.GetInstance();
            var curr = originalNamespace;
            while (!curr.IsGlobalNamespace)
            {
                nameParts.Add(curr.Name);
                curr = curr.ContainingNamespace;
            }

            var expandedNamespace = expandedGlobalNamespace;
            for (int i = nameParts.Count - 1; i >= 0; i--)
            {
                // Note, the name may have become ambiguous (e.g. if a type with the same name
                // is now in scope), but we're not rebinding - we're just expanding to the
                // current contents of the same namespace.
                expandedNamespace = expandedNamespace.GetMembers(nameParts[i]).OfType<NamespaceSymbol>().Single();
            }
            nameParts.Free();

            return expandedNamespace;
        }

        public static Imports Create(
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            Debug.Assert(usingAliases != null);
            Debug.Assert(!usings.IsDefault);
            Debug.Assert(!externs.IsDefault);

            if (usingAliases.IsEmpty && usings.IsEmpty && externs.IsEmpty)
            {
                return Empty;
            }

            return new Imports(usingAliases, usings, externs);
        }

        /// <remarks>
        /// Does not preserve diagnostics.
        /// </remarks>
        internal Imports Concat(Imports otherImports)
        {
            Debug.Assert(otherImports != null);

            if (this == Empty)
            {
                return otherImports;
            }

            if (otherImports == Empty)
            {
                return this;
            }

            var usingAliases = this.UsingAliases.SetItems(otherImports.UsingAliases); // NB: SetItems, rather than AddRange
            var usings = this.Usings.AddRange(otherImports.Usings).Distinct(UsingTargetComparer.Instance);
            var externAliases = ConcatExternAliases(this.ExternAliases, otherImports.ExternAliases);

            return Imports.Create(usingAliases, usings, externAliases);
        }

        private static ImmutableArray<AliasAndExternAliasDirective> ConcatExternAliases(ImmutableArray<AliasAndExternAliasDirective> externs1, ImmutableArray<AliasAndExternAliasDirective> externs2)
        {
            if (externs1.Length == 0)
            {
                return externs2;
            }

            if (externs2.Length == 0)
            {
                return externs1;
            }

            var replacedExternAliases = PooledHashSet<string>.GetInstance();
            replacedExternAliases.AddAll(externs2.Select(e => e.Alias.Name));
            return externs1.WhereAsArray((e, replacedExternAliases) => !replacedExternAliases.Contains(e.Alias.Name), replacedExternAliases).AddRange(externs2);
        }

        private class UsingTargetComparer : IEqualityComparer<NamespaceOrTypeAndUsingDirective>
        {
            public static readonly IEqualityComparer<NamespaceOrTypeAndUsingDirective> Instance = new UsingTargetComparer();

            private UsingTargetComparer() { }

            bool IEqualityComparer<NamespaceOrTypeAndUsingDirective>.Equals(NamespaceOrTypeAndUsingDirective x, NamespaceOrTypeAndUsingDirective y)
            {
                return x.NamespaceOrType.Equals(y.NamespaceOrType);
            }

            int IEqualityComparer<NamespaceOrTypeAndUsingDirective>.GetHashCode(NamespaceOrTypeAndUsingDirective obj)
            {
                return obj.NamespaceOrType.GetHashCode();
            }
        }
    }
}
