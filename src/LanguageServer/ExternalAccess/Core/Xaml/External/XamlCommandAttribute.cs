// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Xaml;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;
#endif

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class XamlCommandAttribute(string command) : CommandAttribute(command);
