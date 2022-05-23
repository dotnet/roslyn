// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false), MetadataAttribute]
internal class ExportCSharpVisualBasicStatelessLspServiceAttribute : ExportStatelessLspServiceAttribute
{
    public ExportCSharpVisualBasicStatelessLspServiceAttribute(Type type, WellKnownLspServerKinds serverKind = WellKnownLspServerKinds.Any) : base(type, ProtocolConstants.RoslynLspLanguagesContract, serverKind)
    {
    }
}
