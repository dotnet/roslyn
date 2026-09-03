// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Templates.Editorconfig.Command.Commands;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Kinds;
using System;
using System.Runtime.InteropServices;
using System.Threading;
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
        await CommandBase.InitializeAsync<AddEditorConfigFileCommand>(this);
    }
}
