using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using MelonLoader;
using BuildInfo = BonelabMultiplayerMockup.BuildInfo;

[assembly: AssemblyTitle(BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(BuildInfo.Company)]
[assembly: AssemblyProduct(BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + BuildInfo.Author)]
[assembly: AssemblyTrademark(BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(BuildInfo.Version)]
[assembly: AssemblyFileVersion(BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly:
    MelonInfo(typeof(BonelabMultiplayerMockup.BonelabMultiplayerMockup), BuildInfo.Name, BuildInfo.Version,
        BuildInfo.Author)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame]