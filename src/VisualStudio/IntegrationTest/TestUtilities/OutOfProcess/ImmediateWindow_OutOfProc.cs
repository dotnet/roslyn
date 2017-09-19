// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.Test.Apex.VisualStudio.Shell;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class ImmediateWindow_OutOfProc : OutOfProcComponent
    {
        private static readonly TimeSpan defaultTimeout = new TimeSpan(TimeSpan.TicksPerSecond * 10);
        private readonly VisualStudioInstance _instance;
        private readonly ImmediateWindowService _immediateWindowService;

        internal ImmediateWindow_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _immediateWindowService = visualStudioInstance.VisualStudioHost.ObjectModel.Shell.ToolWindows.ImmediateWindow;
        }

        public void ValidateCommand(string command, string expectedResult)
        {
            _immediateWindowService.Clear();
            _immediateWindowService.TypeCommand(command);
            Assert.True(_immediateWindowService.Verify.ContainsText(expectedResult, defaultTimeout));
        }
    }
}
