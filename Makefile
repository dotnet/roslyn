SHELL = /usr/bin/env bash
OS_NAME = $(shell uname -s)
NUGET_PACKAGE_NAME = nuget.67
BUILD_CONFIGURATION = Debug
BOOTSTRAP_PATH = $(shell pwd)/Binaries/Bootstrap
BUILD_LOG_PATH =
XUNIT_VERSION = 2.1.0

MSBUILD_ADDITIONALARGS := /v:m /fl /fileloggerparameters:Verbosity=normal /p:DebugSymbols=false /p:Configuration=$(BUILD_CONFIGURATION)

ifeq ($(OS_NAME),Linux)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=ubuntu.14.04
	MONO_TOOLSET_NAME = mono.linux.4
	ROSLYN_TOOLSET_NAME = roslyn.linux.3
else ifeq ($(OS_NAME),Darwin)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=osx.10.10
	MONO_TOOLSET_NAME = mono.mac.5
	ROSLYN_TOOLSET_NAME = roslyn.mac.3
endif

ifneq ($(BUILD_LOG_PATH),)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /fileloggerparameters:LogFile=$(BUILD_LOG_PATH)
endif

ifeq ($(BOOTSTRAP),true)
	ROSLYN_TOOLSET_PATH = $(BOOTSTRAP_PATH)
else
	ROSLYN_TOOLSET_PATH = /tmp/$(ROSLYN_TOOLSET_NAME)
endif

MONO_PATH = /tmp/$(MONO_TOOLSET_NAME)/bin/mono
MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:MonoToolsetPath=$(MONO_PATH)
TOOLSET_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolPath=$(ROSLYN_TOOLSET_PATH) /p:CscToolExe=csc /p:VbcToolPath=$(ROSLYN_TOOLSET_PATH) /p:VbcToolExe=vbc

all: toolset
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0/lib/MSBuild.exe $(TOOLSET_ARGS) CrossPlatform.sln

bootstrap: toolset
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0/lib/MSBuild.exe $(TOOLSET_ARGS) src/Compilers/CSharp/CscCore/CscCore.csproj ; \
	$(MONO_PATH) ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0/lib/MSBuild.exe $(TOOLSET_ARGS) src/Compilers/VisualBasic/VbcCore/VbcCore.csproj ; \
	mkdir -p $(BOOTSTRAP_PATH) ; \
	cp Binaries/$(BUILD_CONFIGURATION)/csccore/* $(BOOTSTRAP_PATH) ; \
	cp Binaries/$(BUILD_CONFIGURATION)/vbccore/* $(BOOTSTRAP_PATH) ; \
	build/scripts/crossgen.sh $(BOOTSTRAP_PATH) ;
	rm -rf Binaries/$(BUILD_CONFIGURATION)

test:
	build/scripts/tests.sh $(BUILD_CONFIGURATION)

clean:
	@rm -rf Binaries

toolset: /tmp/$(ROSLYN_TOOLSET_NAME).tar.bz2  /tmp/$(MONO_TOOLSET_NAME).tar.bz2 /tmp/$(NUGET_PACKAGE_NAME).zip

clean_toolset:
	rm /tmp/$(ROSLYN_TOOLSET_NAME).tar.bz2 ; \
	rm /tmp/$(MONO_TOOLSET_NAME).tar.bz2 ; \
	rm /tmp/$(NUGET_PACKAGE_NAME).zip

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
	chmod +x ~/.nuget/packages/Microsoft.Build.Mono.Debug/14.1.0/lib/MSBuild.exe
