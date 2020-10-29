#! python

from subprocess import call

import subprocess
import fileinput
import re
import os
import sys
import getopt

if len(sys.argv) < 2:
    print("usage: ./update_version.py --major")
    print("or:    ./update_version.py --minor")
    print("or:    ./update_version.py --revision")
    sys.exit(1)

origPath = os.getcwd();
version_level = sys.argv[1];
valid_version_levels = ['--major', '--minor', '--revision']
if version_level not in valid_version_levels:
    print("Version level not recognised, must be one of")
    print(valid_version_levels)
    sys.exit(1)
    
if version_level in  ['--major', '--minor']:
    print('major and minor version levels not yet implemented')
    print('WARNING: for version levels > revision, you might need to change extra GUIDs in the installer')    

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
        
def get_short_sha():
    cmd = "git rev-parse --short HEAD"
    out = subprocess.check_output(cmd, shell=True)
    return out.decode("utf-8").strip()

def safeExit():
    print ("Exiting...")
    os.chdir(origPath)
    sys.exit(1)
    print("To reset state, you probably want to run: ")
    print("git reset --hard head")
#    safeProcess("git reset --hard head")

def update_assembly_version(filename):
    pattern = re.compile("\[assembly:\s*AssemblyVersion\(\"(\d*).(\d*).(\d*)\"\)\]")
    
    new_version = None
    for line in fileinput.input(filename, inplace=True):
        if re.search(pattern, line): 
            major = int(pattern.search(line).groups()[0])
            minor = int(pattern.search(line).groups()[1])
            revision = int(pattern.search(line).groups()[2])
            
            if version_level == "--major":
                major += 1
                minor = 0
                revision = 0
            elif version_level == "--minor":
                minor += 1
                revision = 0
            elif version_level == "--revision":
                revision += 1
            new_version =  "{}.{}.{}".format(major, minor, revision)
            line = re.sub(pattern, "[assembly: AssemblyVersion(\"{}\")]".format(new_version), line);        
        print(line.rstrip('\n'))
    if not new_version:
        raise Exception("Could not find version in file {}".format(filename))
    return new_version

def update_installer_version(filename, new_version):
    # AI has commandline tools for updating version, inc GUID
    if not safeProcess("AdvancedInstaller.com /edit {} /SetVersion {}".format(filename, new_version)):
        print("Failed updating version of AI project")
        safeExit()
    
# Don't continue if working copy is dirty
if not safeProcess('git diff-index --quiet HEAD --'):
    print( "Cannot continue, git working copy dirty")
    safeExit()
    
# Update the source files that OptiKey uses
version_file1 = 'src/JuliusSweetland.OptiKey.Core/Properties/AssemblyInfo.cs'
new_version = update_assembly_version(version_file1)

version_file2 = 'src/JuliusSweetland.OptiKey.Crayta/Properties/AssemblyInfo.cs'
new_version = update_assembly_version(version_file2)

# Update the SHA in the VisualsViewModel
# in git bash this can be a one-liner: 
# sed -i  "s/RELEASE_SHA/$("git rev-parse --short HEAD)/" src/JuliusSweetland.OptiKey.Crayta/UI/ViewModels/Management/AboutViewModel.cs
vvmFile = 'src/JuliusSweetland.OptiKey.Crayta/UI/ViewModels/Management/AboutViewModel.cs'
sha=get_short_sha() 
safeProcess("sed -i  \"s/RELEASE_SHA/{}/\" {}".format(sha, vvmFile))

# Commit changes
safeProcess("git add {}".format(version_file1))
safeProcess("git add {}".format(version_file2))
safeProcess("git add {}".format(vvmFile))

print("")
print("Updated version and SHA. Next please:")
print("(1) Update vdproj version # manually & commit changes")
print("(2) Run release.py ")    

