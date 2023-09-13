// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer;

[DataContract]
internal record NamedPipeInformation(
    [property: DataMember(Name = "pipeName")] string PipeName);
