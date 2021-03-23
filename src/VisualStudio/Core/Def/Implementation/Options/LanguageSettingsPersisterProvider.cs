// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    internal sealed class LanguageSettingsPersisterProvider : IOptionPersisterProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IGlobalOptionService _optionService;
        private LanguageSettingsPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LanguageSettingsPersisterProvider(
            IThreadingContext threadingContext,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IGlobalOptionService optionService)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
            _optionService = optionService;
        }

        public IOptionPersister GetPersister()
        {
            if (_lazyPersister is not null)
            {
                return _lazyPersister;
            }

            _threadingContext.ThrowIfNotOnUIThread();

            var textManager = _serviceProvider.GetService<SVsTextManager, IVsTextManager4>();
            Assumes.Present(textManager);

            _lazyPersister ??= new LanguageSettingsPersister(_threadingContext, textManager, _optionService);
            return _lazyPersister;
        }
    }
}
