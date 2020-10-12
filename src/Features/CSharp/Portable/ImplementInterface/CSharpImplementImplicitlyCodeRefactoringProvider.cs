﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpImplementImplicitlyCodeRefactoringProvider :
        AbstractChangeImplementionCodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpImplementImplicitlyCodeRefactoringProvider()
        {
        }

        protected override string Implement_0 => FeaturesResources.Implement_0_implicitly;
        protected override string Implement_all_interfaces => FeaturesResources.Implement_all_interfaces_implicitly;
        protected override string Implement => FeaturesResources.Implement_implicitly;

        // We need to be an explicit impl in order to convert to implicit.
        protected override bool CheckExplicitNameAllowsConversion(ExplicitInterfaceSpecifierSyntax? explicitName)
            => explicitName != null;

        // If we don't implement any interface members explicitly we can't convert this to be
        // implicit.
        protected override bool CheckMemberCanBeConverted(ISymbol member)
        {
            var memberInterfaceImplementations = member.ExplicitInterfaceImplementations();
            if (memberInterfaceImplementations.Length == 0)
                return false;
            var containingTypeInterfaces = member.ContainingType.AllInterfaces;
            if (containingTypeInterfaces.Length == 0)
                return false;
            return memberInterfaceImplementations.Any(impl => containingTypeInterfaces.Contains(impl.ContainingType));
        }

        // When converting to implicit, we don't need to update any references.
        protected override Task UpdateReferencesAsync(Project project, SolutionEditor solutionEditor, ISymbol implMember, INamedTypeSymbol containingType, CancellationToken cancellationToken)
            => Task.CompletedTask;

        protected override SyntaxNode ChangeImplementation(SyntaxGenerator generator, SyntaxNode decl, ISymbol _)
            => generator.WithAccessibility(WithoutExplicitImpl(decl), Accessibility.Public);

        private static SyntaxNode? WithoutExplicitImpl(SyntaxNode decl)
            => decl switch
            {
                MethodDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(null),
                PropertyDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(null),
                EventDeclarationSyntax member => member.WithExplicitInterfaceSpecifier(null),
                _ => throw ExceptionUtilities.UnexpectedValue(decl),
            };
    }
}
