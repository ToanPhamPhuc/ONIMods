using System;
using System.Reflection;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace OxygenNotIncluded.Mods.ModTemplate
{
    public class Loader : UserMod2
    {
        public static AssemblyName AssemblyName => Assembly.GetExecutingAssembly().GetName();
        public static Version Version => AssemblyName.Version;
        public static string Name => AssemblyName.Name;

        public override void OnLoad(HarmonyLib.Harmony harmony)
        {
            base.OnLoad(harmony);

            PUtil.InitLibrary();
            new POptions().RegisterOptions(this, typeof(PlayerConfig));

            Console.WriteLine($"Mod <{Name}> loaded: {Version}");
        }
    }
}