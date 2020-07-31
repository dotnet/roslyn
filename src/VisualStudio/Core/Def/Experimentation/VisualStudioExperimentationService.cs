// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Cache of values we've queried from the underlying VS service.  These values are expected to last for the
        /// lifetime of the session, so it's fine for us to cache things to avoid the heavy cost of querying for them
        /// over and over.
        /// </summary>
        private readonly Dictionary<string, bool> _experimentEnabledMap = new Dictionary<string, bool>();

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

            // First look in our cache to see if this has already been computed and cached.
            lock (_experimentEnabledMap)
            {
                if (_experimentEnabledMap.TryGetValue(experimentName, out var result))
                    return result;
            }

            // Otherwise, compute and cache this ourselves.  It's fine if multiple callers cause this to happen.  We'll
            // just let the last one win.
            var enabled = IsExperimentEnabledWorker(experimentName);

            lock (_experimentEnabledMap)
            {
                _experimentEnabledMap[experimentName] = enabled;
            }

            return enabled;
        }

        private bool IsExperimentEnabledWorker(string experimentName)
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
