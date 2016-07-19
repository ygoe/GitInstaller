using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyProduct("GitInstaller")]
[assembly: AssemblyTitle("GitInstaller")]
[assembly: AssemblyDescription("Installs and configures Git, LFS and TortoiseGit.")]
[assembly: AssemblyCopyright("© 2016 Yves Goergen")]
[assembly: AssemblyCompany("unclassified software development")]

// Assembly identity version. Must be a dotted-numeric version.
[assembly: AssemblyVersion("1.0")]

// Repeat for Win32 file version resource because the assembly version is expanded to 4 parts.
[assembly: AssemblyFileVersion("1.0")]

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
// 1.0 (2016-07-19)
// * Created project
