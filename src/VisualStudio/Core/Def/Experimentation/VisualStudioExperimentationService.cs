// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [Export(typeof(VisualStudioExperimentationService))]
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal class VisualStudioExperimentationService : ForegroundThreadAffinitizedObject, IExperimentationService
    {
        private readonly object _experimentationServiceOpt;
        private readonly MethodInfo _isCachedFlightEnabledInfo;
        private readonly IVsFeatureFlags _featureFlags;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExperimentationService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            object experimentationServiceOpt = null;
            MethodInfo isCachedFlightEnabledInfo = null;
            IVsFeatureFlags featureFlags = null;

            threadingContext.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    featureFlags = (IVsFeatureFlags)await ((IAsyncServiceProvider)serviceProvider).GetServiceAsync(typeof(SVsFeatureFlags)).ConfigureAwait(false);
                    experimentationServiceOpt = await ((IAsyncServiceProvider)serviceProvider).GetServiceAsync(typeof(SVsExperimentationService)).ConfigureAwait(false);
                    if (experimentationServiceOpt != null)
                    {
                        isCachedFlightEnabledInfo = experimentationServiceOpt.GetType().GetMethod(
                            "IsCachedFlightEnabled", BindingFlags.Public | BindingFlags.Instance);
                    }
                }
                catch
                {
                }
            });

            _featureFlags = featureFlags;
            _experimentationServiceOpt = experimentationServiceOpt;
            _isCachedFlightEnabledInfo = isCachedFlightEnabledInfo;
        }

        public bool IsExperimentEnabled(string experimentName)
        {
            ThisCanBeCalledOnAnyThread();
            if (_isCachedFlightEnabledInfo != null)
            {
                try
                {
                    // check whether "." exist in the experimentName since it is requirement for featureflag service.
                    // we do this since RPS complains about resource file being loaded for invalid name exception
                    // we are not testing all rules but just simple "." check
                    if (experimentName.IndexOf(".") > 0)
                    {
                        var enabled = _featureFlags.IsFeatureEnabled(experimentName, defaultValue: false);
                        if (enabled)
                        {
                            return enabled;
                        }
                    }
                }
                catch
                {
                    // featureFlags can throw if given name is in incorrect format which can happen for us
                    // since we use this for experimentation service as well
                }

                try
                {
                    return (bool)_isCachedFlightEnabledInfo.Invoke(_experimentationServiceOpt, new object[] { experimentName });
                }
                catch
                {

                }
            }

            return false;
        }
    }
}
