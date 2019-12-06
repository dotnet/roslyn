// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor
{
    // This is defined only to allow TypeScript to still import it and pass it to the VenusCommandHandler constructor.
    // The commit that is is introducing this type can be reverted once TypeScript has moved off of the use.
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal interface ICommandHandlerServiceFactory
    {
    }
}
