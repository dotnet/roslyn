@echo off

:: Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
call "%VS140COMNTOOLS%VsDevCmd.bat"

pushd %~dp0

:: Create a task which passes the location of the share to monitor as the parameter to look for new binary
:: First Argument - Share path to get the binaries from
:: Second Argument - Destination path to copy the binaries and start the Perf Tests
csi %~dp0fetch_build.csx %1  %~dp0..\..\..\..\Binaries\Release

popd
