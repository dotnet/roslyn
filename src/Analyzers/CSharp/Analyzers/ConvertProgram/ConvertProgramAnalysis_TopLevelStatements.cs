// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;

internal static partial class ConvertProgramAnalysis
{
    public static bool CanOfferUseTopLevelStatements(CodeStyleOption2<bool> option, bool forAnalyzer)
    {
        var userPrefersTopLevelStatements = option.Value == true;
        var analyzerDisabled = option.Notification.Severity == ReportDiagnostic.Suppress;
        var forRefactoring = !forAnalyzer;

        // If the user likes top level statements, then we offer to convert to them from the diagnostic analyzer.
        // If the user prefers Program.Main then we offer to use top-level-statements from the refactoring provider.
        // If the analyzer is disabled completely, the refactoring is enabled in both directions.
        var canOffer = userPrefersTopLevelStatements == forAnalyzer || (forRefactoring && analyzerDisabled);
        return canOffer;
    }

    public static Location GetUseTopLevelStatementsDiagnosticLocation(MethodDeclarationSyntax methodDeclaration, bool isHidden)
    {
        // if the diagnostic is hidden, show it anywhere on the main method. Otherwise, just put the diagnostic on
        // the the 'Main' identifier.
        return isHidden ? methodDeclaration.GetLocation() : methodDeclaration.Identifier.GetLocation();
    }

    public static string? GetMainTypeName(Compilation compilation)
    {
        var mainTypeFullName = compilation.Options.MainTypeName;
        var mainTypeName = mainTypeFullName?.Split('.').Last();
        return mainTypeName;
    }

    public static bool IsProgramMainMethod(
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        string? mainTypeName,
        CancellationToken cancellationToken,
        out bool canConvertToTopLevelStatements)
    {
        canConvertToTopLevelStatements = false;

        // Quick syntactic checks to allow us to avoid most methods.  We basically filter out anything that isn't
        // `static Main` immediately.
        //
        // For simplicity, we require the method to have a body so that we don't have to care about
        // expression-bodied members later.
        if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            methodDeclaration.TypeParameterList is not null ||
            methodDeclaration.Identifier.ValueText != WellKnownMemberNames.EntryPointMethodName ||
            methodDeclaration.Parent is not TypeDeclarationSyntax containingTypeDeclaration ||
            methodDeclaration.Body == null)
        {
            return false;
        }

        // If the compilation options specified a type name that Main should be found in, then do a quick check that
        // our containing type matches that.
        if (mainTypeName != null && containingTypeDeclaration.Identifier.ValueText != mainTypeName)
            return false;

        // If the user renamed the 'args' parameter, we can't convert to top level statements.
        if (methodDeclaration.ParameterList.Parameters is [{ Identifier.ValueText: not "args" }])
            return false;

        // Found a suitable candidate.  See if this matches the entrypoint the compiler has actually chosen.
        var entryPointMethod = semanticModel.Compilation.GetEntryPoint(cancellationToken);
        if (entryPointMethod == null)
            return false;

        var thisMethod = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (!entryPointMethod.Equals(thisMethod))
            return false;

        // We found the entrypoint.  However, we can only effectively convert this to top-level-statements
        // if the existing type is amenable to that.
        canConvertToTopLevelStatements = TypeCanBeConverted(entryPointMethod.ContainingType, containingTypeDeclaration);
        return true;
    }

    private static bool TypeCanBeConverted(INamedTypeSymbol containingType, TypeDeclarationSyntax typeDeclaration)
    {
        // Can't convert if our Program type derives or implements anything special.
        if (containingType.BaseType?.SpecialType != SpecialType.System_Object)
            return false;

        if (containingType.AllInterfaces.Length > 0)
            return false;

        // Too complex to convert many parts to top-level statements.  Just bail on this for now.
        if (containingType.DeclaringSyntaxReferences.Length > 1)
            return false;

        // Too complex to support converting a nested type.
        if (containingType.ContainingType != null)
            return false;

        // If the type wasn't internal it might have been public and something outside this assembly might be using it.
        if (containingType.DeclaredAccessibility == Accessibility.Public)
            return false;

        // type can't be converted with attributes.
        if (typeDeclaration.AttributeLists.Count > 0)
            return false;

        // can't convert doc comments to top level statements.
        if (typeDeclaration.GetLeadingTrivia().Any(t => t.IsDocComment()))
            return false;

        // All the members of the type need to be private/static.  And we can only have fields or methods. that's to
        // ensure that no one else was calling into this type, and that we can convert everything in the type to
        // either locals or local-functions.

        foreach (var member in typeDeclaration.Members)
        {
            // method can't be converted with attributes.  While a local function could support it, it would likely
            // change the meaning of the program if reflection is being used to try to find this method.
            if (member.AttributeLists.Count > 0)
                return false;

            // if not private, can't convert as something may be referencing it.
            if (member.Modifiers.Any(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword))
                return false;

            if (!member.Modifiers.Any(SyntaxKind.StaticKeyword))
                return false;

            if (member is not FieldDeclarationSyntax and not MethodDeclarationSyntax)
                return false;

            // if a method, it has to actually have a body so we can convert it to a local function.
            if (member is MethodDeclarationSyntax { Body: null, ExpressionBody: null })
                return false;

            // can't convert doc comments to top level statements.
            if (member.GetLeadingTrivia().Any(t => t.IsDocComment()))
                return false;
        }

        return true;
    }
}
