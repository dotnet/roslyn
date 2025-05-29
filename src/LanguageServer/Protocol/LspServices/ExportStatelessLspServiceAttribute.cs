// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Defines an attribute to export an instance of <see cref="ILspService"/> that is re-used across
/// all server instances in the same mef container.  Services using this export attribute should not
/// store any kind of server specific state in them.
/// 
/// MEF will dispose of these services when the container is disposed of.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false), MetadataAttribute]
internal class ExportStatelessLspServiceAttribute(
    Type serviceType, string contractName, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any)
    : AbstractExportLspServiceAttribute(
        serviceType, contractName, contractType: typeof(ILspService), isStateless: true, serverKind);
