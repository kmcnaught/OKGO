#! python

from subprocess import call

import subprocess
import fileinput
import re
import os
import sys
import getopt

origPath = os.getcwd();

def safeProcess( cmd ):
    "Run command, return boolean for success"
    print(cmd);
    try:
        out = subprocess.check_output(cmd, shell=True)
        print(out.decode("utf-8").replace("\\\n", "\n"))
        return True;
    except subprocess.CalledProcessError as e:                                                                                                   
        print("Status : FAIL", e.returncode, e.output)
        return False;
        
def safeExit():
    print ("Exiting...")
    os.chdir(origPath)
    sys.exit(1)
#    safeProcess("git reset --hard head")

def get_version(filename):
    pattern = re.compile('"ProductVersion" = "8:(\d+(\.\d+)+)"');
    for line in fileinput.input(filename):
        if re.search(pattern, line): 
            version = pattern.search(line).groups()[0]
            return version
    return None;        
  
# Don't continue if working copy is dirty
if not safeProcess('git diff-index --quiet HEAD --'):
    print( "Cannot build, git working copy dirty")
    safeExit()
    
# Build main project
# FYI if you're running this directly in git bash, you need to escape the forward slashes in the options (e.g. //Project)
eyemine = 'src/JuliusSweetland.OptiKey.EyeMine/JuliusSweetland.OptiKey.EyeMine.csproj'
build = 'devenv.com OptiKey.sln /Project {} /Build "Release"'.format(eyemine)
if not safeProcess(build):
    print("Error building project")
    safeExit()

# Build installer
installer_file = "installer/EyeMine.aip"
buildInstall = "AdvancedInstaller.com //rebuild {}".format(installer_file)
if not safeProcess(buildInstall):
    print("Error building installer")
    safeExit()

# Discard local changes to InstallerStrings (these are a build artefact)
if not safeProcess("git checkout src/JuliusSweetland.OptiKey.InstallerActions/InstallerStrings.cs"):
    print("Error checking out InstallerStrings.cs")
    safeExit()

# Tag code by version
version = get_version(vdproj)
safeProcess("git tag release/{}".format(version))



