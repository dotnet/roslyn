// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class CSharpCompilation
    {
        private class UsingsFromOptionsAndDiagnostics
        {
            public static readonly UsingsFromOptionsAndDiagnostics Empty = new UsingsFromOptionsAndDiagnostics() { UsingNamespacesOrTypes = ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty, Diagnostics = null };

            public ImmutableArray<NamespaceOrTypeAndUsingDirective> UsingNamespacesOrTypes { get; init; }
            public DiagnosticBag? Diagnostics { get; init; }

            // completion state that tracks whether validation was done/not done/currently in process. 
            private SymbolCompletionState _state;

            public static UsingsFromOptionsAndDiagnostics FromOptions(CSharpCompilation compilation)
            {
                var usings = compilation.Options.Usings;

                if (usings.Length == 0)
                {
                    return Empty;
                }

                var diagnostics = new DiagnosticBag();
                var usingsBinder = new InContainerBinder(compilation.GlobalNamespace, new BuckStopsHereBinder(compilation, associatedFileIdentifier: null));
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
                    Debug.Assert(directiveDiagnostics.DiagnosticBag is object);
                    Debug.Assert(directiveDiagnostics.DependenciesBag is object);

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

                uniqueUsings.Free();

                if (boundUsings.Count == 0 && diagnostics is null)
                {
                    boundUsings.Free();
                    return Empty;
                }

                return new UsingsFromOptionsAndDiagnostics() { UsingNamespacesOrTypes = boundUsings.ToImmutableAndFree(), Diagnostics = diagnostics };
            }

            internal void Complete(CSharpCompilation compilation, CancellationToken cancellationToken)
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var incompletePart = _state.NextIncompletePart;
                    switch (incompletePart)
                    {
                        case CompletionPart.StartValidatingImports:
                            {
                                if (_state.NotePartComplete(CompletionPart.StartValidatingImports))
                                {
                                    Validate(compilation);
                                    _state.NotePartComplete(CompletionPart.FinishValidatingImports);
                                }
                            }
                            break;

                        case CompletionPart.FinishValidatingImports:
                            // some other thread has started validating imports (otherwise we would be in the case above) so
                            // we just wait for it to both finish and report the diagnostics.
                            Debug.Assert(_state.HasComplete(CompletionPart.StartValidatingImports));
                            _state.SpinWaitComplete(CompletionPart.FinishValidatingImports, cancellationToken);
                            break;

                        case CompletionPart.None:
                            return;

                        default:
                            // any other values are completion parts intended for other kinds of symbols
                            _state.NotePartComplete(CompletionPart.All & ~CompletionPart.ImportsAll);
                            break;
                    }

                    _state.SpinWaitComplete(incompletePart, cancellationToken);
                }
            }

            private void Validate(CSharpCompilation compilation)
            {
                if (this == Empty)
                {
                    return;
                }

                DiagnosticBag semanticDiagnostics = compilation.DeclarationDiagnostics;
                var diagnostics = BindingDiagnosticBag.GetInstance();
                Debug.Assert(diagnostics.DiagnosticBag is object);
                Debug.Assert(diagnostics.DependenciesBag is object);

                var corLibrary = compilation.SourceAssembly.CorLibrary;
                var conversions = corLibrary.TypeConversions;
                foreach (var @using in UsingNamespacesOrTypes)
                {
                    diagnostics.Clear();
                    diagnostics.AddDependencies(@using.Dependencies);

                    NamespaceOrTypeSymbol target = @using.NamespaceOrType;

                    // Check if `using static` directives meet constraints.
                    Debug.Assert(@using.UsingDirective is null);
                    if (target.IsType)
                    {
                        var typeSymbol = (TypeSymbol)target;
                        var location = NoLocation.Singleton;
                        typeSymbol.CheckAllConstraints(compilation, conversions, location, diagnostics);
                    }

                    semanticDiagnostics.AddRange(diagnostics.DiagnosticBag);

                    recordImportDependencies(target);
                }

                if (Diagnostics != null && !Diagnostics.IsEmptyWithoutResolution)
                {
                    semanticDiagnostics.AddRange(Diagnostics.AsEnumerable());
                }

                diagnostics.Free();

                void recordImportDependencies(NamespaceOrTypeSymbol target)
                {
                    if (target.IsNamespace)
                    {
                        diagnostics.AddAssembliesUsedByNamespaceReference((NamespaceSymbol)target);
                    }

                    compilation.AddUsedAssemblies(diagnostics.DependenciesBag);
                }
            }
        }
    }
}
