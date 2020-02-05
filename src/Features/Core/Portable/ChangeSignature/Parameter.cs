// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal abstract class Parameter
    {
        public abstract bool HasExplicitDefaultValue { get; }
        public abstract string Name { get; }
    }

    internal sealed class ExistingParameter : Parameter
    {
        public IParameterSymbol Symbol { get; }

        public ExistingParameter(IParameterSymbol param)
        {
            Symbol = param;
        }

        public override bool HasExplicitDefaultValue => Symbol.HasExplicitDefaultValue;
        public override string Name => Symbol.Name;
    }

    internal sealed class AddedParameter : Parameter
    {
        public AddedParameter(ITypeSymbol type, string typeNameDisplayWithErrorIndicator, string parameter, string callSiteValue)
        {
            Type = type;
            TypeNameDisplayWithErrorIndicator = typeNameDisplayWithErrorIndicator;
            ParameterName = parameter;
            CallSiteValue = callSiteValue;
        }

        public ITypeSymbol Type { get; set; }
        public string ParameterName { get; set; }
        public string CallSiteValue { get; set; }

        public override bool HasExplicitDefaultValue => false;
        public override string Name => ParameterName;

        public string TypeNameDisplayWithErrorIndicator { get; set; }

        // For test purposes: to display assert failure details in tests.
        public override string ToString() => $"{Type.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters))} {Name} ({CallSiteValue})";
    }
}
