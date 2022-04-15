// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.EncapsulateField;

[DataContract]
internal readonly record struct EncapsulateFieldOptions(
    [property: DataMember(Order = 0)] SimplifierOptions SimplifierOptions);
