// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Options;

[Export(typeof(IOptionPersisterProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PackageSettingsPersisterProvider(
    IThreadingContext threadingContext,
    [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider,
    IGlobalOptionService optionService) : IOptionPersisterProvider
{
    private PackageSettingsPersister? _lazyPersister;

    public ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
    {
        _lazyPersister ??= new PackageSettingsPersister(threadingContext, serviceProvider, optionService);
        return new ValueTask<IOptionPersister>(_lazyPersister);
    }
}
