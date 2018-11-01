// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

        private object _experimentationServiceOpt;
        private Optional<MethodInfo> _isCachedFlightEnabledInfo;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExperimentationService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _asyncServiceProvider = (IAsyncServiceProvider)serviceProvider;
        }

        public async ValueTask<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
        {
            if (!_isCachedFlightEnabledInfo.HasValue)
            {
                MethodInfo isCachedFlightEnabledInfo;

                try
                {
                    _experimentationServiceOpt = await _asyncServiceProvider.GetServiceAsync(typeof(SVsExperimentationService));
                    isCachedFlightEnabledInfo = _experimentationServiceOpt?.GetType().GetMethod(
                        "IsCachedFlightEnabled", BindingFlags.Public | BindingFlags.Instance);
                }
                catch
                {
                    isCachedFlightEnabledInfo = null;
                }

                _isCachedFlightEnabledInfo = isCachedFlightEnabledInfo;
            }

            if (_isCachedFlightEnabledInfo.Value != null)
            {
                try
                {
                    return (bool)_isCachedFlightEnabledInfo.Value.Invoke(_experimentationServiceOpt, new object[] { experimentName });
                }
                catch
                {

                }
            }

            return false;
        }
    }
}
