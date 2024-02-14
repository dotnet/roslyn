// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class XamlCommandAttribute : CommandAttribute
{
    public XamlCommandAttribute(string command) : base(command)
    {
    }
}
