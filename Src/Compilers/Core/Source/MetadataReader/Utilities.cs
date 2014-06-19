//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.MetadataReader
{
    internal static class Utilities
    {
        public const char DotDelimeter = '.';
        public const char GenericTypeNameManglingChar = '`';
        public const char OldGenericTypeNameManglingChar = '!';
        public const int MaxStringLengthForParamSize = 22;
        public const int MaxStringLengthForIntToStringConversion = 22;

        // File: CompilerUtilities.cpp
        // Lines: 2637 - 2637
        // STRING* .::GetActualTypeNameFromEmittedTypeName( [ _In_z_ STRING* EmittedTypeName ] [ int GenericParamCount ] [ Compiler* CompilerInstance ] [ unsigned* ActualGenericParamCount ] )
        internal static string GetActualTypeNameFromEmittedTypeName(
            string emittedTypeName,
            int genericParamCount,
            out int actualGenericParamCount)
        {
            Contract.Requires(emittedTypeName != null, "NULL actual name unexpected!!!");
            Contract.Requires(genericParamCount >= -1);

            actualGenericParamCount = 0;

            if (genericParamCount == 0)
            {
                return emittedTypeName;
            }

            int emittedTypeNameLength = emittedTypeName.Length;

            int indexOfManglingChar;
            for (indexOfManglingChar = emittedTypeNameLength; indexOfManglingChar >= 1; indexOfManglingChar--)
            {
                if (emittedTypeName[indexOfManglingChar - 1] == GenericTypeNameManglingChar ||
                    emittedTypeName[indexOfManglingChar - 1] == OldGenericTypeNameManglingChar)
                {
                    break;
                }
            }

            if (indexOfManglingChar == 0 ||
               (emittedTypeNameLength - indexOfManglingChar) == 0 ||
               emittedTypeNameLength - indexOfManglingChar > MaxStringLengthForParamSize)
            {
                return emittedTypeName;
            }

            string stringRepresentingParamcount = emittedTypeName.Substring(indexOfManglingChar);

            // Given a name corresponding to <ActualName>!<Number Of Type Parameters>,
            // unmanagle the name to be <ActualName>
            int genericParamCountInName;
            bool nonNumericCharFound = !int.TryParse(stringRepresentingParamcount, out genericParamCountInName);

            if (nonNumericCharFound)
            {
                return emittedTypeName;
            }

            actualGenericParamCount = genericParamCountInName;

            // -1 indicates match against any arity.
            if (genericParamCount >= 0 &&
               genericParamCount != genericParamCountInName)
            {
                return emittedTypeName;
            }

            return emittedTypeName.Substring(0, indexOfManglingChar - 1);
        }

        // File: F:\Dev11\VB\Conversion\Native\VB\Language\Compiler\CompilerUtilities.cpp
        // Lines: 2552 - 2552
        // STRING* .::GetEmittedTypeNameFromActualTypeName( [ _In_z_ STRING* ActualTypeName ] [ BCSYM_GenericParam* ListOfGenericParamsForThisType ] [ Compiler* CompilerInstance ] )
        internal static string GetEmittedTypeNameFromActualTypeName(
            string actualTypeName,
            int arity)
        {
            Contract.ThrowIfNull(actualTypeName, "NULL actual name unexpected!!!");
            Contract.ThrowIfFalse(arity >= 0);

            if (arity == 0)
            {
                return actualTypeName;
            }

            // Generate a name corresponding to the following :
            //     <ActualName>`<Number of Type Parameters>
            return actualTypeName + GenericTypeNameManglingChar + arity.ToString();
        }

        // File: D:\dd\vs_langs01\src\Conversion\Native\VB\Language\Compiler\Compiler.cpp
        // Lines: 2696 - 2696
        // .Compiler::SplitQualifiedName( [ _In_z_ STRING* pstrName ] [ unsigned cNames ] [ _Out_cap_(cNames)STRING** rgpstrNames ] )
        internal static string[] SplitQualifiedName(
            string pstrName)
        {
            Contract.ThrowIfNull(pstrName);

            return pstrName.Split(DotDelimeter);
        }
    }
}