// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram
{
    internal static class ConvertProgramAnalysis
    {
        public static bool IsApplication(Compilation compilation)
            => IsApplication(compilation.Options);

        public static bool IsApplication(CompilationOptions options)
            => options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication;

        public static bool CanOfferUseProgramMain(
            CodeStyleOption2<bool> option,
            CompilationUnitSyntax root,
            Compilation compilation,
            bool forAnalyzer)
        {
            if (!HasGlobalStatement(root))
                return false;

            if (!CanOfferUseProgramMain(option, forAnalyzer))
                return false;

            // resiliency check for later on.  This shouldn't happen but we don't want to crash if we are in a weird
            // state where we have top level statements but no 'Program' type.
            var programType = compilation.GetBestTypeByMetadataName(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
            if (programType == null)
                return false;

            if (programType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).FirstOrDefault() is not IMethodSymbol)
                return false;

            return true;
        }

        private static bool HasGlobalStatement(CompilationUnitSyntax root)
        {
            foreach (var member in root.Members)
            {
                if (member.Kind() is SyntaxKind.GlobalStatement)
                    return true;
            }

            return false;
        }

        private static bool CanOfferUseProgramMain(CodeStyleOption2<bool> option, bool forAnalyzer)
        {
            var userPrefersProgramMain = option.Value == false;
            var analyzerDisabled = option.Notification.Severity == ReportDiagnostic.Suppress;
            var forRefactoring = !forAnalyzer;

            // If the user likes Program.Main, then we offer to conver to Program.Main from the diagnostic analyzer.
            // If the user prefers Ttop-level-statements then we offer to use Program.Main from the refactoring provider.
            // If the analyzer is disabled completely, the refactoring is enabled in both directions.
            var canOffer = userPrefersProgramMain == forAnalyzer || (forRefactoring && analyzerDisabled);
            return canOffer;
        }

        public static Location GetUseProgramMainDiagnosticLocation(CompilationUnitSyntax root, bool isHidden)
        {
            // if the diagnostic is hidden, show it anywhere from the top of the file through the end of the last global
            // statement.  That way the user can make the change anywhere in teh top level code.  Otherwise, just put
            // the diagnostic on the start of the first global statement.
            if (!isHidden)
                return root.Members.OfType<GlobalStatementSyntax>().First().GetFirstToken().GetLocation();

            // note: the legal start has to come after any #pragma directives.  We don't want this to be suppressed, but
            // then have the span of the diagnostic end up outside the suppression.
            var lastPragma = root.GetFirstToken().LeadingTrivia.LastOrDefault(t => t.Kind() is SyntaxKind.PragmaWarningDirectiveTrivia);
            var start = lastPragma == default ? 0 : lastPragma.FullSpan.End;

            return Location.Create(
                root.SyntaxTree,
                TextSpan.FromBounds(start, root.Members.OfType<GlobalStatementSyntax>().Last().FullSpan.End));
        }

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

            if (!methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                methodDeclaration.TypeParameterList is not null ||
                methodDeclaration.Identifier.ValueText != WellKnownMemberNames.EntryPointMethodName ||
                methodDeclaration.Parent is not TypeDeclarationSyntax containingTypeDeclaration ||
                methodDeclaration.Body == null)
            {
                return false;
            }

            if (mainTypeName != null && containingTypeDeclaration.Identifier.ValueText != mainTypeName)
                return false;

            if (methodDeclaration.ParameterList.Parameters.Count == 1 &&
                methodDeclaration.ParameterList.Parameters[0].Identifier.ValueText != "args")
            {
                return false;
            }

            // Have a `static Main` method, and the containing type either matched the type-name the options
            // specify, or the options specified no type name.  This is a reasonable candidate for the 
            // program entrypoint.
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
            // Can't convert if our Program type derives from anything special.
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

            if (containingType.DeclaredAccessibility != Accessibility.Internal)
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
                // method can't be converted with attributes.
                if (member.AttributeLists.Count > 0)
                    return false;

                // if not private, can't convert as something may be referencing it.
                if (member.Modifiers.Any(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.InternalKeyword))
                    return false;

                if (!member.Modifiers.Any(SyntaxKind.StaticKeyword))
                    return false;

                if (member is not FieldDeclarationSyntax and not MethodDeclarationSyntax)
                    return false;

                if (member is MethodDeclarationSyntax methodDeclaration)
                {
                    // if a method, it has to actually have a body so we can convert it to a local function.
                    if (methodDeclaration is { Body: null, ExpressionBody: null })
                        return false;

                    // local functions can't be unsafe
                    if (methodDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword))
                        return false;
                }

                // can't convert doc comments to top level statements.
                if (member.GetLeadingTrivia().Any(t => t.IsDocComment()))
                    return false;
            }

            return true;
        }
    }
}
