﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedVariable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.CodeCleanup;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using __VSHPROPID8 = Microsoft.VisualStudio.Shell.Interop.__VSHPROPID8;
using IVsHierarchyItemManager = Microsoft.VisualStudio.Shell.IVsHierarchyItemManager;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService
{
    [Export(typeof(CodeCleanUpFixer))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpCodeCleanUpFixer : CodeCleanUpFixer
    {
        private const string RemoveUnusedImportsFixId = "RemoveUnusedImportsFixId";
        private const string SortImportsFixId = "SortImportsFixId";

        [Export]
        [FixId(IDEDiagnosticIds.UseImplicitTypeDiagnosticId)]
        [Name(IDEDiagnosticIds.UseImplicitTypeDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_implicit_explicit_type_preferences))]
        public static readonly FixIdDefinition UseImplicitTypeDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExplicitTypeDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExplicitTypeDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_implicit_explicit_type_preferences))]
        public static readonly FixIdDefinition UseExplicitTypeDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddQualificationDiagnosticId)]
        [Name(IDEDiagnosticIds.AddQualificationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_this_qualification_preferences))]
        public static readonly FixIdDefinition AddQualificationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveQualificationDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveQualificationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_this_qualification_preferences))]
        public static readonly FixIdDefinition RemoveQualificationDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [Name(IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [Order(After = IDEDiagnosticIds.InlineDeclarationDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_language_framework_type_preferences))]
        public static readonly FixIdDefinition PreferBuiltInOrFrameworkTypeDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddBracesDiagnosticId)]
        [Name(IDEDiagnosticIds.AddBracesDiagnosticId)]
        [Order(After = SortImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Add_remove_braces_for_single_line_control_statements))]
        public static readonly FixIdDefinition AddBracesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [Name(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddBracesDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Add_accessibility_modifiers))]
        public static readonly FixIdDefinition AddAccessibilityModifiersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [Name(IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Sort_accessibility_modifiers))]
        public static readonly FixIdDefinition OrderModifiersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [Name(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [Order(After = IDEDiagnosticIds.AddQualificationDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Make_private_field_readonly_when_possible))]
        public static readonly FixIdDefinition MakeFieldReadonlyDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [Name(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [Order(After = IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Remove_unnecessary_casts))]
        public static readonly FixIdDefinition RemoveUnnecessaryCastDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForConstructorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForConstructorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForMethodsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForMethodsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForConversionOperatorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForConversionOperatorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForOperatorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForOperatorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForPropertiesDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForIndexersDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForIndexersDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId)]
        [Name(IDEDiagnosticIds.UseExpressionBodyForAccessorsDiagnosticId)]
        [Order(After = IDEDiagnosticIds.OrderModifiersDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_expression_block_body_preferences))]
        public static readonly FixIdDefinition UseExpressionBodyForAccessorsDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.InlineDeclarationDiagnosticId)]
        [Name(IDEDiagnosticIds.InlineDeclarationDiagnosticId)]
        [Order(After = IDEDiagnosticIds.UseImplicitTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_inline_out_variable_preferences))]
        public static readonly FixIdDefinition InlineDeclarationDiagnosticId;

        [Export]
        [FixId(CSharpRemoveUnusedVariableCodeFixProvider.CS0168)]
        [Name(CSharpRemoveUnusedVariableCodeFixProvider.CS0168)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Remove_unused_variables))]
        public static readonly FixIdDefinition CS0168;

        [Export]
        [FixId(CSharpRemoveUnusedVariableCodeFixProvider.CS0219)]
        [Name(CSharpRemoveUnusedVariableCodeFixProvider.CS0219)]
        [Order(After = IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Remove_unused_variables))]
        public static readonly FixIdDefinition CS0219;

        [Export]
        [FixId(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [Name(IDEDiagnosticIds.UseObjectInitializerDiagnosticId)]
        [Order(After = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_object_collection_initialization_preferences))]
        public static readonly FixIdDefinition UseObjectInitializerDiagnosticId;

        [Export]
        [FixId(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)]
        [Name(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)]
        [Order(After = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [LocalizedName(typeof(CSharpFeaturesResources), nameof(CSharpFeaturesResources.Apply_object_collection_initialization_preferences))]
        public static readonly FixIdDefinition UseCollectionInitializerDiagnosticId;

        [Export]
        [FixId(RemoveUnusedImportsFixId)]
        [Name(RemoveUnusedImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(CSharpVSResources), nameof(CSharpVSResources.Remove_unnecessary_usings))]
        public static readonly FixIdDefinition RemoveUnusedImports;

        [Export]
        [FixId(SortImportsFixId)]
        [Name(SortImportsFixId)]
        [Order(After = RemoveUnusedImportsFixId)]
        [ConfigurationKey("unused")]
        [HelpLink("https://www.microsoft.com")]
        [ExportMetadata("EnableByDefault", true)]
        [LocalizedName(typeof(CSharpVSResources), nameof(CSharpVSResources.Sort_usings))]
        public static readonly FixIdDefinition SortImports;

        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeCleanUpFixer(IThreadingContext threadingContext, VisualStudioWorkspaceImpl workspace, IVsHierarchyItemManager vsHierarchyItemManager)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _vsHierarchyItemManager = vsHierarchyItemManager;
        }

        public override Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            switch (scope)
            {
                case TextBufferCodeCleanUpScope textBufferScope:
                    return FixTextBufferAsync(textBufferScope, context, cancellationToken);
                case IVsHierarchyCodeCleanupScope hierarchyContentScope:
                    return FixHierarchyContentAsync(hierarchyContentScope, context, cancellationToken);
                default:
                    return Task.FromResult(false);
            }
        }

        private async Task<bool> FixHierarchyContentAsync(IVsHierarchyCodeCleanupScope hierarchyContent, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            var hierarchy = hierarchyContent.Hierarchy;
            if (hierarchy == null)
            {
                return await FixSolutionAsync(_workspace.CurrentSolution, context, cancellationToken).ConfigureAwait(true);
            }

            var itemId = hierarchyContent.ItemId;
            if (itemId == (uint)VSConstants.VSITEMID.Root)
            {
                // Map the hierarchy to a ProjectId. For hierarchies mapping to multitargeted projects, we first try to
                // get the project in the most recent active context, but fall back to the first target framework if no
                // active context is available.
                var hierarchyToProjectMap = _workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();

                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                ProjectId projectId = null;
                if (ErrorHandler.Succeeded(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, out var contextProjectNameObject))
                    && contextProjectNameObject is string contextProjectName)
                {
                    projectId = _workspace.GetProjectWithHierarchyAndName(hierarchy, contextProjectName)?.Id;
                }

                if (projectId is null)
                {
                    var projectHierarchyItem = _vsHierarchyItemManager.GetHierarchyItem(hierarchyContent.Hierarchy, itemId);
                    if (!hierarchyToProjectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker: null, out projectId))
                    {
                        return false;
                    }
                }

                await TaskScheduler.Default;

                var project = _workspace.CurrentSolution.GetProject(projectId);
                if (project == null)
                {
                    return false;
                }

                return await FixProjectAsync(project, context, cancellationToken).ConfigureAwait(true);
            }
            else if (hierarchy.GetCanonicalName(itemId, out var path) == 0)
            {
                var attr = File.GetAttributes(path);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    // directory
                    // TODO: this one will be implemented later
                    // https://github.com/dotnet/roslyn/issues/30165
                }
                else
                {
                    // document
                    // TODO: this one will be implemented later
                    // https://github.com/dotnet/roslyn/issues/30165
                }
            }

            return false;
        }

        private Task<bool> FixSolutionAsync(Solution solution, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            return FixAsync(solution.Workspace, ApplyFixAsync, context, cancellationToken);

            // Local function
            Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken innerCancellationToken)
            {
                return FixSolutionAsync(solution, context.EnabledFixIds, progressTracker, cancellationToken);
            }
        }

        private Task<bool> FixProjectAsync(Project project, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            return FixAsync(project.Solution.Workspace, ApplyFixAsync, context, cancellationToken);

            // Local function
            async Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken innerCancellationToken)
            {
                var newProject = await FixProjectAsync(project, context.EnabledFixIds, progressTracker, addProgressItemsForDocuments: true, cancellationToken).ConfigureAwait(true);
                return newProject.Solution;
            }
        }

        private Task<bool> FixTextBufferAsync(TextBufferCodeCleanUpScope textBufferScope, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            var buffer = textBufferScope.SubjectBuffer;
            return FixAsync(buffer.GetWorkspace(), ApplyFixAsync, context, cancellationToken);

            // Local function
            async Task<Solution> ApplyFixAsync(ProgressTracker progressTracker, CancellationToken innerCancellationToken)
            {
                var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                var newDoc = await FixDocumentAsync(document, context.EnabledFixIds, progressTracker, innerCancellationToken).ConfigureAwait(true);
                return newDoc.Project.Solution;
            }
        }

        private async Task<bool> FixAsync(Workspace workspace, Func<ProgressTracker, CancellationToken, Task<Solution>> applyFixAsync, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken)
        {
            using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.OperationContext.UserCancellationToken, cancellationToken))
            {
                cancellationToken = cancellationTokenSource.Token;

                using (var scope = context.OperationContext.AddScope(allowCancellation: true, description: EditorFeaturesResources.Applying_changes))
                {
                    var progressTracker = new ProgressTracker((description, completed, total) =>
                    {
                        if (scope != null)
                        {
                            scope.Description = description;
                            scope.Progress.Report(new ProgressInfo(completed, total));
                        }
                    });

                    var solution = await applyFixAsync(progressTracker, cancellationToken).ConfigureAwait(true);

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    return workspace.TryApplyChanges(solution, progressTracker);
                }
            }
        }

        private async Task<Solution> FixSolutionAsync(Solution solution, FixIdContainer enabledFixIds, ProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            // Prepopulate the solution progress tracker with the total number of documents to process
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                if (!CanCleanupProject(project))
                {
                    continue;
                }

                progressTracker.AddItems(project.DocumentIds.Count);
            }

            foreach (var projectId in solution.ProjectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = solution.GetProject(projectId);
                var newProject = await FixProjectAsync(project, enabledFixIds, progressTracker, addProgressItemsForDocuments: false, cancellationToken).ConfigureAwait(false);
                solution = newProject.Solution;
            }

            return solution;
        }

        private async Task<Project> FixProjectAsync(Project project, FixIdContainer enabledFixIds, ProgressTracker progressTracker, bool addProgressItemsForDocuments, CancellationToken cancellationToken)
        {
            if (!CanCleanupProject(project))
            {
                return project;
            }

            if (addProgressItemsForDocuments)
            {
                progressTracker.AddItems(project.DocumentIds.Count);
            }

            foreach (var documentId in project.DocumentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var document = project.GetDocument(documentId);
                progressTracker.Description = document.Name;

                // FixDocumentAsync reports progress within a document, but we limit progress reporting for a project
                // to the current document.
                var documentProgressTracker = new ProgressTracker();

                var fixedDocument = await FixDocumentAsync(document, enabledFixIds, documentProgressTracker, cancellationToken).ConfigureAwait(false);
                project = fixedDocument.Project;
                progressTracker.ItemCompleted();
            }

            return project;
        }

        private static bool CanCleanupProject(Project project)
            => project.LanguageServices.GetService<ICodeCleanupService>() != null;

        private async Task<Document> FixDocumentAsync(Document document, FixIdContainer enabledFixIds, ProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            if (document.IsGeneratedCode(cancellationToken))
            {
                return document;
            }

            var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();

            var allDiagnostics = codeCleanupService.GetAllDiagnostics();

            var enabedDiagnosticSets = ArrayBuilder<DiagnosticSet>.GetInstance();

            foreach (var diagnostic in allDiagnostics.Diagnostics)
            {
                foreach (var diagnosticId in diagnostic.DiagnosticIds)
                {
                    if (enabledFixIds.IsFixIdEnabled(diagnosticId))
                    {
                        enabedDiagnosticSets.Add(diagnostic);
                        break;
                    }
                }
            }

            var isRemoveUnusedUsingsEnabled = enabledFixIds.IsFixIdEnabled(RemoveUnusedImportsFixId);
            var isSortUsingsEnabled = enabledFixIds.IsFixIdEnabled(SortImportsFixId);
            var enabledDiagnostics = new EnabledDiagnosticOptions(enabedDiagnosticSets.ToImmutableArray(),
                new OrganizeUsingsSet(isRemoveUnusedUsingsEnabled, isSortUsingsEnabled));

            return await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, progressTracker, cancellationToken).ConfigureAwait(false);
        }
    }
}
