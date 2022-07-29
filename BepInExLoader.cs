using BepInEx;
using UnhollowerRuntimeLib;
using HarmonyLib;
using RF5.HisaCat.Lib.LocalizedTextHelper;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RF5.HisaCat.EnumIDNameDumper
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class BepInExLoader : BepInEx.IL2CPP.BasePlugin
    {
        public const string
            MODNAME = "Lib.EnumIDNameDumper",
            AUTHOR = "HisaCat",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.0.0.0";

        public static BepInEx.Logging.ManualLogSource log;
        public BepInExLoader()
        {
            log = Log;
        }

        public override void Load()
        {
            try
            {
                Harmony.CreateAndPatchAll(typeof(AssetManagerHooker));
            }
            catch
            {
                log.LogError($"[{GUID}] FAILED to Register Il2Cpp Types!");
            }
        }

        [HarmonyPatch]
        public class AssetManagerHooker
        {
            public static UnityEngine.SystemLanguage SysLangToSystemLanguage(SysLang lang)
            {
                switch (lang)
                {
                    case SysLang.JP:
                        return UnityEngine.SystemLanguage.Japanese;
                    case SysLang.EN:
                        return UnityEngine.SystemLanguage.English;
                    case SysLang.ZH:
                        return UnityEngine.SystemLanguage.ChineseSimplified;
                    case SysLang.ZK:
                        return UnityEngine.SystemLanguage.ChineseTraditional;
                    case SysLang.KR:
                        return UnityEngine.SystemLanguage.Korean;
                    case SysLang.FR:
                        return UnityEngine.SystemLanguage.French;
                    case SysLang.DE:
                        return UnityEngine.SystemLanguage.German;

                    case SysLang.None:
                    case SysLang.Max:
                    default:
                        return 0;
                }
            }

            [System.Serializable]
            public class DumpData
            {
                public string IDType;
                public List<Value> Values;

                [System.Serializable]
                public class Value
                {
                    public string ID;
                    public List<LocalizedValue> LocalizedValues;

                    [System.Serializable]
                    public class LocalizedValue
                    {
                        public string Lang;
                        public string Name;
                    }
                }
            }

            private static bool IsInitialized = false;
            [HarmonyPatch(typeof(SV), nameof(SV.CreateUIRes))]
            [HarmonyPostfix]
            public static void CreateUIResPostfix(SV __instance)
            {
                if (IsInitialized) return;
                if (Loader.AssetManager.IsReady == false) return;
                if (LocalizedText.ItemUIName.IsGameDataReady() == false) return;
                if (LocalizedText.ItemUIDiscript.IsGameDataReady() == false) return;
                if (LocalizedText.UIText.IsGameDataReady() == false) return;

                IsInitialized = true;

                var dumpLang = new SysLang[] { SysLang.JP, SysLang.EN, SysLang.ZH, SysLang.ZK, SysLang.KR, SysLang.FR, SysLang.DE };

                var dumpPath = System.IO.Path.Combine(Paths.PluginPath, BepInExLoader.MODNAME);
                if (System.IO.Directory.Exists(dumpPath) == false)
                    System.IO.Directory.CreateDirectory(dumpPath);

                LocalizedText.PrepareLocalizedTextTypeAllSupportedLanguages(onAllSuccess: (System.Action)(() =>
                {
                    {
                        var itemIdType = typeof(ItemID);
                        log.LogMessage($"Dump {itemIdType.Name}...");

                        var dumpData = new DumpData();
                        dumpData.IDType = itemIdType.FullName;
                        dumpData.Values = new List<DumpData.Value>();

                        foreach (var itemIdName in itemIdType.GetEnumNames())
                        {
                            log.LogMessage($"Dump {itemIdType.Name} - {itemIdName}...");
                            if (itemIdName.Equals(ItemID.ITEM_EMPTY.ToString()) || itemIdName.Equals(ItemID.ITEM_MAX.ToString()))
                                continue;
                            ItemID itemId;
                            if (System.Enum.TryParse<ItemID>(itemIdName, out itemId))
                            {
                                var valueData = new DumpData.Value();
                                valueData.ID = itemIdName;
                                valueData.LocalizedValues = new List<DumpData.Value.LocalizedValue>();
                                foreach (var lang in dumpLang)
                                {
                                    var localizedValueData = new DumpData.Value.LocalizedValue();
                                    localizedValueData.Lang = lang.ToString();
                                    localizedValueData.Name = LocalizedText.ItemUIName.GetText(itemId, SysLangToSystemLanguage(lang));
                                    valueData.LocalizedValues.Add(localizedValueData);
                                }
                                dumpData.Values.Add(valueData);
                            }
                        }
                        var json = JsonConvert.SerializeObject(dumpData);
                        var filePath = System.IO.Path.Combine(dumpPath, $"{itemIdType.FullName}.json");
                        System.IO.File.WriteAllText(filePath, json);

                        log.LogMessage($"Dump {itemIdType.Name} done");
                    }

                    {
                        var monsterIdType = typeof(MonsterID);
                        log.LogMessage($"Dump {monsterIdType.Name}...");

                        var dumpData = new DumpData();
                        dumpData.IDType = monsterIdType.FullName;
                        dumpData.Values = new List<DumpData.Value>();
                        foreach (var monsterIdName in monsterIdType.GetEnumNames())
                        {
                            log.LogMessage($"Dump {monsterIdType.Name} - {monsterIdName}...");
                            if (monsterIdName.Equals(MonsterID.Empty.ToString()) || monsterIdName.Equals(MonsterID.Max.ToString()))
                                continue;
                            MonsterID monsterId;
                            if (System.Enum.TryParse<MonsterID>(monsterIdName, out monsterId))
                            {
                                var valueData = new DumpData.Value();
                                valueData.ID = monsterIdName;
                                valueData.LocalizedValues = new List<DumpData.Value.LocalizedValue>();
                                foreach (var lang in dumpLang)
                                {
                                    var localizedValueData = new DumpData.Value.LocalizedValue();
                                    localizedValueData.Lang = lang.ToString();
                                    localizedValueData.Name = LocalizedText.MonsterName.GetText(monsterId, SysLangToSystemLanguage(lang));
                                    valueData.LocalizedValues.Add(localizedValueData);
                                }
                                dumpData.Values.Add(valueData);
                            }
                        }
                        var json = JsonConvert.SerializeObject(dumpData);
                        var filePath = System.IO.Path.Combine(dumpPath, $"{monsterIdType.FullName}.json");
                        System.IO.File.WriteAllText(filePath, json);

                        log.LogMessage($"Dump {monsterIdType.Name} done");
                    }
                }));
            }
        }
    }
}
