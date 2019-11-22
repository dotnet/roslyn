// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ObjectExtensions
    {
        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (obj is TDerivedType5)
            {
                return matchFunc5((TDerivedType5)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
            where TDerivedType6 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (obj is TDerivedType5)
            {
                return matchFunc5((TDerivedType5)obj);
            }
            else if (obj is TDerivedType6)
            {
                return matchFunc6((TDerivedType6)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
            where TDerivedType6 : TBaseType
            where TDerivedType7 : TBaseType
            where TDerivedType8 : TBaseType
            where TDerivedType9 : TBaseType
            where TDerivedType10 : TBaseType
            where TDerivedType11 : TBaseType
            where TDerivedType12 : TBaseType
            where TDerivedType13 : TBaseType
            where TDerivedType14 : TBaseType
            where TDerivedType15 : TBaseType
            where TDerivedType16 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (obj is TDerivedType5)
            {
                return matchFunc5((TDerivedType5)obj);
            }
            else if (obj is TDerivedType6)
            {
                return matchFunc6((TDerivedType6)obj);
            }
            else if (obj is TDerivedType7)
            {
                return matchFunc7((TDerivedType7)obj);
            }
            else if (obj is TDerivedType8)
            {
                return matchFunc8((TDerivedType8)obj);
            }
            else if (obj is TDerivedType9)
            {
                return matchFunc9((TDerivedType9)obj);
            }
            else if (obj is TDerivedType10)
            {
                return matchFunc10((TDerivedType10)obj);
            }
            else if (obj is TDerivedType11)
            {
                return matchFunc11((TDerivedType11)obj);
            }
            else if (obj is TDerivedType12)
            {
                return matchFunc12((TDerivedType12)obj);
            }
            else if (obj is TDerivedType13)
            {
                return matchFunc13((TDerivedType13)obj);
            }
            else if (obj is TDerivedType14)
            {
                return matchFunc14((TDerivedType14)obj);
            }
            else if (obj is TDerivedType15)
            {
                return matchFunc15((TDerivedType15)obj);
            }
            else if (obj is TDerivedType16)
            {
                return matchFunc16((TDerivedType16)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
            where TDerivedType6 : TBaseType
            where TDerivedType7 : TBaseType
            where TDerivedType8 : TBaseType
            where TDerivedType9 : TBaseType
            where TDerivedType10 : TBaseType
            where TDerivedType11 : TBaseType
            where TDerivedType12 : TBaseType
            where TDerivedType13 : TBaseType
            where TDerivedType14 : TBaseType
            where TDerivedType15 : TBaseType
            where TDerivedType16 : TBaseType
            where TDerivedType17 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (obj is TDerivedType5)
            {
                return matchFunc5((TDerivedType5)obj);
            }
            else if (obj is TDerivedType6)
            {
                return matchFunc6((TDerivedType6)obj);
            }
            else if (obj is TDerivedType7)
            {
                return matchFunc7((TDerivedType7)obj);
            }
            else if (obj is TDerivedType8)
            {
                return matchFunc8((TDerivedType8)obj);
            }
            else if (obj is TDerivedType9)
            {
                return matchFunc9((TDerivedType9)obj);
            }
            else if (obj is TDerivedType10)
            {
                return matchFunc10((TDerivedType10)obj);
            }
            else if (obj is TDerivedType11)
            {
                return matchFunc11((TDerivedType11)obj);
            }
            else if (obj is TDerivedType12)
            {
                return matchFunc12((TDerivedType12)obj);
            }
            else if (obj is TDerivedType13)
            {
                return matchFunc13((TDerivedType13)obj);
            }
            else if (obj is TDerivedType14)
            {
                return matchFunc14((TDerivedType14)obj);
            }
            else if (obj is TDerivedType15)
            {
                return matchFunc15((TDerivedType15)obj);
            }
            else if (obj is TDerivedType16)
            {
                return matchFunc16((TDerivedType16)obj);
            }
            else if (obj is TDerivedType17)
            {
                return matchFunc17((TDerivedType17)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TDerivedType18, TDerivedType19, TDerivedType20, TDerivedType21, TDerivedType22, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TDerivedType18, TResult> matchFunc18, Func<TDerivedType19, TResult> matchFunc19, Func<TDerivedType20, TResult> matchFunc20, Func<TDerivedType21, TResult> matchFunc21, Func<TDerivedType22, TResult> matchFunc22, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
            where TDerivedType6 : TBaseType
            where TDerivedType7 : TBaseType
            where TDerivedType8 : TBaseType
            where TDerivedType9 : TBaseType
            where TDerivedType10 : TBaseType
            where TDerivedType11 : TBaseType
            where TDerivedType12 : TBaseType
            where TDerivedType13 : TBaseType
            where TDerivedType14 : TBaseType
            where TDerivedType15 : TBaseType
            where TDerivedType16 : TBaseType
            where TDerivedType17 : TBaseType
            where TDerivedType18 : TBaseType
            where TDerivedType19 : TBaseType
            where TDerivedType20 : TBaseType
            where TDerivedType21 : TBaseType
            where TDerivedType22 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (obj is TDerivedType5)
            {
                return matchFunc5((TDerivedType5)obj);
            }
            else if (obj is TDerivedType6)
            {
                return matchFunc6((TDerivedType6)obj);
            }
            else if (obj is TDerivedType7)
            {
                return matchFunc7((TDerivedType7)obj);
            }
            else if (obj is TDerivedType8)
            {
                return matchFunc8((TDerivedType8)obj);
            }
            else if (obj is TDerivedType9)
            {
                return matchFunc9((TDerivedType9)obj);
            }
            else if (obj is TDerivedType10)
            {
                return matchFunc10((TDerivedType10)obj);
            }
            else if (obj is TDerivedType11)
            {
                return matchFunc11((TDerivedType11)obj);
            }
            else if (obj is TDerivedType12)
            {
                return matchFunc12((TDerivedType12)obj);
            }
            else if (obj is TDerivedType13)
            {
                return matchFunc13((TDerivedType13)obj);
            }
            else if (obj is TDerivedType14)
            {
                return matchFunc14((TDerivedType14)obj);
            }
            else if (obj is TDerivedType15)
            {
                return matchFunc15((TDerivedType15)obj);
            }
            else if (obj is TDerivedType16)
            {
                return matchFunc16((TDerivedType16)obj);
            }
            else if (obj is TDerivedType17)
            {
                return matchFunc17((TDerivedType17)obj);
            }
            else if (obj is TDerivedType18)
            {
                return matchFunc18((TDerivedType18)obj);
            }
            else if (obj is TDerivedType19)
            {
                return matchFunc19((TDerivedType19)obj);
            }
            else if (obj is TDerivedType20)
            {
                return matchFunc20((TDerivedType20)obj);
            }
            else if (obj is TDerivedType21)
            {
                return matchFunc21((TDerivedType21)obj);
            }
            else if (obj is TDerivedType22)
            {
                return matchFunc22((TDerivedType22)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }

        [return: MaybeNull]
        public static TResult TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TDerivedType18, TDerivedType19, TDerivedType20, TDerivedType21, TDerivedType22, TDerivedType23, TDerivedType24, TDerivedType25, TDerivedType26, TDerivedType27, TDerivedType28, TDerivedType29, TDerivedType30, TDerivedType31, TDerivedType32, TDerivedType33, TDerivedType34, TDerivedType35, TDerivedType36, TDerivedType37, TDerivedType38, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TDerivedType18, TResult> matchFunc18, Func<TDerivedType19, TResult> matchFunc19, Func<TDerivedType20, TResult> matchFunc20, Func<TDerivedType21, TResult> matchFunc21, Func<TDerivedType22, TResult> matchFunc22, Func<TDerivedType23, TResult> matchFunc23, Func<TDerivedType24, TResult> matchFunc24, Func<TDerivedType25, TResult> matchFunc25, Func<TDerivedType26, TResult> matchFunc26, Func<TDerivedType27, TResult> matchFunc27, Func<TDerivedType28, TResult> matchFunc28, Func<TDerivedType29, TResult> matchFunc29, Func<TDerivedType30, TResult> matchFunc30, Func<TDerivedType31, TResult> matchFunc31, Func<TDerivedType32, TResult> matchFunc32, Func<TDerivedType33, TResult> matchFunc33, Func<TDerivedType34, TResult> matchFunc34, Func<TDerivedType35, TResult> matchFunc35, Func<TDerivedType36, TResult> matchFunc36, Func<TDerivedType37, TResult> matchFunc37, Func<TDerivedType38, TResult> matchFunc38, Func<TBaseType, TResult>? defaultFunc = null)
            where TDerivedType1 : TBaseType
            where TDerivedType2 : TBaseType
            where TDerivedType3 : TBaseType
            where TDerivedType4 : TBaseType
            where TDerivedType5 : TBaseType
            where TDerivedType6 : TBaseType
            where TDerivedType7 : TBaseType
            where TDerivedType8 : TBaseType
            where TDerivedType9 : TBaseType
            where TDerivedType10 : TBaseType
            where TDerivedType11 : TBaseType
            where TDerivedType12 : TBaseType
            where TDerivedType13 : TBaseType
            where TDerivedType14 : TBaseType
            where TDerivedType15 : TBaseType
            where TDerivedType16 : TBaseType
            where TDerivedType17 : TBaseType
            where TDerivedType18 : TBaseType
            where TDerivedType19 : TBaseType
            where TDerivedType20 : TBaseType
            where TDerivedType21 : TBaseType
            where TDerivedType22 : TBaseType
            where TDerivedType23 : TBaseType
            where TDerivedType24 : TBaseType
            where TDerivedType25 : TBaseType
            where TDerivedType26 : TBaseType
            where TDerivedType27 : TBaseType
            where TDerivedType28 : TBaseType
            where TDerivedType29 : TBaseType
            where TDerivedType30 : TBaseType
            where TDerivedType31 : TBaseType
            where TDerivedType32 : TBaseType
            where TDerivedType33 : TBaseType
            where TDerivedType34 : TBaseType
            where TDerivedType35 : TBaseType
            where TDerivedType36 : TBaseType
            where TDerivedType37 : TBaseType
            where TDerivedType38 : TBaseType
        {
            if (obj is TDerivedType1)
            {
                return matchFunc1((TDerivedType1)obj);
            }
            else if (obj is TDerivedType2)
            {
                return matchFunc2((TDerivedType2)obj);
            }
            else if (obj is TDerivedType3)
            {
                return matchFunc3((TDerivedType3)obj);
            }
            else if (obj is TDerivedType4)
            {
                return matchFunc4((TDerivedType4)obj);
            }
            else if (obj is TDerivedType5)
            {
                return matchFunc5((TDerivedType5)obj);
            }
            else if (obj is TDerivedType6)
            {
                return matchFunc6((TDerivedType6)obj);
            }
            else if (obj is TDerivedType7)
            {
                return matchFunc7((TDerivedType7)obj);
            }
            else if (obj is TDerivedType8)
            {
                return matchFunc8((TDerivedType8)obj);
            }
            else if (obj is TDerivedType9)
            {
                return matchFunc9((TDerivedType9)obj);
            }
            else if (obj is TDerivedType10)
            {
                return matchFunc10((TDerivedType10)obj);
            }
            else if (obj is TDerivedType11)
            {
                return matchFunc11((TDerivedType11)obj);
            }
            else if (obj is TDerivedType12)
            {
                return matchFunc12((TDerivedType12)obj);
            }
            else if (obj is TDerivedType13)
            {
                return matchFunc13((TDerivedType13)obj);
            }
            else if (obj is TDerivedType14)
            {
                return matchFunc14((TDerivedType14)obj);
            }
            else if (obj is TDerivedType15)
            {
                return matchFunc15((TDerivedType15)obj);
            }
            else if (obj is TDerivedType16)
            {
                return matchFunc16((TDerivedType16)obj);
            }
            else if (obj is TDerivedType17)
            {
                return matchFunc17((TDerivedType17)obj);
            }
            else if (obj is TDerivedType18)
            {
                return matchFunc18((TDerivedType18)obj);
            }
            else if (obj is TDerivedType19)
            {
                return matchFunc19((TDerivedType19)obj);
            }
            else if (obj is TDerivedType20)
            {
                return matchFunc20((TDerivedType20)obj);
            }
            else if (obj is TDerivedType21)
            {
                return matchFunc21((TDerivedType21)obj);
            }
            else if (obj is TDerivedType22)
            {
                return matchFunc22((TDerivedType22)obj);
            }
            else if (obj is TDerivedType23)
            {
                return matchFunc23((TDerivedType23)obj);
            }
            else if (obj is TDerivedType24)
            {
                return matchFunc24((TDerivedType24)obj);
            }
            else if (obj is TDerivedType25)
            {
                return matchFunc25((TDerivedType25)obj);
            }
            else if (obj is TDerivedType26)
            {
                return matchFunc26((TDerivedType26)obj);
            }
            else if (obj is TDerivedType27)
            {
                return matchFunc27((TDerivedType27)obj);
            }
            else if (obj is TDerivedType28)
            {
                return matchFunc28((TDerivedType28)obj);
            }
            else if (obj is TDerivedType29)
            {
                return matchFunc29((TDerivedType29)obj);
            }
            else if (obj is TDerivedType30)
            {
                return matchFunc30((TDerivedType30)obj);
            }
            else if (obj is TDerivedType31)
            {
                return matchFunc31((TDerivedType31)obj);
            }
            else if (obj is TDerivedType32)
            {
                return matchFunc32((TDerivedType32)obj);
            }
            else if (obj is TDerivedType33)
            {
                return matchFunc33((TDerivedType33)obj);
            }
            else if (obj is TDerivedType34)
            {
                return matchFunc34((TDerivedType34)obj);
            }
            else if (obj is TDerivedType35)
            {
                return matchFunc35((TDerivedType35)obj);
            }
            else if (obj is TDerivedType36)
            {
                return matchFunc36((TDerivedType36)obj);
            }
            else if (obj is TDerivedType37)
            {
                return matchFunc37((TDerivedType37)obj);
            }
            else if (obj is TDerivedType38)
            {
                return matchFunc38((TDerivedType38)obj);
            }
            else if (defaultFunc != null)
            {
                return defaultFunc(obj);
            }
            else
            {
                return default!;
            }
        }
    }
}
