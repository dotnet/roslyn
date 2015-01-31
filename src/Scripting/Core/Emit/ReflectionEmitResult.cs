// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Emit
{
    public class ReflectionEmitResult : EmitResult
    {
        private readonly MethodInfo _entryPoint;
        private readonly bool _isUncollectible;

        internal ReflectionEmitResult(MethodInfo entryPoint, bool success, bool isUncollectible, ImmutableArray<Diagnostic> diagnostics)
            : base(success, diagnostics)
        {
            _entryPoint = entryPoint;
            _isUncollectible = isUncollectible;
        }

        /// <summary>
        /// Gets method information about the entrypoint of the emitted assembly.
        /// </summary>
        public MethodInfo EntryPoint
        {
            get { return _entryPoint; }
        }

        /// <summary>
        /// Indicates whether the emitted assembly can be garbage collected.
        /// </summary>
        public bool IsUncollectible
        {
            get { return _isUncollectible; }
        }
    }
}
