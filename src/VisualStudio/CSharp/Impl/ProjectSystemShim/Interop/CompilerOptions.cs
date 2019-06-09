// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
{
    internal enum CompilerOptions
    {
        OPTID_WARNINGLEVEL = 1,
        OPTID_WARNINGSAREERRORS,
        OPTID_CCSYMBOLS,
        OPTID_NOSTDLIB,
        OPTID_EMITDEBUGINFO,
        OPTID_OPTIMIZATIONS,
        OPTID_IMPORTS,
        OPTID_INTERNALTESTS = 8,

        OPTID_MODULES = 18,

        OPTID_NOWARNLIST = 20,

        OPTID_XML_DOCFILE = 24,
        OPTID_CHECKED,
        OPTID_UNSAFE,
        OPTID_DEBUGTYPE,
        OPTID_LIBPATH,
        OPTID_DELAYSIGN,
        OPTID_KEYFILE,
        OPTID_KEYNAME,
        OPTID_COMPATIBILITY,
        OPTID_WARNASERRORLIST,
        OPTID_WATSONMODE,
        OPTID_PDBALTPATH,
        OPTID_SOURCEMAP,
        OPTID_PLATFORM,
        OPTID_MODULEASSEMBLY,
        OPTID_MANIFESTFILE,

        OPTID_IMPORTSUSINGNOPIA = 41,
        OPTID_FUSIONCONFIG,
        OPTID_HIGHENTROPYASLR,
        OPTID_SUBSYSTEMVERSION,

        OPTID_WARNNOTASERRORLIST,

        LARGEST_OPTION_ID
    }
}
