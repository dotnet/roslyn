using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal static class ProcessUtil
    {
        internal static int? TryGetParentProcessId(Process p)
        {
            try
            {
                ManagementObject mo = new ManagementObject("win32_process.handle='" + p.Id + "'");
                mo.Get();
                return Convert.ToInt32(mo["ParentProcessId"]);
            }
            catch 
            {
                return null;
            }
        }

        /// <summary>
        /// Return the list of processes which are direct children of the provided <paramref name="process"/> 
        /// instance.
        /// </summary>
        /// <remarks>
        /// This is a best effort API.  It can be thwarted by process instances starting / stopping during
        /// the building of this list.
        /// </remarks>
        internal static List<Process> GetProcessChildren(Process process) => GetProcessChildrenCore(process, Process.GetProcesses());

        private static List<Process> GetProcessChildrenCore(Process parentProcess, IEnumerable<Process> processes)
        {
            var list = new List<Process>();
            foreach (var process in processes)
            {
                var parentId = TryGetParentProcessId(process);
                if (parentId == parentProcess.Id)
                {
                    list.Add(process);
                }
            }

            return list;
        }

        /// <summary>
        /// Return the list of processes which are direct or indirect children of the provided <paramref name="process"/> 
        /// instance.
        /// </summary>
        /// <remarks>
        /// This is a best effort API.  It can be thwarted by process instances starting / stopping during
        /// the building of this list.
        /// </remarks>
        internal static List<Process> GetProcessTree(Process process)
        {
            var processes = Process.GetProcesses();
            var list = new List<Process>();
            var toVisit = new Queue<Process>();
            toVisit.Enqueue(process);

            while (toVisit.Count > 0)
            {
                var cur = toVisit.Dequeue();
                var children = GetProcessChildrenCore(cur, processes);
                foreach (var child in children)
                {
                    toVisit.Enqueue(child);
                    list.Add(child);
                }
            }

            return list;
        }

        internal static bool Is64Bit(Process process)
        {
            if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
            {
                return false;
            }

            bool isWow64;
            if (!IsWow64Process(process.Handle, out isWow64))
            {
                throw new Exception($"{nameof(IsWow64Process)} failed with {Marshal.GetLastWin32Error()}");
            }

            return !isWow64;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }
}
