@echo off

:: Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
call "%VS140COMNTOOLS%VsDevCmd.bat"

pushd %~dp0

:: Start Test Automation from the binaries directory
csi %~dp0..\..\..\..\Binaries\Release\Perf\infra\automation.csx --verbose

popd