' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ExpTreeTestResources

        ' results\checkedarithmeticbinaryoperators.txt                                                                          
        Private Shared _checkedArithmeticBinaryOperators As String
        Public Shared ReadOnly Property CheckedArithmeticBinaryOperators As String
            Get
                Return GetOrCreate("checkedarithmeticbinaryoperators.txt", _checkedArithmeticBinaryOperators)
            End Get
        End Property

        ' results\uncheckedarithmeticbinaryoperators.txt                                                                        
        Private Shared _uncheckedArithmeticBinaryOperators As String
        Public Shared ReadOnly Property UncheckedArithmeticBinaryOperators As String
            Get
                Return GetOrCreate("uncheckedarithmeticbinaryoperators.txt", _uncheckedArithmeticBinaryOperators)
            End Get
        End Property

        ' results\checkedandorxor.txt                                                                                           
        Private Shared _checkedAndOrXor As String
        Public Shared ReadOnly Property CheckedAndOrXor As String
            Get
                Return GetOrCreate("checkedandorxor.txt", _checkedAndOrXor)
            End Get
        End Property

        ' results\uncheckedandorxor.txt                                                                                         
        Private Shared _uncheckedAndOrXor As String
        Public Shared ReadOnly Property UncheckedAndOrXor As String
            Get
                Return GetOrCreate("uncheckedandorxor.txt", _uncheckedAndOrXor)
            End Get
        End Property

        ' results\checkedshortcircuit.txt                                                                                       
        Private Shared _checkedShortCircuit As String
        Public Shared ReadOnly Property CheckedShortCircuit As String
            Get
                Return GetOrCreate("checkedshortcircuit.txt", _checkedShortCircuit)
            End Get
        End Property

        ' results\uncheckedshortcircuit.txt                                                                                     
        Private Shared _uncheckedShortCircuit As String
        Public Shared ReadOnly Property UncheckedShortCircuit As String
            Get
                Return GetOrCreate("uncheckedshortcircuit.txt", _uncheckedShortCircuit)
            End Get
        End Property

        ' results\checkedcomparisonoperators.txt                                                                                
        Private Shared _checkedComparisonOperators As String
        Public Shared ReadOnly Property CheckedComparisonOperators As String
            Get
                Return GetOrCreate("checkedcomparisonoperators.txt", _checkedComparisonOperators)
            End Get
        End Property

        ' results\uncheckedcomparisonoperators.txt                                                                              
        Private Shared _uncheckedComparisonOperators As String
        Public Shared ReadOnly Property UncheckedComparisonOperators As String
            Get
                Return GetOrCreate("uncheckedcomparisonoperators.txt", _uncheckedComparisonOperators)
            End Get
        End Property

        ' results\checkedanduncheckedisisnotnothing.txt                                                                         
        Private Shared _checkedAndUncheckedIsIsNotNothing As String
        Public Shared ReadOnly Property CheckedAndUncheckedIsIsNotNothing As String
            Get
                Return GetOrCreate("checkedanduncheckedisisnotnothing.txt", _checkedAndUncheckedIsIsNotNothing)
            End Get
        End Property

        ' results\checkedanduncheckedisisnot.txt                                                                                
        Private Shared _checkedAndUncheckedIsIsNot As String
        Public Shared ReadOnly Property CheckedAndUncheckedIsIsNot As String
            Get
                Return GetOrCreate("checkedanduncheckedisisnot.txt", _checkedAndUncheckedIsIsNot)
            End Get
        End Property

        ' results\checkedconcatenate.txt                                                                                        
        Private Shared _checkedConcatenate As String
        Public Shared ReadOnly Property CheckedConcatenate As String
            Get
                Return GetOrCreate("checkedconcatenate.txt", _checkedConcatenate)
            End Get
        End Property

        ' results\uncheckedconcatenate.txt                                                                                      
        Private Shared _uncheckedConcatenate As String
        Public Shared ReadOnly Property UncheckedConcatenate As String
            Get
                Return GetOrCreate("uncheckedconcatenate.txt", _uncheckedConcatenate)
            End Get
        End Property

        ' results\checkedlike.txt                                                                                               
        Private Shared _checkedLike As String
        Public Shared ReadOnly Property CheckedLike As String
            Get
                Return GetOrCreate("checkedlike.txt", _checkedLike)
            End Get
        End Property

        ' results\uncheckedlike.txt                                                                                             
        Private Shared _uncheckedLike As String
        Public Shared ReadOnly Property UncheckedLike As String
            Get
                Return GetOrCreate("uncheckedlike.txt", _uncheckedLike)
            End Get
        End Property

        ' results\checkedanduncheckedwithdate.txt                                                                               
        Private Shared _checkedAndUncheckedWithDate As String
        Public Shared ReadOnly Property CheckedAndUncheckedWithDate As String
            Get
                Return GetOrCreate("checkedanduncheckedwithdate.txt", _checkedAndUncheckedWithDate)
            End Get
        End Property

        ' sources\exprlambdautils.vb                                                                                            
        Private Shared _exprLambdaUtils As String
        Public Shared ReadOnly Property ExprLambdaUtils As String
            Get
                Return GetOrCreate("exprlambdautils.vb", _exprLambdaUtils)
            End Get
        End Property

        ' sources\userdefinedbinaryoperators.vb                                                                                 
        Private Shared _userDefinedBinaryOperators As String
        Public Shared ReadOnly Property UserDefinedBinaryOperators As String
            Get
                Return GetOrCreate("userdefinedbinaryoperators.vb", _userDefinedBinaryOperators)
            End Get
        End Property

        ' results\checkeduserdefinedbinaryoperators.txt                                                                         
        Private Shared _checkedUserDefinedBinaryOperators As String
        Public Shared ReadOnly Property CheckedUserDefinedBinaryOperators As String
            Get
                Return GetOrCreate("checkeduserdefinedbinaryoperators.txt", _checkedUserDefinedBinaryOperators)
            End Get
        End Property

        ' results\uncheckeduserdefinedbinaryoperators.txt                                                                       
        Private Shared _uncheckedUserDefinedBinaryOperators As String
        Public Shared ReadOnly Property UncheckedUserDefinedBinaryOperators As String
            Get
                Return GetOrCreate("uncheckeduserdefinedbinaryoperators.txt", _uncheckedUserDefinedBinaryOperators)
            End Get
        End Property

        ' results\checkedanduncheckednothingconversions.txt                                                                     
        Private Shared _checkedAndUncheckedNothingConversions As String
        Public Shared ReadOnly Property CheckedAndUncheckedNothingConversions As String
            Get
                Return GetOrCreate("checkedanduncheckednothingconversions.txt", _checkedAndUncheckedNothingConversions)
            End Get
        End Property

        ' results\checkedanduncheckedtypeparameters.txt                                                                         
        Private Shared _checkedAndUncheckedTypeParameters As String
        Public Shared ReadOnly Property CheckedAndUncheckedTypeParameters As String
            Get
                Return GetOrCreate("checkedanduncheckedtypeparameters.txt", _checkedAndUncheckedTypeParameters)
            End Get
        End Property

        ' results\checkeddirecttryspecificconversions.txt                                                                       
        Private Shared _checkedDirectTrySpecificConversions As String
        Public Shared ReadOnly Property CheckedDirectTrySpecificConversions As String
            Get
                Return GetOrCreate("checkeddirecttryspecificconversions.txt", _checkedDirectTrySpecificConversions)
            End Get
        End Property

        ' results\uncheckeddirecttryspecificconversions.txt                                                                     
        Private Shared _uncheckedDirectTrySpecificConversions As String
        Public Shared ReadOnly Property UncheckedDirectTrySpecificConversions As String
            Get
                Return GetOrCreate("uncheckeddirecttryspecificconversions.txt", _uncheckedDirectTrySpecificConversions)
            End Get
        End Property

        ' results\checkedctypeandimplicitconversionseven.txt                                                                    
        Private Shared _checkedCTypeAndImplicitConversionsEven As String
        Public Shared ReadOnly Property CheckedCTypeAndImplicitConversionsEven As String
            Get
                Return GetOrCreate("checkedctypeandimplicitconversionseven.txt", _checkedCTypeAndImplicitConversionsEven)
            End Get
        End Property

        ' results\uncheckedctypeandimplicitconversionseven.txt                                                                  
        Private Shared _uncheckedCTypeAndImplicitConversionsEven As String
        Public Shared ReadOnly Property UncheckedCTypeAndImplicitConversionsEven As String
            Get
                Return GetOrCreate("uncheckedctypeandimplicitconversionseven.txt", _uncheckedCTypeAndImplicitConversionsEven)
            End Get
        End Property

        ' results\checkedctypeandimplicitconversionsodd.txt                                                                     
        Private Shared _checkedCTypeAndImplicitConversionsOdd As String
        Public Shared ReadOnly Property CheckedCTypeAndImplicitConversionsOdd As String
            Get
                Return GetOrCreate("checkedctypeandimplicitconversionsodd.txt", _checkedCTypeAndImplicitConversionsOdd)
            End Get
        End Property

        ' results\uncheckedctypeandimplicitconversionsodd.txt                                                                   
        Private Shared _uncheckedCTypeAndImplicitConversionsOdd As String
        Public Shared ReadOnly Property UncheckedCTypeAndImplicitConversionsOdd As String
            Get
                Return GetOrCreate("uncheckedctypeandimplicitconversionsodd.txt", _uncheckedCTypeAndImplicitConversionsOdd)
            End Get
        End Property

        ' tests\testconversion_typematrix_usertypes.vb                                                                          
        Private Shared _testConversion_TypeMatrix_UserTypes As String
        Public Shared ReadOnly Property TestConversion_TypeMatrix_UserTypes As String
            Get
                Return GetOrCreate("testconversion_typematrix_usertypes.vb", _testConversion_TypeMatrix_UserTypes)
            End Get
        End Property

        ' results\checkedanduncheckedusertypeconversions.txt                                                                    
        Private Shared _checkedAndUncheckedUserTypeConversions As String
        Public Shared ReadOnly Property CheckedAndUncheckedUserTypeConversions As String
            Get
                Return GetOrCreate("checkedanduncheckedusertypeconversions.txt", _checkedAndUncheckedUserTypeConversions)
            End Get
        End Property

        ' tests\testconversion_narrowing_udc.vb                                                                                 
        Private Shared _testConversion_Narrowing_UDC As String
        Public Shared ReadOnly Property TestConversion_Narrowing_UDC As String
            Get
                Return GetOrCreate("testconversion_narrowing_udc.vb", _testConversion_Narrowing_UDC)
            End Get
        End Property

        ' results\checkedanduncheckednarrowingudc.txt                                                                           
        Private Shared _checkedAndUncheckedNarrowingUDC As String
        Public Shared ReadOnly Property CheckedAndUncheckedNarrowingUDC As String
            Get
                Return GetOrCreate("checkedanduncheckednarrowingudc.txt", _checkedAndUncheckedNarrowingUDC)
            End Get
        End Property

        ' tests\testconversion_widening_udc.vb                                                                                  
        Private Shared _testConversion_Widening_UDC As String
        Public Shared ReadOnly Property TestConversion_Widening_UDC As String
            Get
                Return GetOrCreate("testconversion_widening_udc.vb", _testConversion_Widening_UDC)
            End Get
        End Property

        ' results\checkedanduncheckedwideningudc.txt                                                                            
        Private Shared _checkedAndUncheckedWideningUDC As String
        Public Shared ReadOnly Property CheckedAndUncheckedWideningUDC As String
            Get
                Return GetOrCreate("checkedanduncheckedwideningudc.txt", _checkedAndUncheckedWideningUDC)
            End Get
        End Property

        ' results\checkedunaryplusminusnot.txt                                                                                  
        Private Shared _checkedUnaryPlusMinusNot As String
        Public Shared ReadOnly Property CheckedUnaryPlusMinusNot As String
            Get
                Return GetOrCreate("checkedunaryplusminusnot.txt", _checkedUnaryPlusMinusNot)
            End Get
        End Property

        ' results\uncheckedunaryplusminusnot.txt                                                                                
        Private Shared _uncheckedUnaryPlusMinusNot As String
        Public Shared ReadOnly Property UncheckedUnaryPlusMinusNot As String
            Get
                Return GetOrCreate("uncheckedunaryplusminusnot.txt", _uncheckedUnaryPlusMinusNot)
            End Get
        End Property

        ' results\checkedanduncheckedistrueisfalse.txt                                                                          
        Private Shared _checkedAndUncheckedIsTrueIsFalse As String
        Public Shared ReadOnly Property CheckedAndUncheckedIsTrueIsFalse As String
            Get
                Return GetOrCreate("checkedanduncheckedistrueisfalse.txt", _checkedAndUncheckedIsTrueIsFalse)
            End Get
        End Property

        ' results\checkedanduncheckedudounaryplusminusnot.txt                                                                   
        Private Shared _checkedAndUncheckedUdoUnaryPlusMinusNot As String
        Public Shared ReadOnly Property CheckedAndUncheckedUdoUnaryPlusMinusNot As String
            Get
                Return GetOrCreate("checkedanduncheckedudounaryplusminusnot.txt", _checkedAndUncheckedUdoUnaryPlusMinusNot)
            End Get
        End Property

        ' results\checkedcoalescewithnullableboolean.txt                                                                        
        Private Shared _checkedCoalesceWithNullableBoolean As String
        Public Shared ReadOnly Property CheckedCoalesceWithNullableBoolean As String
            Get
                Return GetOrCreate("checkedcoalescewithnullableboolean.txt", _checkedCoalesceWithNullableBoolean)
            End Get
        End Property

        ' tests\testunary_udo_plusminusnot.vb                                                                                   
        Private Shared _testUnary_UDO_PlusMinusNot As String
        Public Shared ReadOnly Property TestUnary_UDO_PlusMinusNot As String
            Get
                Return GetOrCreate("testunary_udo_plusminusnot.vb", _testUnary_UDO_PlusMinusNot)
            End Get
        End Property

        ' results\checkedanduncheckedudounaryplusminusnot.txt                                                                   
        Private Shared _checkedAndUncheckedUdoUnaryPlusMinusNot1 As String
        Public Shared ReadOnly Property CheckedAndUncheckedUdoUnaryPlusMinusNot1 As String
            Get
                Return GetOrCreate("checkedanduncheckedudounaryplusminusnot.txt", _checkedAndUncheckedUdoUnaryPlusMinusNot1)
            End Get
        End Property

        ' results\checkedcoalescewithuserdefinedconversions.txt                                                                 
        Private Shared _checkedCoalesceWithUserDefinedConversions As String
        Public Shared ReadOnly Property CheckedCoalesceWithUserDefinedConversions As String
            Get
                Return GetOrCreate("checkedcoalescewithuserdefinedconversions.txt", _checkedCoalesceWithUserDefinedConversions)
            End Get
        End Property

        ' results\checkedobjectinitializers.txt                                                                                 
        Private Shared _checkedObjectInitializers As String
        Public Shared ReadOnly Property CheckedObjectInitializers As String
            Get
                Return GetOrCreate("checkedobjectinitializers.txt", _checkedObjectInitializers)
            End Get
        End Property

        ' results\checkedarrayinitializers.txt                                                                                  
        Private Shared _checkedArrayInitializers As String
        Public Shared ReadOnly Property CheckedArrayInitializers As String
            Get
                Return GetOrCreate("checkedarrayinitializers.txt", _checkedArrayInitializers)
            End Get
        End Property

        ' results\checkedcollectioninitializers.txt                                                                             
        Private Shared _checkedCollectionInitializers As String
        Public Shared ReadOnly Property CheckedCollectionInitializers As String
            Get
                Return GetOrCreate("checkedcollectioninitializers.txt", _checkedCollectionInitializers)
            End Get
        End Property

        ' results\checkedmiscellaneousa.txt                                                                                     
        Private Shared _checkedMiscellaneousA As String
        Public Shared ReadOnly Property CheckedMiscellaneousA As String
            Get
                Return GetOrCreate("checkedmiscellaneousa.txt", _checkedMiscellaneousA)
            End Get
        End Property

        ' tests\testunary_udo_istrueisfalse.vb                                                                                  
        Private Shared _testUnary_UDO_IsTrueIsFalse As String
        Public Shared ReadOnly Property TestUnary_UDO_IsTrueIsFalse As String
            Get
                Return GetOrCreate("testunary_udo_istrueisfalse.vb", _testUnary_UDO_IsTrueIsFalse)
            End Get
        End Property

        ' sources\queryhelper.vb                                                                                                
        Private Shared _queryHelper As String
        Public Shared ReadOnly Property QueryHelper As String
            Get
                Return GetOrCreate("queryhelper.vb", _queryHelper)
            End Get
        End Property

        ' results\exprtree_legacytests07_result.txt                                                                             
        Private Shared _exprTree_LegacyTests07_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests07_Result As String
            Get
                Return GetOrCreate("exprtree_legacytests07_result.txt", _exprTree_LegacyTests07_Result)
            End Get
        End Property

        ' results\exprtree_legacytests08_result.txt                                                                             
        Private Shared _exprTree_LegacyTests08_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests08_Result As String
            Get
                Return GetOrCreate("exprtree_legacytests08_result.txt", _exprTree_LegacyTests08_Result)
            End Get
        End Property

        ' results\exprtree_legacytests09_result.txt                                                                             
        Private Shared _exprTree_LegacyTests09_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests09_Result As String
            Get
                Return GetOrCreate("exprtree_legacytests09_result.txt", _exprTree_LegacyTests09_Result)
            End Get
        End Property

        ' results\xmlliteralsinexprlambda01_result.txt                                                                          
        Private Shared _xmlLiteralsInExprLambda01_Result As String
        Public Shared ReadOnly Property XmlLiteralsInExprLambda01_Result As String
            Get
                Return GetOrCreate("xmlliteralsinexprlambda01_result.txt", _xmlLiteralsInExprLambda01_Result)
            End Get
        End Property

        ' results\xmlliteralsinexprlambda02_result.txt                                                                          
        Private Shared _xmlLiteralsInExprLambda02_Result As String
        Public Shared ReadOnly Property XmlLiteralsInExprLambda02_Result As String
            Get
                Return GetOrCreate("xmlliteralsinexprlambda02_result.txt", _xmlLiteralsInExprLambda02_Result)
            End Get
        End Property

        ' results\xmlliteralsinexprlambda03_result.txt                                                                          
        Private Shared _xmlLiteralsInExprLambda03_Result As String
        Public Shared ReadOnly Property XmlLiteralsInExprLambda03_Result As String
            Get
                Return GetOrCreate("xmlliteralsinexprlambda03_result.txt", _xmlLiteralsInExprLambda03_Result)
            End Get
        End Property

        ' results\exprtree_legacytests10_result.txt                                                                             
        Private Shared _exprTree_LegacyTests10_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests10_Result As String
            Get
                Return GetOrCreate("exprtree_legacytests10_result.txt", _exprTree_LegacyTests10_Result)
            End Get
        End Property

        ' results\exprtree_legacytests02_v40_result.txt                                                                         
        Private Shared _exprTree_LegacyTests02_v40_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests02_v40_Result As String
            Get
                Return GetOrCreate("exprtree_legacytests02_v40_result.txt", _exprTree_LegacyTests02_v40_Result)
            End Get
        End Property

        ' results\exprtree_legacytests02_v45_result.txt                                                                         
        Private Shared _exprTree_LegacyTests02_v45_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests02_v45_Result As String
            Get
                Return GetOrCreate("exprtree_legacytests02_v45_result.txt", _exprTree_LegacyTests02_v45_Result)
            End Get
        End Property

        Private Shared Function GetOrCreate(ByVal name As String, ByRef value As String) As String
            If Not value Is Nothing Then
                Return value
            End If

            value = GetManifestResourceString(name)
            Return value
        End Function

        Private Shared Function GetManifestResourceString(name As String) As String
            Using reader As New StreamReader(GetType(EmitResourceUtil).GetTypeInfo().Assembly.GetManifestResourceStream(name))
                Return reader.ReadToEnd()
            End Using
        End Function
    End Class

End Namespace
