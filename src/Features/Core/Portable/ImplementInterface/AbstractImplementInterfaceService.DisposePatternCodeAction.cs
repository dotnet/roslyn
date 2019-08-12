// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        private static INamedTypeSymbol TryGetSymbolForIDisposable(Compilation compilation)
        {
            // Get symbol for 'System.IDisposable'.
            var idisposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            if ((idisposable != null) && (idisposable.TypeKind == TypeKind.Interface))
            {
                var idisposableMembers = idisposable.GetMembers().ToArray();

                // Get symbol for 'System.IDisposable.Dispose()'.
                IMethodSymbol disposeMethod = null;
                if ((idisposableMembers.Length == 1) && (idisposableMembers[0].Kind == SymbolKind.Method) &&
                    (idisposableMembers[0].Name == "Dispose"))
                {
                    disposeMethod = idisposableMembers[0] as IMethodSymbol;
                    if ((disposeMethod != null) && (!disposeMethod.IsStatic) && disposeMethod.ReturnsVoid &&
                        (disposeMethod.Arity == 0) && (disposeMethod.Parameters.Length == 0))
                    {
                        return idisposable;
                    }
                }
            }

            return null;
        }

        private bool ShouldImplementDisposePattern(Document document, State state, bool explicitly)
        {
            // Dispose pattern should be implemented only if -
            // 1. An interface named 'System.IDisposable' is unimplemented.
            // 2. This interface has one and only one member - a non-generic method named 'Dispose' that takes no arguments and returns 'void'.
            // 3. The implementing type is a class that does not already declare any conflicting members named 'disposedValue' or 'Dispose'
            //    (because we will be generating a 'disposedValue' field and a couple of methods named 'Dispose' as part of implementing 
            //    the dispose pattern).
            var unimplementedMembers = explicitly ? state.UnimplementedExplicitMembers : state.UnimplementedMembers;
            var idisposable = TryGetSymbolForIDisposable(state.Model.Compilation);
            return (idisposable != null) &&
                   unimplementedMembers.Any(m => m.type.Equals(idisposable)) &&
                   CanImplementDisposePattern(state.ClassOrStructType, state.ClassOrStructDecl);
        }

        internal class ImplementInterfaceWithDisposePatternCodeAction : ImplementInterfaceCodeAction
        {
            internal ImplementInterfaceWithDisposePatternCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state,
                bool explicitly,
                bool abstractly,
                ISymbol throughMember) : base(service, document, state, explicitly, abstractly, throughMember)
            {
            }

            public static ImplementInterfaceWithDisposePatternCodeAction CreateImplementWithDisposePatternCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state)
            {
                return new ImplementInterfaceWithDisposePatternCodeAction(service, document, state, explicitly: false, abstractly: false, throughMember: null);
            }

            public static ImplementInterfaceWithDisposePatternCodeAction CreateImplementExplicitlyWithDisposePatternCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state)
            {
                return new ImplementInterfaceWithDisposePatternCodeAction(service, document, state, explicitly: true, abstractly: false, throughMember: null);
            }

            public override string Title
            {
                get
                {
                    if (Explicitly)
                    {
                        return FeaturesResources.Implement_interface_explicitly_with_Dispose_pattern;
                    }
                    else
                    {
                        return FeaturesResources.Implement_interface_with_Dispose_pattern;
                    }
                }
            }

            private static readonly SyntaxAnnotation s_implementingTypeAnnotation = new SyntaxAnnotation("ImplementingType");

            public override async Task<Document> GetUpdatedDocumentAsync(
                Document document,
                ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
                INamedTypeSymbol classOrStructType,
                SyntaxNode classOrStructDecl,
                CancellationToken cancellationToken)
            {
                var result = document;
                var compilation = await result.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                // Add an annotation to the type declaration node so that we can find it again to append the dispose pattern implementation below.
                result = await result.ReplaceNodeAsync(
                    classOrStructDecl,
                    classOrStructDecl.WithAdditionalAnnotations(s_implementingTypeAnnotation),
                    cancellationToken).ConfigureAwait(false);
                var root = await result.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                classOrStructDecl = root.GetAnnotatedNodes(s_implementingTypeAnnotation).Single();
                compilation = await result.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                classOrStructType = classOrStructType.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as INamedTypeSymbol;

                // Use the code generation service to generate all unimplemented members except those that are
                // part of the dispose pattern. We can't use the code generation service to implement the dispose
                // pattern since the code generation service doesn't support injection of the custom boiler
                // plate code required for implementing the dispose pattern.
                var idisposable = TryGetSymbolForIDisposable(compilation);
                result = await base.GetUpdatedDocumentAsync(
                    result,
                    unimplementedMembers.WhereAsArray(m => !m.type.Equals(idisposable)),
                    classOrStructType,
                    classOrStructDecl,
                    cancellationToken).ConfigureAwait(false);

                // Now append the dispose pattern implementation.
                root = await result.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                classOrStructDecl = root.GetAnnotatedNodes(s_implementingTypeAnnotation).Single();
                compilation = await result.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                classOrStructType = classOrStructType.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as INamedTypeSymbol;
                result = Service.ImplementDisposePattern(result, root, classOrStructType, classOrStructDecl.SpanStart, Explicitly);

                // Remove the annotation since we don't need it anymore.
                root = await result.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                classOrStructDecl = root.GetAnnotatedNodes(s_implementingTypeAnnotation).Single();
                result = await result.ReplaceNodeAsync(
                    classOrStructDecl,
                    classOrStructDecl.WithoutAnnotations(s_implementingTypeAnnotation),
                    cancellationToken).ConfigureAwait(false);

                return result;
            }
        }
    }
}
