using System.Runtime.InteropServices;

namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    [Guid("84C6751B-0B11-4A7B-98D2-DD1061111EA0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsRemoteControlService
    {
        IVsRemoteControlClient CreateClient([ComAliasName("OLE.LPCOLESTR")] string szHostId, [ComAliasName("OLE.LPCOLESTR")] string szRelativeFilePath, int pollingIntervalMinutes);
    }
}