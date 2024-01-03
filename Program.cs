using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace UnturnedHwidUnbanner
{
    internal class Program
    {
        private static readonly Random Random = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("Unturned Server Spoofer\nby Julson\n");

            if (!IsAdministrator())
            {
                Console.WriteLine("Please run as administrator");
                Console.ReadKey();
                return;
            }

            bool exceptionThrown = false;

            try
            {
                Console.WriteLine("Modifying registry GUID...");
                ModifyRegistryGuid();
                Console.WriteLine("Done.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong while changing GUID\n" + ex.ToString());
                exceptionThrown = true;
            }

            try
            {
                Console.WriteLine("Modifying registry game data...");
                ModifyGameCloudHash();
                Console.WriteLine("Done.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong while changing cloud registry\n" + ex.ToString());
                exceptionThrown = true;
            }

            try
            {
                Console.WriteLine("Modifying game stored info...");
                ModifyGameStoredInfo();
                Console.WriteLine("Done.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong while changing JSON\nThis might be an issue if the file doesn't exist\n" + ex.ToString());
                exceptionThrown = true;
            }

            if (!exceptionThrown)
                Console.WriteLine("You're all spoofed! Now you just need a VPN and a new account.\nPress any key or close");

            Console.ReadKey();
        }

        private static bool IsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void ModifyRegistryGuid()
        {
            var guidKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Microsoft\\Cryptography", true);
            string oldGuid = guidKey.GetValue("MachineGuid") as string;
            string newGuid = GetNewGUID(oldGuid, out bool fitsPattern);
            if (!fitsPattern)
                Console.Write("Your current HWID is either zero'd or doesn't fit the HWID pattern. Result may be incorrect");
            guidKey.SetValue("MachineGuid", newGuid);
            Console.WriteLine($"{oldGuid} -> {newGuid}");
        }

        private static void ModifyGameCloudHash()
        {
            var cloudKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey("SOFTWARE\\Smartly Dressed Games\\Unturned", true);
            byte[] oldCloud = cloudKey.GetValue("CloudStorageHash_h1878083263") as byte[];
            string asString = GetStringFromBytes(oldCloud);
            string newCloud = RandomString(asString.Length);
            byte[] newCloudBytes = GetBytesFromString(newCloud);
            cloudKey.SetValue("CloudStorageHash_h1878083263", newCloudBytes);
            Console.WriteLine($"{asString} -> {newCloud}");
        }

        private static void ModifyGameStoredInfo()
        {
            bool didChangeAnything = false;
            string dir = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 304930", false)
                .GetValue("InstallLocation") as string;

            if (File.Exists(Path.Combine(dir, "Cloud", "ConvenientSavedata.json")))
            {
                funkyclass c = JsonConvert.DeserializeObject<funkyclass>(File.ReadAllText(Path.Combine(dir, "Cloud", "ConvenientSavedata.json")));
                if (c.Strings.TryGetValue("ItemStoreCache", out string str))
                {
                    string newCache = RandomString(str.Length);
                    c.Strings["ItemStoreCache"] = newCache;
                    File.WriteAllText(Path.Combine(dir, "Cloud", "ConvenientSavedata.json"), JsonConvert.SerializeObject(c, Formatting.Indented));
                    didChangeAnything = true;
                    Console.WriteLine($"{str} -> {newCache}");
                }
            }

            if (!didChangeAnything)
                Console.WriteLine("No changes needed");

        }

        private static string GetNewGUID(string old, out bool fitsPattern)
        {
            fitsPattern = false;
            if (!String.IsNullOrEmpty(old))
            {
                string[] split = old.Split('-');
                if (split.Length > 0)
                {
                    split[0] = RandomString(split[0].Length);
                    fitsPattern = true;
                    return string.Join("-", split);
                }
            }
            return RandomString(36);
        }

        private static string RandomString(int length)
        {
            const string chars = "abcdef0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        private static string GetStringFromBytes(byte[] bytes)
        {
            return bytes.Where(b => b != 0).Aggregate("", (current, b) => current + (char)b);
        }

        private static byte[] GetBytesFromString(string str)
        {
            return str.Select(c => (byte)c).Concat(new byte[] { 0 }).ToArray();
        }
    }

    public class funkyclass
    {
        public Dictionary<string, string> Strings = new Dictionary<string, string>();
        public Dictionary<string, DateTime> DateTimes = new Dictionary<string, DateTime>();
        public Dictionary<string, bool> Booleans = new Dictionary<string, bool>();
        public Dictionary<string, long> Integers = new Dictionary<string, long>();
        public HashSet<string> Flags = new HashSet<string>();
    }
}
