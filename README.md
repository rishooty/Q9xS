# Quick 9x Slipstreamer

A utility designed to ease the process of slipstreaming updates into a Windows 95/98/ME iso.

## Why

It's possible to slipstream some updates into Windows 9x series OSes so long as they match
either files that already exist on the disc or within its .CAB files. The chance of 
success increases if you modify the layout.inf files to match your updates.

However, I found the process of extracting the iso, dropping the files, modifying the layout*.inf files 
and recompressing a bootable iso tedious.

As for "Why Windows 9x?", yes there are people that still use these OSes. Namely, vintage gaming enthusiasts.

## Doesn't this exist already?
Yes, but it's out of date and won't run on modern OSes. I've tried to employ the similar method of directly
slipstreaming into the .CAB files, but it didn't work out for a number of reasons. 
	
* The best method I found involved using external tools and wasn't exactly cross-platform. 
* All of the available C# libraries couldn't extract split cabs individually.
* Driver related updates don't need to match existing files, and would be missed if I used the above method. MSBATCH.inf included.
* Encouraging people to automatically update a large mass of files didn't seem like a good idea in general. Slipstreaming is 
  already finicky as it is, so the user should know exactly which files they're updating and why.

## Getting Started

### Prerequisites

#### User

* Windows 7 or higher
	* [.NET Core Runtimes](https://dotnet.microsoft.com/download/dotnet-core/2.1)
* Linux with .NET Core 2.1 support. See [here](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x) for details.

#### Dev
* [Visual Studio 2017 15.7 or higher](https://visualstudio.microsoft.com/vs/)
* [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/2.1)

### Installation
Download the latest release [here](https://github.com/rishooty/Q9xS/releases), and extract it to the directory of your choice.

## Preparation

Create a directory in which to extract all your updates. No subdirectories, zips, cabs, or even self-extracting
exes should be in this folder. Use [7-zip](https://www.7-zip.org/) or a similar archive utility to extract them.

Take Q216204.exe for example, an unofficial update which fixes detection of some Intel cpus.
Dropping the file itself into this directory wouldn't work, but extracting its contents would.

Keep and overwrite files in this folder as you see fit. You may want the latest versions,
or you may want to keep a specific version of a given file. Just make sure that everything
is a plain file or executable.

## Running

Windows:
```
dotnet Q9xS.dll updatesDirectory isoPath bootImagePath
```

Linux:
```
dotnet ./Q9xS.dll updatesDirectory isoPath bootImagePath
```

Example:
```
dotnet Q9xS.dll C:\9x\updates "C:\9x\Windows 98 Second Edition.iso" 
```

When it's finished, an updated iso will be in the same directory as the executable.
Because slipstreaming is so finicky, I would try it out in a virtual machine
before attempting it on the real thing. Chances are that if something goes wrong,
you just need to delete whichever file the installer gives you an error message for.
If not that, something related to it.

Have fun customizing your 9x discs!

## Caveats ##
This hasn't been tested with windows editions other than the latest of each,
and the way it generates layouts is based on them. That being said,
there's no guaruntee editions of 95 or 98 other than 95OSR2.5 and 98SE
will work.

If you do run into issues, extract the layout*.inf files from
```
extractedDiscFiles\precopy2.cab
```
and overwrite the existing ones in
```
layouts\win95 (or \win98 or \win9x)
```

I would have had it extract it straight from the cab, but again
there's no real cross platform way to do this that doesn't rely
on external tools on a per-os basis.

There are also some files which are both very sensitive to having their layout.inf
differ from their actual filsize and can't have their filesizes read correctly
by this application. For these files, you either want to delete them
or update their entries in layout.inf manually.

Before:
```
SETUPPP.INF=2,,44520
```
After
```
SETUPPP.INF=2,,4452

You can find this value by right clicking the file, opening its properties, and looking at its size
in bytes.

Finally, there are files which simply will not slipstream properly, even after fixing
it's filesize value in layout.inf. The only solution is to delete these from both the updates
and extracted iso folders. You can easily recognize them by related errors coming up during
installation. Most often these are false positives and don't actually stop your installation,
but better safe than sorry.

### List of Known Layout Sensitive Files

* SETUPPP.INF

### List of Known Slipstream Incompatible Files 

* rpcrt4.dll

## Built With

* .NET Core 2.1
* Visual Studio 16.2.3
* [DiscUtils](https://github.com/DiscUtils/DiscUtils)
* [Markdown by Hey-Red](https://github.com/hey-red/Markdown)

## Author

**Nicholas Ricciuti** - [rishooty](https://github.com/rishooty)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
