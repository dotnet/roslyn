// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// D:\Roslyn\Main\Open\Binaries\Debug\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Debugger
{
    /// <summary>
    /// This mock of DkmWorkList doesn't really reflect the details of the *real* implementation.
    /// It simply serves as a useful mechanism for testing async calls (in a way that resembles
    /// the Concord dispatcher).
    /// </summary>
    public sealed class DkmWorkList
    {
        private readonly Queue<Action> _workList;

        /// <summary>
        /// internal helper for testing only (not available on *real* DkmWorkList)...
        /// </summary>
        internal DkmWorkList()
        {
            _workList = new Queue<Action>(1);
        }

        /// <summary>
        /// internal helper for testing only (not available on *real* DkmWorkList)...
        /// </summary>
        internal void AddWork(Action item)
        {
            _workList.Enqueue(item);
        }

        /// <summary>
        /// internal helper for testing only (not available on *real* DkmWorkList)...
        /// </summary>
        internal int Length
        {
            get { return _workList.Count; }
        }

        public void Execute()
        {
            while (_workList.Count > 0)
            {
                var item = _workList.Dequeue();
                item.Invoke();
            }
        }
    }
}
