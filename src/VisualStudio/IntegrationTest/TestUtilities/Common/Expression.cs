// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    [Serializable]
    public class Expression
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Value { get; set; }

        public Expression(EnvDTE.Expression input)
        {
            Name = input.Name;
            Type = input.Type;
            Value = input.Value;
        }
    }
}
