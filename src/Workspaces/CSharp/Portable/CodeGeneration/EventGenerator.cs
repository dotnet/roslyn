// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers;
using static Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class EventGenerator
    {
        private static MemberDeclarationSyntax AfterMember(
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

        private static MemberDeclarationSyntax BeforeMember(
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
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GenerateEventDeclaration(@event, CodeGenerationDestination.CompilationUnit, options);

            // Place the event depending on its shape.  Field style events go with fields, property
            // style events go with properties.  If there 
            var members = Insert(destination.Members, declaration, options, availableIndices,
                after: list => AfterMember(list, declaration), before: list => BeforeMember(list, declaration));
            return destination.WithMembers(members.ToSyntaxList());
        }

        internal static TypeDeclarationSyntax AddEventTo(
            TypeDeclarationSyntax destination,
            IEventSymbol @event,
            CodeGenerationOptions options,
            IList<bool> availableIndices)
        {
            var declaration = GenerateEventDeclaration(@event, GetDestination(destination), options);

            var members = Insert(destination.Members, declaration, options, availableIndices,
                after: list => AfterMember(list, declaration),
                before: list => BeforeMember(list, declaration));

            // Find the best place to put the field.  It should go after the last field if we already
            // have fields, or at the beginning of the file if we don't.

            return AddMembersTo(destination, members);
        }

        public static MemberDeclarationSyntax GenerateEventDeclaration(
            IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var reusableSyntax = GetReuseableSyntaxNodeForSymbol<MemberDeclarationSyntax>(@event, options);
            if (reusableSyntax != null)
            {
                return reusableSyntax;
            }

            var declaration = !options.GenerateMethodBodies || @event.IsAbstract || @event.AddMethod == null || @event.RemoveMethod == null
                ? GenerateEventFieldDeclaration(@event, destination, options)
                : GenerateEventDeclarationWorker(@event, destination, options);

            return ConditionallyAddDocumentationCommentTo(declaration, @event, options);
        }

        private static MemberDeclarationSyntax GenerateEventFieldDeclaration(
            IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            return AddFormatterAndCodeGeneratorAnnotationsTo(
                AddAnnotationsTo(@event,
                    SyntaxFactory.EventFieldDeclaration(
                        AttributeGenerator.GenerateAttributeLists(@event.GetAttributes(), options),
                        GenerateModifiers(@event, destination, options),
                        SyntaxFactory.VariableDeclaration(
                            @event.Type.WithNullability(@event.NullableAnnotation).GenerateTypeSyntax(),
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(@event.Name.ToIdentifierToken()))))));
        }

        private static MemberDeclarationSyntax GenerateEventDeclarationWorker(
            IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var explicitInterfaceSpecifier = GenerateExplicitInterfaceSpecifier(@event.ExplicitInterfaceImplementations);

            return AddFormatterAndCodeGeneratorAnnotationsTo(SyntaxFactory.EventDeclaration(
                attributeLists: AttributeGenerator.GenerateAttributeLists(@event.GetAttributes(), options),
                modifiers: GenerateModifiers(@event, destination, options),
                type: @event.Type.WithNullability(@event.NullableAnnotation).GenerateTypeSyntax(),
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                identifier: @event.Name.ToIdentifierToken(),
                accessorList: GenerateAccessorList(@event, destination, options)));
        }

        private static AccessorListSyntax GenerateAccessorList(
            IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var accessors = new List<AccessorDeclarationSyntax>
            {
                GenerateAccessorDeclaration(@event, @event.AddMethod, SyntaxKind.AddAccessorDeclaration, destination, options),
                GenerateAccessorDeclaration(@event, @event.RemoveMethod, SyntaxKind.RemoveAccessorDeclaration, destination, options),
            };

            return SyntaxFactory.AccessorList(accessors.WhereNotNull().ToSyntaxList());
        }

        private static AccessorDeclarationSyntax GenerateAccessorDeclaration(
            IEventSymbol @event,
            IMethodSymbol accessor,
            SyntaxKind kind,
            CodeGenerationDestination destination,
            CodeGenerationOptions options)
        {
            var hasBody = options.GenerateMethodBodies && HasAccessorBodies(@event, destination, accessor);
            return accessor == null
                ? null
                : GenerateAccessorDeclaration(accessor, kind, hasBody);
        }

        private static AccessorDeclarationSyntax GenerateAccessorDeclaration(
            IMethodSymbol accessor,
            SyntaxKind kind,
            bool hasBody)
        {
            return AddAnnotationsTo(accessor, SyntaxFactory.AccessorDeclaration(kind)
                                .WithBody(hasBody ? GenerateBlock(accessor) : null)
                                .WithSemicolonToken(hasBody ? default : SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        private static BlockSyntax GenerateBlock(IMethodSymbol accessor)
        {
            return SyntaxFactory.Block(
                StatementGenerator.GenerateStatements(CodeGenerationMethodInfo.GetStatements(accessor)));
        }

        private static bool HasAccessorBodies(
            IEventSymbol @event,
            CodeGenerationDestination destination,
            IMethodSymbol accessor)
        {
            return destination != CodeGenerationDestination.InterfaceType && @event is { IsAbstract: false } && accessor is { IsAbstract: false };
        }

        private static SyntaxTokenList GenerateModifiers(
            IEventSymbol @event, CodeGenerationDestination destination, CodeGenerationOptions options)
        {
            var tokens = ArrayBuilder<SyntaxToken>.GetInstance();

            // Most modifiers not allowed if we're an explicit impl.
            if (!@event.ExplicitInterfaceImplementations.Any())
            {
                if (destination != CodeGenerationDestination.InterfaceType)
                {
                    AddAccessibilityModifiers(@event.DeclaredAccessibility, tokens, options, Accessibility.Private);

                    if (@event.IsStatic)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
                    }

                    // An event is readonly if its accessors are readonly.
                    // If one accessor is readonly and the other one is not,
                    // the event is malformed and cannot be properly displayed.
                    // See https://github.com/dotnet/roslyn/issues/34213
                    // Don't show the readonly modifier if the containing type is already readonly
                    if (@event.AddMethod?.IsReadOnly == true && !@event.ContainingType.IsReadOnly)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));
                    }

                    if (@event.IsAbstract)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
                    }

                    if (@event.IsOverride)
                    {
                        tokens.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
                    }
                }
            }

            if (CodeGenerationEventInfo.GetIsUnsafe(@event))
            {
                tokens.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword));
            }

            return tokens.ToSyntaxTokenListAndFree();
        }
    }
}
