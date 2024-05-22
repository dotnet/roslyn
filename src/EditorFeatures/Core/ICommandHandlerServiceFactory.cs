// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Editor;

// This is defined only to allow TypeScript to still import it and pass it to the VenusCommandHandler constructor.
// The commit that is is introducing this type can be reverted once TypeScript has moved off of the use.
[Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
internal interface ICommandHandlerServiceFactory
{
}
