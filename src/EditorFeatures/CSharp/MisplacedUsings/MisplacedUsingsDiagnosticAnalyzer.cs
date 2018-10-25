// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class MisplacedUsingsDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly CodeStyleOption<UsingPlacementPreference> s_noPreferenceOption =
            new CodeStyleOption<UsingPlacementPreference>(UsingPlacementPreference.NoPreference, NotificationOption.None);

        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
            nameof(CSharpEditorResources.Misplaced_using), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpEditorResources.Using_directives_must_be_placed_inside_a_namespace_declaration), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        private static readonly LocalizableResourceString s_localizableOutsideMessage = new LocalizableResourceString(
            nameof(CSharpEditorResources.Using_directives_must_be_placed_outside_a_namespace_declaration), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        internal static readonly DiagnosticDescriptor _insideDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId,
            s_localizableTitle,
            s_localizableInsideMessage,
            configurable: true);

        internal static readonly DiagnosticDescriptor _outsideDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId,
            s_localizableTitle,
            s_localizableOutsideMessage,
            configurable: true);

        private readonly ICodingConventionsManager _conventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();

        public MisplacedUsingsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId, s_localizableTitle)
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CompilationUnit);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.NamespaceDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var statement = context.Node;
            var cancellationToken = context.CancellationToken;

            var option = GetPreferredPlacementOption(context);
            if (option.Value == UsingPlacementPreference.NoPreference)
            {
                return;
            }

            if (statement is CompilationUnitSyntax compilationUnit
                && option.Value == UsingPlacementPreference.InsideNamespace
                && !ShouldSurpressDiagnostic(compilationUnit))
            {
                ReportDiagnostics(context, _insideDescriptor, compilationUnit.Usings, option);
            }
            else if (statement is NamespaceDeclarationSyntax namespaceDeclaration
                && option.Value == UsingPlacementPreference.OutsideNamespace)
            {
                ReportDiagnostics(context, _outsideDescriptor, namespaceDeclaration.Usings, option);
            }
        }

        private CodeStyleOption<UsingPlacementPreference> GetPreferredPlacementOption(SyntaxNodeAnalysisContext context)
        {
            var statement = context.Node;
            var cancellationToken = context.CancellationToken;

            // While running in VisualStudio this should always return an OptionSet.
            var optionSet = context.Options.GetDocumentOptionSetAsync(statement.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet != null)
            {
                return optionSet.GetOption(CSharpCodeStyleOptions.PreferredUsingPlacement);
            }

            // This code path is for tests only. It relies on the test creating a .editorconfig in the current working directory.
            var filePath = context.SemanticModel.SyntaxTree.FilePath;
            var fullPath = string.IsNullOrEmpty(Path.GetDirectoryName(filePath))
                ? Path.Combine(Environment.CurrentDirectory, filePath)
                : filePath;

            // Find the correct key name from the .editorconfig storage location
            var storageLocation = CSharpCodeStyleOptions.PreferredUsingPlacement.StorageLocations
                .OfType<EditorConfigStorageLocation<CodeStyleOption<UsingPlacementPreference>>>()
                .FirstOrDefault();

            var conventionContext = _conventionsManager.GetConventionContextAsync(fullPath, cancellationToken).GetAwaiter().GetResult();
            if (conventionContext.CurrentConventions.TryGetConventionValue(storageLocation.KeyName, out string usingPlacementPreference))
            {
                return CSharpCodeStyleOptions.ParseUsingPlacementPreference(usingPlacementPreference, s_noPreferenceOption);
            }

            return s_noPreferenceOption;
        }

        private bool ShouldSurpressDiagnostic(CompilationUnitSyntax compilationUnit)
        {
            // Surpress if file contains a type declaration in the global namespace
            // or if an attribute is used in the global namespace.
            return compilationUnit.ChildNodes().Any(node =>
                node.IsKind(SyntaxKind.ClassDeclaration)
                || node.IsKind(SyntaxKind.InterfaceDeclaration)
                || node.IsKind(SyntaxKind.EnumDeclaration)
                || node.IsKind(SyntaxKind.StructDeclaration)
                || node.IsKind(SyntaxKind.DelegateDeclaration)
                || node.IsKind(SyntaxKind.AttributeList));
        }

        private void ReportDiagnostics(
            SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
            IEnumerable<UsingDirectiveSyntax> usingDirectives, CodeStyleOption<UsingPlacementPreference> option)
        {
            foreach (var usingDirective in usingDirectives)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    descriptor,
                    usingDirective.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}
