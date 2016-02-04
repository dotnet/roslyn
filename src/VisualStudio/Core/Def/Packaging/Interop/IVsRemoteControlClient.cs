using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    [Guid("2DB673B1-8643-4236-A7DD-CD063D074BB1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsRemoteControlClient
    {
        [DispId(1610678272)]
        string FileUrl { get; }

        void Close();
        ISequentialStream ReadFile([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.VsRemoteControlBehaviorOnStale")] int staleBehavior);
    }

    enum __VsRemoteControlBehaviorOnStale
    {
        /// <summary>
        /// Returns the last locally cached file for this URL or null if no locally cached file found.
        /// </summary>
        ReturnsStale = 0,

        /// <summary>
        /// If the locally cached file exists and it was checked against the server less than pollingIntervalMinutes (specified in CreateClient) ago, returns that. Otherwise null.
        /// </summary>
        ReturnsNull = 1
    }
}