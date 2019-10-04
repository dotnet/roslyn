// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(SolutionUserOptionsProvider))]
    internal sealed class SolutionUserOptionsProvider : IVsSolutionEvents, IDisposable
    {
        private readonly ConcurrentDictionary<string, bool> _optionsMap = new ConcurrentDictionary<string, bool>();
        private IVsSolution? _vsSolution;
        private uint _solutionEventsCookie;

        public void Initialize(IVsSolution vsSolution)
        {
            _vsSolution = vsSolution;
            _vsSolution?.AdviseSolutionEvents(this, out _solutionEventsCookie);
        }

        public bool? GetOption(string key)
            => _optionsMap.TryGetValue(key, out var value) ? value : (bool?)null;

        public bool SetOption(string key, bool value)
            => _optionsMap[key] = value;

        public void OnLoadOption(string key, Stream stream)
        {
            if (SolutionUserOptionNames.AllOptionNames.Contains(key))
            {
                var value = stream.ReadByte() == 1;
                _optionsMap[key] = value;
            }
        }

        public void OnSaveOption(string key, Stream stream)
        {
            if (_optionsMap.TryGetValue(key, out var value))
            {
                stream.WriteByte((byte)(value ? 1 : 0));
            }
        }

        public void Dispose()
        {
            _vsSolution?.UnadviseSolutionEvents(_solutionEventsCookie);
        }

        #region IVsSolutionEvents implementation
        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            _optionsMap.Clear();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.E_NOTIMPL;
        }
        #endregion
    }
}
