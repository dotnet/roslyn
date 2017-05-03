// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class ImmediateWindow_OutOfProc : OutOfProcComponent
    {
        private readonly ImmediateWindow_InProc _immediateWindowInProc;
        private readonly VisualStudioInstance _instance;

        public Verifier Verify { get; }

        public ImmediateWindow_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _immediateWindowInProc = CreateInProcComponent<ImmediateWindow_InProc>(visualStudioInstance);
            Verify = new Verifier(this);
        }

        public void ExecuteCommand(string command)
        {
            _immediateWindowInProc.ExecuteCommand(command);
        }
    }
}
