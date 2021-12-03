// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class LocalUserRegistryOptionPersisterProvider : IOptionPersisterProvider
    {
        private readonly AsyncLazy<LocalUserRegistryOptionPersister> _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LocalUserRegistryOptionPersisterProvider(
            [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider serviceProvider)
        {
            _lazyPersister = new(_ => LocalUserRegistryOptionPersister.CreateAsync(serviceProvider), cacheResult: true);
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
            => await _lazyPersister.GetValueAsync(cancellationToken).ConfigureAwait(false);
    }
}
