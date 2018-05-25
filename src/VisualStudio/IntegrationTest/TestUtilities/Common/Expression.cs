// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
