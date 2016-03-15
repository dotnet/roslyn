SHELL = /usr/bin/env bash
OS_NAME = $(shell uname -s)
NUGET_PACKAGE_NAME = nuget.70
BUILD_CONFIGURATION = Debug
BINARIES_PATH = $(shell pwd)/Binaries
TOOLSET_TMP_PATH = $(BINARIES_PATH)/toolset
BOOTSTRAP_PATH = $(BINARIES_PATH)/Bootstrap
BUILD_LOG_PATH =
XUNIT_VERSION = 2.1.0
HOME_DIR = $(shell cd ~ && pwd)

MSBUILD_ADDITIONALARGS := /v:m /fl /fileloggerparameters:Verbosity=normal /p:Configuration=$(BUILD_CONFIGURATION)

ifeq ($(OS_NAME),Linux)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=ubuntu.14.04
	ROSLYN_TOOLSET_NAME = roslyn.linux.5
else ifeq ($(OS_NAME),Darwin)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=osx.10.10
	ROSLYN_TOOLSET_NAME = roslyn.mac.4
endif

ifneq ($(BUILD_LOG_PATH),)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /fileloggerparameters:LogFile=$(BUILD_LOG_PATH)
endif

ROSLYN_TOOLSET_PATH = $(TOOLSET_TMP_PATH)/$(ROSLYN_TOOLSET_NAME)

ifeq ($(BOOTSTRAP),true)
	MSBUILD_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolPath=$(BOOTSTRAP_PATH) /p:CscToolExe=csc /p:VbcToolPath=$(BOOTSTRAP_PATH) /p:VbcToolExe=vbc
else
	MSBUILD_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolExe=csc /p:VbcToolExe=vbc
endif

MSBUILD_CMD = $(ROSLYN_TOOLSET_PATH)/corerun $(ROSLYN_TOOLSET_PATH)/MSBuild.exe $(MSBUILD_ARGS)

all: toolset
	export ReferenceAssemblyRoot=$(ROSLYN_TOOLSET_PATH)/reference-assemblies/Framework ; \
	export HOME=$(HOME_DIR) ; \
	$(MSBUILD_CMD) CrossPlatform.sln

bootstrap: toolset
	export ReferenceAssemblyRoot=$(ROSLYN_TOOLSET_PATH)/reference-assemblies/Framework ; \
	export HOME=$(HOME_DIR) ; \
	$(MSBUILD_CMD) src/Compilers/CSharp/CscCore/CscCore.csproj && \
	$(MSBUILD_CMD) src/Compilers/VisualBasic/VbcCore/VbcCore.csproj && \
	mkdir -p $(BOOTSTRAP_PATH) && \
	cp -f Binaries/$(BUILD_CONFIGURATION)/csccore/* $(BOOTSTRAP_PATH) && \
	cp -f Binaries/$(BUILD_CONFIGURATION)/vbccore/* $(BOOTSTRAP_PATH) && \
	build/scripts/crossgen.sh $(BOOTSTRAP_PATH) && \
	rm -rf Binaries/$(BUILD_CONFIGURATION)

test:
	build/scripts/tests.sh $(BUILD_CONFIGURATION)

clean:
	@rm -rf Binaries

clean_toolset:
	@rm -rf $(TOOLSET_TMP_PATH)

toolset: $(TOOLSET_TMP_PATH)/$(ROSLYN_TOOLSET_NAME) $(TOOLSET_TMP_PATH)/$(NUGET_PACKAGE_NAME).zip

$(TOOLSET_TMP_PATH)/$(ROSLYN_TOOLSET_NAME): | $(TOOLSET_TMP_PATH)
	@pushd $(TOOLSET_TMP_PATH) ; \
	curl -O https://dotnetci.blob.core.windows.net/roslyn/$(ROSLYN_TOOLSET_NAME).tar.bz2 && \
	tar -jxf $(ROSLYN_TOOLSET_NAME).tar.bz2 && \
	chmod +x $(ROSLYN_TOOLSET_NAME)/corerun

$(TOOLSET_TMP_PATH)/$(NUGET_PACKAGE_NAME).zip: | $(TOOLSET_TMP_PATH)
	@pushd $(TOOLSET_TMP_PATH) && \
	curl -O https://dotnetci.blob.core.windows.net/roslyn/$(NUGET_PACKAGE_NAME).zip && \
	unzip -uoq $(NUGET_PACKAGE_NAME).zip -d ~/

$(TOOLSET_TMP_PATH):
	mkdir -p $(TOOLSET_TMP_PATH)

