using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyProduct("GitInstaller")]
[assembly: AssemblyTitle("GitInstaller")]
[assembly: AssemblyDescription("Installs or updates Git components on a developer computer.")]
[assembly: AssemblyCopyright("© 2016–2017 Yves Goergen")]
[assembly: AssemblyCompany("unclassified software development")]

// Assembly identity version. Must be a dotted-numeric version.
[assembly: AssemblyVersion("1.2.4")]

// Repeat for Win32 file version resource because the assembly version is expanded to 4 parts.
[assembly: AssemblyFileVersion("1.2.4")]

// Indicate the build configuration
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Other attributes
[assembly: ComVisible(false)]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

// Version history:
//
// 1.2.4 (2017-08-22)
// * Improved support for Visual Studio 2017
//
// 1.2.3 (2017-02-28)
// * Fixed: TortoiseGit Uninstall AppId has changed
//
// 1.2.2 (2016-12-07)
// * Fixed: Errors in clean system environment for first installation
//
// 1.2 (2016-09-13)
// * Updated Beyond Compare as patch file viewer for all languages (requires BC 4.1.8)
//
// 1.1 (2016-08-05)
// * Register Beyond Compare as image diff tool and diff/patch file viewer
// * Disable TortoiseGit spell checking
// * Integrated Git installer settings, doesn't need Git-Install-Settings.reg anymore
// * Support GitDiffMargin installer without version number (VS will update it automatically)
// * Show version in the main window
//
// 1.0 (2016-07-19)
// * Created project
