' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Enum RequiredConversion ' "ConversionRequired" in Dev10 compiler 

        '// When we do type inference, we have to unify the types supplied and infer generic parameters.
        '// e.g. if we have Sub f(ByVal x as T(), ByVal y as T) and invoke it with x=AnimalArray, y=Mammal,
        '// then we have to figure out that T should be an Animal. The way that's done:
        '// (1) All the requirements on T are gathered together, e.g.
        '//     T:{Mammal+vb, Animal+arr} means
        '//     (+vb)  "T is something such that argument Mammal can be supplied to parameter T"
        '//     (+arr) "T is something such that argument Animal() can be supplied to parameter T()"
        '// (2) We'll go through each candidate type to see if they work. First T=Mammal. Does it work for each requirement?
        '//     (+vb)  Yes,     argument Mammal can be supplied to parameter Mammal through identity
        '//     (+arr) Sort-of, argument Animal() can be supplied to parameter Mammal() only through narrowing
        '// (3) Now try the next candidate, T=Animal. Does it work for each requirement?
        '//     (+vb)  Yes,     argument Mammal can be supplied to parameter Animal through widening
        '//     (+arr) Yes,     argument Animal() can be supplied to parameter Animal() through identity
        '// (4) At the end, we pick out the one that worked "best". In this case T=Animal worked best.
        '// The criteria for "best" are documented and implemented in ConversionResolution.cpp/FindDominantType.

        '// This enumeration contains the different kinds of requirements...
        '// Each requirement is that some X->Y be a conversion. Inside FindDominantType we will grade each candidate
        '// on whether it could satisfy that requirement with an Identity, a Widening, a Narrowing, or not at all.

        '// Identity:
        '// This restriction requires that req->candidate be an identity conversion according to the CLR.
        '// e.g. supplying "New List(Of Mammal)" to parameter "List(Of T)", we require that Mammal->T be identity
        '// e.g. supplying "New List(Of Mammal)" to a parameter "List(Of T)" we require that Mammal->T be identity
        '// e.g. supplying "Dim ml as ICovariant(Of Mammal) = Nothing" to a parameter "*ByRef* ICovariant(Of T)" we require that Mammal->T be identity
        '// (but for non-ByRef covariance see "ReferenceConversion" below.)
        '// Note that CLR does not include lambda->delegate, and doesn't include user-defined conversions.
        Identity

        '// Any:
        '// This restriction requires that req->candidate be a conversion according to VB.
        '// e.g. supplying "New Mammal" to parameter "T", we require that Mammal->T be a VB conversion
        '// It includes user-defined conversions and all the VB-specific conversions.
        Any

        '// AnyReverse:
        '// This restriction requires that candidate->req be a conversion according to VB.
        '// It might hypothetically be used for "out" parameters if VB ever gets them:
        '// e.g. supplying "Dim m as Mammal" to parameter "Out T" we require that T->Mammal be a VB conversion.
        '// But the actual reason it's included now is as be a symmetric form of AnyConversion:
        '// this simplifies the implementation of InvertConversionRequirement and CombineConversionRequirements
        AnyReverse

        '// AnyAndReverse:
        '// This restriction requires that req->candidate and candidate->hint be conversions according to VB.
        '// e.g. supplying "Dim m as New Mammal" to "ByRef T", we require that Mammal->T be a conversion, and also T->Mammal for the copyback.
        '// Again, each direction includes user-defined conversions and all the VB-specific conversions.
        AnyAndReverse

        '// ArrayElement:
        '// This restriction requires that req->candidate be a array element conversion.
        '// e.g. supplying "new Mammal(){}" to "ByVal T()", we require that Mammal->T be an array-element-conversion.
        '// It consists of the subset of CLR-array-element-conversions that are also allowed by VB.
        '// Note: ArrayElementConversion gives us array covariance, and also by enum()->underlying_integral().
        ArrayElement

        '// Reference:
        '// This restriction requires that req->candidate be a reference conversion.
        '// e.g. supplying "Dim x as ICovariant(Of Mammal)" to "ICovariant(Of T)", we require that Mammal->T be a reference conversion.
        '// It consists of the subset of CLR-reference-conversions that are also allowed by VB.
        Reference

        '// ReverseReference:
        '// This restriction requires that candidate->req be a reference conversion.
        '// e.g. supplying "Dim x as IContravariant(Of Animal)" to "IContravariant(Of T)", we require that T->Animal be a reference conversion.
        '// Note that just because T->U is a widening reference conversion, it doesn't mean that U->T is narrowing, nor vice versa.
        '// Again it consists of the subset of CLR-reference-conversions that are also allowed by VB.
        ReverseReference

        '// None:
        '// This is not a restriction. It allows for the candidate to have any relation, even be completely unrelated,
        '// to the hint type. It is used as a way of feeding in candidate suggestions into the algorithm, but leaving
        '// them purely as suggestions, without any requirement to be satisfied. (e.g. you might add the restriction
        '// that there be a conversion from some literal "1L", and add the type hint "Long", so that Long can be used
        '// as a candidate but it's not required. This is used in computing the dominant type of an array literal.)
        None

        '// These restrictions form a partial order composed of three chains: from less strict to more strict, we have:
        '//    [reverse chain] [None] < AnyReverse < ReverseReference < Identity
        '//    [middle  chain] None < [Any,AnyReverse] < AnyConversionAndReverse < Identity
        '//    [forward chain] [None] < Any < ArrayElement < Reference < Identity
        '//
        '//            =           KEY:
        '//         /  |  \           =     Identity
        '//        /   |   \         +r     Reference
        '//      -r    |    +r       -r     ReverseReference
        '//       |  +-any  |       +-any   AnyConversionAndReverse
        '//       |   /|\   +arr     +arr   ArrayElement
        '//       |  / | \  |        +any   Any
        '//      -any  |  +any       -any   AnyReverse
        '//         \  |  /           none  None
        '//          \ | /
        '//           none
        '//
        '// The routine "CombineConversionRequirements" finds the least upper bound of two elements.
        '// The routine "StrengthenConversionRequirementToReference" walks up the current chain to a reference conversion.
        '// The routine "InvertConversionRequirement" switches from reverse chain to forwards chain or vice versa,
        '// and asserts if given ArrayElementConversion since this has no counterparts in the reverse chain.
        '// These three routines are called by InferTypeArgumentsFromArgumentDirectly, as it matches an
        '// argument type against a parameter.
        '// The routine "CheckHintSatisfaction" is what actually implements the satisfaction-of-restriction check.
        '//
        '// If you make any changes to this enum or the partial order, you'll have to change all the above functions.
        '// They do "VSASSERT(Count==8)" to help remind you to change them, should you make any additions to this enum.
        Count
    End Enum

End Namespace
