// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal class VisualStudioExperimentationService : IExperimentationService
    {
        private readonly object _experimentationServiceOpt;
        private readonly MethodInfo _isCachedFlightEnabledInfo;

        [ImportingConstructor]
        public VisualStudioExperimentationService(
            SVsServiceProvider serviceProvider)
        {
            try
            {
                _experimentationServiceOpt = serviceProvider.GetService(typeof(SVsExperimentationService));
                if (_experimentationServiceOpt != null)
                {
                    _isCachedFlightEnabledInfo = _experimentationServiceOpt.GetType().GetMethod(
                        "IsCachedFlightEnabled", BindingFlags.Public | BindingFlags.Instance);
                }
            }
            catch
            {
            }
        }

        public bool IsExperimentEnabled(string experimentName)
        {
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