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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class LocalUserRegistryOptionPersisterProvider : IOptionPersisterProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private LocalUserRegistryOptionPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LocalUserRegistryOptionPersisterProvider(
            IThreadingContext threadingContext,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
        {
            if (_lazyPersister is not null)
            {
                return _lazyPersister;
            }

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _lazyPersister ??= new LocalUserRegistryOptionPersister(_threadingContext, _serviceProvider);
            return _lazyPersister;
        }
    }
}
