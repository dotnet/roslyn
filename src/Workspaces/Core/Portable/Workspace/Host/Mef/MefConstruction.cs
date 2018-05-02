// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal static class MefConstruction
    {
        internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
        internal const string FactoryMethodMessage = "This factory method only provides services for the MEF export provider.";
    }
}
