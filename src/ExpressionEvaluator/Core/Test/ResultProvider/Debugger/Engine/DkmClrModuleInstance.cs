// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Symbols;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    public class DkmClrModuleInstance : DkmModuleInstance
    {
        internal readonly Assembly Assembly;
        private readonly DkmClrRuntimeInstance _runtimeInstance;
        private int _resolveTypeNameFailures;

        public DkmClrModuleInstance(DkmClrRuntimeInstance runtimeInstance, Assembly assembly, DkmModule module) :
            base(module)
        {
            _runtimeInstance = runtimeInstance;
            this.Assembly = assembly;
        }

        public Guid Mvid
        {
            get { return this.Assembly.Modules.First().ModuleVersionId; }
        }

        public DkmClrRuntimeInstance RuntimeInstance
        {
            get { return _runtimeInstance; }
        }

        public DkmClrType ResolveTypeName(string typeName, ReadOnlyCollection<DkmClrType> typeArguments)
        {
            var type = this.Assembly.GetType(typeName);
            if (type == null)
            {
                Interlocked.Increment(ref _resolveTypeNameFailures);
                throw new ArgumentException();
            }
            Debug.Assert(typeArguments.Count == type.GetGenericArguments().Length);
            if (typeArguments.Count > 0)
            {
                var typeArgs = typeArguments.Select(t => ((TypeImpl)t.GetLmrType()).Type).ToArray();
                type = type.MakeGenericType(typeArgs);
            }
            return _runtimeInstance.GetType((TypeImpl)type);
        }

        internal int ResolveTypeNameFailures
        {
            get { return _resolveTypeNameFailures; }
        }
    }
}
