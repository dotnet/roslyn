SHELL = /usr/bin/env bash
OS_NAME = $(shell uname -s)
BUILD_CONFIGURATION = Debug
BINARIES_PATH = $(shell pwd)/Binaries
SCRIPTS_PATH = $(shell pwd)/build/scripts
TOOLSET_PATH = $(BINARIES_PATH)/toolset
RESTORE_SEMAPHORE_PATH = $(TOOLSET_PATH)/restore.semaphore
BOOTSTRAP_PATH = $(BINARIES_PATH)/Bootstrap
BUILD_LOG_PATH =
HOME_DIR = $(shell cd ~ && pwd)
DOTNET_VERSION = 1.0.0-preview2-002911
NUGET_VERSION = 3.5.0-beta2
NUGET_EXE = $(shell pwd)/nuget.exe

MSBUILD_ADDITIONALARGS := /v:m /fl /fileloggerparameters:Verbosity=normal /p:Configuration=$(BUILD_CONFIGURATION)

ifeq ($(OS_NAME),Linux)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=ubuntu.14.04
	ROSLYN_TOOLSET_NAME = roslyn.linux.8
	DOTNET_PLATFORM = ubuntu-x64
else ifeq ($(OS_NAME),Darwin)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /p:BaseNuGetRuntimeIdentifier=osx.10.10
	ROSLYN_TOOLSET_NAME = roslyn.mac.8
	DOTNET_PLATFORM = osx-x64
endif

ifneq ($(BUILD_LOG_PATH),)
	MSBUILD_ADDITIONALARGS := $(MSBUILD_ADDITIONALARGS) /fileloggerparameters:LogFile=$(BUILD_LOG_PATH)
endif

ROSLYN_TOOLSET_PATH = $(TOOLSET_PATH)/$(ROSLYN_TOOLSET_NAME)

ifeq ($(BOOTSTRAP),true)
	MSBUILD_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolPath=$(BOOTSTRAP_PATH) /p:CscToolExe=csc /p:VbcToolPath=$(BOOTSTRAP_PATH) /p:VbcToolExe=vbc
else
	MSBUILD_ARGS = $(MSBUILD_ADDITIONALARGS) /p:CscToolExe=csc /p:VbcToolExe=vbc
endif

MSBUILD_CMD = $(ROSLYN_TOOLSET_PATH)/corerun $(ROSLYN_TOOLSET_PATH)/MSBuild.exe $(MSBUILD_ARGS)

.PHONY: all bootstrap test restore toolset nuget

all: $(ROSLYN_TOOLSET_PATH) $(RESTORE_SEMAPHORE_PATH)
	export ReferenceAssemblyRoot=$(ROSLYN_TOOLSET_PATH)/reference-assemblies/Framework ; \
	export HOME=$(HOME_DIR) ; \
	$(MSBUILD_CMD) CrossPlatform.sln

bootstrap: $(ROSLYN_TOOLSET_PATH) $(RESTORE_SEMAPHORE_PATH)
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

restore: $(NUGET_EXE) $(RESTORE_SEMAPHORE_PATH)

$(RESTORE_SEMAPHORE_PATH): $(ROSLYN_TOOLSET_PATH)
	@build/scripts/restore.sh $(ROSLYN_TOOLSET_PATH) $(NUGET_EXE) && \
	touch $(RESTORE_SEMAPHORE_PATH)

$(NUGET_EXE):
	curl https://dist.nuget.org/win-x86-commandline/v$(NUGET_VERSION)/NuGet.exe -o $(NUGET_EXE) --create-dirs

nuget: $(NUGET_EXE)

clean:
	@rm -rf Binaries

clean_toolset:
	@rm -rf $(TOOLSET_PATH)

toolset: $(ROSLYN_TOOLSET_PATH)

$(ROSLYN_TOOLSET_PATH): | $(TOOLSET_PATH)
	@pushd $(TOOLSET_PATH) ; \
	curl -O https://dotnetci.blob.core.windows.net/roslyn/$(ROSLYN_TOOLSET_NAME).zip && \
	curl -O https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/$(DOTNET_VERSION)/dotnet-dev-$(DOTNET_PLATFORM).$(DOTNET_VERSION).tar.gz && \
	mkdir -p $(ROSLYN_TOOLSET_NAME)/dotnet-cli && \
	$(SCRIPTS_PATH)/unzip.sh $(ROSLYN_TOOLSET_NAME).zip $(ROSLYN_TOOLSET_NAME) && \
	tar -zxf dotnet-dev-$(DOTNET_PLATFORM).$(DOTNET_VERSION).tar.gz -C $(ROSLYN_TOOLSET_NAME)/dotnet-cli && \
	chmod +x $(ROSLYN_TOOLSET_NAME)/corerun && \
	chmod +x $(ROSLYN_TOOLSET_NAME)/RoslynRestore && \
	chmod +x $(ROSLYN_TOOLSET_NAME)/csc && \
	chmod +x $(ROSLYN_TOOLSET_NAME)/vbc

$(TOOLSET_PATH):
	mkdir -p $(TOOLSET_PATH)
