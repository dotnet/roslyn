// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.SolutionExplorer;

[Export(typeof(SolutionExplorerSymbolTreeItemCommandHandler)), Shared]
internal sealed class SolutionExplorerSymbolTreeItemCommandHandler
{
    private readonly IThreadingContext _threadingContext;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SolutionExplorerSymbolTreeItemCommandHandler(
        IThreadingContext threadingContext)
    {
        _threadingContext = threadingContext;
    }

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // _serviceProvider = (IServiceProvider)serviceProvider;

        // Hook up the "Remove Unused References" menu command for CPS based managed projects.
        var menuCommandService = await serviceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(throwOnFailure: false, cancellationToken).ConfigureAwait(false);
        if (menuCommandService != null)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VisualStudioCommandHandlerHelpers.AddCommand(
                menuCommandService,
                ID.RoslynCommands.SolutionExplorerSymbolItemFindAllReferences,
                Guids.RoslynGroupId,
                (sender, args) =>
                {
                    var command = (OleMenuCommand)sender;
                    command.Visible = true;
                    command.Enabled = true;
                }, 
                (sender, args) =>
                {

                });
        }
    }
}
