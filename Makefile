OS_NAME = $(shell uname -s)
NUGET_PACKAGE_NAME = nuget.35

ifeq ($(OS_NAME),Linux)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=ubuntu.14.04
	MONO_TOOLSET_NAME=mono.linux.4
	ROSLYN_TOOLSET_NAME=roslyn.linux.1
else ifeq ($(OS_NAME),Darwin)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=osx.10.10
	MONO_TOOLSET_NAME=mono.mac.5
	ROSLYN_TOOLSET_NAME=roslyn.mac.1
endif

MONO_PATH = /tmp/$(MONO_TOOLSET_NAME)/bin/mono
BOOTSTRAP_ARGS=$(MSBUILD_ADDITIONALARGS) /p:CscToolPath=/tmp/$(ROSLYN_TOOLSET_NAME) /p:CscToolExe=csc /p:VbcToolPath=/tmp/$(ROSLYN_TOOLSET_NAME) /p:VbcToolExe=vbc

all: /tmp/$(ROSLYN_TOOLSET_NAME).tar.bz2  /tmp/$(MONO_TOOLSET_NAME).tar.bz2 /tmp/$(NUGET_PACKAGE_NAME).zip
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe $(BOOTSTRAP_ARGS) /p:SignAssembly=false /p:DebugSymbols=false CrossPlatform.sln

/tmp/$(ROSLYN_TOOLSET_NAME).tar.bz2:
	@pushd /tmp/ ; \
	curl -O https://dotnetci.blob.core.windows.net/roslyn/$(ROSLYN_TOOLSET_NAME).tar.bz2 ; \
	tar -jxf $(ROSLYN_TOOLSET_NAME).tar.bz2

/tmp/$(MONO_TOOLSET_NAME).tar.bz2:
	@pushd /tmp/ ; \
	curl -O https://dotnetci.blob.core.windows.net/roslyn/$(MONO_TOOLSET_NAME).tar.bz2 ; \
	tar -jxf $(MONO_TOOLSET_NAME).tar.bz2

/tmp/$(NUGET_PACKAGE_NAME).zip:
	@pushd /tmp/ ; \
	curl -O https://dotnetci.blob.core.windows.net/roslyn/$(NUGET_PACKAGE_NAME).zip ; \
	unzip -uoq $(NUGET_PACKAGE_NAME).zip -d ~/ ; \
	chmod +x ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe

