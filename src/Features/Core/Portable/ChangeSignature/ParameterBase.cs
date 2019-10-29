// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class ParameterBase
    {
        public abstract bool HasExplicitDefaultValue { get; }
        public abstract string Name { get; }
        public abstract IParameterSymbol Symbol { get; }
    }

    internal class ExistingParameter : ParameterBase
    {
        public override IParameterSymbol Symbol { get; }

        public ExistingParameter(IParameterSymbol param)
        {
            Symbol = param;
        }

        public override bool HasExplicitDefaultValue => Symbol.HasExplicitDefaultValue;
        public override string Name => Symbol.Name;
    }

    internal class AddedParameter : ParameterBase
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

        public override bool HasExplicitDefaultValue => false;
        public override string Name => ParameterName;
        public override IParameterSymbol Symbol => null;
    }
}
