// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class CoolParameter
    {

    }

    internal class ExistingParameter : CoolParameter
    {
        public ExistingParameter(IParameterSymbol param)
        {
            Symbol = param;
        }

        public IParameterSymbol Symbol { get; set; }
    }

    internal class AddedParameter : CoolParameter
    {
        public AddedParameter(string type, string parameter, string callsite)
        {
            TypeName = type;
            ParameterName = parameter;
            CallsiteValue = callsite;
        }

        public string TypeName { get; set; }
        public string ParameterName { get; set; }
        public string CallsiteValue { get; set; }
    }
}
