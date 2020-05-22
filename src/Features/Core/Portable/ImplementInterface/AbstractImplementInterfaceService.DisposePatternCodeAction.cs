﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        // Parts of the name `disposedValue`.  Used so we can generate a field correctly with 
        // the naming style that the user has specified.
        private static ImmutableArray<string> s_disposedValueNameParts =
            ImmutableArray.Create("disposed", "value");

        // C#: `Dispose(bool disposed)`.  VB: `Dispose(disposed As Boolean)`
        private static readonly SymbolDisplayFormat s_format = new SymbolDisplayFormat(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static IMethodSymbol? TryGetIDisposableDispose(Compilation compilation)
        {
            // Get symbol for 'System.IDisposable'.
            var idisposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
            if (idisposable?.TypeKind == TypeKind.Interface)
            {
                var idisposableMembers = idisposable.GetMembers(nameof(IDisposable.Dispose));
                foreach (var member in idisposableMembers)
                {
                    if (member is IMethodSymbol disposeMethod &&
                        !disposeMethod.IsStatic &&
                        disposeMethod.ReturnsVoid &&
                        disposeMethod.Arity == 0 &&
                        disposeMethod.Parameters.Length == 0)
                    {
                        return disposeMethod;
                    }
                }
            }

            return null;
        }

        private static bool ShouldImplementDisposePattern(State state, bool explicitly)
        {
            // Dispose pattern should be implemented only if -
            // 1. An interface named 'System.IDisposable' is unimplemented.
            // 2. This interface has one and only one member - a non-generic method named 'Dispose' that takes no arguments and returns 'void'.
            // 3. The implementing type is a class that does not already declare any conflicting members named 'disposedValue' or 'Dispose'
            //    (because we will be generating a 'disposedValue' field and a couple of methods named 'Dispose' as part of implementing 
            //    the dispose pattern).
            if (state.ClassOrStructType.TypeKind != TypeKind.Class)
                return false;

            var disposeMethod = TryGetIDisposableDispose(state.Model.Compilation);
            if (disposeMethod == null)
                return false;

            var idisposableType = disposeMethod.ContainingType;
            var unimplementedMembers = explicitly
                ? state.MembersWithoutExplicitImplementation
                : state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented;
            if (!unimplementedMembers.Any(m => m.type.Equals(idisposableType)))
                return false;

            // The dispose pattern is only applicable if the implementing type does
            // not already have an implementation of IDisposableDispose.
            return state.ClassOrStructType.FindImplementationForInterfaceMember(disposeMethod) == null;
        }

        private class ImplementInterfaceWithDisposePatternCodeAction : ImplementInterfaceCodeAction
        {
            public ImplementInterfaceWithDisposePatternCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state,
                bool explicitly,
                bool abstractly,
                ISymbol? throughMember) : base(service, document, state, explicitly, abstractly, onlyRemaining: !explicitly, throughMember)
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

            public override async Task<Document> GetUpdatedDocumentAsync(
                Document document,
                ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
                INamedTypeSymbol classType,
                SyntaxNode classDecl,
                CancellationToken cancellationToken)
            {
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                var disposedValueField = await CreateDisposedValueFieldAsync(
                    document, classType, cancellationToken).ConfigureAwait(false);

                var disposeMethod = TryGetIDisposableDispose(compilation)!;
                var (disposableMethods, finalizer) = CreateDisposableMethods(compilation, document, classType, disposeMethod, disposedValueField);

                // First, implement all the interfaces (except for IDisposable).
                var docWithCoreMembers = await GetUpdatedDocumentAsync(
                    document,
                    unimplementedMembers.WhereAsArray(m => !m.type.Equals(disposeMethod.ContainingType)),
                    classType,
                    classDecl,
                    extraMembers: ImmutableArray.Create<ISymbol>(disposedValueField),
                    cancellationToken).ConfigureAwait(false);

                // Next, add the Dispose pattern methods at the end of the type (we want to keep all
                // the members together).
                var rootWithCoreMembers = await docWithCoreMembers.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var firstGeneratedMember = rootWithCoreMembers.GetAnnotatedNodes(CodeGenerator.Annotation).First();
                var typeDeclarationWithCoreMembers = firstGeneratedMember.Parent!;

                var typeDeclarationWithAllMembers = CodeGenerator.AddMemberDeclarations(
                    typeDeclarationWithCoreMembers,
                    disposableMethods,
                    document.Project.Solution.Workspace,
                    new CodeGenerationOptions(
                        addImports: false,
                        parseOptions: rootWithCoreMembers.SyntaxTree.Options,
                        sortMembers: false,
                        autoInsertionLocation: false));

                var docWithAllMembers = docWithCoreMembers.WithSyntaxRoot(
                    rootWithCoreMembers.ReplaceNode(
                        typeDeclarationWithCoreMembers, typeDeclarationWithAllMembers));

                // Finally, add a commented out finalizer with the Dispose methods. We have to do
                // this ourselves as our code-gen helpers can create real methods, but not commented
                // out ones.
                return await AddFinalizerCommentAsync(docWithAllMembers, finalizer, cancellationToken).ConfigureAwait(false);
            }

            private static async Task<Document> AddFinalizerCommentAsync(
                Document document, SyntaxNode finalizer, CancellationToken cancellationToken)
            {
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var lastGeneratedMember = root.GetAnnotatedNodes(CodeGenerator.Annotation)
                                              .OrderByDescending(n => n.SpanStart)
                                              .First();

                finalizer = finalizer.NormalizeWhitespace();
                var finalizerLines = finalizer.ToFullString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
                var finalizerComments = CreateCommentTrivia(generator, finalizerLines);

                var lastMemberWithComments = lastGeneratedMember.WithPrependedLeadingTrivia(
                    finalizerComments.Insert(0, generator.CarriageReturnLineFeed)
                                     .Add(generator.CarriageReturnLineFeed));

                var finalRoot = root.ReplaceNode(lastGeneratedMember, lastMemberWithComments);
                return document.WithSyntaxRoot(finalRoot);
            }

            private (ImmutableArray<ISymbol>, SyntaxNode) CreateDisposableMethods(
                Compilation compilation,
                Document document,
                INamedTypeSymbol classType,
                IMethodSymbol disposeMethod,
                IFieldSymbol disposedValueField)
            {
                var disposeImplMethod = CreateDisposeImplementationMethod(compilation, document, classType, disposeMethod, disposedValueField);

                var disposeMethodDisplayString = this.Service.ToDisplayString(disposeImplMethod, s_format);

                var disposeInterfaceMethod = CreateDisposeInterfaceMethod(
                    compilation, document, classType, disposeMethod,
                    disposedValueField, disposeMethodDisplayString);

                var g = document.GetRequiredLanguageService<SyntaxGenerator>();
                var finalizer = Service.CreateFinalizer(g, classType, disposeMethodDisplayString);

                return (ImmutableArray.Create<ISymbol>(disposeImplMethod, disposeInterfaceMethod), finalizer);
            }

            private IMethodSymbol CreateDisposeImplementationMethod(
                Compilation compilation,
                Document document,
                INamedTypeSymbol classType,
                IMethodSymbol disposeMethod,
                IFieldSymbol disposedValueField)
            {
                var accessibility = classType.IsSealed
                    ? Accessibility.Private
                    : Accessibility.Protected;

                var modifiers = classType.IsSealed
                    ? DeclarationModifiers.None
                    : DeclarationModifiers.Virtual;

                var g = document.GetRequiredLanguageService<SyntaxGenerator>();

                // if (disposing)
                // {
                //     // TODO: dispose managed state...
                // }
                var ifDisposingStatement = g.IfStatement(g.IdentifierName(DisposingName), Array.Empty<SyntaxNode>());
                ifDisposingStatement = Service.AddCommentInsideIfStatement(
                    ifDisposingStatement,
                    CreateCommentTrivia(g, FeaturesResources.TODO_colon_dispose_managed_state_managed_objects))
                        .WithoutTrivia().WithTrailingTrivia(g.CarriageReturnLineFeed, g.CarriageReturnLineFeed);

                // TODO: free unmanaged ...
                // TODO: set large fields...
                // disposedValue = true
                var disposedValueEqualsTrueStatement = AddComments(g,
                    FeaturesResources.TODO_colon_free_unmanaged_resources_unmanaged_objects_and_override_finalizer,
                    FeaturesResources.TODO_colon_set_large_fields_to_null,
                    g.AssignmentStatement(
                        g.IdentifierName(disposedValueField.Name), g.TrueLiteralExpression()));

                var ifStatement = g.IfStatement(
                    g.LogicalNotExpression(g.IdentifierName(disposedValueField.Name)),
                    new[] { ifDisposingStatement, disposedValueEqualsTrueStatement });

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    disposeMethod,
                    containingType: classType,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    name: disposeMethod.Name,
                    parameters: ImmutableArray.Create(
                        CodeGenerationSymbolFactory.CreateParameterSymbol(
                            compilation.GetSpecialType(SpecialType.System_Boolean),
                            DisposingName)),
                    statements: ImmutableArray.Create(ifStatement));
            }

            private IMethodSymbol CreateDisposeInterfaceMethod(
                Compilation compilation,
                Document document,
                INamedTypeSymbol classType,
                IMethodSymbol disposeMethod,
                IFieldSymbol disposedValueField,
                string disposeMethodDisplayString)
            {
                using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var statements);

                var g = document.GetRequiredLanguageService<SyntaxGenerator>();
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                // // Do not change...
                // Dispose(true);
                statements.Add(AddComment(g,
                    string.Format(FeaturesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, disposeMethodDisplayString),
                    g.ExpressionStatement(
                        g.InvocationExpression(
                            g.IdentifierName(nameof(IDisposable.Dispose)),
                            g.Argument(DisposingName, RefKind.None, g.TrueLiteralExpression())))));

                // GC.SuppressFinalize(this);
                statements.Add(g.ExpressionStatement(
                    g.InvocationExpression(
                        g.MemberAccessExpression(
                            g.TypeExpression(compilation.GetTypeByMetadataName(typeof(GC).FullName!)),
                            nameof(GC.SuppressFinalize)),
                        g.ThisExpression())));

                var modifiers = DeclarationModifiers.From(disposeMethod);
                modifiers = modifiers.WithIsAbstract(false);

                var explicitInterfaceImplementations = Explicitly || !Service.CanImplementImplicitly
                    ? ImmutableArray.Create(disposeMethod) : default;

                var result = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    disposeMethod,
                    accessibility: Explicitly ? Accessibility.NotApplicable : Accessibility.Public,
                    modifiers: modifiers,
                    explicitInterfaceImplementations: explicitInterfaceImplementations,
                    statements: statements.ToImmutable());

                return result;
            }

            private static async Task<IFieldSymbol> CreateDisposedValueFieldAsync(
                Document document,
                INamedTypeSymbol containingType,
                CancellationToken cancellationToken)
            {
                var rule = await document.GetApplicableNamingRuleAsync(
                    SymbolKind.Field, Accessibility.Private, cancellationToken).ConfigureAwait(false);
                var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var requireAccessiblity = options.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers);

                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
                var accessibilityLevel = requireAccessiblity.Value == AccessibilityModifiersRequired.Never || requireAccessiblity.Value == AccessibilityModifiersRequired.OmitIfDefault
                    ? Accessibility.NotApplicable
                    : Accessibility.Private;

                var uniqueName = GenerateUniqueNameForDisposedValueField(containingType, rule);

                return CodeGenerationSymbolFactory.CreateFieldSymbol(
                    default,
                    accessibilityLevel,
                    DeclarationModifiers.None,
                    boolType, uniqueName);
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
