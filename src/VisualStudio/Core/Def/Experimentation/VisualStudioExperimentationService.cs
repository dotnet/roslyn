// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal class VisualStudioExperimentationService : ForegroundThreadAffinitizedObject, IExperimentationService
    {
        private readonly IAsyncServiceProvider _asyncServiceProvider;

        private Func<string, bool> _isCachedFlightEnabled;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExperimentationService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _asyncServiceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public async ValueTask<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            if (_isCachedFlightEnabled is null)
            {
                Func<string, bool> isCachedFlightEnabled;

                try
                {
                    var experimentationService = await _asyncServiceProvider.GetServiceAsync(typeof(SVsExperimentationService));
                    var isCachedFlightEnabledInfo = experimentationService?.GetType().GetMethod(
                        "IsCachedFlightEnabled", BindingFlags.Public | BindingFlags.Instance);

                    isCachedFlightEnabled = (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), experimentationService, isCachedFlightEnabledInfo);
                }
                catch
                {
                    isCachedFlightEnabled = _ => false;
                }

                Interlocked.CompareExchange(ref _isCachedFlightEnabled, isCachedFlightEnabled, null);
            }

            return _isCachedFlightEnabled(experimentName);
        }
    }
}
