// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class EditorBrowsableHelpers
    {
        /// <summary>
        /// Finds the constructor which takes exactly one argument, which must be of type EditorBrowsableState.
        /// It does not require that the EditorBrowsableAttribute and EditorBrowsableState types be those
        /// shipped by Microsoft, but it does demand the types found follow the expected pattern. If at any
        /// point that pattern appears to be violated, return null to indicate that an appropriate constructor
        /// could not be found.
        /// </summary>
        public static IMethodSymbol GetSpecialEditorBrowsableAttributeConstructor(Compilation compilation)
        {
            var editorBrowsableAttributeType = compilation.EditorBrowsableAttributeType();
            var editorBrowsableStateType = compilation.EditorBrowsableStateType();

            if (editorBrowsableAttributeType == null || editorBrowsableStateType == null)
            {
                return null;
            }

            var candidateConstructors = editorBrowsableAttributeType.Constructors
                                                                    .Where(c => c.Parameters.Length == 1 && Equals(c.Parameters[0].Type, editorBrowsableStateType));

            // Ensure the constructor adheres to the expected EditorBrowsable pattern
            candidateConstructors = candidateConstructors.Where(c => (!c.IsVararg &&
                                                                      !c.Parameters[0].IsRefOrOut() &&
                                                                      !c.Parameters[0].CustomModifiers.Any()));

            // If there are multiple constructors that look correct then the discovered types do not match the
            // expected pattern, so return null.
            if (candidateConstructors.Count() <= 1)
            {
                return candidateConstructors.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        public static List<IMethodSymbol> GetSpecialTypeLibTypeAttributeConstructors(Compilation compilation)
        {
            return GetSpecialTypeLibAttributeConstructorsWorker(
                compilation,
                "System.Runtime.InteropServices.TypeLibTypeAttribute",
                "System.Runtime.InteropServices.TypeLibTypeFlags");
        }

        public static List<IMethodSymbol> GetSpecialTypeLibFuncAttributeConstructors(Compilation compilation)
        {
            return GetSpecialTypeLibAttributeConstructorsWorker(
                compilation,
                "System.Runtime.InteropServices.TypeLibFuncAttribute",
                "System.Runtime.InteropServices.TypeLibFuncFlags");
        }

        public static List<IMethodSymbol> GetSpecialTypeLibVarAttributeConstructors(Compilation compilation)
        {
            return GetSpecialTypeLibAttributeConstructorsWorker(
                compilation,
                "System.Runtime.InteropServices.TypeLibVarAttribute",
                "System.Runtime.InteropServices.TypeLibVarFlags");
        }

        /// <summary>
        /// The TypeLib*Attribute classes that accept TypeLib*Flags with FHidden as an option all have two constructors,
        /// one accepting a TypeLib*Flags and the other a short. This methods gets those two constructor symbols for any
        /// of these attribute classes. It does not require that the either of these types be those shipped by Microsoft,
        /// but it does demand the types found follow the expected pattern. If at any point that pattern appears to be
        /// violated, return an empty enumerable to indicate that no appropriate constructors were found.
        /// </summary>
        private static List<IMethodSymbol> GetSpecialTypeLibAttributeConstructorsWorker(
            Compilation compilation,
            string attributeMetadataName,
            string flagsMetadataName)
        {
            var typeLibAttributeType = compilation.GetTypeByMetadataName(attributeMetadataName);
            var typeLibFlagsType = compilation.GetTypeByMetadataName(flagsMetadataName);
            var shortType = compilation.GetSpecialType(SpecialType.System_Int16);

            if (typeLibAttributeType == null || typeLibFlagsType == null || shortType == null)
            {
                return new List<IMethodSymbol>();
            }

            var candidateConstructors = typeLibAttributeType.Constructors
                                                            .Where(c => c.Parameters.Length == 1 &&
                                                                        (Equals(c.Parameters[0].Type, typeLibFlagsType) || Equals(c.Parameters[0].Type, shortType)));

            candidateConstructors = candidateConstructors.Where(c => (!c.IsVararg &&
                                                                      !c.Parameters[0].IsRefOrOut() &&
                                                                      !c.Parameters[0].CustomModifiers.Any()));

            return candidateConstructors.ToList();
        }
    }
}
