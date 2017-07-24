SHELL := /usr/bin/env bash
OS_NAME := $(shell uname -s)
BUILD_CONFIGURATION := Debug
# $(dir) ends with slash
THIS_MAKEFILE_PATH := $(dir $(realpath $(lastword $(MAKEFILE_LIST))))
BINARIES_PATH := $(THIS_MAKEFILE_PATH)Binaries
SRC_PATH := $(THIS_MAKEFILE_PATH)src
BOOTSTRAP_PATH := $(BINARIES_PATH)/Bootstrap
BUILD_LOG_PATH :=
DOTNET_VERSION := 1.0.1
DOTNET_PATH := $(BINARIES_PATH)/dotnet-cli
DOTNET := $(DOTNET_PATH)/dotnet
export PATH := $(DOTNET_PATH):$(PATH)
TARGET_FX := netcoreapp1.1

# Workaround, see https://github.com/dotnet/roslyn/issues/10210
ifeq ($(origin HOME), undefined)
    # Note that while ~ usually refers to $HOME, in the case where $HOME is unset,
    # it looks up the current user's home dir, which is what we want.
    # https://www.gnu.org/software/bash/manual/html_node/Tilde-Expansion.html
    export HOME := $(shell cd ~ && pwd)
endif

ifeq ($(OS_NAME),Linux)
    RUNTIME_ID := $(shell . /etc/os-release && echo $$ID.$$VERSION_ID)-x64
else ifeq ($(OS_NAME),Darwin)
    RUNTIME_ID := osx.10.12-x64
else
    $(error "Unknown OS_NAME: $(OS_NAME)")
endif

MSBUILD_ARGS := /p:TreatWarningsAsErrors=true /warnaserror /nologo '/consoleloggerparameters:Verbosity=minimal;summary' /p:Configuration=$(BUILD_CONFIGURATION)

ifneq ($(BUILD_LOG_PATH),)
	MSBUILD_ARGS += /filelogger '/fileloggerparameters:Verbosity=normal;logFile=$(BUILD_LOG_PATH)'
endif

ifeq ($(BOOTSTRAP),true)
    MSBUILD_ARGS += /p:BootstrapBuildPath=$(BOOTSTRAP_PATH)
endif

BUILD_CMD := dotnet build $(MSBUILD_ARGS)

.PHONY: all bootstrap test restore

all: restore
	$(BUILD_CMD) $(THIS_MAKEFILE_PATH)CrossPlatform.sln

bootstrap: restore
	$(BUILD_CMD) $(SRC_PATH)/Compilers/CSharp/CscCore
	$(BUILD_CMD) $(SRC_PATH)/Compilers/VisualBasic/VbcCore
	mkdir -p $(BOOTSTRAP_PATH)/csc
	mkdir -p $(BOOTSTRAP_PATH)/vbc
	dotnet publish -c $(BUILD_CONFIGURATION) -r $(RUNTIME_ID) $(SRC_PATH)/Compilers/CSharp/CscCore -o $(BOOTSTRAP_PATH)/csc
	dotnet publish -c $(BUILD_CONFIGURATION) -r $(RUNTIME_ID) $(SRC_PATH)/Compilers/VisualBasic/VbcCore -o $(BOOTSTRAP_PATH)/vbc
	rm -rf $(BINARIES_PATH)/$(BUILD_CONFIGURATION)

test:
	dotnet publish -r $(RUNTIME_ID) $(SRC_PATH)/Test/DeployCoreClrTestRuntime -o $(BINARIES_PATH)/$(BUILD_CONFIGURATION)/CoreClrTest -p:RoslynRuntimeIdentifier=$(RUNTIME_ID)
	$(THIS_MAKEFILE_PATH)build/scripts/tests.sh $(BUILD_CONFIGURATION)

restore: $(DOTNET)
	$(THIS_MAKEFILE_PATH)build/scripts/restore.sh

$(DOTNET):
	curl https://raw.githubusercontent.com/dotnet/cli/rel/$(DOTNET_VERSION)/scripts/obtain/dotnet-install.sh | \
	$(SHELL) -s -- --version "$(DOTNET_VERSION)" --install-dir "$(DOTNET_PATH)"

clean:
	rm -rf $(BINARIES_PATH)
