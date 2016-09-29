// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace ProcessWatchdog
{
    /// <summary>
    /// Represents a process whose identity is guaranteed to be unique across time.
    /// </summary>
    /// <remarks>
    /// The integer id that Windows associates with each process is not guaranteed to be
    /// unique across time. Windows "recycles" these process ids; that is after a process
    /// with a given id has terminated, Windows might create another process with the
    /// same id. To uniquely identify a process, we use the combination of the process id
    /// and its creation time.
    /// </remarks>
    internal class UniqueProcess : IEquatable<UniqueProcess>
    {
        private const string CreationDatePropertyName = "CreationDate";

        internal static bool TryCreate(Process process, out UniqueProcess uniqueProcess)
        {
            bool result = false;
            uniqueProcess = null;

            string query = string.Format(
                CultureInfo.InvariantCulture,
                "SELECT * FROM Win32_Process WHERE ProcessId={0}",
                process.Id);

            var searcher = new ManagementObjectSearcher(query);

            ManagementObjectCollection.ManagementObjectEnumerator enumerator = searcher.Get().GetEnumerator();

            // If the process passed in as the argument had already terminated, the query
            // won't have returned any objects, so make sure there is one.
            if (enumerator.MoveNext())
            {
                var wmiProcess = enumerator.Current as ManagementObject;
                DateTime creationTime =
                    ManagementDateTimeConverter.ToDateTime(wmiProcess[CreationDatePropertyName] as string);

                if (!process.HasExited)
                {
                    // If the process is still running, the Win32_Process object we just
                    // got from WMI must be the must refer to this process, so we can
                    // safely associate this process with this creation time.
                    uniqueProcess = new UniqueProcess(process, creationTime);
                    result = true;
                }
            }

            return result;
        }

        private UniqueProcess(Process process, DateTime creationTime)
        {
            Process = process;
            CreationTime = creationTime;
        }

        internal Process Process { get; }

        internal DateTime CreationTime { get; }

        internal int Id => Process.Id;

        internal bool HasExited => Process.HasExited;

        internal void Kill()
        {
            if (!Process.HasExited)
            {
                try
                {
                    Process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // This will happen if the process ended between the call to
                    // Process.HasExited and the call to Process.Kill. It doesn't
                    // indicate an error, so ignore it.
                }
            }
        }

        #region Object overrides

        public override bool Equals(object other)
        {
            return Equals(other as UniqueProcess);
        }

        public override int GetHashCode()
        {
            int result = 17;

            unchecked
            {
                result = (result * 31) + Process.GetHashCode();
                result = (result * 31) + CreationTime.GetHashCode();
            }

            return result;
        }

        #endregion Object overrides

        #region IEquatable<T>

        public bool Equals(UniqueProcess other)
        {
            if (other == null)
            {
                return false;
            }

            return Process.Id == other.Process.Id
                && CreationTime == other.CreationTime;
        }

        #endregion IEquatable<T>
    }
}
