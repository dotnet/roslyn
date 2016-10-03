// Groovy Script: http://www.groovy-lang.org/syntax.html
// Jenkins DSL: https://github.com/jenkinsci/job-dsl-plugin/wiki

import jobs.generation.*;

// The input project name (e.g. dotnet/corefx)
def projectName = GithubProject
// The input branch name (e.g. master)
def branchName = GithubBranchName
// Folder that the project jobs reside in (project/branch)
def projectFoldername = Utilities.getFolderName(projectName) + '/' + Utilities.getFolderName(branchName)

// Email the results of aborted / failed jobs to our infrastructure alias
static void addEmailPublisher(def myJob) {
  myJob.with {
    publishers {
      extendedEmail('mlinfraswat@microsoft.com', '$DEFAULT_SUBJECT', '$DEFAULT_CONTENT') {
        // trigger(trigger name, subject, body, recipient list, send to developers, send to requester, include culprits, send to recipient list)
        trigger('Aborted', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, false, false, false, true)
        trigger('Failure', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, false, false, false, true)
      }
    }
  }
}

// Calls a web hook on Jenkins build events.  Allows our build monitoring jobs to be push notified
// vs. polling
static void addBuildEventWebHook(def myJob) {
  myJob.with {
    notifications {
      endpoint('https://jaredpar.azurewebsites.net/api/BuildEvent?code=tts2pvyelahoiliwu7lo6flxr8ps9kaip4hyr4m0ofa3o3l3di77tzcdpk22kf9gex5m6cbrcnmi') {
        event('all')
      }
    }
  }   
}

// Generates the standard trigger phrases.  This is the regex which ends up matching lines like:
//  test win32 please
static String generateTriggerPhrase(String jobName, String opsysName, String triggerKeyword = 'this') {
    return "(?i).*test\\W+(${jobName.replace('_', '/').substring(7)}|${opsysName}|${triggerKeyword}|${opsysName}\\W+${triggerKeyword}|${triggerKeyword}\\W+${opsysName})\\W+please.*";
}

static void addRoslynJob(def myJob, String jobName, String branchName, Boolean isPr, String triggerPhraseExtra, Boolean triggerPhraseOnly = false) {
  def archiveSettings = new ArchivalSettings()
  archiveSettings.addFiles('Binaries/**/*.pdb')
  archiveSettings.addFiles('Binaries/**/*.xml')
  archiveSettings.addFiles('Binaries/**/*.log')
  archiveSettings.addFiles('Binaries/**/*.dmp')
  archiveSettings.addFiles('Binaries/**/*.zip')
  archiveSettings.addFiles('Binaries/**/*.png')
  archiveSettings.addFiles('Binaries/**/*.xml')
  archiveSettings.excludeFiles('Binaries/Obj/**')
  archiveSettings.excludeFiles('Binaries/Bootstrap/**')
  archiveSettings.excludeFiles('Binaries/**/nuget*.zip')
  // Only archive if failed/aborted
  archiveSettings.setArchiveOnFailure()
  archiveSettings.setFailIfNothingArchived()
  Utilities.addArchival(myJob, archiveSettings)

  // Create the standard job.  This will setup parameter, SCM, timeout, etc ...
  def projectName = 'dotnet/roslyn'
  def defaultBranch = "*/${branchName}"
  Utilities.standardJobSetup(myJob, projectName, isPr, defaultBranch)

  // Need to setup the triggers for the job
  if (isPr) {
    def triggerCore = "open|all|${jobName}"
    if (triggerPhraseExtra) {
      triggerCore = "${triggerCore}|${triggerPhraseExtra}"
    }
    def triggerPhrase = "(?i)^\\s*(@?dotnet-bot\\s+)?test\\s+(${triggerCore})(\\s+please)?\\s*\$";
    def contextName = jobName
    Utilities.addGithubPRTriggerForBranch(myJob, branchName, contextName, triggerPhrase, triggerPhraseOnly)
  } else {
    Utilities.addGithubPushTrigger(myJob)
    addEmailPublisher(myJob)
  }

  addBuildEventWebHook(myJob)
}

// True when this is a PR job, false for commit.  On feature branches we do PR jobs only. 
def commitPullList = [false, true]
if (branchName.startsWith("features/")) {
  commitPullList = [true]
} 

// Windows     
commitPullList.each { isPr -> 
  ['debug', 'release'].each { configuration ->
        ['unit32', 'unit64'].each { buildTarget ->
      def jobName = Utilities.getFullJobName(projectName, "windows_${configuration}_${buildTarget}", isPr)
            def myJob = job(jobName) {
        description("Windows ${configuration} tests on ${buildTarget}")
                  steps {
                    batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd ${(configuration == 'debug') ? '/debug' : '/release'} ${(buildTarget == 'unit32') ? '/test32' : '/test64'}""")
                  }
                }

      def triggerPhraseOnly = false
      def triggerPhraseExtra = ""
      Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
      Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
      addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
    }
  }
}

// Linux
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "linux_debug", isPr)
  def myJob = job(jobName) {
    description("Linux tests")
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
                }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "linux"
  Utilities.setMachineAffinity(myJob, 'Ubuntu14.04', 'latest-or-auto')
  Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Mac
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "mac_debug", isPr)
  def myJob = job(jobName) {
    description("Mac tests")
                  label('mac-roslyn')
                  steps {
                    shell("./cibuild.sh --nocache --debug")
                  }
            }

  def triggerPhraseOnly = true
  def triggerPhraseExtra = "mac"
  Utilities.addXUnitDotNETResults(myJob, '**/xUnitResults/*.xml')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
  }

// Determinism
commitPullList.each { isPr -> 
  def jobName = Utilities.getFullJobName(projectName, "windows_determinism", isPr)
  def myJob = job(jobName) {
    description('Determinism tests')
    label('windows-roslyn')
    steps {
      batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
.\\cibuild.cmd /testDeterminism""")
    }
  }

  def triggerPhraseOnly = true
  def triggerPhraseExtra = "determinism"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Perf Correctness
commitPullList.each { isPr ->
  def jobName = Utilities.getFullJobName(projectName, "perf_correctness", isPr)
  def myJob = job(jobName) {
    description('perf test correctness')
    label('windows-roslyn')
    steps {
      batchFile(""".\\cibuild.cmd /testPerfCorrectness""")
    }
  }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "perf-correctness"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Microbuild
commitPullList.each { isPr ->
  def jobName = Utilities.getFullJobName(projectName, "microbuild", isPr)
  def myJob = job(jobName) {
    description('MicroBuild test')
    label('windows-roslyn')
    steps {
      batchFile(""".\\src\\Tools\\MicroBuild\\cibuild.cmd""")
    }
  }

  def triggerPhraseOnly = false
  def triggerPhraseExtra = "microbuild"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

// Open Integration Tests
commitPullList.each { isPr ->
  def jobName = Utilities.getFullJobName(projectName, "open-vsi", isPr)
  def myJob = job(jobName) {
    description('open integration tests')
    label('auto-win2012-20160912')
    steps {
      batchFile("""set TEMP=%WORKSPACE%\\Binaries\\Temp
mkdir %TEMP%
set TMP=%TEMP%
set VS150COMNTOOLS=%ProgramFiles(x86)%\\Microsoft Visual Studio\\VS15Preview\\Common7\\Tools\\
.\\cibuild.cmd /debug /testVsi""")
    }
  }

  def triggerPhraseOnly = true
  def triggerPhraseExtra = "open-vsi"
  Utilities.setMachineAffinity(myJob, 'Windows_NT', 'latest-or-auto-dev15-preview5')
  addRoslynJob(myJob, jobName, branchName, isPr, triggerPhraseExtra, triggerPhraseOnly)
}

JobReport.Report.generateJobReport(out)

// Make the call to generate the help job
Utilities.createHelperJob(this, projectName, branchName,
    "Welcome to the ${projectName} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
