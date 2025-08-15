#------------------------------[ HELP INFO ]-------------------------------

<#
.SYNOPSIS
Publishes the release version of the NuGet package to the NuGet Gallery.

.DESCRIPTION
Use this script to publish the latest release build of the library.

.INPUTS
None.

.OUTPUTS
None.

.EXAMPLE
.\publish.ps1
Execute script to publish the release build.
#>

#------------------------[ COMMAND-LINE SWITCHES ]-------------------------

[CmdletBinding(DefaultParameterSetName="default")]
param (
)

#------------------------[ CONFIGURABLE VARIABLES ]------------------------

# TODO: ADD SCRIPT VARIABLES THAT CAN BE OVERWRITTEN VIA THE CONFIG FILE.

#----------------------[ NON-CONFIGURABLE VARIABLES ]----------------------

# TODO: DEFINE SCRIPT VARIABLES THAT CANNOT BE OVERWRITTEN VIA THE CONFIG FILE.

#------------------------------[ CONSTANTS ]-------------------------------

# Unique identifier of this DotNetExtras library.
$AssemblyId = "Mail"

# The following conditions must be met for this script to work
# (substitute Xyz with the assembly ID from the $AssemblyId variable above):
# - This script is in the root of the solution.
# - The library project is in the XyzLib subfolder under the solution folder.
# - The library project is named XyzLib.
# - The library assembly is named DotNetExtras.Xyz.dll.
# - The library project uses the default directories for the release build.
# - The library project is built for .NET 8.
$AssemblyName = "DotNetExtras." + $AssemblyId
$SourceBaseDir = $PSScriptRoot
$ReleaseFolderPath = Join-Path $SourceBaseDir "${AssemblyId}Lib\bin\Release"
$AssemblyFolderPath = Join-Path $ReleaseFolderPath "net8.0"
$AssemblyPath  = Join-Path $AssemblyFolderPath "${AssemblyName}.dll"
$Source = "https://api.nuget.org/v3/index.json"

#--------------------------------------------------------------------------
# GetAssemblyVersion
#   Returns version of the assembly file.
function GetAssemblyVersion {
    [CmdletBinding()]
    param(
        $assemblyPath
    )

    $version = [System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version
    
    $major = $version.Major
    $minor = $version.Minor
    $build = $version.Build
    
    return "$major.$minor.$build"
}

#---------------------------------[ MAIN ]---------------------------------
# We will trap errors in the try-catch blocks.
$ErrorActionPreference = 'Stop'

# Make sure we have no pending errors.
$Error.Clear()

# MAIN SCRIPT LOGIC
$version = GetAssemblyVersion $AssemblyPath

$Nupkg = Join-Path $ReleaseFolderPath "$AssemblyName.$version.nupkg"

Write-Host "Publishing $Nupkg to NuGet Gallery..."
nuget push $Nupkg -Source $Source
Write-Host "Done."
# THE END
#--------------------------------------------------------------------------