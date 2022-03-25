// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.TopLevelStatements
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConvertToTopLevelStatementsDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConvertToTopLevelStatementsDiagnosticAnalyzer()
            : base(
                  IDEDiagnosticIds.UseTopLevelStatementsId,
                  EnforceOnBuildValues.UseTopLevelStatements,
                  CSharpCodeStyleOptions.PreferTopLevelStatements,
                  LanguageNames.CSharp,
                  new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_to_top_level_statements), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                // can only suggest moving to top level statement on c# 9 or above.
                if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp9)
                    return;

                context.RegisterSyntaxNodeAction(ProcessCompilationUnit, SyntaxKind.CompilationUnit);
            });
        }

        private void ProcessCompilationUnit(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var root = (CompilationUnitSyntax)context.Node;

            // Don't want to suggest moving if the user doesn't have a preference for top-level-statements.
            var optionSet = options.GetAnalyzerOptionSet(root.SyntaxTree, context.CancellationToken);
            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);
            if (!ConvertProgramAnalysis.CanOfferUseTopLevelStatements(option, forAnalyzer: true))
                return;

            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var compilation = semanticModel.Compilation;
            var mainTypeName = compilation.Options.MainTypeName;
            var mainLastTypeName = mainTypeName?.Split('.').Last();

            // Ok, the user does like top level statements.  Check if we can find a suitable hit in this type that
            // indicates we're on the entrypoint of the program.
            foreach (var child in root.DescendantNodes(n => n is CompilationUnitSyntax or NamespaceDeclarationSyntax or ClassDeclarationSyntax))
            {
                if (child is MethodDeclarationSyntax methodDeclaration &&
                    methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                    methodDeclaration.TypeParameterList is null &&
                    methodDeclaration.Identifier.ValueText == WellKnownMemberNames.EntryPointMethodName)
                {
                    if (mainLastTypeName != null && (child.Parent as TypeDeclarationSyntax)?.Identifier.ValueText != mainLastTypeName)
                        continue;

                    // Have a `static Main` method, and the containing type either matched the type-name the options
                    // specify, or the options specified no type name.  This is a reasonable candidate for the 
                    // program entrypoint.
                    var entryPointMethod = compilation.GetEntryPoint(cancellationToken);
                    if (entryPointMethod == null)
                        continue;

                    var thisMethod = semanticModel.GetDeclaredSymbol(methodDeclaration);
                    if (!entryPointMethod.Equals(thisMethod))
                        continue;

                    // We found the entrypoint.  However, we can only effectively convert this to top-level-statements
                    // if the existing type is amenable to that.
                    if (!TypeCanBeConverted(entryPointMethod.ContainingType))
                        return;

                    // Looks good.  Let the user know this type/method can be converted to a top level program.
                    var severity = option.Notification.Severity;
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        this.Descriptor,
                        ConvertProgramAnalysis.GetUseTopLevelStatementsDiagnosticLocation(
                            methodDeclaration, isHidden: severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden),
                        severity,
                        ImmutableArray<Location>.Empty,
                        ImmutableDictionary<string, string?>.Empty));
                }
            }
        }

        private static bool TypeCanBeConverted(INamedTypeSymbol containingType)
        {
            // Can't convert if our Program type derives from anything special.
            if (containingType.BaseType?.SpecialType != SpecialType.System_Object)
                return false;

            if (containingType.AllInterfaces.Length > 0)
                return false;

            // Too complex to convert many parts to top-level statements.  Just bail on this for now.
            if (containingType.DeclaringSyntaxReferences.Length > 1)
                return false;

            // All the members of the type need to be private/static.  And we can only have fields or methods. that's to
            // ensure that no one else was calling into this type, and that we can convert everything in the type to
            // either locals or local-functions.
            foreach (var member in containingType.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Private)
                    return false;

                if (!member.IsStatic)
                    return false;

                if (member is not IFieldSymbol and not IMethodSymbol)
                    return false;
            }

            return true;
        }
    }
}
