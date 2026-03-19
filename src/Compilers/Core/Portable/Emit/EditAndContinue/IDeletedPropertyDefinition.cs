// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue;

internal interface IDeletedPropertyDefinition : Cci.IPropertyDefinition
{
    public PropertyDefinitionHandle MetadataHandle { get; }
}
