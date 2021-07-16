' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Utility functions to check if two implemented interfaces have variance ambiguity.
    ''' 
    ''' What is "Variance Ambiguity"? Here's an example:
    ''' Class ReflectionType
    '''   Implements IEnumerable(Of Field)
    '''   Implements IEnumerable(Of Method)
    '''   Public Sub GetEnumeratorF() As IEnumerator(Of Field) Implements IEnumerable(Of Field).GetEnumerator ...
    '''   Public Sub GetEnumeratorM() As IEnumerator(Of Method) Implements IEnumerable(Of Method).GetEnumerator ...
    ''' End Class
    ''' Dim x as new ReflectionType
    ''' Dim y as IEnumerable(Of Member) = x
    ''' Dim z = y.GetEnumerator()
    '''
    ''' Note that, through variance, both IEnumerable(Of Field) and IEnumerable(Of Method) have widening
    ''' conversions to IEnumerable(Of Member). So it's ambiguous whether the initialization of "z" would
    ''' invoke GetEnumeratorF or GetEnumeratorM. This function avoids such ambiguity at the declaration
    ''' level, i.e. it reports a warning on the two implements classes inside ReflectionType that they
    ''' may lead to ambiguity.
    ''' </summary>
    Friend Class VarianceAmbiguity
        ''' <summary>
        ''' Determine if two interfaces that were constructed from the same original definition
        ''' have variance ambiguity.
        ''' 
        ''' We have something like left=ICocon(Of Mammal, int32[]), right=ICocon(Of Fish, int32[])
        ''' for some interface ICocon(Of Out T, In U). And we have to decide if left and right 
        ''' might lead to ambiguous member-lookup later on in execution.
        '''
        ''' To do this: go through each type parameter T, U...
        '''   * For "Out T", judge whether the arguments Mammal/Fish cause ambiguity or prevent it.
        '''   * For "In T", judge whether the arguments int32[]/int32[] cause ambiguity or prevent it.
        '''
        ''' "Causing/preventing ambiguity" is described further below.
        ''' 
        ''' Given all that, ambiguity was prevented in any positions, then left/right are fine.
        ''' Otherwise, if ambiguity wasn't caused in any positions, then left/right are fine.
        ''' Otherwise, left/right have an ambiguity.
        ''' </summary>
        Public Shared Function HasVarianceAmbiguity(containingType As NamedTypeSymbol, i1 As NamedTypeSymbol, i2 As NamedTypeSymbol, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As Boolean
            Debug.Assert(i1.IsInterfaceType() AndAlso i2.IsInterfaceType())
            Debug.Assert(i1.IsGenericType AndAlso i2.IsGenericType)
            Debug.Assert(TypeSymbol.Equals(i1.OriginalDefinition, i2.OriginalDefinition, TypeCompareKind.ConsiderEverything))

            ' Go through all type arguments of all containing types. Order doesn't matters, so we 
            ' go inside-out.
            Dim nestingType1 = i1
            Dim nestingType2 = i2

            Dim causesAmbiguity As Boolean = False
            Dim preventsAmbiguity As Boolean = False

            Do
                Dim arity = nestingType1.Arity
                For iTypeParameter = 0 To arity - 1
                    ' Below call passes "causesAmbiguity" and "preventsAmbiguity" by reference.
                    CheckCorrespondingTypeArguments(containingType,
                                                    nestingType1.TypeParameters(iTypeParameter).Variance,
                                                    nestingType1.TypeArgumentWithDefinitionUseSiteDiagnostics(iTypeParameter, useSiteInfo),
                                                    nestingType2.TypeArgumentWithDefinitionUseSiteDiagnostics(iTypeParameter, useSiteInfo),
                                                    causesAmbiguity, preventsAmbiguity,
                                                    useSiteInfo)
                Next
                nestingType1 = nestingType1.ContainingType
                nestingType2 = nestingType2.ContainingType
            Loop While nestingType1 IsNot Nothing

            ' If some type parameters caused ambiguity, and none prevented it, then we have an ambiguity.
            Return causesAmbiguity And Not preventsAmbiguity
        End Function

        ''' <summary>
        ''' Check two corresponding type arguments T1 and T2 and determine if the cause or prevent variable ambiguity.
        ''' 
        ''' Identical types never cause or prevent ambiguity.
        ''' 
        ''' If there could exist a **distinct** third type T3, such that T1 and T2 both convert via the variance
        ''' conversion to T3, then ambiguity is caused. This boils down to:
        '''   * Invariant parameters never cause ambiguity
        '''   * Covariant parameters "Out T": ambiguity is caused when the two type arguments 
        '''     are non-object types not known to be values (T3=Object)
        '''   * Contravariant parameters "In U": ambiguity is caused when both:
        '''       - Neither T1 or T2 is a value type or a sealed (NotInheritable) reference type
        '''       - If T1 and T2 are both class types, one derives from the other. 
        '''         (T3 is some type deriving or implementing both T1 and T2)
        ''' 
        '''  Ambiguity is prevented when there T1 and T2 cannot unify to the same type, and there 
        '''  cannot be a (not necessarily distinct) third type T3 that both T1 and T2 convert to via
        '''  the variance conversion.
        ''' 
        '''  This boils down to:
        '''   * Invariant parameters: Ambiguity is prevented when:
        '''       - they are non-unifying
        '''   * Covariant parameters "Out T": Ambiguity is prevented when both:
        '''       - they are non-unifying
        '''       - at least one is a value type
        '''   * Contravariant parameters "In U": Ambiguity is prevented when:
        '''       - they are non-unifying AND
        '''          - at least one is known to be a value type OR
        '''          - both are known to be class types and neither derives from the other.
        ''' </summary>
        Private Shared Sub CheckCorrespondingTypeArguments(containingType As NamedTypeSymbol,
                                                           variance As VarianceKind,
                                                           typeArgument1 As TypeSymbol,
                                                           typeArgument2 As TypeSymbol,
                                                           ByRef causesAmbiguity As Boolean,
                                                           ByRef preventsAmbiguity As Boolean,
                                                           <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            If Not typeArgument1.IsSameTypeIgnoringAll(typeArgument2) Then
                Select Case variance
                    Case VarianceKind.In
                        Dim bothAreClasses = (typeArgument1.IsClassType() AndAlso typeArgument2.IsClassType())
                        Dim oneDerivesFromOther = bothAreClasses AndAlso
                                (Conversions.ClassifyDirectCastConversion(typeArgument1, typeArgument2, useSiteInfo) And ConversionKind.Reference) <> 0

                        ' (Note that value types are always NotInheritable)
                        If Not typeArgument1.IsNotInheritable() AndAlso Not typeArgument2.IsNotInheritable() AndAlso
                           (Not bothAreClasses OrElse oneDerivesFromOther) Then
                            causesAmbiguity = True
                        ElseIf (typeArgument1.IsValueType OrElse typeArgument2.IsValueType OrElse (bothAreClasses AndAlso Not oneDerivesFromOther)) AndAlso
                           Not TypeUnification.CanUnify(containingType, typeArgument1, typeArgument2) Then
                            preventsAmbiguity = True
                        End If

                    Case VarianceKind.Out
                        If typeArgument1.SpecialType <> SpecialType.System_Object AndAlso
                           typeArgument2.SpecialType <> SpecialType.System_Object AndAlso
                           Not typeArgument1.IsValueType AndAlso
                           Not typeArgument2.IsValueType Then
                            causesAmbiguity = True
                        ElseIf (typeArgument1.IsValueType OrElse typeArgument2.IsValueType) AndAlso
                           Not TypeUnification.CanUnify(containingType, typeArgument1, typeArgument2) Then
                            preventsAmbiguity = True
                        End If

                    Case VarianceKind.None
                        If Not TypeUnification.CanUnify(containingType, typeArgument1, typeArgument2) Then
                            preventsAmbiguity = True
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(variance)
                End Select
            End If
        End Sub
    End Class
End Namespace

