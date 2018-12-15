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
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal class VisualStudioExperimentationService : ForegroundThreadAffinitizedObject, IExperimentationService
    {
        private readonly object _experimentationServiceOpt;
        private readonly MethodInfo _isCachedFlightEnabledInfo;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExperimentationService(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            object experimentationServiceOpt = null;
            MethodInfo isCachedFlightEnabledInfo = null;

            threadingContext.JoinableTaskFactory.Run(async () =>
            {
                try
                {
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
