// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.CompilerServices;

namespace Roslyn.Test.Performance.Utilities
{
    /// <summary>
    /// An abstract class that concrete perf-tests implement in order to be run.
    /// </summary>
    public abstract class PerfTest : RelativeDirectory
    {
        /// <summary>
        /// Constructor for PerfTest that sets up the correct working path location.
        /// </summary>
        /// <param name="workingFile"></param>
        public PerfTest([CallerFilePath] string workingFile = "") : base(workingFile) { }

        /// <summary>
        /// Setup is called once for every test on a run.  This is where you should do all
        /// setup, including downloading files and preparing paths.
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// The body of the test.  In most cases, this method will be shelling out to an 
        /// external tool.
        /// </summary>
        public abstract void Test();

        /// <summary>
        /// The number of iterations that the test should run for.
        /// </summary>
        public virtual int Iterations => 1;

        /// <summary>
        /// The human-readable name of the test.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The name of the process that the profiler should pay attention to.
        /// 
        /// 'csc' is an example.
        /// </summary>
        public abstract string MeasuredProc { get; }

        /// <summary>
        /// Returns true if the test provides its own CPC scenarios.
        /// </summary>
        public abstract bool ProvidesScenarios { get; }

        /// <summary>
        /// A list of scenarios.
        /// </summary>
        /// <returns></returns>
        public abstract string[] GetScenarios();

        public virtual ITraceManager GetTraceManager()
        {
            return TraceManagerFactory.GetBestTraceManager();
        }
    }
}
