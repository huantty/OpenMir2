﻿using GameSvr.Npc;
using SystemModule;
using SystemModule.Common;
using SystemModule.Data;

namespace GameSvr.Maps
{
    public class Maps
    {
        public static int LoadMapInfo()
        {
            var sFlag = string.Empty;
            var s34 = string.Empty;
            var sLine = string.Empty;
            var sMapName = string.Empty;
            var sMapDesc = string.Empty;
            var sReConnectMap = string.Empty;
            int nX;
            int n18;
            int n1C;
            int n20;
            int nServerIndex;
            Merchant QuestNPC;
            string sMapInfoFile;
            var result = -1;
            var sFileName = Path.Combine(M2Share.BasePath, M2Share.Config.EnvirDir, "MapInfo.txt");
            if (File.Exists(sFileName))
            {
                var LoadList = new StringList();
                LoadList.LoadFromFile(sFileName);
                if (LoadList.Count < 0)
                {
                    return result;
                }
                var count = 0;
                while (true)
                {
                    if (count >= LoadList.Count)
                    {
                        break;
                    }
                    if (HUtil32.CompareLStr("ConnectMapInfo", LoadList[count]))
                    {
                        sMapInfoFile = HUtil32.GetValidStr3(LoadList[count], ref sFlag, new[] { " ", "\t" });
                        LoadList.RemoveAt(count);
                        if (sMapInfoFile != "")
                        {
                            LoadMapInfo_LoadSubMapInfo(LoadList, sMapInfoFile);
                        }
                    }
                    count++;
                }
                result = 1;
                // 加载地图设置
                for (var i = 0; i < LoadList.Count; i++)
                {
                    sFlag = LoadList[i];
                    if (!string.IsNullOrEmpty(sFlag) && sFlag[0] == '[')
                    {
                        sMapName = "";
                        TMapFlag MapFlag = new TMapFlag
                        {
                            boSAFE = false
                        };
                        sFlag = HUtil32.ArrestStringEx(sFlag, "[", "]", ref sMapName);
                        sMapDesc = HUtil32.GetValidStrCap(sMapName, ref sMapName, new[] { " ", ",", "\t" });
                        if (sMapDesc != "" && sMapDesc[0] == '\"')
                        {
                            HUtil32.ArrestStringEx(sMapDesc, "\"", "\"", ref sMapDesc);
                        }
                        string s4C = HUtil32.GetValidStr3(sMapDesc, ref sMapDesc, new[] { " ", ",", "\t" }).Trim();
                        nServerIndex = HUtil32.StrToInt(s4C, 0);
                        if (sMapName == "")
                        {
                            continue;
                        }
                        MapFlag.nL = 1;
                        QuestNPC = null;
                        MapFlag.boSAFE = false;
                        MapFlag.nNEEDSETONFlag = -1;
                        MapFlag.nNeedONOFF = -1;
                        MapFlag.nMUSICID = -1;
                        while (true)
                        {
                            if (sFlag == "")
                            {
                                break;
                            }
                            sFlag = HUtil32.GetValidStr3(sFlag, ref s34, new[] { " ", ",", "\t" });
                            if (s34 == "")
                            {
                                break;
                            }
                            if (s34.Equals("SAFE", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boSAFE = true;
                                continue;
                            }
                            if (string.Compare(s34, "DARK", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                MapFlag.boDarkness = true;
                                continue;
                            }
                            if (string.Compare(s34, "FIGHT", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                MapFlag.boFightZone = true;
                                continue;
                            }
                            if (string.Compare(s34, "FIGHT3", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                MapFlag.boFight3Zone = true;
                                continue;
                            }
                            if (string.Compare(s34, "DAY", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                MapFlag.boDayLight = true;
                                continue;
                            }
                            if (string.Compare(s34, "QUIZ", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                MapFlag.boQUIZ = true;
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "NORECONNECT"))
                            {
                                MapFlag.boNORECONNECT = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sReConnectMap);
                                MapFlag.sNoReConnectMap = sReConnectMap;
                                if (MapFlag.sNoReConnectMap == "")
                                {
                                }
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "CHECKQUEST"))
                            {
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                QuestNPC = LoadMapInfo_LoadMapQuest(sLine);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "NEEDSET_ON"))
                            {
                                MapFlag.nNeedONOFF = 1;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nNEEDSETONFlag = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "NEEDSET_OFF"))
                            {
                                MapFlag.nNeedONOFF = 0;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nNEEDSETONFlag = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "MUSIC"))
                            {
                                MapFlag.boMUSIC = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nMUSICID = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "EXPRATE"))
                            {
                                MapFlag.boEXPRATE = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nEXPRATE = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "PKWINLEVEL"))
                            {
                                MapFlag.boPKWINLEVEL = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nPKWINLEVEL = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "PKWINEXP"))
                            {
                                MapFlag.boPKWINEXP = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nPKWINEXP = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "PKLOSTLEVEL"))
                            {
                                MapFlag.boPKLOSTLEVEL = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nPKLOSTLEVEL = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "PKLOSTEXP"))
                            {
                                MapFlag.boPKLOSTEXP = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nPKLOSTEXP = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "DECHP"))
                            {
                                MapFlag.boDECHP = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nDECHPPOINT = HUtil32.StrToInt(HUtil32.GetValidStr3(sLine, ref sLine, HUtil32.Backslash), -1);
                                MapFlag.nDECHPTIME = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "INCHP"))
                            {
                                MapFlag.boINCHP = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nINCHPPOINT = HUtil32.StrToInt(HUtil32.GetValidStr3(sLine, ref sLine, HUtil32.Backslash), -1);
                                MapFlag.nINCHPTIME = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "DECGAMEGOLD"))
                            {
                                MapFlag.boDECGAMEGOLD = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nDECGAMEGOLD = HUtil32.StrToInt(HUtil32.GetValidStr3(sLine, ref sLine, HUtil32.Backslash), -1);
                                MapFlag.nDECGAMEGOLDTIME = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "DECGAMEPOINT"))
                            {
                                MapFlag.boDECGAMEPOINT = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nDECGAMEPOINT = HUtil32.StrToInt(HUtil32.GetValidStr3(sLine, ref sLine, HUtil32.Backslash), -1);
                                MapFlag.nDECGAMEPOINTTIME = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "INCGAMEGOLD"))
                            {
                                MapFlag.boINCGAMEGOLD = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nINCGAMEGOLD = HUtil32.StrToInt(HUtil32.GetValidStr3(sLine, ref sLine, HUtil32.Backslash), -1);
                                MapFlag.nINCGAMEGOLDTIME = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "INCGAMEPOINT"))
                            {
                                MapFlag.boINCGAMEPOINT = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nINCGAMEPOINT = HUtil32.StrToInt(HUtil32.GetValidStr3(sLine, ref sLine, HUtil32.Backslash), -1);
                                MapFlag.nINCGAMEPOINTTIME = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (s34.Equals("RUNHUMAN", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boRUNHUMAN = true;
                                continue;
                            }
                            if (s34.Equals("RUNMON", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boRUNMON = true;
                                continue;
                            }
                            if (s34.Equals("NEEDHOLE", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNEEDHOLE = true;
                                continue;
                            }
                            if (s34.Equals("NORECALL", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNORECALL = true;
                                continue;
                            }
                            if (s34.Equals("NOGUILDRECALL", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNOGUILDRECALL = true;
                                continue;
                            }
                            if (s34.Equals("NODEARRECALL", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNODEARRECALL = true;
                                continue;
                            }
                            if (s34.Equals("NOMASTERRECALL", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNOMASTERRECALL = true;
                                continue;
                            }
                            if (s34.Equals("NORANDOMMOVE", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNORANDOMMOVE = true;
                                continue;
                            }
                            if (s34.Equals("NODRUG", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNODRUG = true;
                                continue;
                            }
                            if (s34.Equals("MINE", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boMINE = true;
                                continue;
                            }
                            if (s34.Equals("MINE2", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boMINE2 = true;
                                continue;
                            }
                            if (s34.Equals("NOTHROWITEM", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNOTHROWITEM = true;
                                continue;
                            }
                            if (s34.Equals("NODROPITEM", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNODROPITEM = true;
                                continue;
                            }
                            if (s34.Equals("NOPOSITIONMOVE", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNOPOSITIONMOVE = true;
                                continue;
                            }
                            if (s34.Equals("NOHORSE", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNOPOSITIONMOVE = true;
                                continue;
                            }
                            if (s34.Equals("NOCHAT", StringComparison.OrdinalIgnoreCase))
                            {
                                MapFlag.boNOCHAT = true;
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "KILLFUNC"))
                            {
                                MapFlag.boKILLFUNC = true;
                                HUtil32.ArrestStringEx(s34, "(", ")", ref sLine);
                                MapFlag.nKILLFUNCNO = HUtil32.StrToInt(sLine, -1);
                                continue;
                            }
                            if (HUtil32.CompareLStr(s34, "NOHUMNOMON"))
                            {
                                // 有人才开始刷怪
                                MapFlag.boNOHUMNOMON = true;
                                continue;
                            }
                            if (s34[0] == 'L')
                            {
                                MapFlag.nL = HUtil32.StrToInt(s34.Substring(1, s34.Length - 1), 1);
                            }
                        }
                        if (M2Share.MapMgr.AddMapInfo(sMapName, sMapDesc, nServerIndex, MapFlag, QuestNPC) == null)
                        {
                        }
                        result = 1;
                    }
                }

                // 加载地图连接点
                for (var i = 0; i < LoadList.Count; i++)
                {
                    sFlag = LoadList[i];
                    if (!string.IsNullOrEmpty(sFlag) && sFlag[0] != '[' && sFlag[0] != ';')
                    {
                        sFlag = HUtil32.GetValidStr3(sFlag, ref s34, HUtil32.Separator);
                        sMapName = s34;
                        sFlag = HUtil32.GetValidStr3(sFlag, ref s34, HUtil32.Separator);
                        nX = HUtil32.StrToInt(s34, 0);
                        sFlag = HUtil32.GetValidStr3(sFlag, ref s34, HUtil32.Separator);
                        n18 = HUtil32.StrToInt(s34, 0);
                        sFlag = HUtil32.GetValidStr3(sFlag, ref s34, new[] { " ", ",", "-", ">", "\t" });
                        string s44 = s34;
                        sFlag = HUtil32.GetValidStr3(sFlag, ref s34, HUtil32.Separator);
                        n1C = HUtil32.StrToInt(s34, 0);
                        sFlag = HUtil32.GetValidStr3(sFlag, ref s34, new[] { " ", ",", ";", "\t" });
                        n20 = HUtil32.StrToInt(s34, 0);
                        M2Share.MapMgr.AddMapRoute(sMapName, nX, n18, s44, n1C, n20);
                    }
                }
            }
            return result;
        }

        public static int LoadMinMap()
        {
            var sMapNO = string.Empty;
            var sMapIdx = string.Empty;
            var result = 0;
            var sFileName = Path.Combine(M2Share.BasePath, M2Share.Config.EnvirDir, "MiniMap.txt");
            if (File.Exists(sFileName))
            {
                M2Share.MiniMapList.Clear();
                var tMapList = new StringList();
                tMapList.LoadFromFile(sFileName);
                for (var i = 0; i < tMapList.Count; i++)
                {
                    var tStr = tMapList[i];
                    if (tStr != "" && tStr[0] != ';')
                    {
                        tStr = HUtil32.GetValidStr3(tStr, ref sMapNO, new[] { " ", "\t" });
                        tStr = HUtil32.GetValidStr3(tStr, ref sMapIdx, new[] { " ", "\t" });
                        var nIdx = HUtil32.StrToInt(sMapIdx, 0);
                        if (nIdx > 0)
                        {
                            if (M2Share.MiniMapList.ContainsKey(sMapNO))
                            {
                                M2Share.Log.LogError($"重复小地图配置信息[{sMapNO}]");
                                continue;
                            }
                            M2Share.MiniMapList.TryAdd(sMapNO, nIdx);
                        }
                    }
                }
            }
            return result;
        }

        private static Merchant LoadMapInfo_LoadMapQuest(string sName)
        {
            var questNPC = new Merchant
            {
                MapName = "0",
                CurrX = 0,
                CurrY = 0,
                ChrName = sName,
                m_nFlag = 0,
                Appr = 0,
                m_sFilePath = "MapQuest_def",
                m_boIsHide = true,
                m_boIsQuest = false
            };
            M2Share.WorldEngine.QuestNpcList.Add(questNPC);
            return questNPC;
        }

        private static void LoadMapInfo_LoadSubMapInfo(StringList LoadList, string sFileName)
        {
            string sFilePatchName;
            StringList LoadMapList;
            string sFileDir = Path.Combine(M2Share.BasePath, M2Share.Config.EnvirDir, "MapInfo");
            if (!Directory.Exists(sFileDir))
            {
                Directory.CreateDirectory(sFileDir);
            }
            sFilePatchName = sFileDir + sFileName;
            if (File.Exists(sFilePatchName))
            {
                LoadMapList = new StringList();
                LoadMapList.LoadFromFile(sFilePatchName);
                for (var i = 0; i < LoadMapList.Count; i++)
                {
                    LoadList.Add(LoadMapList[i]);
                }
            }
        }

    }
}
