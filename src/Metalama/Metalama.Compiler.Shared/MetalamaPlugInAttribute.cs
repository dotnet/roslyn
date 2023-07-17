// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Metalama.Compiler;

/// <summary>
/// Custom attribute that, when applied to a class, means that an instance
/// of this class must be created and exposed to the <see cref="TransformerContext.Plugins"/> property.
/// This instance is then available in Metalama as a service, and exposed to <see cref="IServiceProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MetalamaPlugInAttribute : Attribute { }
