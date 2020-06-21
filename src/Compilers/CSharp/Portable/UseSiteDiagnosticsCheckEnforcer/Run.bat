REM Licensed to the .NET Foundation under one or more agreements.
REM The .NET Foundation licenses this file to you under the MIT license.
REM See the LICENSE file in the project root for more information.


@REM %1 - OutDir
@REM %2 - TargetFileName
@REM %3 - TargetPath 
@REM %4 - ProjectDir
@REM %5 - ILDASMPath

@REM @echo %1
@REM @echo %2
@REM @echo %3
@REM @echo %4
@REM @echo %5

@echo Begin "%4UseSiteDiagnosticsCheckEnforcer\Run.bat" %1 %2 %3 %4 %5 

@set WorkFolder="%1%CSUseSiteChecks"
@set ILFileName="%2%.il"
@set TargetPath="%3%"
@set BaseLine="%4%UseSiteDiagnosticsCheckEnforcer\BaseLine.txt"
@set ILDASMPath=%5% 

@IF NOT EXIST %WorkFolder% (
    @goto label_md
)

@rd /S /Q %WorkFolder%
@IF ERRORLEVEL 1 (
    @goto label_rdFailed
)

:label_md
@md %WorkFolder%

@IF ERRORLEVEL 1 (
    @goto label_mdFailed
)

@pushd %WorkFolder%

@%ILDASMPath% /NOBAR /OUT=%ILFileName% %TargetPath%

@IF ERRORLEVEL 1 (
    @goto label_ildasmFailed
)

@findstr /C:Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbol::get_BaseType() %ILFileName% > Found.txt
@findstr /C:Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbol::get_Interfaces() %ILFileName% >> Found.txt
@findstr /C:Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbol::get_AllInterfaces() %ILFileName% >> Found.txt
@findstr /C:Microsoft.CodeAnalysis.CSharp.Symbols.NamedTypeSymbol::get_TypeArguments() %ILFileName% >> Found.txt
@findstr /C:Microsoft.CodeAnalysis.CSharp.Symbols.TypeParameterSymbol::get_ConstraintTypes() %ILFileName% >> Found.txt

@REM notepad Found.txt

@fc %BaseLine% Found.txt > ComparisonResult.txt

@IF ERRORLEVEL 1 (
    @goto label_baselineMismatch
)

@popd
@rd /S /Q %WorkFolder%
@echo End "%4%UseSiteDiagnosticsCheckEnforcer\Run.bat"
@EXIT /B 0

:label_baselineMismatch
@ECHO **********  Unexpected IL content in %TargetPath%
@TYPE ComparisonResult.txt
@ECHO **********  
@popd
@rd /S /Q %WorkFolder%
@EXIT /B 1

:label_ildasmFailed
@ECHO ILDASM failed for: %TargetPath%
@echo %ILDASMPath% /NOBAR /OUT=%ILFileName% %TargetPath% 
@popd
@rd /S /Q %WorkFolder%
@EXIT /B 1

:label_rdFailed
@ECHO Failed to delete temporary working folder: "%WorkFolder%"
@EXIT /B 1

:label_mdFailed
@ECHO Failed to create temporary working folder: "%WorkFolder%"
@EXIT /B 1
