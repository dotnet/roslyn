﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

#if CODE_STYLE
using DeclarationModifiers = Microsoft.CodeAnalysis.Internal.Editing.DeclarationModifiers;
#else
using DeclarationModifiers = Microsoft.CodeAnalysis.Editing.DeclarationModifiers;
#endif

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract partial class AbstractImplementInterfaceService
{
    private sealed partial class ImplementInterfaceGenerator
    {
        // Parts of the name `disposedValue`.  Used so we can generate a field correctly with 
        // the naming style that the user has specified.
        private static readonly ImmutableArray<string> s_disposedValueNameParts = ["disposed", "value"];

        // C#: `Dispose(bool disposed)`.  VB: `Dispose(disposed As Boolean)`
        private static readonly SymbolDisplayFormat s_format = new(
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private async Task<Document> ImplementDisposePatternAsync(
            ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
            CancellationToken cancellationToken)
        {
            var document = this.Document;
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var disposedValueField = await CreateDisposedValueFieldAsync(
                document, this.Service.SyntaxFormatting, State.ClassOrStructType, cancellationToken).ConfigureAwait(false);

            var disposeMethod = TryGetIDisposableDispose(compilation)!;
            var (disposableMethods, finalizer) = CreateDisposableMethods(compilation, disposeMethod, disposedValueField);

            // First, implement all the interfaces (except for IDisposable).
            var docWithCoreMembers = await ImplementInterfaceAsync(
                unimplementedMembers.WhereAsArray(m => !m.type.Equals(disposeMethod.ContainingType)),
                extraMembers: [disposedValueField],
                cancellationToken).ConfigureAwait(false);

            // Next, add the Dispose pattern methods at the end of the type (we want to keep all
            // the members together).
            var rootWithCoreMembers = await docWithCoreMembers.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var firstGeneratedMember = rootWithCoreMembers.GetAnnotatedNodes(CodeGenerator.Annotation).First();
            var typeDeclarationWithCoreMembers = firstGeneratedMember.Parent!;

            var context = new CodeGenerationContext(
                addImports: false,
                sortMembers: false,
                autoInsertionLocation: false);

            var info = await document.GetCodeGenerationInfoAsync(context, cancellationToken).ConfigureAwait(false);

            var typeDeclarationWithAllMembers = info.Service.AddMembers(
                typeDeclarationWithCoreMembers,
                disposableMethods,
                info,
                cancellationToken);

            var docWithAllMembers = docWithCoreMembers.WithSyntaxRoot(
                rootWithCoreMembers.ReplaceNode(
                    typeDeclarationWithCoreMembers, typeDeclarationWithAllMembers));

            // Finally, add a commented out finalizer with the Dispose methods. We have to do
            // this ourselves as our code-gen helpers can create real methods, but not commented
            // out ones.
            return await AddFinalizerCommentAsync(docWithAllMembers, finalizer, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Document> AddFinalizerCommentAsync(
            Document document, SyntaxNode finalizer, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var lastGeneratedMember = root.GetAnnotatedNodes(CodeGenerator.Annotation)
                                          .OrderByDescending(n => n.SpanStart)
                                          .First();

            finalizer = finalizer.NormalizeWhitespace();
            var finalizerLines = finalizer.ToFullString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
            var finalizerComments = this.Service.CreateCommentTrivia(finalizerLines);

            var lastMemberWithComments = lastGeneratedMember.WithPrependedLeadingTrivia(
                finalizerComments.Insert(0, this.Service.SyntaxGeneratorInternal.CarriageReturnLineFeed)
                                 .Add(this.Service.SyntaxGeneratorInternal.CarriageReturnLineFeed));

            var finalRoot = root.ReplaceNode(lastGeneratedMember, lastMemberWithComments);
            return document.WithSyntaxRoot(finalRoot);
        }

        private (ImmutableArray<ISymbol>, SyntaxNode) CreateDisposableMethods(
            Compilation compilation,
            IMethodSymbol disposeMethod,
            IFieldSymbol disposedValueField)
        {
            var disposeImplMethod = CreateDisposeImplementationMethod(compilation, disposeMethod, disposedValueField);

            var disposeMethodDisplayString = this.Service.ToDisplayString(disposeImplMethod, s_format);

            var disposeInterfaceMethod = CreateDisposeInterfaceMethod(
                compilation, disposeMethod, disposeMethodDisplayString);

            var g = this.Document.GetRequiredLanguageService<SyntaxGenerator>();
            var finalizer = Service.CreateFinalizer(g, State.ClassOrStructType, disposeMethodDisplayString);

            return (ImmutableArray.Create<ISymbol>(disposeImplMethod, disposeInterfaceMethod), finalizer);
        }

        private IMethodSymbol CreateDisposeImplementationMethod(
            Compilation compilation,
            IMethodSymbol disposeMethod,
            IFieldSymbol disposedValueField)
        {
            var accessibility = State.ClassOrStructType.IsSealed
                ? Accessibility.Private
                : Accessibility.Protected;

            var modifiers = State.ClassOrStructType.IsSealed
                ? DeclarationModifiers.None
                : DeclarationModifiers.Virtual;

            var g = this.Document.GetRequiredLanguageService<SyntaxGenerator>();
            var gi = this.Service.SyntaxGeneratorInternal;

            // if (disposing)
            // {
            //     // TODO: dispose managed state...
            // }
            var ifDisposingStatement = g.IfStatement(g.IdentifierName(DisposingName), []);
            ifDisposingStatement = Service.AddCommentInsideIfStatement(
                ifDisposingStatement,
                this.Service.CreateCommentTrivia(CodeFixesResources.TODO_colon_dispose_managed_state_managed_objects))
                    .WithoutTrivia().WithTrailingTrivia(gi.CarriageReturnLineFeed, gi.CarriageReturnLineFeed);

            // TODO: free unmanaged ...
            // TODO: set large fields...
            // disposedValue = true
            var disposedValueEqualsTrueStatement = this.Service.AddComments(
                CodeFixesResources.TODO_colon_free_unmanaged_resources_unmanaged_objects_and_override_finalizer,
                CodeFixesResources.TODO_colon_set_large_fields_to_null,
                g.AssignmentStatement(
                    g.IdentifierName(disposedValueField.Name), g.TrueLiteralExpression()));

            var ifStatement = g.IfStatement(
                g.LogicalNotExpression(g.IdentifierName(disposedValueField.Name)),
                [ifDisposingStatement, disposedValueEqualsTrueStatement]);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                disposeMethod,
                containingType: State.ClassOrStructType,
                accessibility: accessibility,
                modifiers: modifiers,
                name: disposeMethod.Name,
                parameters: [CodeGenerationSymbolFactory.CreateParameterSymbol(
                    compilation.GetSpecialType(SpecialType.System_Boolean),
                    DisposingName)],
                statements: [ifStatement]);
        }

        private IMethodSymbol CreateDisposeInterfaceMethod(
            Compilation compilation,
            IMethodSymbol disposeMethod,
            string disposeMethodDisplayString)
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var statements);

            var g = this.Document.GetRequiredLanguageService<SyntaxGenerator>();
            var syntaxFacts = this.Document.GetRequiredLanguageService<ISyntaxFactsService>();

            // // Do not change...
            // Dispose(true);
            statements.Add(this.Service.AddComment(
                string.Format(CodeFixesResources.Do_not_change_this_code_Put_cleanup_code_in_0_method, disposeMethodDisplayString),
                g.ExpressionStatement(
                    g.InvocationExpression(
                        g.IdentifierName(nameof(IDisposable.Dispose)),
                        g.Argument(DisposingName, RefKind.None, g.TrueLiteralExpression())))));

            // GC.SuppressFinalize(this);
            var gcType = compilation.GetTypeByMetadataName(typeof(GC).FullName!);
            if (gcType != null)
            {
                statements.Add(g.ExpressionStatement(
                    g.InvocationExpression(
                        g.MemberAccessExpression(
                            g.TypeExpression(gcType),
                            nameof(GC.SuppressFinalize)),
                        g.ThisExpression())));
            }

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
            ISyntaxFormatting syntaxFormatting,
            INamedTypeSymbol containingType,
            CancellationToken cancellationToken)
        {
            var rule = await document.GetApplicableNamingRuleAsync(
                SymbolKind.Field, Accessibility.Private, cancellationToken).ConfigureAwait(false);

            var options = await document.GetSyntaxFormattingOptionsAsync(syntaxFormatting, cancellationToken).ConfigureAwait(false);
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);
            var accessibilityLevel = options.AccessibilityModifiersRequired is AccessibilityModifiersRequired.Never or AccessibilityModifiersRequired.OmitIfDefault
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
