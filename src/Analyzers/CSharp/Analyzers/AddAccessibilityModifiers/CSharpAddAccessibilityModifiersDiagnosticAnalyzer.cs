// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpAddAccessibilityModifiersDiagnosticAnalyzer
        : AbstractAddAccessibilityModifiersDiagnosticAnalyzer<CompilationUnitSyntax>
    {
        protected override void ProcessCompilationUnit(
            SyntaxTreeAnalysisContext context,
            CodeStyleOption2<AccessibilityModifiersRequired> option, CompilationUnitSyntax compilationUnit)
        {
            ProcessMembers(context, option, compilationUnit.Members);
        }

        private void ProcessMembers(
            SyntaxTreeAnalysisContext context,
            CodeStyleOption2<AccessibilityModifiersRequired> option,
            SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (var memberDeclaration in members)
                ProcessMemberDeclaration(context, option, memberDeclaration);
        }

        private void ProcessMemberDeclaration(
            SyntaxTreeAnalysisContext context,
            CodeStyleOption2<AccessibilityModifiersRequired> option, MemberDeclarationSyntax member)
        {
            if (!context.ShouldAnalyzeSpan(member.Span))
                return;

            if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration)
                ProcessMembers(context, option, namespaceDeclaration.Members);

            // If we have a class or struct, recurse inwards.
            if (member is TypeDeclarationSyntax(
                    SyntaxKind.ClassDeclaration or
                    SyntaxKind.StructDeclaration or
                    SyntaxKind.RecordDeclaration or
                    SyntaxKind.RecordStructDeclaration) typeDeclaration)
            {
                ProcessMembers(context, option, typeDeclaration.Members);
            }

#if false
            // Add this once we have the language version for C# that supports accessibility
            // modifiers on interface methods.
            if (option.Value == AccessibilityModifiersRequired.Always &&
                member.IsKind(SyntaxKind.InterfaceDeclaration, out typeDeclaration))
            {
                // Only recurse into an interface if the user wants accessibility modifiers on 
                ProcessTypeDeclaration(context, generator, option, typeDeclaration);
            }
#endif

            if (!CSharpAddAccessibilityModifiers.Instance.ShouldUpdateAccessibilityModifier(
                    CSharpAccessibilityFacts.Instance, member, option.Value, out var name, out var modifiersAdded))
            {
                return;
            }

            // Have an issue to flag, either add or remove. Report issue to user.
            var additionalLocations = ImmutableArray.Create(member.GetLocation());
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                name.GetLocation(),
                option.Notification.Severity,
                additionalLocations: additionalLocations,
                modifiersAdded ? ModifiersAddedProperties : null));
        }
    }
}
