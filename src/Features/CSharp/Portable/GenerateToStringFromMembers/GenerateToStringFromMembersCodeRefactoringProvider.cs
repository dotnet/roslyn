// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateToStringFromMembers;

using static GenerateFromMembersHelpers;

/// <summary>
/// Offers to explicitly generate the <see cref="object.ToString"/> and
/// <c>PrintMembers</c> members the compiler synthesizes for records, so their logic
/// can be customized instead of written from scratch.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateToStringFromMembers), Shared]
[ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
internal sealed partial class GenerateToStringFromMembersCodeRefactoringProvider : CodeRefactoringProvider
{
    private const string ToStringName = nameof(object.ToString);
    private const string PrintMembersName = WellKnownMemberNames.PrintMembersMethodName;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public GenerateToStringFromMembersCodeRefactoringProvider()
    {
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
        {
            return;
        }

        var actions = await GenerateToStringFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        context.RegisterRefactorings(actions);

        if (actions.IsDefaultOrEmpty && textSpan.IsEmpty)
        {
            await HandleNonSelectionAsync(context).ConfigureAwait(false);
        }
    }

    private static async Task HandleNonSelectionAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;

        var helpers = document.GetRequiredLanguageService<IRefactoringHelpersService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // We offer the refactoring when the user is on the header of a class,
        // struct, or record. We deliberately do not offer it between members: the
        // caret landing there is too weak a signal, and the offering would show up
        // in every gap between members of every type.
        if (!helpers.IsOnTypeHeader(root, textSpan.Start, out var typeDeclaration))
        {
            return;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var containingType = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        if (containingType is not { TypeKind: TypeKind.Class or TypeKind.Struct, IsStatic: false })
            return;

        if (!await IsStringBuilderAvailableAsync(document, cancellationToken).ConfigureAwait(false))
            return;

        // For records, the synthesized PrintMembers prints the public instance fields
        // and properties declared in the record, so generate from exactly those
        // members. For other types, generate from all readable instance fields and
        // properties declared in the type.
        var members = containingType.IsRecord
            ? GetPrintableMembers(containingType)
            : containingType.GetMembers().WhereAsArray(m =>
                IsReadableInstanceFieldOrProperty(m) &&
                SymbolEqualityComparer.Default.Equals(m.ContainingType, containingType));

        if (members.IsEmpty)
            return;

        var actions = CreateActions(document, typeDeclaration, containingType, members);
        context.RegisterRefactorings(actions, textSpan);
    }

    private static async Task<bool> IsStringBuilderAvailableAsync(Document document, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        return compilation.GetTypeByMetadataName("System.Text.StringBuilder") != null;
    }

    /// <summary>
    /// The members the synthesized <c>PrintMembers</c> prints: public instance fields
    /// and properties declared in the record (properties must have a getter and must
    /// not be indexers or overrides).
    /// </summary>
    private static ImmutableArray<ISymbol> GetPrintableMembers(INamedTypeSymbol containingType)
        => containingType.GetMembers().WhereAsArray(m => IsPrintableMember(containingType, m));

    private static bool IsPrintableMember(INamedTypeSymbol containingType, ISymbol member)
    {
        if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, containingType))
            return false;

        return member switch
        {
            IFieldSymbol field => field is { AssociatedSymbol: null, CanBeReferencedByName: true },
            IPropertySymbol property => property is { Parameters.IsEmpty: true, CanBeReferencedByName: true, GetMethod: not null, IsIndexer: false, IsOverride: false },
            _ => false,
        };
    }

    private static void GetExistingMemberInfo(INamedTypeSymbol containingType, out bool hasToString, out bool hasPrintMembers)
    {
        hasToString = containingType
            .GetMembers(ToStringName)
            .Any(m => m is IMethodSymbol { Parameters.Length: 0, IsStatic: false, IsImplicitlyDeclared: false });

        // Only a PrintMembers declared in this type itself counts. A derived record
        // inherits PrintMembers from its base record, and we still want to offer to
        // override it here.
        hasPrintMembers = containingType
            .GetMembers(PrintMembersName)
            .Any(m => m is IMethodSymbol { Parameters.Length: 1, IsStatic: false, IsImplicitlyDeclared: false } &&
                SymbolEqualityComparer.Default.Equals(m.ContainingType, containingType));
    }

    public static async Task<ImmutableArray<CodeAction>> GenerateToStringFromMembersAsync(
        Document document,
        TextSpan textSpan,
        CancellationToken cancellationToken)
    {
        var info = await GetSelectedMemberInfoAsync(document, textSpan, allowPartialSelection: false, cancellationToken).ConfigureAwait(false);
        if (info != null &&
            info.SelectedMembers.All(IsReadableInstanceFieldOrProperty))
        {
            // Records get the ToString/PrintMembers options; other classes and structs
            // get a standalone ToString generated from the selected members.
            if (info.ContainingType is { TypeKind: TypeKind.Class or TypeKind.Struct, IsStatic: false })
            {
                if (!await IsStringBuilderAvailableAsync(document, cancellationToken).ConfigureAwait(false))
                    return default;

                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var typeDeclaration = syntaxFacts.GetContainingTypeDeclaration(root, textSpan.Start);
                RoslynDebug.AssertNotNull(typeDeclaration);

                return CreateActions(document, typeDeclaration, info.ContainingType, info.SelectedMembers);
            }
        }

        return default;
    }

    private static ImmutableArray<CodeAction> CreateActions(
        Document document, SyntaxNode typeDeclaration, INamedTypeSymbol containingType, ImmutableArray<ISymbol> members)
    {
        GetExistingMemberInfo(containingType, out var hasToString, out var hasPrintMembers);

        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);

        if (!containingType.IsRecord)
        {
            // A class or struct that isn't a record has no synthesized PrintMembers
            // to customize, so just offer a standalone ToString generated from the
            // selected members.
            if (!hasToString)
            {
                actions.Add(new GenerateToStringAction(
                    document, typeDeclaration, containingType, members,
                    generateToString: true, generatePrintMembers: false));
            }

            return actions.ToImmutableAndClear();
        }

        if (!hasToString && !hasPrintMembers)
        {
            // if we don't have either ToString or PrintMembers then offer:
            //  "Generate ToString()" and
            //  "Generate ToString() and PrintMembers(...)"
            //
            // Don't bother offering to just "Generate PrintMembers(...)" as it's very
            // unlikely the user would need to bother just generating that member
            // without also generating 'ToString' as well.
            actions.Add(new GenerateToStringAction(
                document, typeDeclaration, containingType, members,
                generateToString: true, generatePrintMembers: false));
            actions.Add(new GenerateToStringAction(
                document, typeDeclaration, containingType, members,
                generateToString: true, generatePrintMembers: true));
        }
        else if (!hasToString)
        {
            actions.Add(new GenerateToStringAction(
                document, typeDeclaration, containingType, members,
                generateToString: true, generatePrintMembers: false));
        }
        else if (!hasPrintMembers)
        {
            actions.Add(new GenerateToStringAction(
                document, typeDeclaration, containingType, members,
                generateToString: false, generatePrintMembers: true));
        }

        return actions.ToImmutableAndClear();
    }
}
