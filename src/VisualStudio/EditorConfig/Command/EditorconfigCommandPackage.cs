// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Command;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(PackageGuids.AddEditorConfigString)]
public sealed class EditorconfigCommandPackage : AsyncPackage
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        using var _ = LogOperation(OperationId.InitializePackage);
        await CommandBase.InitializeAsync<AddEditorConfigFileCommand>(this).ConfigureAwait(true);
    }
}
