// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ObjectExtensions
{
    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TBaseType, TResult>? defaultFunc = null)
        where TDerivedType1 : TBaseType
        where TDerivedType2 : TBaseType
        where TDerivedType3 : TBaseType
    {
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TBaseType, TResult>? defaultFunc = null)
        where TDerivedType1 : TBaseType
        where TDerivedType2 : TBaseType
        where TDerivedType3 : TBaseType
        where TDerivedType4 : TBaseType
    {
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TBaseType, TResult>? defaultFunc = null)
        where TDerivedType1 : TBaseType
        where TDerivedType2 : TBaseType
        where TDerivedType3 : TBaseType
        where TDerivedType4 : TBaseType
        where TDerivedType5 : TBaseType
    {
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TBaseType, TResult>? defaultFunc = null)
        where TDerivedType1 : TBaseType
        where TDerivedType2 : TBaseType
        where TDerivedType3 : TBaseType
        where TDerivedType4 : TBaseType
        where TDerivedType5 : TBaseType
        where TDerivedType6 : TBaseType
    {
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            TDerivedType6 d => matchFunc6(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TBaseType, TResult>? defaultFunc = null)
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
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            TDerivedType6 d => matchFunc6(d),
            TDerivedType7 d => matchFunc7(d),
            TDerivedType8 d => matchFunc8(d),
            TDerivedType9 d => matchFunc9(d),
            TDerivedType10 d => matchFunc10(d),
            TDerivedType11 d => matchFunc11(d),
            TDerivedType12 d => matchFunc12(d),
            TDerivedType13 d => matchFunc13(d),
            TDerivedType14 d => matchFunc14(d),
            TDerivedType15 d => matchFunc15(d),
            TDerivedType16 d => matchFunc16(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TBaseType, TResult>? defaultFunc = null)
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
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            TDerivedType6 d => matchFunc6(d),
            TDerivedType7 d => matchFunc7(d),
            TDerivedType8 d => matchFunc8(d),
            TDerivedType9 d => matchFunc9(d),
            TDerivedType10 d => matchFunc10(d),
            TDerivedType11 d => matchFunc11(d),
            TDerivedType12 d => matchFunc12(d),
            TDerivedType13 d => matchFunc13(d),
            TDerivedType14 d => matchFunc14(d),
            TDerivedType15 d => matchFunc15(d),
            TDerivedType16 d => matchFunc16(d),
            TDerivedType17 d => matchFunc17(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TDerivedType18, TDerivedType19, TDerivedType20, TDerivedType21, TDerivedType22, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TDerivedType18, TResult> matchFunc18, Func<TDerivedType19, TResult> matchFunc19, Func<TDerivedType20, TResult> matchFunc20, Func<TDerivedType21, TResult> matchFunc21, Func<TDerivedType22, TResult> matchFunc22, Func<TBaseType, TResult>? defaultFunc = null)
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
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            TDerivedType6 d => matchFunc6(d),
            TDerivedType7 d => matchFunc7(d),
            TDerivedType8 d => matchFunc8(d),
            TDerivedType9 d => matchFunc9(d),
            TDerivedType10 d => matchFunc10(d),
            TDerivedType11 d => matchFunc11(d),
            TDerivedType12 d => matchFunc12(d),
            TDerivedType13 d => matchFunc13(d),
            TDerivedType14 d => matchFunc14(d),
            TDerivedType15 d => matchFunc15(d),
            TDerivedType16 d => matchFunc16(d),
            TDerivedType17 d => matchFunc17(d),
            TDerivedType18 d => matchFunc18(d),
            TDerivedType19 d => matchFunc19(d),
            TDerivedType20 d => matchFunc20(d),
            TDerivedType21 d => matchFunc21(d),
            TDerivedType22 d => matchFunc22(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TDerivedType18, TDerivedType19, TDerivedType20, TDerivedType21, TDerivedType22, TDerivedType23, TDerivedType24, TDerivedType25, TDerivedType26, TDerivedType27, TDerivedType28, TDerivedType29, TDerivedType30, TDerivedType31, TDerivedType32, TDerivedType33, TDerivedType34, TDerivedType35, TDerivedType36, TDerivedType37, TDerivedType38, TDerivedType39, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TDerivedType18, TResult> matchFunc18, Func<TDerivedType19, TResult> matchFunc19, Func<TDerivedType20, TResult> matchFunc20, Func<TDerivedType21, TResult> matchFunc21, Func<TDerivedType22, TResult> matchFunc22, Func<TDerivedType23, TResult> matchFunc23, Func<TDerivedType24, TResult> matchFunc24, Func<TDerivedType25, TResult> matchFunc25, Func<TDerivedType26, TResult> matchFunc26, Func<TDerivedType27, TResult> matchFunc27, Func<TDerivedType28, TResult> matchFunc28, Func<TDerivedType29, TResult> matchFunc29, Func<TDerivedType30, TResult> matchFunc30, Func<TDerivedType31, TResult> matchFunc31, Func<TDerivedType32, TResult> matchFunc32, Func<TDerivedType33, TResult> matchFunc33, Func<TDerivedType34, TResult> matchFunc34, Func<TDerivedType35, TResult> matchFunc35, Func<TDerivedType36, TResult> matchFunc36, Func<TDerivedType37, TResult> matchFunc37, Func<TDerivedType38, TResult> matchFunc38, Func<TDerivedType39, TResult> matchFunc39, Func<TBaseType, TResult>? defaultFunc = null)
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
        where TDerivedType39 : TBaseType
    {
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            TDerivedType6 d => matchFunc6(d),
            TDerivedType7 d => matchFunc7(d),
            TDerivedType8 d => matchFunc8(d),
            TDerivedType9 d => matchFunc9(d),
            TDerivedType10 d => matchFunc10(d),
            TDerivedType11 d => matchFunc11(d),
            TDerivedType12 d => matchFunc12(d),
            TDerivedType13 d => matchFunc13(d),
            TDerivedType14 d => matchFunc14(d),
            TDerivedType15 d => matchFunc15(d),
            TDerivedType16 d => matchFunc16(d),
            TDerivedType17 d => matchFunc17(d),
            TDerivedType18 d => matchFunc18(d),
            TDerivedType19 d => matchFunc19(d),
            TDerivedType20 d => matchFunc20(d),
            TDerivedType21 d => matchFunc21(d),
            TDerivedType22 d => matchFunc22(d),
            TDerivedType23 d => matchFunc23(d),
            TDerivedType24 d => matchFunc24(d),
            TDerivedType25 d => matchFunc25(d),
            TDerivedType26 d => matchFunc26(d),
            TDerivedType27 d => matchFunc27(d),
            TDerivedType28 d => matchFunc28(d),
            TDerivedType29 d => matchFunc29(d),
            TDerivedType30 d => matchFunc30(d),
            TDerivedType31 d => matchFunc31(d),
            TDerivedType32 d => matchFunc32(d),
            TDerivedType33 d => matchFunc33(d),
            TDerivedType34 d => matchFunc34(d),
            TDerivedType35 d => matchFunc35(d),
            TDerivedType36 d => matchFunc36(d),
            TDerivedType37 d => matchFunc37(d),
            TDerivedType38 d => matchFunc38(d),
            TDerivedType39 d => matchFunc39(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }

    public static TResult? TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4, TDerivedType5, TDerivedType6, TDerivedType7, TDerivedType8, TDerivedType9, TDerivedType10, TDerivedType11, TDerivedType12, TDerivedType13, TDerivedType14, TDerivedType15, TDerivedType16, TDerivedType17, TDerivedType18, TDerivedType19, TDerivedType20, TDerivedType21, TDerivedType22, TDerivedType23, TDerivedType24, TDerivedType25, TDerivedType26, TDerivedType27, TDerivedType28, TDerivedType29, TDerivedType30, TDerivedType31, TDerivedType32, TDerivedType33, TDerivedType34, TDerivedType35, TDerivedType36, TDerivedType37, TDerivedType38, TDerivedType39, TDerivedType40, TResult>(this TBaseType obj, Func<TDerivedType1, TResult> matchFunc1, Func<TDerivedType2, TResult> matchFunc2, Func<TDerivedType3, TResult> matchFunc3, Func<TDerivedType4, TResult> matchFunc4, Func<TDerivedType5, TResult> matchFunc5, Func<TDerivedType6, TResult> matchFunc6, Func<TDerivedType7, TResult> matchFunc7, Func<TDerivedType8, TResult> matchFunc8, Func<TDerivedType9, TResult> matchFunc9, Func<TDerivedType10, TResult> matchFunc10, Func<TDerivedType11, TResult> matchFunc11, Func<TDerivedType12, TResult> matchFunc12, Func<TDerivedType13, TResult> matchFunc13, Func<TDerivedType14, TResult> matchFunc14, Func<TDerivedType15, TResult> matchFunc15, Func<TDerivedType16, TResult> matchFunc16, Func<TDerivedType17, TResult> matchFunc17, Func<TDerivedType18, TResult> matchFunc18, Func<TDerivedType19, TResult> matchFunc19, Func<TDerivedType20, TResult> matchFunc20, Func<TDerivedType21, TResult> matchFunc21, Func<TDerivedType22, TResult> matchFunc22, Func<TDerivedType23, TResult> matchFunc23, Func<TDerivedType24, TResult> matchFunc24, Func<TDerivedType25, TResult> matchFunc25, Func<TDerivedType26, TResult> matchFunc26, Func<TDerivedType27, TResult> matchFunc27, Func<TDerivedType28, TResult> matchFunc28, Func<TDerivedType29, TResult> matchFunc29, Func<TDerivedType30, TResult> matchFunc30, Func<TDerivedType31, TResult> matchFunc31, Func<TDerivedType32, TResult> matchFunc32, Func<TDerivedType33, TResult> matchFunc33, Func<TDerivedType34, TResult> matchFunc34, Func<TDerivedType35, TResult> matchFunc35, Func<TDerivedType36, TResult> matchFunc36, Func<TDerivedType37, TResult> matchFunc37, Func<TDerivedType38, TResult> matchFunc38, Func<TDerivedType39, TResult> matchFunc39, Func<TDerivedType40, TResult> matchFunc40, Func<TBaseType, TResult>? defaultFunc = null)
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
        where TDerivedType39 : TBaseType
        where TDerivedType40 : TBaseType
    {
        return obj switch
        {
            TDerivedType1 d => matchFunc1(d),
            TDerivedType2 d => matchFunc2(d),
            TDerivedType3 d => matchFunc3(d),
            TDerivedType4 d => matchFunc4(d),
            TDerivedType5 d => matchFunc5(d),
            TDerivedType6 d => matchFunc6(d),
            TDerivedType7 d => matchFunc7(d),
            TDerivedType8 d => matchFunc8(d),
            TDerivedType9 d => matchFunc9(d),
            TDerivedType10 d => matchFunc10(d),
            TDerivedType11 d => matchFunc11(d),
            TDerivedType12 d => matchFunc12(d),
            TDerivedType13 d => matchFunc13(d),
            TDerivedType14 d => matchFunc14(d),
            TDerivedType15 d => matchFunc15(d),
            TDerivedType16 d => matchFunc16(d),
            TDerivedType17 d => matchFunc17(d),
            TDerivedType18 d => matchFunc18(d),
            TDerivedType19 d => matchFunc19(d),
            TDerivedType20 d => matchFunc20(d),
            TDerivedType21 d => matchFunc21(d),
            TDerivedType22 d => matchFunc22(d),
            TDerivedType23 d => matchFunc23(d),
            TDerivedType24 d => matchFunc24(d),
            TDerivedType25 d => matchFunc25(d),
            TDerivedType26 d => matchFunc26(d),
            TDerivedType27 d => matchFunc27(d),
            TDerivedType28 d => matchFunc28(d),
            TDerivedType29 d => matchFunc29(d),
            TDerivedType30 d => matchFunc30(d),
            TDerivedType31 d => matchFunc31(d),
            TDerivedType32 d => matchFunc32(d),
            TDerivedType33 d => matchFunc33(d),
            TDerivedType34 d => matchFunc34(d),
            TDerivedType35 d => matchFunc35(d),
            TDerivedType36 d => matchFunc36(d),
            TDerivedType37 d => matchFunc37(d),
            TDerivedType38 d => matchFunc38(d),
            TDerivedType39 d => matchFunc39(d),
            TDerivedType40 d => matchFunc40(d),
            _ => defaultFunc == null ? default : defaultFunc.Invoke(obj),
        };
    }
}
