// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    // xunit is crashing due to a bug in the CLR, see:https://github.com/dotnet/roslyn/issues/6358.
    //
    // This issue tends to occur more often on test binaries with 
    // very short running tests. This class solely exists to avoid hitting
    // it, and can be deleted when more tests are added to the assembly 
    // or when the CLR bug is fixed.
    public class EventListenerCrash_Workaround
    {
        [Fact]
        public void WaitFor10Seconds()
        {
            // Wait for ten seconds
            Thread.Sleep(10000);
        }
    }
}
