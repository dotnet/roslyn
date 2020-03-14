// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        private static ImmutableArray<string> s_disposedValueNameParts =
            ImmutableArray.Create("disposed", "value");

        private static INamedTypeSymbol TryGetSymbolForIDisposable(Compilation compilation)
        {
            // Get symbol for 'System.IDisposable'.
            var idisposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            if (idisposable?.TypeKind == TypeKind.Interface)
            {
                var idisposableMembers = idisposable.GetMembers();

                // Get symbol for 'System.IDisposable.Dispose()'.
                if (idisposableMembers.Length == 1 &&
                    idisposableMembers[0] is IMethodSymbol disposeMethod &&
                    disposeMethod.Name == nameof(IDisposable.Dispose) &&
                    !disposeMethod.IsStatic &&
                    disposeMethod.ReturnsVoid &&
                    disposeMethod.Arity == 0 &&
                    disposeMethod.Parameters.Length == 0)
                {
                    return idisposable;
                }
            }

            return null;
        }

        private bool ShouldImplementDisposePattern(State state, bool explicitly)
        {
            // Dispose pattern should be implemented only if -
            // 1. An interface named 'System.IDisposable' is unimplemented.
            // 2. This interface has one and only one member - a non-generic method named 'Dispose' that takes no arguments and returns 'void'.
            // 3. The implementing type is a class that does not already declare any conflicting members named 'disposedValue' or 'Dispose'
            //    (because we will be generating a 'disposedValue' field and a couple of methods named 'Dispose' as part of implementing 
            //    the dispose pattern).
            if (state.ClassOrStructType.TypeKind != TypeKind.Class)
                return false;

            var unimplementedMembers = explicitly ? state.UnimplementedExplicitMembers : state.UnimplementedMembers;
            var idisposable = TryGetSymbolForIDisposable(state.Model.Compilation);
            if (idisposable == null)
                return false;

            if (!unimplementedMembers.Any(m => m.type.Equals(idisposable)))
                return false;

            // The dispose pattern is only applicable if the implementing type is a class that does
            // not already declare any conflicting members named 'disposedValue' or 'Dispose'
            // (because we will be generating a 'disposedValue' field and a couple of methods named
            // 'Dispose' as part of implementing the dispose pattern).
            if (state.ClassOrStructType.GetMembers(nameof(IDisposable.Dispose)).Any())
                return false;

            return true;
        }

        private class ImplementInterfaceWithDisposePatternCodeAction : ImplementInterfaceCodeAction
        {
            public ImplementInterfaceWithDisposePatternCodeAction(
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
                => Explicitly
                    ? FeaturesResources.Implement_interface_explicitly_with_Dispose_pattern
                    : FeaturesResources.Implement_interface_with_Dispose_pattern;

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

                var rules = await document.GetNamingRulesAsync(FallbackNamingRules.RefactoringMatchLookupRules, cancellationToken).ConfigureAwait(false);
                var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var requireAccessiblity = options.GetOption(CodeStyleOptions.RequireAccessibilityModifiers);
                var disposedValueField = CreateDisposedValueField(
                    compilation, classOrStructType, requireAccessiblity, rules);

                // Use the code generation service to generate all unimplemented members except
                // those that are part of the dispose pattern. We can't use the code generation
                // service to implement the dispose pattern since the code generation service
                // doesn't support injection of the custom boiler plate code required for
                // implementing the dispose pattern.
                var idisposable = TryGetSymbolForIDisposable(compilation);

                result = await GetUpdatedDocumentAsync(
                    result,
                    unimplementedMembers.WhereAsArray(m => !m.type.Equals(idisposable)),
                    classOrStructType,
                    classOrStructDecl,
                    extraMembers: ImmutableArray.Create<ISymbol>(disposedValueField),
                    cancellationToken).ConfigureAwait(false);

                // Now append the dispose pattern implementation.
                root = await result.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                classOrStructDecl = root.GetAnnotatedNodes(s_implementingTypeAnnotation).Single();
                compilation = await result.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                classOrStructType = classOrStructType.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as INamedTypeSymbol;
                result = Service.ImplementDisposePattern(
                    result, root,
                    classOrStructType, disposedValueField,
                    classOrStructDecl.SpanStart, Explicitly);

                // Remove the annotation since we don't need it anymore.
                root = await result.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                classOrStructDecl = root.GetAnnotatedNodes(s_implementingTypeAnnotation).Single();
                result = await result.ReplaceNodeAsync(
                    classOrStructDecl,
                    classOrStructDecl.WithoutAnnotations(s_implementingTypeAnnotation),
                    cancellationToken).ConfigureAwait(false);

                return result;
            }

            private IFieldSymbol CreateDisposedValueField(
                Compilation compilation,
                INamedTypeSymbol containingType,
                CodeStyleOption<AccessibilityModifiersRequired> requireAccessibilityModifiers,
                ImmutableArray<NamingRule> rules)
            {
                var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
                var accessibilityLevel = requireAccessibilityModifiers.Value == AccessibilityModifiersRequired.Never || requireAccessibilityModifiers.Value == AccessibilityModifiersRequired.OmitIfDefault
                    ? Accessibility.NotApplicable
                    : Accessibility.Private;

                foreach (var rule in rules)
                {
                    if (rule.SymbolSpecification.AppliesTo(SymbolKind.Field, Accessibility.Private))
                    {
                        var uniqueName = GenerateUniqueNameForDisposedValueField(containingType, rule);

                        return CodeGenerationSymbolFactory.CreateFieldSymbol(
                            default,
                            accessibilityLevel,
                            DeclarationModifiers.None,
                            boolType, uniqueName);
                    }
                }

                // We place a special rule in s_builtInRules that matches all fields.  So we should 
                // always find a matching rule.
                throw ExceptionUtilities.Unreachable;
            }

            private static string GenerateUniqueNameForDisposedValueField(INamedTypeSymbol containingType, NamingRule rule)
            {
                // Determine an appropriate name to call the new field.
                var baseName = rule.NamingStyle.CreateName(s_disposedValueNameParts);

                // Ensure that the name is unique in the containing type so we
                // don't stomp on an existing member.
                var uniqueName = NameGenerator.GenerateUniqueName(
                    baseName, n => containingType.GetMembers(n).IsEmpty);
                return uniqueName;
            }
        }
    }
}
