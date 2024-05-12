# Welcome to CTFAK 2.0 (UnEx)!
By Kostya and Yunivers

[Discord](https://www.discord.com/invite/wsH3KNtvvJ)
| Table of Contents | Description |
|--|--|
| [What is CTFAK UnEx?](https://github.com/AITYunivers/CTFAK-UnEx#what-is-ctfak-unex) | A short description of what CTFAK UnEx is and why it exists. |
| [Installation](https://github.com/AITYunivers/CTFAK-UnEx#installation) | How to install a precompiled version of CTFAK UnEx. |
| [Compilation](https://github.com/AITYunivers/CTFAK-UnEx#compilation) | How to compile CTFAK UnEx manually. |
| [Usage](https://github.com/AITYunivers/CTFAK-UnEx#usage) | How to use CTFAK UnEx. |
| [Parameters](https://github.com/AITYunivers/CTFAK-UnEx#parameters) | All CTFAK UnEx parameters. |
| [Command Arguments](https://github.com/AITYunivers/CTFAK-UnEx#command-arguments) | All CTFAK UnEx command arguments. |
| [Full Credits](https://github.com/AITYunivers/CTFAK-UnEx#full-credits) | Everyone who helped make CTFAK UnEx a reality. |

# What is CTFAK UnEx?
CTFAK UnEx (Standing for **C**lick**T**eam **F**usion **A**rmy **K**nife **Un**finished **Ex**periment) is an experimental version of a tool developed by Kostya with help from Yunivers which can be used to either decompile or dump assets of games made with the Clickteam Fusion 2.5 game engine.

CTFAK UnEx exists only to store a version of CTFAK 2.0 I most likely will never finish and never publish on the real repo due to lack of stability.

Please check out the original repository for [CTFAK 2.0](https://github.com/CTFAK/CTFAK2.0). If you need to post issues, put them in there, and if you have issues with this version, use the original.

# Installation
## Dependencies
CTFAK UnEx requires [.NET 6.0's Runtime, Core Runtime, and Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).

After running the x64 installers for all 3 runtimes, you may proceed with the installation.
## Installing a precompiled artifact

To install an artifact, you must be logged into a Github account, then you must make your way over to [Actions](https://github.com/AITYunivers/CTFAK-UnEx/actions), and from there select the latest workflow. On that page, if you scroll down you should find `Artifacts`, from there just click on `CTFAK` and it will start downloading.

From here, make your way over to [Usage](https://github.com/AITYunivers/CTFAK-UnEx#usage).

# Compilation
## Dependencies
CTFAK UnEx requires [.NET 6.0's Runtime, Core Runtime, and Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).

After running the x64 installers for all 3 runtimes, you may proceed with the compilation.

## Cloning the repo with Visual Studio 2022

Make sure you have [Visual Studio 2022](https://visualstudio.microsoft.com/) installed and open.

On the GitHub branch, click `Code` and copy the HTTPS URL.

In Visual Studio 2022, under `Get started`, click `Clone a repository`, then paste the HTTPS URL from earlier. Input your desired path and press `Clone`.

## Compiling CTFAK UnEx

**Compiling CTFAK is not recommended.** Please go to [installation](https://github.com/AITYunivers/CTFAK-UnEx#installation) to download CTFAK precompiled.

Compiling CTFAK does not get you newer features compared to the actions.

If you'd like to compile CTFAK anyway, right click the solution on the right and press `Build Solution` or do it through the key bind `Control + Shift + B`, then right click the solution once again and press `Open Folder in File Explorer`.

Open the `build` folder and you should be able to run `CTFAK.Cli.exe` without problems!

# Usage
CTFAK UnEx is very easy to use and requires little input from the user.

To get started, open `CTFAK.Cli.exe` and drag in your Clickteam Fusion 2.5 exe, apk, ccn, dat, bin, or mfa file and press enter.

In parameters, you can input anything listed in [Parameters](https://github.com/AITYunivers/CTFAK-UnEx#parameters), but make sure to put a `-` before each one. If you don't want to input any parameters (which you normally shouldn't need to do) then you can leave it blank. After you've filled out your parameters, press enter.

If you're using a ccn, dat, or bin file it will bring up a prompt asking you to select a file reader. In any case, press `1` for CCN.

After these steps, it will start reading the application. If it closes or gives an error during this process, run `CTFAK.Cli.exe` in command prompt, repeat the process, and then send the error in our [Discord](https://www.discord.com/invite/wsH3KNtvvJ), try the original [CTFAK 2.0](https://github.com/CTFAK/CTFAK2.0), or [open an issue on the original repository](https://github.com/CTFAK/CTFAK2.0/issues).

If all goes according to plan, you should see a screen saying `Reading finished in _ seconds` along with some information about the game. From here you may run any plugins you have installed. Normal installations should have `Export as MFA`, `Dump Everything`, `Image Dumper`, `Sound Dumper`, `Packed Data Dumper`, and `Sorted Image Dumper`.

Do not use the `Restart CTFAK` option as it is non-functional. Please remember that this is an unfinished version of CTFAK.

If you run into any issues with those 6 plugins, you may send the error in our [Discord](https://www.discord.com/invite/wsH3KNtvvJ), try the original [CTFAK 2.0](https://github.com/CTFAK/CTFAK2.0), or [open an issue on the original repository](https://github.com/CTFAK/CTFAK2.0/issues). If the plugin is not on that list, we cannot troubleshoot it for you.

Finally, you may close CTFAK UnEx and find any outputs your plugins gave, in the `Dumps` folder.

# Parameters
All parameters should start with `-`.
| Parameter | Description |
|--|--|
| onlyimages | Prevents CTFAK UnEx from reading any data unrelated to images. |
| noimg | Prevents CTFAK UnEx from reading any images. |
| noevnt | Prevents CTFAK UnEx from reading any events. |
| nosounds | Prevents CTFAK UnEx from reading any sounds. |
| noalpha | Prevents CTFAK UnEx from reading any alpha on images. |
| sorteddumpstrings | Has CTFAK UnEx dump strings to text files alongside images in the Sorted Image Dumper. |
| srcexp | Forces CTFAK UnEx to read a Source Explorer output. The unsorted output should be in a newly made `ImageBank` folder within your CTFAK 2.0 folder. |
| notrans | Prevents CTFAK UnEx from applying Alpha, Color, or Shaders to objects. |
| noicons | Prevents CTFAK UnEx from writing any object icons. |
| trace_chunks | Forces CTFAK UnEx to write all chunks to `CHUNK_TRACE`. |
| dumpnewchunks | Forces CTFAK UnEx to write chunks without a reader to `UnkChunks`. You must create this folder yourself. |
| f1.5 | Forces CTFAK UnEx to read the input as MMF 1.5. |
| f3 | Forces CTFAK UnEx to read the input as CTF 3.0. |
| android | Forces CTFAK UnEx to read the input as android. |
| excludeframe([id]) | Forces CTFAK UnEx to ignore the specified frame. ID indexes at 0. |
| log | Causes CTFAK UnEx to log thread information about the Sorted Image Dumper. |
| badblend | Forces CTFAK UnEx to revert to the old blend coeff fix. |
| chunk_info | Has CTFAK UnEx log the size and offset of chunks. Also logs effects. |

# Command Arguments
These are command arguments for batch files or running CTFAK through cmd.
All arguments should start with `-` and should be followed up by data wrapped in quotations if data is required for said argument.
| Argument | Description |
|--|--|
| path | Automatically starts reading the inputted file path. |
| ____ | Leaving the arguments blank allows you to do the same thing as '-path'. |
| parameters | Allows you to input parameters that CTFAK UnEx will read. |
| forcetype | Forces the kind of file type the file will be read as. Options: 'exe', 'apk', 'ccn', 'mfa' |
| tool | Uses the name of a plugin (such as 'Export as MFA') to run said tool as soon as it's done reading. |
| closeonfinish | Closes CTFAK UnEx after it finishes reading, or if you are using '-tool', after it finishes running the plugin. |

# Full Credits
|Name| Credit for... |
|--|--|
| [Mathias Kaerlev](https://github.com/matpow2) | Developer of Anaconda Mode 3. |
| [Kostya](https://github.com/1987kostya1) | Developer of CTFAK and CTFAK 2.0. |
| [Yunivers](https://github.com/AITYunivers) | Developer of CTFAK 2.0 and CTFAK UnEx. |
| [Slxdy](https://github.com/Slxdy) | Assistant developer of CTFAK 2.0. |
| [RED_EYE](https://github.com/REDxEYE) | Developer of the decryption library. |
| [LAK132](https://github.com/LAK132) | Coding help for the Image Bank rewrite. |
| [Liz](https://github.com/lily-snow-9) | Coding help for Child Events, Sub-App port. |

CTFAK 2.0 and CTFAK UnEx is licensed under [AGPL-3.0](https://github.com/CTFAK/CTFAK2.0/blob/master/LICENSE).

Last Updated May 12th, 2024.
