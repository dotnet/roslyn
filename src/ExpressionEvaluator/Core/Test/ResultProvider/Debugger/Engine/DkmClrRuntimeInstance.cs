// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Symbols;
using System;
using System.Linq;
using System.Reflection;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    internal delegate DkmClrValue GetMemberValueDelegate(DkmClrValue value, string memberName);

    internal delegate DkmClrModuleInstance GetModuleDelegate(DkmClrRuntimeInstance runtime, Assembly assembly);

    public class DkmClrRuntimeInstance : DkmRuntimeInstance
    {
        internal static readonly DkmClrRuntimeInstance DefaultRuntime = new DkmClrRuntimeInstance(new Assembly[0]);

        internal readonly Assembly[] Assemblies;
        internal readonly DkmClrModuleInstance[] Modules;
        private readonly DkmClrModuleInstance _defaultModule;
        private readonly DkmClrAppDomain _appDomain; // exactly one for now
        internal readonly GetMemberValueDelegate GetMemberValue;

        internal DkmClrRuntimeInstance(
            Assembly[] assemblies,
            GetModuleDelegate getModule = null,
            GetMemberValueDelegate getMemberValue = null,
            bool enableNativeDebugging = false)
            : base(enableNativeDebugging)
        {
            if (getModule == null)
            {
                getModule = (r, a) => new DkmClrModuleInstance(r, a, (a != null) ? new DkmModule(a.GetName().Name + ".dll") : null);
            }
            this.Assemblies = assemblies;
            this.Modules = assemblies.Select(a => getModule(this, a)).Where(m => m != null).ToArray();
            _defaultModule = getModule(this, null);
            _appDomain = new DkmClrAppDomain(this);
            this.GetMemberValue = getMemberValue;
        }

        internal DkmClrModuleInstance DefaultModule
        {
            get { return _defaultModule; }
        }

        internal DkmClrAppDomain DefaultAppDomain
        {
            get { return _appDomain; }
        }

        internal DkmClrType GetType(Type type)
        {
            var assembly = ((AssemblyImpl)type.Assembly).Assembly;
            var module = this.Modules.FirstOrDefault(m => m.Assembly == assembly) ?? _defaultModule;
            return new DkmClrType(module, _appDomain, type);
        }

        internal DkmClrType GetType(System.Type type)
        {
            var assembly = type.Assembly;
            var module = this.Modules.First(m => m.Assembly == assembly);
            return new DkmClrType(module, _appDomain, (TypeImpl)type);
        }

        internal DkmClrType GetType(string typeName)
        {
            foreach (var module in this.Modules)
            {
                var assembly = module.Assembly;
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return new DkmClrType(module, _appDomain, (TypeImpl)type);
                }
            }
            return null;
        }

        internal DkmClrModuleInstance FindClrModuleInstance(Guid mvid)
        {
            return this.Modules.FirstOrDefault(m => m.Mvid == mvid) ?? _defaultModule;
        }
    }
}
