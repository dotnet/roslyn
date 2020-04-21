// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    /// <summary>
    /// Base type for Parameter information, whether the parameter
    /// is preexisting or new.
    /// </summary>
    internal abstract class Parameter
    {
        public abstract bool HasDefaultValue { get; }
        public abstract string Name { get; }
    }

    internal sealed class ExistingParameter : Parameter
    {
        public IParameterSymbol Symbol { get; }

        public ExistingParameter(IParameterSymbol param)
        {
            Symbol = param;
        }

        public override bool HasDefaultValue => Symbol.HasExplicitDefaultValue;
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
            bool isCallsiteTodo = false,
            bool typeBinds = true)
        {
            Type = type;
            TypeBinds = typeBinds;
            TypeName = typeName;
            Name = name;
            CallSiteValue = callSiteValue;

            IsRequired = isRequired;
            DefaultValue = defaultValue;
            IsCallsiteTodo = isCallsiteTodo;
            IsCallsiteOmitted = isCallsiteOmitted;
            UseNamedArguments = useNamedArguments;

            if (IsCallsiteTodo)
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
        public override bool HasDefaultValue => !string.IsNullOrWhiteSpace(DefaultValue);

        public ITypeSymbol Type { get; }
        public string TypeName { get; }
        public bool TypeBinds { get; }
        public string CallSiteValue { get; }

        /// <summary>
        /// True if required, false if optional with a default value.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Value to use in the declaration of an optional parameter.
        /// E.g. the "3" in M(int x = 3);
        /// </summary>
        public string DefaultValue { get; }

        /// <summary>
        /// When introducing an argument, this indicates whether it
        /// should be named even if not required to be named. Often
        /// useful for literal callsite values like "true" or "null".
        /// </summary>
        public bool UseNamedArguments { get; }

        /// <summary>
        /// When an optional parameter is added, passing an argument for
        /// it is not required. This indicates that the corresponding argument 
        /// should be omitted. This often results in subsequent arguments needing
        /// to become named arguments
        /// </summary>
        public bool IsCallsiteOmitted { get; }

        /// <summary>
        /// Indicates whether a "TODO" should be introduced at callsites
        /// to cause errors that the user can then go visit and fix up.
        /// </summary>
        public bool IsCallsiteTodo { get; }

        // For test purposes: to display assert failure details in tests.
        public override string ToString() => $"{Type.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters))} {Name} ({CallSiteValue})";
    }
}
