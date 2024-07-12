// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass;

internal sealed class ImplementAbstractClassData(
    Document document, ImplementTypeOptions options, SyntaxNode classNode, SyntaxToken classIdentifier,
    INamedTypeSymbol classType, INamedTypeSymbol abstractClassType,
    ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers)
{
    private readonly Document _document = document;
    private readonly ImplementTypeOptions _options = options;
    private readonly SyntaxNode _classNode = classNode;
    private readonly SyntaxToken _classIdentifier = classIdentifier;
    private readonly ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> _unimplementedMembers = unimplementedMembers;

    public readonly INamedTypeSymbol ClassType = classType;
    public readonly INamedTypeSymbol AbstractClassType = abstractClassType;

    public static async Task<ImplementAbstractClassData?> TryGetDataAsync(
        Document document, SyntaxNode classNode, SyntaxToken classIdentifier, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetDeclaredSymbol(classNode, cancellationToken) is not INamedTypeSymbol classType)
            return null;

        if (classType.IsAbstract)
            return null;

        var abstractClassType = classType.BaseType;
        if (abstractClassType == null || !abstractClassType.IsAbstractClass())
            return null;

        if (!CodeGenerator.CanAdd(document.Project.Solution, classType, cancellationToken))
            return null;

        var unimplementedMembers = classType.GetAllUnimplementedMembers(
            [abstractClassType],
            includeMembersRequiringExplicitImplementation: false,
            cancellationToken);

        if (unimplementedMembers.IsEmpty)
            return null;

        var options = await document.GetImplementTypeOptionsAsync(cancellationToken).ConfigureAwait(false);

        return new ImplementAbstractClassData(
            document, options, classNode, classIdentifier,
            classType, abstractClassType, unimplementedMembers);
    }

    public static async Task<Document?> TryImplementAbstractClassAsync(
        Document document, SyntaxNode classNode, SyntaxToken classIdentifier, CancellationToken cancellationToken)
    {
        var data = await TryGetDataAsync(document, classNode, classIdentifier, cancellationToken).ConfigureAwait(false);
        if (data == null)
            return null;

        return await data.ImplementAbstractClassAsync(throughMember: null, canDelegateAllMembers: null, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Document> ImplementAbstractClassAsync(
        ISymbol? throughMember, bool? canDelegateAllMembers, CancellationToken cancellationToken)
    {
        var compilation = await _document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var memberDefinitions = GenerateMembers(compilation, throughMember, _options.PropertyGenerationBehavior, cancellationToken);
        var groupMembers = _options.InsertionBehavior == ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind;

        // If we're implementing through one of our members, but we can't delegate all members
        // through it, then give an error message on the class decl letting the user know.

        var classNodeToAddMembersTo = _classNode;
        if (throughMember != null && canDelegateAllMembers == false)
        {
            classNodeToAddMembersTo = _classNode.ReplaceToken(
                _classIdentifier,
                _classIdentifier.WithAdditionalAnnotations(ConflictAnnotation.Create(
                    FeaturesResources.Base_classes_contain_inaccessible_unimplemented_members)));
        }

        var context = new CodeGenerationContext(
            contextLocation: classNodeToAddMembersTo.GetLocation(),
            autoInsertionLocation: groupMembers,
            sortMembers: groupMembers);

        var info = await _document.GetCodeGenerationInfoAsync(context, cancellationToken).ConfigureAwait(false);

        var updatedClassNode = info.Service.AddMembers(
            classNodeToAddMembersTo,
            memberDefinitions,
            info,
            cancellationToken);

        var root = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(_classNode, updatedClassNode);

        return _document.WithSyntaxRoot(newRoot);
    }

    private ImmutableArray<ISymbol> GenerateMembers(
        Compilation compilation, ISymbol? throughMember,
        ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
        CancellationToken cancellationToken)
    {
        return _unimplementedMembers
            .SelectMany(t => t.members)
            .Select(m => GenerateMember(compilation, m, throughMember, propertyGenerationBehavior, cancellationToken))
            .WhereNotNull()
            .ToImmutableArray();
    }

    private ISymbol? GenerateMember(
        Compilation compilation, ISymbol member, ISymbol? throughMember,
        ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check if we need to add 'unsafe' to the signature we're generating.
        var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
        var addUnsafe = member.RequiresUnsafeModifier() && !syntaxFacts.IsUnsafeContext(_classNode);

        return GenerateMember(compilation, member, throughMember, addUnsafe, propertyGenerationBehavior);
    }

    private ISymbol? GenerateMember(
        Compilation compilation, ISymbol member, ISymbol? throughMember, bool addUnsafe,
        ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
    {
        var modifiers = new DeclarationModifiers(isOverride: true, isUnsafe: addUnsafe, isRequired: member.IsRequired());
        var accessibility = member.ComputeResultantAccessibility(ClassType);

        // only call through one of members for this symbol if we can actually access the symbol
        // from our type.
        if (throughMember != null &&
            !member.IsAccessibleWithin(ClassType, throughMember.GetMemberType()))
        {
            return null;
        }

        return member switch
        {
            IMethodSymbol method => GenerateMethod(compilation, method, throughMember, modifiers, accessibility),
            IPropertySymbol property => GenerateProperty(compilation, property, throughMember, modifiers, accessibility, propertyGenerationBehavior),
            IEventSymbol @event => GenerateEvent(@event, throughMember, accessibility, modifiers),
            _ => null,
        };
    }

    private ISymbol GenerateMethod(
        Compilation compilation, IMethodSymbol method, ISymbol? throughMember,
        DeclarationModifiers modifiers, Accessibility accessibility)
    {
        var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
        var body = throughMember == null
            ? generator.CreateThrowNotImplementedStatement(compilation)
            : generator.GenerateDelegateThroughMemberStatement(method, throughMember);

        method = method.EnsureNonConflictingNames(ClassType, syntaxFacts);

        return CodeGenerationSymbolFactory.CreateMethodSymbol(
            method,
            accessibility: accessibility,
            modifiers: modifiers,
            statements: [body]);
    }

    private IPropertySymbol GenerateProperty(
        Compilation compilation,
        IPropertySymbol property,
        ISymbol? throughMember,
        DeclarationModifiers modifiers,
        Accessibility accessibility,
        ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
    {
        if (property.GetMethod == null)
        {
            // Can't generate an auto-prop for a setter-only property.
            propertyGenerationBehavior = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties;
        }

        var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
        var preferAutoProperties = propertyGenerationBehavior == ImplementTypePropertyGenerationBehavior.PreferAutoProperties;

        var getMethod = ShouldGenerateAccessor(property.GetMethod)
            ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                property.GetMethod,
                attributes: default,
                accessibility: property.GetMethod.ComputeResultantAccessibility(ClassType),
                statements: generator.GetGetAccessorStatements(
                    compilation, property, throughMember, preferAutoProperties))
            : null;

        var setMethod = ShouldGenerateAccessor(property.SetMethod)
            ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                property.SetMethod,
                attributes: default,
                accessibility: property.SetMethod.ComputeResultantAccessibility(ClassType),
                statements: generator.GetSetAccessorStatements(
                    compilation, property, throughMember, preferAutoProperties))
            : null;

        return CodeGenerationSymbolFactory.CreatePropertySymbol(
            property,
            accessibility: accessibility,
            modifiers: modifiers,
            getMethod: getMethod,
            setMethod: setMethod);
    }

    private IEventSymbol GenerateEvent(
        IEventSymbol @event, ISymbol? throughMember, Accessibility accessibility, DeclarationModifiers modifiers)
    {
        var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
        return CodeGenerationSymbolFactory.CreateEventSymbol(
            @event, accessibility: accessibility, modifiers: modifiers,
            addMethod: GetEventAddOrRemoveMethod(@event, @event.AddMethod, throughMember, generator.AddEventHandler),
            removeMethod: GetEventAddOrRemoveMethod(@event, @event.RemoveMethod, throughMember, generator.RemoveEventHandler));
    }

    private IMethodSymbol? GetEventAddOrRemoveMethod(
        IEventSymbol @event, IMethodSymbol? accessor, ISymbol? throughMember,
        Func<SyntaxNode, SyntaxNode, SyntaxNode> createAddOrRemoveHandler)
    {
        if (accessor == null || throughMember == null)
            return null;

        var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
        var throughExpression = generator.CreateDelegateThroughExpression(@event, throughMember);
        var statement = generator.ExpressionStatement(createAddOrRemoveHandler(
            generator.MemberAccessExpression(throughExpression, @event.Name),
            generator.IdentifierName("value")));

        return CodeGenerationSymbolFactory.CreateAccessorSymbol(
            attributes: default,
            accessibility: Accessibility.NotApplicable,
            statements: [statement]);
    }

    private bool ShouldGenerateAccessor([NotNullWhen(true)] IMethodSymbol? method)
        => method != null && ClassType.FindImplementationForAbstractMember(method) == null;

    public ImmutableArray<(ISymbol symbol, bool canDelegateAllMembers)> GetDelegatableMembers(CancellationToken cancellationToken)
    {
        var members = ImplementHelpers.GetDelegatableMembers(
            _document,
            ClassType,
            t => InheritsFromOrEquals(t, AbstractClassType),
            cancellationToken);

        using var _ = ArrayBuilder<(ISymbol symbol, bool canDelegateAllMembers)>.GetInstance(out var result);

        var allUnimplementedMembers = _unimplementedMembers.SelectManyAsArray(t => t.members);

        // Have to make sure the field or prop has at least one unimplemented member exposed that we could actually
        // call from our type.  For example, if we're calling through a type that isn't derived from us, then we
        // can't access protected members.
        foreach (var member in members)
        {
            var memberType = member.GetMemberType();
            var accessibleCount = allUnimplementedMembers.Count(m => m.IsAccessibleWithin(ClassType, throughType: memberType));
            if (accessibleCount > 0)
            {
                // there was at least one unimplemented member that we could implement here through one of our
                // members.  Return this as a a delegatable member.  Also indicate if we will be able to delegate
                // all unimplemented members through this.  If not, we'll let the user know that they can delegate
                // through this but that it will not fully fix the error.
                result.Add((member, canDelegateAllMembers: accessibleCount == allUnimplementedMembers.Length));
            }
        }

        return result.ToImmutableAndClear();
    }

    private static bool InheritsFromOrEquals(ITypeSymbol type, ITypeSymbol baseType)
        => GetBaseTypesAndThis(type).Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, baseType));

    private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol? type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;

            // Workaround until https://github.com/dotnet/roslyn/issues/41733 is fixed. Right
            // now the compiler gives no way to determine the effective base class of a type
            // parameter.  So we do a poor mans version and attempt to figure it out from the
            // constraints ourselves.
            if (current.BaseType == null &&
                current is ITypeParameterSymbol typeParameter)
            {
                var constraints = typeParameter.ConstraintTypes;
                current =
                    constraints.OfType<INamedTypeSymbol>().FirstOrDefault() ??
                    constraints.FirstOrDefault(t => t.IsReferenceType);
                continue;
            }

            current = current.BaseType;
        }
    }
}
