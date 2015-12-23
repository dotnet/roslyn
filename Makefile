SHELL = /bin/bash
OS_NAME = $(shell uname -s)
NUGET_PACKAGE_NAME = nuget.40
BUILD_CONFIGURATION = Debug
BOOTSTRAP_PATH = $(shell pwd)/Binaries/Bootstrap


MSBUILD_ADDITIONALARGS = /v:m /fl /fileloggerparameters:Verbosity=normal /p:SignAssembly=false /p:DebugSymbols=false

ifeq ($(OS_NAME),Linux)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=ubuntu.14.04
	MONO_TOOLSET_NAME=mono.linux.4
	ROSLYN_TOOLSET_NAME=roslyn.linux.1
else ifeq ($(OS_NAME),Darwin)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=osx.10.10
	MONO_TOOLSET_NAME=mono.mac.5
	ROSLYN_TOOLSET_NAME=roslyn.mac.1
endif

ifeq ($(BOOTSTRAP),true)
	ROSLYN_TOOLSET_PATH = $(BOOTSTRAP_PATH)
else
	ROSLYN_TOOLSET_PATH = /tmp/$(ROSLYN_TOOLSET_NAME)
endif

MONO_PATH = /tmp/$(MONO_TOOLSET_NAME)/bin/mono
TOOLSET_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolPath=$(ROSLYN_TOOLSET_PATH) /p:CscToolExe=csc /p:VbcToolPath=$(ROSLYN_TOOLSET_PATH) /p:VbcToolExe=vbc

all: tools_packages
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe $(TOOLSET_ARGS) CrossPlatform.sln


bootstrap: tools_packages
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe $(TOOLSET_ARGS) src/Compilers/CSharp/CscCore/CscCore.csproj ; \
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0-prerelease/lib/MSBuild.exe $(TOOLSET_ARGS) src/Compilers/VisualBasic/VbcCore/VbcCore.csproj ; \
	mkdir -p $(BOOTSTRAP_PATH) ; \
	cp Binaries/$(BUILD_CONFIGURATION)/csccore/* $(BOOTSTRAP_PATH) ; \
	cp Binaries/$(BUILD_CONFIGURATION)/vbccore/* $(BOOTSTRAP_PATH) ; \
	rm -rf Binaries/$(BUILD_CONFIGURATION)

clean:
	@rm -rf Binaries

tools_packages: /tmp/$(ROSLYN_TOOLSET_NAME).tar.bz2  /tmp/$(MONO_TOOLSET_NAME).tar.bz2 /tmp/$(NUGET_PACKAGE_NAME).zip

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

