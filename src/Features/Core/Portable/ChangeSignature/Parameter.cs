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
        public AddedParameter(
            ITypeSymbol type,
            string typeName,
            string name,
            string callSiteValue,
            bool isRequired = true,
            string defaultValue = "",
            bool useNamedArguments = false,
            bool isCallsiteOmitted = false,
            bool isCallsiteError = false,
            bool typeBinds = true)
        {
            Type = type;
            TypeBinds = typeBinds;
            TypeName = typeName;
            Name = name;
            CallSiteValue = callSiteValue;

            IsRequired = isRequired;
            DefaultValue = defaultValue;
            IsCallsiteError = isCallsiteError;
            IsCallsiteOmitted = isCallsiteOmitted;
            UseNamedArguments = useNamedArguments;

            if (IsCallsiteError)
            {
                CallSiteValue = FeaturesResources.ChangeSignature_NewParameterIntroduceTODOVariable;
            }
            else if (isCallsiteOmitted)
            {
                CallSiteValue = FeaturesResources.ChangeSignature_NewParameterOmitValue;
            }
            else
            {
                CallSiteValue = callSiteValue;
            }
        }

        public override string Name { get; }
        public override bool HasExplicitDefaultValue => !string.IsNullOrWhiteSpace(DefaultValue);

        public ITypeSymbol Type { get; }
        public string TypeName { get; }
        public bool TypeBinds { get; }
        public string CallSiteValue { get; }

        public bool IsRequired { get; }
        public string DefaultValue { get; }
        public bool UseNamedArguments { get; }
        public bool IsCallsiteOmitted { get; }
        public bool IsCallsiteError { get; }

        // For test purposes: to display assert failure details in tests.
        public override string ToString() => $"{Type.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters))} {Name} ({CallSiteValue})";
    }
}
