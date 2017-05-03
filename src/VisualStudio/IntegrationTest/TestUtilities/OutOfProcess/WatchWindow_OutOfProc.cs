// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class WatchWindow_OutOfProc : OutOfProcComponent
    {
        public Verifier Verify { get; }

        public WatchWindow_OutOfProc(VisualStudioInstance visualStudioInstance) : base(visualStudioInstance)
        {
            Verify = new Verifier(this);
        }

        public void AddEntry(string expression)
        {
            throw new NotImplementedException();
        }
        public void DeleteAllEntries()
        {
            throw new NotImplementedException();
        }
    }
}