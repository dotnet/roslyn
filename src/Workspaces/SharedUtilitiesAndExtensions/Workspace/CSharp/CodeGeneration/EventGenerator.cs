// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

using static CodeGenerationHelpers;
using static CSharpCodeGenerationHelpers;
using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class EventGenerator
{
    private static MemberDeclarationSyntax? AfterMember(
        SyntaxList<MemberDeclarationSyntax> members,
        MemberDeclarationSyntax eventDeclaration)
    {
        if (eventDeclaration.Kind() == SyntaxKind.EventFieldDeclaration)
        {
            // Field style events go after the last field event, or after the last field.
            var lastEvent = members.LastOrDefault(m => m is EventFieldDeclarationSyntax);

            return lastEvent ?? LastField(members);
        }

        if (eventDeclaration.Kind() == SyntaxKind.EventDeclaration)
        {
            // Property style events go after existing events, then after existing constructors.
            var lastEvent = members.LastOrDefault(m => m is EventDeclarationSyntax);

            return lastEvent ?? LastConstructor(members);
        }

        return null;
    }

    private static MemberDeclarationSyntax? BeforeMember(
        SyntaxList<MemberDeclarationSyntax> members,
        MemberDeclarationSyntax eventDeclaration)
    {
        // If it's a field style event, then it goes before everything else if we don't have any
        // existing fields/events.
        if (eventDeclaration.Kind() == SyntaxKind.FieldDeclaration)
        {
            return members.FirstOrDefault();
        }

        // Otherwise just place it before the methods.
        return FirstMethod(members);
    }

    internal static CompilationUnitSyntax AddEventTo(
        CompilationUnitSyntax destination,
        IEventSymbol @event,
        CSharpCodeGenerationContextInfo info,
        IList<bool> availableIndices,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateEventDeclaration(@event, CodeGenerationDestination.CompilationUnit, info, cancellationToken);

        // Place the event depending on its shape.  Field style events go with fields, property
        // style events go with properties.  If there 
        var members = Insert(destination.Members, declaration, info, availableIndices,
            after: list => AfterMember(list, declaration), before: list => BeforeMember(list, declaration));
        return destination.WithMembers(List(members));
    }

    internal static TypeDeclarationSyntax AddEventTo(
        TypeDeclarationSyntax destination,
        IEventSymbol @event,
        CSharpCodeGenerationContextInfo info,
        IList<bool>? availableIndices,
        CancellationToken cancellationToken)
    {
        var declaration = GenerateEventDeclaration(@event, GetDestination(destination), info, cancellationToken);

        var members = Insert(destination.Members, declaration, info, availableIndices,
            after: list => AfterMember(list, declaration),
            before: list => BeforeMember(list, declaration));

        // Find the best place to put the field.  It should go after the last field if we already
        // have fields, or at the beginning of the file if we don't.

        return AddMembersTo(destination, members, cancellationToken);
    }

    public static MemberDeclarationSyntax GenerateEventDeclaration(
        IEventSymbol @event, CodeGenerationDestination destination, CSharpCodeGenerationContextInfo info, CancellationToken cancellationToken)
    {
        var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MemberDeclarationSyntax>(@event, info);
        if (reusableSyntax != null)
        {
            return reusableSyntax;
        }

        var declaration = !info.Context.GenerateMethodBodies || @event.IsAbstract || @event.AddMethod == null || @event.RemoveMethod == null
            ? GenerateEventFieldDeclaration(@event, destination, info)
            : GenerateEventDeclarationWorker(@event, destination, info);

        return ConditionallyAddDocumentationCommentTo(declaration, @event, info, cancellationToken);
    }

    private static MemberDeclarationSyntax GenerateEventFieldDeclaration(
        IEventSymbol @event, CodeGenerationDestination destination, CSharpCodeGenerationContextInfo info)
    {
        return AddFormatterAndCodeGeneratorAnnotationsTo(
            AddAnnotationsTo(@event,
                EventFieldDeclaration(
                    AttributeGenerator.GenerateAttributeLists(@event.GetAttributes(), info),
                    GenerateModifiers(@event, destination, info),
                    VariableDeclaration(
                        @event.Type.GenerateTypeSyntax(),
                        SeparatedList<VariableDeclaratorSyntax>().Add(VariableDeclarator(@event.Name.ToIdentifierToken()))))));
    }

    private static MemberDeclarationSyntax GenerateEventDeclarationWorker(
        IEventSymbol @event, CodeGenerationDestination destination, CSharpCodeGenerationContextInfo info)
    {
        var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(@event.ExplicitInterfaceImplementations);

        return AddFormatterAndCodeGeneratorAnnotationsTo(EventDeclaration(
            attributeLists: AttributeGenerator.GenerateAttributeLists(@event.GetAttributes(), info),
            modifiers: GenerateModifiers(@event, destination, info),
            type: @event.Type.GenerateTypeSyntax(),
            explicitInterfaceSpecifier: explicitInterfaceSpecifier,
            identifier: @event.Name.ToIdentifierToken(),
            accessorList: GenerateAccessorList(@event, destination, info)));
    }

    private static AccessorListSyntax GenerateAccessorList(
        IEventSymbol @event, CodeGenerationDestination destination, CSharpCodeGenerationContextInfo info)
    {
        var accessors = new List<AccessorDeclarationSyntax?>
        {
            GenerateAccessorDeclaration(@event, @event.AddMethod, SyntaxKind.AddAccessorDeclaration, destination, info),
            GenerateAccessorDeclaration(@event, @event.RemoveMethod, SyntaxKind.RemoveAccessorDeclaration, destination, info),
        };

        return AccessorList(List(accessors.WhereNotNull()));
    }

    private static AccessorDeclarationSyntax? GenerateAccessorDeclaration(
        IEventSymbol @event,
        IMethodSymbol? accessor,
        SyntaxKind kind,
        CodeGenerationDestination destination,
        CSharpCodeGenerationContextInfo info)
    {
        var hasBody = info.Context.GenerateMethodBodies && HasAccessorBodies(@event, destination, accessor);
        return accessor == null
            ? null
            : GenerateAccessorDeclaration(accessor, kind, hasBody);
    }

    private static AccessorDeclarationSyntax GenerateAccessorDeclaration(
        IMethodSymbol accessor,
        SyntaxKind kind,
        bool hasBody)
    {
        return AddAnnotationsTo(accessor, AccessorDeclaration(kind)
                            .WithBody(hasBody ? GenerateBlock(accessor) : null)
                            .WithSemicolonToken(hasBody ? default : SemicolonToken));
    }

    private static BlockSyntax GenerateBlock(IMethodSymbol accessor)
    {
        return Block(
            StatementGenerator.GenerateStatements(CodeGenerationMethodInfo.GetStatements(accessor)));
    }

    private static bool HasAccessorBodies(
        IEventSymbol @event,
        CodeGenerationDestination destination,
        IMethodSymbol? accessor)
    {
        return destination != CodeGenerationDestination.InterfaceType &&
            !@event.IsAbstract &&
            accessor != null &&
            !accessor.IsAbstract;
    }

    private static SyntaxTokenList GenerateModifiers(
        IEventSymbol @event, CodeGenerationDestination destination, CSharpCodeGenerationContextInfo info)
    {
        using var _ = ArrayBuilder<SyntaxToken>.GetInstance(out var tokens);

        // Only "static" allowed if we're an explicit impl.
        if (@event.ExplicitInterfaceImplementations.Any())
        {
            if (@event.IsStatic)
                tokens.Add(StaticKeyword);
        }
        else
        {
            // If we're generating into an interface, then allow modifiers for static abstract members
            if (destination is CodeGenerationDestination.InterfaceType)
            {
                if (@event.IsStatic)
                {
                    tokens.Add(StaticKeyword);

                    // We only generate the abstract keyword in interfaces for static abstract members
                    if (@event.IsAbstract)
                        tokens.Add(AbstractKeyword);
                }
            }
            else
            {
                AddAccessibilityModifiers(@event.DeclaredAccessibility, tokens, info, Accessibility.Private);

                if (@event.IsStatic)
                    tokens.Add(StaticKeyword);

                // An event is readonly if its accessors are readonly.
                // If one accessor is readonly and the other one is not,
                // the event is malformed and cannot be properly displayed.
                // See https://github.com/dotnet/roslyn/issues/34213
                // Don't show the readonly modifier if the containing type is already readonly
                if (@event.AddMethod?.IsReadOnly == true && !@event.ContainingType.IsReadOnly)
                    tokens.Add(ReadOnlyKeyword);

                if (@event.IsAbstract)
                    tokens.Add(AbstractKeyword);

                if (@event.IsOverride)
                    tokens.Add(OverrideKeyword);
            }
        }

        if (CodeGenerationEventInfo.GetIsUnsafe(@event))
            tokens.Add(UnsafeKeyword);

        return TokenList(tokens);
    }
}
