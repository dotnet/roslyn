﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Commanding
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    [Export(typeof(ICommandHandlerServiceFactory))]
    internal sealed class LegacyCommandHandlerServiceFactory : ICommandHandlerServiceFactory
    {
        [ImportingConstructor]
        public LegacyCommandHandlerServiceFactory()
        {
        }
    }
}
