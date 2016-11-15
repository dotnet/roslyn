﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddMissingReference
{
    internal abstract partial class AbstractAddMissingReferenceCodeFixProvider<TIdentifierNameSyntax> : 
        AbstractAddPackageCodeFixProvider
        where TIdentifierNameSyntax : SyntaxNode
    {
        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AbstractAddMissingReferenceCodeFixProvider(
            IPackageInstallerService packageInstallerService = null,
            ISymbolSearchService symbolSearchService = null)
            : base(packageInstallerService, symbolSearchService)
        {
        }

        protected override bool IncludePrerelease => false;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var uniqueIdentities = await GetUniqueIdentitiesAsync(context).ConfigureAwait(false);

            var assemblyNames = uniqueIdentities.Select(i => i.Name).ToSet();
            var addPackageCodeActions = await GetAddPackagesCodeActionsAsync(context, assemblyNames).ConfigureAwait(false);
            var addReferenceCodeActions = await GetAddReferencesCodeActionsAsync(context, uniqueIdentities).ConfigureAwait(false);

            context.RegisterFixes(addPackageCodeActions, context.Diagnostics);
            context.RegisterFixes(addReferenceCodeActions, context.Diagnostics);
        }

        private static async Task<ImmutableArray<CodeAction>> GetAddReferencesCodeActionsAsync(CodeFixContext context, ISet<AssemblyIdentity> uniqueIdentities)
        {
            var result = ArrayBuilder<CodeAction>.GetInstance();
            foreach (var identity in uniqueIdentities)
            {
                var codeAction = await AddMissingReferenceCodeAction.CreateAsync(
                    context.Document.Project, identity, context.CancellationToken).ConfigureAwait(false);
                result.Add(codeAction);
            }

            return result.ToImmutableAndFree();
        }

        private async Task<ISet<AssemblyIdentity>> GetUniqueIdentitiesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            var uniqueIdentities = new HashSet<AssemblyIdentity>();
            foreach (var diagnostic in context.Diagnostics)
            {
                var nodes = FindNodes(root, diagnostic);
                var types = GetTypesForNodes(semanticModel, nodes, cancellationToken).Distinct();
                var message = diagnostic.GetMessage();
                var identity = GetAssemblyIdentity(types, message);

                if (identity != null &&
                    !identity.Equals(compilation.Assembly.Identity))
                {
                    uniqueIdentities.Add(identity);
                }
            }

            return uniqueIdentities;
        }

        /// <summary>
        /// Find all the identifier names in the given location, any of these could be the symbols triggering the diagnostic.
        /// </summary>
        private static IEnumerable<SyntaxNode> FindNodes(SyntaxNode root, Diagnostic diagnostic)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            return node.DescendantNodesAndSelf().OfType<TIdentifierNameSyntax>();
        }

        /// <summary>
        /// Get all of the symbols we can for the given nodes since we have no way to determine up front which symbol is triggering the error.
        /// </summary>
        private static IEnumerable<ITypeSymbol> GetTypesForNodes(SemanticModel model, IEnumerable<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            var symbols = new List<ITypeSymbol>();
            foreach (var node in nodes)
            {
                symbols.Add(model.GetTypeInfo(node, cancellationToken).Type);
                var symbol = model.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
                symbols.AddRange(GetTypesFromSymbol(symbol));
                symbols.AddRange(GetTypesFromSymbol(symbol?.OriginalDefinition));
            }

            return symbols;
        }

        /// <summary>
        /// Look for additional symbols related to the symbol that we have.
        /// All of these are candidates for the IErrorTypeSymbol that is causing the missing assembly reference.
        /// </summary>
        private static IEnumerable<ITypeSymbol> GetTypesFromSymbol(ISymbol symbol)
        {
            if (symbol != null)
            {
                if (symbol is IMethodSymbol)
                {
                    var methodSymbol = symbol as IMethodSymbol;
                    foreach (var param in methodSymbol.Parameters)
                    {
                        yield return param.Type;
                    }

                    yield return methodSymbol.ReturnType;
                }
                if (symbol is IPropertySymbol)
                {
                    var propertySymbol = symbol as IPropertySymbol;
                    foreach (var param in propertySymbol.Parameters)
                    {
                        yield return param.Type;
                    }

                    yield return propertySymbol.Type;
                }

                yield return symbol?.GetContainingTypeOrThis();
            }
        }

        /// <summary>
        /// Look for the first error type symbol that has a valid containing assembly.
        /// This is how the missing assembly error is triggered by the compiler, 
        /// so it is safe to assume if this case exists for one of the symbols given 
        /// it is the assembly we want to add.
        /// </summary>
        private static AssemblyIdentity GetAssemblyIdentity(IEnumerable<ITypeSymbol> types, string message)
        {
            foreach (var type in types)
            {
                var identity = type?.GetBaseTypesAndThis().OfType<IErrorTypeSymbol>().FirstOrDefault()?.ContainingAssembly?.Identity;
                if (identity != null && message.Contains(identity.ToString()))
                {
                    return identity;
                }

                identity = type?.AllInterfaces.OfType<IErrorTypeSymbol>().FirstOrDefault()?.ContainingAssembly?.Identity;
                if (identity != null && message.Contains(identity.ToString()))
                {
                    return identity;
                }
            }

            return null;
        }
    }
}