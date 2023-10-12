using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using BymlView;
using LibBlitz.Lp.Byml;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.Tools.FsSystem;
using ZstdSharp;

class MngEdit
{
    private static object infoweapon;

    public static void Main()
    {

        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
        for (int i = 0; i < files.Length; i++) {
            files[i] = files[i].Split("\\")[^1];
        } 

        string[] infos = new string[100];

        int inf = 0;
        for (int i = 0; i < files.Length; i++) {
            if (files[i].StartsWith("WeaponInfo")) {
                infos[inf] = files[i];
                inf++;
            }
        }

        string[] linesTemp = File.ReadAllLines("config.txt");
        List<string> lines = new List<string>(linesTemp);
        for (int line = 0; line < lines.Count; line++) {
            if (lines[line].StartsWith("#") || string.IsNullOrEmpty(lines[line])) {
                lines.RemoveAt(line);
                line--;
            }
        }

        Dictionary<string, string> settings = new Dictionary<string, string>();

        for (int line = 0; line < lines.Count; line++) {
            string[] tempset = lines[line].Split("=");
            settings.Add(tempset[0], tempset[1]);
        }

        var src = File.ReadAllBytes(infos[0]);
        using var decompressor = new Decompressor();
        var decompressed = decompressor.Unwrap(src);

        var mem = new MemoryStorage(decompressed.ToArray());
		Byml WeaponInfoMain = new(mem);
        Byml WeaponInfoMain2 = null;
        Byml WeaponInfoSub = null;
        Byml WeaponInfoSpecial = null;

        if (settings["type"] == "obtain") {
            var srcM2 = File.ReadAllBytes(infos[1]);
            using var decompressorM2 = new Decompressor();
            var decompressedM2 = decompressorM2.Unwrap(srcM2);
            
            var mem2 = new MemoryStorage(decompressedM2.ToArray());
            WeaponInfoMain2 = new(mem2);
        } else {
            var srcSpecial = File.ReadAllBytes(infos[1]);
            using var decompressorSpecial = new Decompressor();
            var decompressedSpecial = decompressorSpecial.Unwrap(srcSpecial);
            
            var memSpecial = new MemoryStorage(decompressedSpecial.ToArray());
            WeaponInfoSpecial = new(memSpecial);

            var srcSub = File.ReadAllBytes(infos[2]);
            using var decompressorSub = new Decompressor();
            var decompressedSub = decompressorSub.Unwrap(srcSub);
            
            var memSub = new MemoryStorage(decompressedSub.ToArray());
            WeaponInfoSub = new(memSub);
        }
    

        int classnum = 15; // Only for display, loads up to 15 classes
        List<string>[] weapons = new List<string>[classnum];
        for (int x = 0; x < classnum; x++) {
            weapons[x] = new List<string>();
        }
        List<string> subs = new List<string>();
        List<string> specials = new List<string>();

        List<string> weaponcomparison1 = new List<string>();
        List<string> weaponcomparison2 = new List<string>();
        Dictionary<string, int> PFS = new Dictionary<string, int>();

        List<KeyValuePair<dynamic, dynamic>> nodeschanged = new List<KeyValuePair<dynamic, dynamic>>();

        //Console.WriteLine((byml.Root as BymlArrayNode).Length);

        if (settings["type"] == "obtain") {
            for (int i = 0; i < (WeaponInfoMain.Root as BymlArrayNode).Length; i++) {
                dynamic node = (WeaponInfoMain.Root as BymlArrayNode)[i];

                // Check the first part of the name (__RowId)
                string first = node["__RowId"].Data;
                weaponcomparison1.Add(first);
            }

            for (int i = 0; i < (WeaponInfoMain2.Root as BymlArrayNode).Length; i++) {
                dynamic node = (WeaponInfoMain2.Root as BymlArrayNode)[i];

                // Check the first part of the name (__RowId)
                string first = node["__RowId"].Data;
                weaponcomparison2.Add(first);
            }

            for (int j = 0; j < (WeaponInfoMain2.Root as BymlArrayNode).Length; j++) {
                dynamic node = (WeaponInfoMain2.Root as BymlArrayNode)[j];
                int first = node["SpecialPoint"].Data;
                PFS.Add(weaponcomparison2[j], first);
            }
        } else {
            for (int i = 0; i < (WeaponInfoMain.Root as BymlArrayNode).Length; i++) {
                dynamic node = (WeaponInfoMain.Root as BymlArrayNode)[i];

                // Check the first part of the name (__RowId)
                string first = node["__RowId"].Data;
                first = first.Remove(first.Contains('_') ? first.IndexOf("_") : first.Length);

                // Compare it to the list of enums using GetName and a for loop
                for (int q = 0; q < Enum.GetNames(typeof(WeaponType)).Length; q++) {
                    // If it matches, add it to the list of things
                    if (first.Contains(Enum.GetName(typeof(WeaponType), q))) {
                        List<string> tempweapon = weapons[q];
                        if (!((node["__RowId"].Data.EndsWith("_Coop") && !node["__RowId"].Data.Contains("Bear")) || node["__RowId"].Data.EndsWith("Msn")))
                            tempweapon.Add(node["__RowId"].Data); 
                            weapons[q] = tempweapon;
                    }
                }
            }

            for (int j = 0; j < (WeaponInfoSub.Root as BymlArrayNode).Length; j++) {
                dynamic nodeSub = (WeaponInfoSub.Root as BymlArrayNode)[j];
                string name = nodeSub["__RowId"].Data;
                if (!(
                    (settings["herosubs"] == "true" ? false : nodeSub["__RowId"].Data.EndsWith("_Hero")) || 
                    nodeSub["__RowId"].Data.EndsWith("_Mission") || 
                    (settings["bombsplashbig"] == "true" ? false : nodeSub["__RowId"].Data.EndsWith("_Coop")) || 
                    nodeSub["__RowId"].Data.EndsWith("_Rival") ||
                    nodeSub["__RowId"].Data.EndsWith("Buddy")) || settings["illegalsubs"] == "true") {
                    subs.Add(name);
                    Console.WriteLine("Sub: " + name);
                }
            }

            for (int j = 0; j < (WeaponInfoSpecial.Root as BymlArrayNode).Length; j++) {
                dynamic nodeSpec = (WeaponInfoSpecial.Root as BymlArrayNode)[j];
                string name = nodeSpec["__RowId"].Data;
                if (!(
                    (settings["splashdown"] == "true" ? false : nodeSpec["__RowId"].Data.EndsWith("SuperLanding")) ||
                    (settings["unintendedspecs"] == "true" ? false : (nodeSpec["__RowId"].Data.EndsWith("Gachihoko") || nodeSpec["__RowId"].Data.EndsWith("IkuraShoot"))) ||
                    nodeSpec["__RowId"].Data.EndsWith("_Coop") || 
                    nodeSpec["__RowId"].Data.EndsWith("_Mission") || 
                    nodeSpec["__RowId"].Data.EndsWith("_Rival")) || settings["illegalspecials"] == "true") {
                    specials.Add(name);
                    Console.WriteLine("Special: " + name);
                }
            }
        }

        if (settings["type"] == "edit") {
            bool working = true;
            while (working) {
                Console.WriteLine("What class would you like to edit?");
                for (int classes = 0; classes < Enum.GetNames(typeof(WeaponType)).Length; classes++) {
                    Console.WriteLine(classes + " => " + Enum.GetName(typeof(WeaponType), classes));
                }
                int type = Int32.Parse(Console.ReadLine());

                Console.WriteLine("------------------\nWhat weapon would you like to edit?");
                for (int weapon = 0; weapon < weapons[type].Count; weapon++) {
                    if (!((weapons[type][weapon].EndsWith("_Coop") && !weapons[type][weapon].Contains("Bear")) || weapons[type][weapon].EndsWith("Msn")))
                        Console.WriteLine(weapon + " => " + weapons[type][weapon]);
                }
                int selected = Int32.Parse(Console.ReadLine());

                Console.WriteLine("------------------\nWhat sub would you like to give the weapon?");
                for (int sub = 0; sub < subs.Count; sub++) {
                    Console.WriteLine(sub + " => " + subs[sub]);
                }
                int subselect = Int32.Parse(Console.ReadLine());

                Console.WriteLine("------------------\nWhat special would you like to give the weapon?");
                for (int spec = 0; spec < specials.Count; spec++) {
                    Console.WriteLine(spec + " => " + specials[spec]);
                }
                int specselect = Int32.Parse(Console.ReadLine());

                Console.WriteLine("------------------\nHow many points for special?");
                int pfs = Int32.Parse(Console.ReadLine());

                dynamic root = (BymlArrayNode) WeaponInfoMain.Root;

                for (int i = 0; i < (WeaponInfoMain.Root as BymlArrayNode).Length; i++) {
                    dynamic node = (WeaponInfoMain.Root as BymlArrayNode)[i];
                    if (node["__RowId"].Data == weapons[type][selected]) {
                        node["SpecialWeapon"].Data = "Work/Gyml/" + specials[specselect] + ".spl__WeaponInfoSpecial.gyml";
                        node["SubWeapon"].Data = "Work/Gyml/" + subs[subselect] + ".spl__WeaponInfoSub.gyml";
                        node["SpecialPoint"].Data = pfs;
                        break;
                    }
                }

                Console.Write("------------------\nSaving...");
                save(WeaponInfoMain, infos[0]);
                Console.WriteLine(" Saved!\n------------------");
            }
        } else if (settings["type"] == "random") {
            Random randomizer = new Random();

            for (int i = 0; i < (WeaponInfoMain.Root as BymlArrayNode).Length; i++) {
                dynamic node = (WeaponInfoMain.Root as BymlArrayNode)[i];
                node["SpecialWeapon"].Data = "Work/Gyml/" + specials[randomizer.Next(0, specials.Count)] + ".spl__WeaponInfoSpecial.gyml";
                node["SubWeapon"].Data = "Work/Gyml/" + subs[randomizer.Next(0, subs.Count)] + ".spl__WeaponInfoSub.gyml";
                node["SpecialPoint"].Data = (randomizer.Next(1, 5) * 10) + 170;
            }

            Console.Write("------------------\nSaving...");
            save(WeaponInfoMain, infos[0]);
            Console.WriteLine(" Saved!\n------------------");
        } else if (settings["type"] == "obtain") {
           String[] comparison = weaponcomparison2.Except(weaponcomparison1).ToArray();            
            for (int i = 0; i < comparison.Length; i++) {
                if (!(comparison[i].EndsWith("_Coop") && !(comparison[i].Contains("_Bear"))))
                    Console.WriteLine(comparison[i] + " - " + PFS[comparison[i]] + "p");
            }  

            //Console.ReadLine();
            //System.Environment.Exit(1);
        } else {
            Console.WriteLine("Incorrect type! Please use the correct type when inputting into MngEdit.");
        }
    }

    public static void save(Byml infoweapon, String path) {
        BymlWriter writer = new();
        writer.PushIter(infoweapon.Root);

        using (Stream outstream = new FileInfo(path).Create()) {
            writer.Write(outstream);
        }

        var srcTemp = File.ReadAllBytes(path);
        using var compressor = new Compressor();
        var compressed = compressor.Wrap(srcTemp);

        File.WriteAllBytes(path, compressed.ToArray());
        var upmem = new MemoryStorage(srcTemp.ToArray());
        infoweapon = new(upmem);

    }
}

enum WeaponType {
    Blaster,
    Brush,
    Charger,
    Maneuver,
    Roller,
    Saber,
    Shelter,
    Shooter,
    Slosher,
    Spinner,
    Stringer
}