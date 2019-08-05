// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTableCommandHandler))]
    internal partial class VisualStudioDiagnosticListTableCommandHandler
    {
        private readonly VisualStudioSuppressionFixService _suppressionFixService;
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;

        [ImportingConstructor]
        public VisualStudioDiagnosticListTableCommandHandler(
            IVisualStudioSuppressionFixService suppressionFixService,
            IVisualStudioDiagnosticListSuppressionStateService suppressionStateService)
        {
            _suppressionFixService = (VisualStudioSuppressionFixService)suppressionFixService;
            _suppressionStateService = (VisualStudioDiagnosticListSuppressionStateService)suppressionStateService;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            // Add command handlers for bulk suppression commands.
            var menuCommandService = (IMenuCommandService)serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            {
                // The Add/Remove suppression(s) have been moved to the VS code analysis layer, so we don't add the commands here.

                // TODO: Figure out how to access menu commands registered by CodeAnalysisPackage and 
                //       add the commands here if we cannot find the new command(s) in the code analysis layer.

                // AddSuppressionsCommandHandlers(menuCommandService);
            }
        }

        private void AddSuppressionsCommandHandlers(IMenuCommandService menuCommandService)
        {
            AddCommand(menuCommandService, ID.RoslynCommands.AddSuppressions, delegate { }, OnAddSuppressionsStatus);
            AddCommand(menuCommandService, ID.RoslynCommands.AddSuppressionsInSource, OnAddSuppressionsInSource, OnAddSuppressionsInSourceStatus);
            AddCommand(menuCommandService, ID.RoslynCommands.AddSuppressionsInSuppressionFile, OnAddSuppressionsInSuppressionFile, OnAddSuppressionsInSuppressionFileStatus);
            AddCommand(menuCommandService, ID.RoslynCommands.RemoveSuppressions, OnRemoveSuppressions, OnRemoveSuppressionsStatus);
        }

        /// <summary>
        /// Add a command handler and status query handler for a menu item
        /// </summary>
        private static OleMenuCommand AddCommand(
            IMenuCommandService menuCommandService,
            int commandId,
            EventHandler invokeHandler,
            EventHandler beforeQueryStatus)
        {
            var commandIdWithGroupId = new CommandID(Guids.RoslynGroupId, commandId);
            var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, commandIdWithGroupId);
            menuCommandService.AddCommand(command);
            return command;
        }

        private void OnAddSuppressionsStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntries;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnRemoveSuppressionsStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanRemoveSuppressionsSelectedEntries;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSourceStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntriesInSource;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSuppressionFileStatus(object sender, EventArgs e)
        {
            var command = sender as MenuCommand;
            command.Visible = _suppressionStateService.CanSuppressSelectedEntriesInSuppressionFiles;
            command.Enabled = command.Visible && !KnownUIContexts.SolutionBuildingContext.IsActive;
        }

        private void OnAddSuppressionsInSource(object sender, EventArgs e)
        {
            _suppressionFixService.AddSuppressions(selectedErrorListEntriesOnly: true, suppressInSource: true, projectHierarchyOpt: null);
        }

        private void OnAddSuppressionsInSuppressionFile(object sender, EventArgs e)
        {
            _suppressionFixService.AddSuppressions(selectedErrorListEntriesOnly: true, suppressInSource: false, projectHierarchyOpt: null);
        }

        private void OnRemoveSuppressions(object sender, EventArgs e)
        {
            _suppressionFixService.RemoveSuppressions(selectedErrorListEntriesOnly: true, projectHierarchyOpt: null);
        }
    }
}
