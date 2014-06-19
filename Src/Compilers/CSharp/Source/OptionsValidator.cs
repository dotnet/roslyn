using System;
using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    internal static class OptionsValidator
    {
        internal static bool IsValidFullName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            char lastChar = '.';
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '.')
                {
                    if (lastChar == '.')
                    {
                        return false;
                    }
                }
                else if (!(lastChar == '.' ? CharacterInfo.IsIdentifierStartCharacter(name[i]) : CharacterInfo.IsIdentifierPartCharacter(name[i])))
                {
                    return false;
                }

                lastChar = name[i];
            }

            return lastChar != '.';
        }

        internal static bool IsValidClrTypeName(string name)
        {
            return !String.IsNullOrEmpty(name) && name.IndexOf('\0') == -1;
        }
    }
}