// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.DocumentationComments;

[DataContract]
internal readonly record struct DocumentationCommentOptions(
    [property: DataMember(Order = 0)] bool AutoXmlDocCommentGeneration,
    [property: DataMember(Order = 1)] int TabSize,
    [property: DataMember(Order = 2)] bool UseTabs,
    [property: DataMember(Order = 3)] string NewLine);
