// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    /// <summary>
    /// Map vsCMPrototype flags to sane names
    /// </summary>
    internal enum PrototypeFlags
    {
        Signature = EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature,
        FullName = EnvDTE.vsCMPrototype.vsCMPrototypeFullname,
        NoName = EnvDTE.vsCMPrototype.vsCMPrototypeNoName,
        TypeName = EnvDTE.vsCMPrototype.vsCMPrototypeClassName,
        BaseName = 0,
        NameMask = (FullName | NoName | TypeName | BaseName),
        ParameterTypes = EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes,
        ParameterNames = EnvDTE.vsCMPrototype.vsCMPrototypeParamNames,
        ParameterDefaultValues = EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues,
        ParametersMask = (ParameterTypes | ParameterNames | ParameterDefaultValues),
        Type = EnvDTE.vsCMPrototype.vsCMPrototypeType,
        Initializer = EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression
    }
}
