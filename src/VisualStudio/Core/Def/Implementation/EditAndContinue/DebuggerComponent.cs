// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Debugger;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    internal static class DebuggerComponent
    {
        /// <summary>
        /// Component id as specified in ManagedEditAndContinueService.vsdconfigxml.
        /// </summary>
        private static readonly Guid ManagedEditAndContinueServiceId = new Guid("A96BBE03-0408-41E3-8613-6086FD494B43");

        public static ThreadInitializer ManagedEditAndContinueService()
            => new ThreadInitializer(ManagedEditAndContinueServiceId);

        public struct ThreadInitializer : IDisposable
        {
            private readonly Guid _id;
            private readonly bool _alreadyInitialized;

            public ThreadInitializer(Guid id)
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    _alreadyInitialized = true;
                }
                else
                {
                    DkmComponentManager.InitializeThread(id, out _alreadyInitialized);
                }

                _id = id;
            }

            public void Dispose()
            {
                if (!_alreadyInitialized)
                {
                    // Since we don't own this thread we need to uninitialize the dispatcher. Otherwise if the thread is reused by another bit of code 
                    // trying to call into the Concord API than its InitializeThread will fail.
                    // The work items we queued to the work list above will be run on a dedicated thread maintained by the Concord dispatcher.
                    DkmComponentManager.UninitializeThread(_id);
                }
            }
        }
    }
}
