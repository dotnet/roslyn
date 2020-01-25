﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class Process : IDisposable
    {
        private readonly bool _shouldEnable;
        private readonly List<Module> _modules;
        private int _shouldEnableRequests;

        internal Process(params Module[] modules) : this(true, modules)
        {
        }

        internal Process(bool shouldEnable, params Module[] modules)
        {
            _shouldEnable = shouldEnable;
            _modules = new List<Module>(modules);
        }

        internal int ShouldEnableRequests => _shouldEnableRequests;

        internal bool ShouldEnableFunctionResolver()
        {
            _shouldEnableRequests++;
            return _shouldEnable;
        }

        internal void AddModule(Module module)
        {
            _modules.Add(module);
        }

        internal Module[] GetModules()
        {
            return _modules.ToArray();
        }

        void IDisposable.Dispose()
        {
            foreach (var module in _modules)
            {
                ((IDisposable)module).Dispose();
            }
        }
    }
}
