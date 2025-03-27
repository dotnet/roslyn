// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ObjectFormatterFixtures
{
    /// <summary>
    /// Follows the shape of the Desktop version of <see cref="Task"/> relevant for debugger display.
    /// </summary>
    [DebuggerTypeProxy(typeof(MockTaskProxy))]
    [DebuggerDisplay("Id = {Id}, Status = {Status}, Method = {DebuggerDisplayMethodDescription}")]
    internal class MockDesktopTask
    {
        private readonly Action m_action;

        public MockDesktopTask(Action action)
        {
            m_action = action;
        }

        public int Id => 1234;
        public object AsyncState => null;
        public TaskCreationOptions CreationOptions => TaskCreationOptions.None;
        public Exception Exception => null;
        public TaskStatus Status => TaskStatus.Created;

        private string DebuggerDisplayMethodDescription
            => m_action.Method.ToString();
    }

    internal class MockTaskProxy
    {
        private readonly MockDesktopTask m_task;
        public object AsyncState => m_task.AsyncState;
        public TaskCreationOptions CreationOptions => m_task.CreationOptions;
        public Exception Exception => m_task.Exception;
        public int Id => m_task.Id;
        public bool CancellationPending => false;
        public TaskStatus Status => m_task.Status;

        public MockTaskProxy(MockDesktopTask task)
        {
            m_task = task;
        }
    }
}
