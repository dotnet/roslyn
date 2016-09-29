// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    public class DkmClrAppDomain
    {
        private readonly DkmClrRuntimeInstance _runtime;

        internal DkmClrAppDomain(DkmClrRuntimeInstance runtime)
        {
            _runtime = runtime;
        }

        public DkmClrRuntimeInstance RuntimeInstance
        {
            get { return _runtime; }
        }

        public DkmClrModuleInstance FindClrModuleInstance(Guid mvid)
        {
            return _runtime.FindClrModuleInstance(mvid);
        }

        public DkmClrModuleInstance[] GetClrModuleInstances()
        {
            return _runtime.Modules;
        }
    }
}
