' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Reflection

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ExpTreeTestResources

        ' ExpressionTrees\Results\CheckedArithmeticBinaryOperators.txt
        Private Shared _checkedArithmeticBinaryOperators As String
        Public Shared ReadOnly Property CheckedArithmeticBinaryOperators As String
            Get
                Return GetOrCreate("CheckedArithmeticBinaryOperators.txt", _checkedArithmeticBinaryOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedArithmeticBinaryOperators.txt
        Private Shared _uncheckedArithmeticBinaryOperators As String
        Public Shared ReadOnly Property UncheckedArithmeticBinaryOperators As String
            Get
                Return GetOrCreate("UncheckedArithmeticBinaryOperators.txt", _uncheckedArithmeticBinaryOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndOrXor.txt
        Private Shared _checkedAndOrXor As String
        Public Shared ReadOnly Property CheckedAndOrXor As String
            Get
                Return GetOrCreate("CheckedAndOrXor.txt", _checkedAndOrXor)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedAndOrXor.txt
        Private Shared _uncheckedAndOrXor As String
        Public Shared ReadOnly Property UncheckedAndOrXor As String
            Get
                Return GetOrCreate("UncheckedAndOrXor.txt", _uncheckedAndOrXor)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedShortCircuit.txt
        Private Shared _checkedShortCircuit As String
        Public Shared ReadOnly Property CheckedShortCircuit As String
            Get
                Return GetOrCreate("CheckedShortCircuit.txt", _checkedShortCircuit)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedShortCircuit.txt
        Private Shared _uncheckedShortCircuit As String
        Public Shared ReadOnly Property UncheckedShortCircuit As String
            Get
                Return GetOrCreate("UncheckedShortCircuit.txt", _uncheckedShortCircuit)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedComparisonOperators.txt
        Private Shared _checkedComparisonOperators As String
        Public Shared ReadOnly Property CheckedComparisonOperators As String
            Get
                Return GetOrCreate("CheckedComparisonOperators.txt", _checkedComparisonOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedComparisonOperators.txt
        Private Shared _uncheckedComparisonOperators As String
        Public Shared ReadOnly Property UncheckedComparisonOperators As String
            Get
                Return GetOrCreate("UncheckedComparisonOperators.txt", _uncheckedComparisonOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedIsIsNotNothing.txt
        Private Shared _checkedAndUncheckedIsIsNotNothing As String
        Public Shared ReadOnly Property CheckedAndUncheckedIsIsNotNothing As String
            Get
                Return GetOrCreate("CheckedAndUncheckedIsIsNotNothing.txt", _checkedAndUncheckedIsIsNotNothing)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedIsIsNot.txt
        Private Shared _checkedAndUncheckedIsIsNot As String
        Public Shared ReadOnly Property CheckedAndUncheckedIsIsNot As String
            Get
                Return GetOrCreate("CheckedAndUncheckedIsIsNot.txt", _checkedAndUncheckedIsIsNot)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedConcatenate.txt
        Private Shared _checkedConcatenate As String
        Public Shared ReadOnly Property CheckedConcatenate As String
            Get
                Return GetOrCreate("CheckedConcatenate.txt", _checkedConcatenate)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedConcatenate.txt
        Private Shared _uncheckedConcatenate As String
        Public Shared ReadOnly Property UncheckedConcatenate As String
            Get
                Return GetOrCreate("UncheckedConcatenate.txt", _uncheckedConcatenate)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedLike.txt
        Private Shared _checkedLike As String
        Public Shared ReadOnly Property CheckedLike As String
            Get
                Return GetOrCreate("CheckedLike.txt", _checkedLike)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedLike.txt
        Private Shared _uncheckedLike As String
        Public Shared ReadOnly Property UncheckedLike As String
            Get
                Return GetOrCreate("UncheckedLike.txt", _uncheckedLike)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedWithDate.txt
        Private Shared _checkedAndUncheckedWithDate As String
        Public Shared ReadOnly Property CheckedAndUncheckedWithDate As String
            Get
                Return GetOrCreate("CheckedAndUncheckedWithDate.txt", _checkedAndUncheckedWithDate)
            End Get
        End Property

        ' ExpressionTrees\sources\ExprLambdaUtils.vb
        Private Shared _exprLambdaUtils As String
        Public Shared ReadOnly Property ExprLambdaUtils As String
            Get
                Return GetOrCreate("ExprLambdaUtils.vb", _exprLambdaUtils)
            End Get
        End Property

        ' ExpressionTrees\sources\UserDefinedBinaryOperators.vb
        Private Shared _userDefinedBinaryOperators As String
        Public Shared ReadOnly Property UserDefinedBinaryOperators As String
            Get
                Return GetOrCreate("UserDefinedBinaryOperators.vb", _userDefinedBinaryOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedUserDefinedBinaryOperators.txt
        Private Shared _checkedUserDefinedBinaryOperators As String
        Public Shared ReadOnly Property CheckedUserDefinedBinaryOperators As String
            Get
                Return GetOrCreate("CheckedUserDefinedBinaryOperators.txt", _checkedUserDefinedBinaryOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedUserDefinedBinaryOperators.txt
        Private Shared _uncheckedUserDefinedBinaryOperators As String
        Public Shared ReadOnly Property UncheckedUserDefinedBinaryOperators As String
            Get
                Return GetOrCreate("UncheckedUserDefinedBinaryOperators.txt", _uncheckedUserDefinedBinaryOperators)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedNothingConversions.txt
        Private Shared _checkedAndUncheckedNothingConversions As String
        Public Shared ReadOnly Property CheckedAndUncheckedNothingConversions As String
            Get
                Return GetOrCreate("CheckedAndUncheckedNothingConversions.txt", _checkedAndUncheckedNothingConversions)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedTypeParameters.txt
        Private Shared _checkedAndUncheckedTypeParameters As String
        Public Shared ReadOnly Property CheckedAndUncheckedTypeParameters As String
            Get
                Return GetOrCreate("CheckedAndUncheckedTypeParameters.txt", _checkedAndUncheckedTypeParameters)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedDirectTrySpecificConversions.txt
        Private Shared _checkedDirectTrySpecificConversions As String
        Public Shared ReadOnly Property CheckedDirectTrySpecificConversions As String
            Get
                Return GetOrCreate("CheckedDirectTrySpecificConversions.txt", _checkedDirectTrySpecificConversions)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedDirectTrySpecificConversions.txt
        Private Shared _uncheckedDirectTrySpecificConversions As String
        Public Shared ReadOnly Property UncheckedDirectTrySpecificConversions As String
            Get
                Return GetOrCreate("UncheckedDirectTrySpecificConversions.txt", _uncheckedDirectTrySpecificConversions)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedCTypeAndImplicitConversionsEven.txt
        Private Shared _checkedCTypeAndImplicitConversionsEven As String
        Public Shared ReadOnly Property CheckedCTypeAndImplicitConversionsEven As String
            Get
                Return GetOrCreate("CheckedCTypeAndImplicitConversionsEven.txt", _checkedCTypeAndImplicitConversionsEven)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedCTypeAndImplicitConversionsEven.txt
        Private Shared _uncheckedCTypeAndImplicitConversionsEven As String
        Public Shared ReadOnly Property UncheckedCTypeAndImplicitConversionsEven As String
            Get
                Return GetOrCreate("UncheckedCTypeAndImplicitConversionsEven.txt", _uncheckedCTypeAndImplicitConversionsEven)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedCTypeAndImplicitConversionsOdd.txt
        Private Shared _checkedCTypeAndImplicitConversionsOdd As String
        Public Shared ReadOnly Property CheckedCTypeAndImplicitConversionsOdd As String
            Get
                Return GetOrCreate("CheckedCTypeAndImplicitConversionsOdd.txt", _checkedCTypeAndImplicitConversionsOdd)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedCTypeAndImplicitConversionsOdd.txt
        Private Shared _uncheckedCTypeAndImplicitConversionsOdd As String
        Public Shared ReadOnly Property UncheckedCTypeAndImplicitConversionsOdd As String
            Get
                Return GetOrCreate("UncheckedCTypeAndImplicitConversionsOdd.txt", _uncheckedCTypeAndImplicitConversionsOdd)
            End Get
        End Property

        ' ExpressionTrees\Tests\TestConversion_TypeMatrix_UserTypes.vb
        Private Shared _testConversion_TypeMatrix_UserTypes As String
        Public Shared ReadOnly Property TestConversion_TypeMatrix_UserTypes As String
            Get
                Return GetOrCreate("TestConversion_TypeMatrix_UserTypes.vb", _testConversion_TypeMatrix_UserTypes)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedUserTypeConversions.txt
        Private Shared _checkedAndUncheckedUserTypeConversions As String
        Public Shared ReadOnly Property CheckedAndUncheckedUserTypeConversions As String
            Get
                Return GetOrCreate("CheckedAndUncheckedUserTypeConversions.txt", _checkedAndUncheckedUserTypeConversions)
            End Get
        End Property

        ' ExpressionTrees\Tests\TestConversion_Narrowing_UDC.vb
        Private Shared _testConversion_Narrowing_UDC As String
        Public Shared ReadOnly Property TestConversion_Narrowing_UDC As String
            Get
                Return GetOrCreate("TestConversion_Narrowing_UDC.vb", _testConversion_Narrowing_UDC)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedNarrowingUDC.txt
        Private Shared _checkedAndUncheckedNarrowingUDC As String
        Public Shared ReadOnly Property CheckedAndUncheckedNarrowingUDC As String
            Get
                Return GetOrCreate("CheckedAndUncheckedNarrowingUDC.txt", _checkedAndUncheckedNarrowingUDC)
            End Get
        End Property

        ' ExpressionTrees\Tests\TestConversion_Widening_UDC.vb
        Private Shared _testConversion_Widening_UDC As String
        Public Shared ReadOnly Property TestConversion_Widening_UDC As String
            Get
                Return GetOrCreate("TestConversion_Widening_UDC.vb", _testConversion_Widening_UDC)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedWideningUDC.txt
        Private Shared _checkedAndUncheckedWideningUDC As String
        Public Shared ReadOnly Property CheckedAndUncheckedWideningUDC As String
            Get
                Return GetOrCreate("CheckedAndUncheckedWideningUDC.txt", _checkedAndUncheckedWideningUDC)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedUnaryPlusMinusNot.txt
        Private Shared _checkedUnaryPlusMinusNot As String
        Public Shared ReadOnly Property CheckedUnaryPlusMinusNot As String
            Get
                Return GetOrCreate("CheckedUnaryPlusMinusNot.txt", _checkedUnaryPlusMinusNot)
            End Get
        End Property

        ' ExpressionTrees\Results\UncheckedUnaryPlusMinusNot.txt
        Private Shared _uncheckedUnaryPlusMinusNot As String
        Public Shared ReadOnly Property UncheckedUnaryPlusMinusNot As String
            Get
                Return GetOrCreate("UncheckedUnaryPlusMinusNot.txt", _uncheckedUnaryPlusMinusNot)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedIsTrueIsFalse.txt
        Private Shared _checkedAndUncheckedIsTrueIsFalse As String
        Public Shared ReadOnly Property CheckedAndUncheckedIsTrueIsFalse As String
            Get
                Return GetOrCreate("CheckedAndUncheckedIsTrueIsFalse.txt", _checkedAndUncheckedIsTrueIsFalse)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedUdoUnaryPlusMinusNot.txt
        Private Shared _checkedAndUncheckedUdoUnaryPlusMinusNot As String
        Public Shared ReadOnly Property CheckedAndUncheckedUdoUnaryPlusMinusNot As String
            Get
                Return GetOrCreate("CheckedAndUncheckedUdoUnaryPlusMinusNot.txt", _checkedAndUncheckedUdoUnaryPlusMinusNot)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedCoalesceWithNullableBoolean.txt
        Private Shared _checkedCoalesceWithNullableBoolean As String
        Public Shared ReadOnly Property CheckedCoalesceWithNullableBoolean As String
            Get
                Return GetOrCreate("CheckedCoalesceWithNullableBoolean.txt", _checkedCoalesceWithNullableBoolean)
            End Get
        End Property

        ' ExpressionTrees\Tests\TestUnary_UDO_PlusMinusNot.vb
        Private Shared _testUnary_UDO_PlusMinusNot As String
        Public Shared ReadOnly Property TestUnary_UDO_PlusMinusNot As String
            Get
                Return GetOrCreate("TestUnary_UDO_PlusMinusNot.vb", _testUnary_UDO_PlusMinusNot)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedAndUncheckedUdoUnaryPlusMinusNot.txt
        Private Shared _checkedAndUncheckedUdoUnaryPlusMinusNot1 As String
        Public Shared ReadOnly Property CheckedAndUncheckedUdoUnaryPlusMinusNot1 As String
            Get
                Return GetOrCreate("CheckedAndUncheckedUdoUnaryPlusMinusNot.txt", _checkedAndUncheckedUdoUnaryPlusMinusNot1)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedCoalesceWithUserDefinedConversions.txt
        Private Shared _checkedCoalesceWithUserDefinedConversions As String
        Public Shared ReadOnly Property CheckedCoalesceWithUserDefinedConversions As String
            Get
                Return GetOrCreate("CheckedCoalesceWithUserDefinedConversions.txt", _checkedCoalesceWithUserDefinedConversions)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedObjectInitializers.txt
        Private Shared _checkedObjectInitializers As String
        Public Shared ReadOnly Property CheckedObjectInitializers As String
            Get
                Return GetOrCreate("CheckedObjectInitializers.txt", _checkedObjectInitializers)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedArrayInitializers.txt
        Private Shared _checkedArrayInitializers As String
        Public Shared ReadOnly Property CheckedArrayInitializers As String
            Get
                Return GetOrCreate("CheckedArrayInitializers.txt", _checkedArrayInitializers)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedCollectionInitializers.txt
        Private Shared _checkedCollectionInitializers As String
        Public Shared ReadOnly Property CheckedCollectionInitializers As String
            Get
                Return GetOrCreate("CheckedCollectionInitializers.txt", _checkedCollectionInitializers)
            End Get
        End Property

        ' ExpressionTrees\Results\CheckedMiscellaneousA.txt
        Private Shared _checkedMiscellaneousA As String
        Public Shared ReadOnly Property CheckedMiscellaneousA As String
            Get
                Return GetOrCreate("CheckedMiscellaneousA.txt", _checkedMiscellaneousA)
            End Get
        End Property

        ' ExpressionTrees\Tests\TestUnary_UDO_IsTrueIsFalse.vb
        Private Shared _testUnary_UDO_IsTrueIsFalse As String
        Public Shared ReadOnly Property TestUnary_UDO_IsTrueIsFalse As String
            Get
                Return GetOrCreate("TestUnary_UDO_IsTrueIsFalse.vb", _testUnary_UDO_IsTrueIsFalse)
            End Get
        End Property

        ' ExpressionTrees\sources\QueryHelper.vb
        Private Shared _queryHelper As String
        Public Shared ReadOnly Property QueryHelper As String
            Get
                Return GetOrCreate("QueryHelper.vb", _queryHelper)
            End Get
        End Property

        ' ExpressionTrees\Results\ExprTree_LegacyTests07_Result.txt
        Private Shared _exprTree_LegacyTests07_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests07_Result As String
            Get
                Return GetOrCreate("ExprTree_LegacyTests07_Result.txt", _exprTree_LegacyTests07_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\ExprTree_LegacyTests08_Result.txt
        Private Shared _exprTree_LegacyTests08_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests08_Result As String
            Get
                Return GetOrCreate("ExprTree_LegacyTests08_Result.txt", _exprTree_LegacyTests08_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\ExprTree_LegacyTests09_Result.txt
        Private Shared _exprTree_LegacyTests09_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests09_Result As String
            Get
                Return GetOrCreate("ExprTree_LegacyTests09_Result.txt", _exprTree_LegacyTests09_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\XmlLiteralsInExprLambda01_Result.txt
        Private Shared _xmlLiteralsInExprLambda01_Result As String
        Public Shared ReadOnly Property XmlLiteralsInExprLambda01_Result As String
            Get
                Return GetOrCreate("XmlLiteralsInExprLambda01_Result.txt", _xmlLiteralsInExprLambda01_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\XmlLiteralsInExprLambda02_Result.txt
        Private Shared _xmlLiteralsInExprLambda02_Result As String
        Public Shared ReadOnly Property XmlLiteralsInExprLambda02_Result As String
            Get
                Return GetOrCreate("XmlLiteralsInExprLambda02_Result.txt", _xmlLiteralsInExprLambda02_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\XmlLiteralsInExprLambda03_Result.txt
        Private Shared _xmlLiteralsInExprLambda03_Result As String
        Public Shared ReadOnly Property XmlLiteralsInExprLambda03_Result As String
            Get
                Return GetOrCreate("XmlLiteralsInExprLambda03_Result.txt", _xmlLiteralsInExprLambda03_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\ExprTree_LegacyTests10_Result.txt
        Private Shared _exprTree_LegacyTests10_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests10_Result As String
            Get
                Return GetOrCreate("ExprTree_LegacyTests10_Result.txt", _exprTree_LegacyTests10_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\ExprTree_LegacyTests02_v40_Result.txt
        Private Shared _exprTree_LegacyTests02_v40_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests02_v40_Result As String
            Get
                Return GetOrCreate("ExprTree_LegacyTests02_v40_Result.txt", _exprTree_LegacyTests02_v40_Result)
            End Get
        End Property

        ' ExpressionTrees\Results\ExprTree_LegacyTests02_v45_Result.txt
        Private Shared _exprTree_LegacyTests02_v45_Result As String
        Public Shared ReadOnly Property ExprTree_LegacyTests02_v45_Result As String
            Get
                Return GetOrCreate("ExprTree_LegacyTests02_v45_Result.txt", _exprTree_LegacyTests02_v45_Result)
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
