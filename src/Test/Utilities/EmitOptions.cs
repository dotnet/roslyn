// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public enum TestEmitters
    {
        All,

        None,

        /// <summary>
        /// Use Ref.Emit only.
        /// </summary>
        RefEmit,

        /// <summary>
        /// Ref.Emit doesn't support this and we have no work around.
        /// </summary>
        RefEmitUnsupported,

        /// <summary>
        /// Bug in ReflectionEmitter.
        /// </summary>
        RefEmitBug,

        CCI = RefEmitBug,

        // Reported bugs in Reflection.Emit:
        RefEmitUnsupported_640494 = RefEmitUnsupported, // Reflection.Emit fails to emit custom attribute with enum parameter
        RefEmitUnsupported_646007 = RefEmitUnsupported, // CustomAttributeBuilder throws if the attribute constructor is private
        RefEmitUnsupported_646014 = RefEmitUnsupported, // FieldBuilder.SetConstant fails on a field whose type is a type builder instantiatiation
        RefEmitUnsupported_646021 = RefEmitUnsupported, // TypeBuilder.CreateType fails if the type contains a static method marked with PreserveSig flag.
        RefEmitUnsupported_646023 = RefEmitUnsupported, // Custom modifiers not supported on element type of array type
        RefEmitBug_646048 = RefEmitBug,         // ParameterBuilder.SetConstant throws if the type of the value doesn't match the type of the parameter
        RefEmitUnsupported_646042 = RefEmitUnsupported, // Types with certain dependencies are not possible to emit
    }
}
