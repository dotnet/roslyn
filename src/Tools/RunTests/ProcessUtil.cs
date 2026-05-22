// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace RunTests
{
    internal static class ProcessUtil
    {
        internal static int? TryGetParentProcessId(Process p)
        {
            // System.Management is not supported outside of Windows.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

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
    }
}
