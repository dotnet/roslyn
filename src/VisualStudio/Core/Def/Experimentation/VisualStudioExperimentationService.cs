// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal class VisualStudioExperimentationService : ForegroundThreadAffinitizedObject, IExperimentationService
    {
        private readonly IExperimentationService _experimentationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioExperimentationService(IThreadingContext threadingContext, ExperimentationService experimentationService)
            : base(threadingContext)
        {
            _experimentationService = experimentationService;
        }

        public bool IsExperimentEnabled(string experimentName)
        {
            ThisCanBeCalledOnAnyThread();

            return _experimentationService.IsExperimentEnabled(experimentName);
        }
    }
}
