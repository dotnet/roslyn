// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This class is a helper for the CSharpGoToDefinition tests

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public partial class CSharpGoToDefinition
    {
        public class EmbeddedClass
        {
#pragma warning disable IDE0052 // Remove unread private members
            private readonly string _field = EmbeddedClassHelper.HelloWorld;
#pragma warning restore IDE0052 // Remove unread private members
        }
    }
}
