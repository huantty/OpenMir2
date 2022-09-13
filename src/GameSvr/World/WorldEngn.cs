using GameSvr.Actor;
using GameSvr.Event.Events;
using GameSvr.Guild;
using GameSvr.Items;
using GameSvr.Maps;
using GameSvr.Monster;
using GameSvr.Monster.Monsters;
using GameSvr.Npc;
using GameSvr.Player;
using GameSvr.RobotPlay;
using GameSvr.Services;
using GameSvr.Snaps;
using NLog;
using System.Collections;
using SystemModule;
using SystemModule.Data;
using SystemModule.Packet.ClientPackets;
using SystemModule.Packet.ServerPackets;

namespace GameSvr.World
{
    public partial class WorldEngine
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private int ProcessMapDoorTick;
        private int ProcessMerchantTimeMax;
        private int ProcessMerchantTimeMin;
        private int ProcessMissionsTime;
        private int ProcessNpcTimeMax;
        private int ProcessNpcTimeMin;
        private int RegenMonstersTick;
        private int SendOnlineHumTime;
        private int ShowOnlineTick;
        private int ProcessLoadPlayTick;
        private int CurrMonGenIdx;
        /// <summary>
        /// 当前怪物列表刷新位置索引
        /// </summary>
        private int MonGenCertListPosition;
        private int MonGenListPosition;
        private int ProcHumIdx;
        private int ProcBotHubIdx;
        /// <summary>
        /// 交易NPC处理位置
        /// </summary>
        private int _merchantPosition;
        /// <summary>
        /// 怪物总数
        /// </summary>
        private int _monsterCount;
        /// <summary>
        /// 处理怪物数，用于统计处理怪物个数
        /// </summary>
        private int MonsterProcessCount;
        /// <summary>
        /// 处理怪物总数位置，用于计算怪物总数
        /// </summary>
        private int MonsterProcessPostion;
        /// <summary>
        /// NPC处理位置
        /// </summary>
        private int NpcPosition;
        /// <summary>
        /// 处理人物开始索引（每次处理人物数限制）
        /// </summary>
        private int ProcessHumanLoopTime;
        /// <summary>
        /// 处理假人间隔
        /// </summary>
        public long RobotLogonTick;
        public readonly IList<TAdminInfo> AdminList;
        private readonly IList<TGoldChangeInfo> _mChangeHumanDbGoldList;
        private readonly IList<SwitchDataInfo> _mChangeServerList;
        private readonly IList<int> _mListOfGateIdx;
        private readonly IList<int> _mListOfSocket;
        /// <summary>
        /// 从DB读取人物数据
        /// </summary>
        protected readonly IList<UserOpenInfo> LoadPlayList;
        protected readonly object LoadPlaySection;
        public readonly IList<MagicEvent> MagicEventList;
        public IList<SystemModule.Packet.ServerPackets.MagicInfo> MagicList;
        public readonly IList<Merchant> MerchantList;
        public readonly IList<MonGenInfo> MonGenList;
        protected readonly IList<PlayObject> NewHumanList;
        protected readonly IList<PlayObject> PlayObjectFreeList;
        protected readonly Dictionary<string, ServerGruopInfo> OtherUserNameList;
        protected readonly IList<PlayObject> PlayObjectList;
        protected readonly IList<PlayObject> BotPlayObjectList;
        internal readonly IList<TMonInfo> MonsterList;
        private readonly ArrayList _oldMagicList;
        public readonly IList<NormNpc> QuestNpcList;
        public readonly IList<StdItem> StdItemList;
        /// <summary>
        /// 假人列表
        /// </summary>
        private readonly IList<RoBotLogon> RobotLogonList;

        public WorldEngine()
        {
            LoadPlaySection = new object();
            LoadPlayList = new List<UserOpenInfo>();
            PlayObjectList = new List<PlayObject>();
            PlayObjectFreeList = new List<PlayObject>();
            _mChangeHumanDbGoldList = new List<TGoldChangeInfo>();
            ShowOnlineTick = HUtil32.GetTickCount();
            SendOnlineHumTime = HUtil32.GetTickCount();
            ProcessMapDoorTick = HUtil32.GetTickCount();
            ProcessMissionsTime = HUtil32.GetTickCount();
            RegenMonstersTick = HUtil32.GetTickCount();
            ProcessLoadPlayTick = HUtil32.GetTickCount();
            CurrMonGenIdx = 0;
            MonGenListPosition = 0;
            MonGenCertListPosition = 0;
            ProcHumIdx = 0;
            ProcBotHubIdx = 0;
            ProcessHumanLoopTime = 0;
            _merchantPosition = 0;
            NpcPosition = 0;
            StdItemList = new List<StdItem>();
            MonsterList = new List<TMonInfo>();
            MonGenList = new List<MonGenInfo>();
            MagicList = new List<SystemModule.Packet.ServerPackets.MagicInfo>();
            AdminList = new List<TAdminInfo>();
            MerchantList = new List<Merchant>();
            QuestNpcList = new List<NormNpc>();
            _mChangeServerList = new List<SwitchDataInfo>();
            MagicEventList = new List<MagicEvent>();
            ProcessMerchantTimeMin = 0;
            ProcessMerchantTimeMax = 0;
            ProcessNpcTimeMin = 0;
            ProcessNpcTimeMax = 0;
            NewHumanList = new List<PlayObject>();
            _mListOfGateIdx = new List<int>();
            _mListOfSocket = new List<int>();
            _oldMagicList = new ArrayList();
            OtherUserNameList = new Dictionary<string, ServerGruopInfo>(StringComparer.OrdinalIgnoreCase);
            RobotLogonList = new List<RoBotLogon>();
            BotPlayObjectList = new List<PlayObject>();
        }

        public int MonsterCount => _monsterCount;
        public int OnlinePlayObject => GetOnlineHumCount();
        public int PlayObjectCount => GetUserCount();
        public int LoadPlayCount => GetLoadPlayCount();

        public IEnumerable<PlayObject> PlayObjects => PlayObjectList;

        public void Execute()
        {
            PrcocessData();
            ProcessRobotPlayData();
        }

        public void Initialize()
        {
            _logger.Info("正在初始化NPC脚本...");
            MerchantInitialize();
            NpCinitialize();
            _logger.Info("初始化NPC脚本完成...");
            for (var i = 0; i < MonGenList.Count; i++)
            {
                if (MonGenList[i] != null)
                    MonGenList[i].nRace = GetMonRace(MonGenList[i].sMonName);
            }
        }

        private void PrcocessData()
        {
            try
            {
                ProcessHumans();
                ProcessMonsters();
                ProcessMerchants();
                ProcessNpcs();
                if ((HUtil32.GetTickCount() - ProcessMissionsTime) > 1000)
                {
                    ProcessMissionsTime = HUtil32.GetTickCount();
                    ProcessMissions();
                    ProcessEvents();
                }
                if ((HUtil32.GetTickCount() - ProcessMapDoorTick) > 500)
                {
                    ProcessMapDoorTick = HUtil32.GetTickCount();
                    ProcessMapDoor();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.StackTrace);
            }
        }

        private int GetMonRace(string sMonName)
        {
            var result = -1;
            for (var i = 0; i < MonsterList.Count; i++)
            {
                var sName = MonsterList[i].sName;
                if (!sName.Equals(sMonName, StringComparison.OrdinalIgnoreCase)) continue;
                result = MonsterList[i].btRace;
                break;
            }
            return result;
        }

        private void MerchantInitialize()
        {
            Merchant merchant;
            for (var i = MerchantList.Count - 1; i >= 0; i--)
            {
                merchant = MerchantList[i];
                merchant.Envir = M2Share.MapMgr.FindMap(merchant.MapName);
                if (merchant.Envir != null)
                {
                    merchant.OnEnvirnomentChanged();
                    merchant.Initialize();
                    if (merchant.AddtoMapSuccess && !merchant.m_boIsHide)
                    {
                        _logger.Warn("Merchant Initalize fail..." + merchant.CharName + ' ' +
                                     merchant.MapName + '(' +
                                     merchant.CurrX + ':' + merchant.CurrY + ')');
                        MerchantList.RemoveAt(i);
                    }
                    else
                    {
                        merchant.LoadMerchantScript();
                        merchant.LoadNPCData();
                    }
                }
                else
                {
                    _logger.Error(merchant.CharName + " - Merchant Initalize fail... (m.PEnvir=nil)");
                    MerchantList.RemoveAt(i);
                }
            }
        }

        private void NpCinitialize()
        {
            NormNpc normNpc;
            for (var i = QuestNpcList.Count - 1; i >= 0; i--)
            {
                normNpc = QuestNpcList[i];
                normNpc.Envir = M2Share.MapMgr.FindMap(normNpc.MapName);
                if (normNpc.Envir != null)
                {
                    normNpc.OnEnvirnomentChanged();
                    normNpc.Initialize();
                    if (normNpc.AddtoMapSuccess && !normNpc.m_boIsHide)
                    {
                        _logger.Warn(normNpc.CharName + " Npc Initalize fail... ");
                        QuestNpcList.RemoveAt(i);
                    }
                    else
                    {
                        normNpc.LoadNPCScript();
                    }
                }
                else
                {
                    _logger.Error(normNpc.CharName + " Npc Initalize fail... (npc.PEnvir=nil) ");
                    QuestNpcList.RemoveAt(i);
                }
            }
        }

        private int GetLoadPlayCount()
        {
            return LoadPlayList.Count;
        }

        private int GetOnlineHumCount()
        {
            return PlayObjectList.Count + BotPlayObjectList.Count;
        }

        private int GetUserCount()
        {
            return PlayObjectList.Count + BotPlayObjectList.Count;
        }

        private bool ProcessHumansIsLogined(string sChrName)
        {
            var result = false;
            if (M2Share.FrontEngine.InSaveRcdList(sChrName))
            {
                result = true;
            }
            else
            {
                for (var i = 0; i < PlayObjectList.Count; i++)
                {
                    if (string.Compare(PlayObjectList[i].CharName, sChrName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        private PlayObject ProcessHumans_MakeNewHuman(UserOpenInfo userOpenInfo)
        {
            PlayObject result = null;
            PlayObject playObject = null;
            SwitchDataInfo switchDataInfo = null;
            const string sExceptionMsg = "[Exception] TUserEngine::MakeNewHuman";
            const string sChangeServerFail1 = "chg-server-fail-1 [{0}] -> [{1}] [{2}]";
            const string sChangeServerFail2 = "chg-server-fail-2 [{0}] -> [{1}] [{2}]";
            const string sChangeServerFail3 = "chg-server-fail-3 [{0}] -> [{1}] [{2}]";
            const string sChangeServerFail4 = "chg-server-fail-4 [{0}] -> [{1}] [{2}]";
            const string sErrorEnvirIsNil = "[Error] PlayObject.PEnvir = nil";
        ReGetMap:
            try
            {
                playObject = new PlayObject();
                if (!M2Share.Config.boVentureServer)
                {
                    userOpenInfo.sChrName = string.Empty;
                    userOpenInfo.LoadUser.nSessionID = 0;
                    switchDataInfo = GetSwitchData(userOpenInfo.sChrName, userOpenInfo.LoadUser.nSessionID);
                }
                else
                {
                    switchDataInfo = null;
                }
                if (switchDataInfo == null)
                {
                    GetHumData(playObject, ref userOpenInfo.HumanRcd);
                    playObject.Race = Grobal2.RC_PLAYOBJECT;
                    if (string.IsNullOrEmpty(playObject.HomeMap))
                    {
                        playObject.HomeMap = GetHomeInfo(playObject.Job, ref playObject.HomeX, ref playObject.HomeY);
                        playObject.MapName = playObject.HomeMap;
                        playObject.CurrX = GetRandHomeX(playObject);
                        playObject.CurrY = GetRandHomeY(playObject);
                        if (playObject.Abil.Level == 0)
                        {
                            var abil = playObject.Abil;
                            abil.Level = 1;
                            abil.AC = 0;
                            abil.MAC = 0;
                            abil.DC = HUtil32.MakeLong(1, 2);
                            abil.MC = HUtil32.MakeLong(1, 2);
                            abil.SC = HUtil32.MakeLong(1, 2);
                            abil.MP = 15;
                            abil.HP = 15;
                            abil.MaxHP = 15;
                            abil.MaxMP = 15;
                            abil.Exp = 0;
                            abil.MaxExp = 100;
                            abil.Weight = 0;
                            abil.MaxWeight = 30;
                            playObject.m_boNewHuman = true;
                        }
                    }
                    Envirnoment envir = M2Share.MapMgr.GetMapInfo(M2Share.ServerIndex, playObject.MapName);
                    if (envir != null)
                    {
                        playObject.MapFileName = envir.MapFileName;
                        if (envir.Flag.boFight3Zone) // 是否在行会战争地图死亡
                        {
                            if (playObject.Abil.HP <= 0 && playObject.FightZoneDieCount < 3)
                            {
                                playObject.Abil.HP = playObject.Abil.MaxHP;
                                playObject.Abil.MP = playObject.Abil.MaxMP;
                                playObject.m_boDieInFight3Zone = true;
                            }
                            else
                            {
                                playObject.FightZoneDieCount = 0;
                            }
                        }
                    }
                    playObject.MyGuild = M2Share.GuildMgr.MemberOfGuild(playObject.CharName);
                    var userCastle = M2Share.CastleMgr.InCastleWarArea(envir, playObject.CurrX, playObject.CurrY);
                    if (envir != null && userCastle != null && (userCastle.PalaceEnvir == envir || userCastle.UnderWar))
                    {
                        userCastle = M2Share.CastleMgr.IsCastleMember(playObject);
                        if (userCastle == null)
                        {
                            playObject.MapName = playObject.HomeMap;
                            playObject.CurrX = (short)(playObject.HomeX - 2 + M2Share.RandomNumber.Random(5));
                            playObject.CurrY = (short)(playObject.HomeY - 2 + M2Share.RandomNumber.Random(5));
                        }
                        else
                        {
                            if (userCastle.PalaceEnvir == envir)
                            {
                                playObject.MapName = userCastle.GetMapName();
                                playObject.CurrX = userCastle.GetHomeX();
                                playObject.CurrY = userCastle.GetHomeY();
                            }
                        }
                    }
                    if (M2Share.MapMgr.FindMap(playObject.MapName) == null) playObject.Abil.HP = 0;
                    if (playObject.Abil.HP <= 0)
                    {
                        playObject.ClearStatusTime();
                        if (playObject.PvpLevel() < 2)
                        {
                            userCastle = M2Share.CastleMgr.IsCastleMember(playObject);
                            if (userCastle != null && userCastle.UnderWar)
                            {
                                playObject.MapName = userCastle.HomeMap;
                                playObject.CurrX = userCastle.GetHomeX();
                                playObject.CurrY = userCastle.GetHomeY();
                            }
                            else
                            {
                                playObject.MapName = playObject.HomeMap;
                                playObject.CurrX = (short)(playObject.HomeX - 2 + M2Share.RandomNumber.Random(5));
                                playObject.CurrY = (short)(playObject.HomeY - 2 + M2Share.RandomNumber.Random(5));
                            }
                        }
                        else
                        {
                            playObject.MapName = M2Share.Config.RedDieHomeMap;// '3'
                            playObject.CurrX = (short)(M2Share.RandomNumber.Random(13) + M2Share.Config.RedDieHomeX);// 839
                            playObject.CurrY = (short)(M2Share.RandomNumber.Random(13) + M2Share.Config.RedDieHomeY);// 668
                        }
                        playObject.Abil.HP = 14;
                    }
                    playObject.AbilCopyToWAbil();
                    envir = M2Share.MapMgr.GetMapInfo(M2Share.ServerIndex, playObject.MapName);//切换其他服务器
                    if (envir == null)
                    {
                        playObject.m_nSessionID = userOpenInfo.LoadUser.nSessionID;
                        playObject.m_nSocket = userOpenInfo.LoadUser.nSocket;
                        playObject.m_nGateIdx = userOpenInfo.LoadUser.nGateIdx;
                        playObject.m_nGSocketIdx = userOpenInfo.LoadUser.nGSocketIdx;
                        playObject.WAbil = playObject.Abil;
                        playObject.m_nServerIndex = M2Share.MapMgr.GetMapOfServerIndex(playObject.MapName);
                        if (playObject.Abil.HP != 14)
                        {
                            _logger.Warn(string.Format(sChangeServerFail1, new object[] { M2Share.ServerIndex, playObject.m_nServerIndex, playObject.MapName }));
                        }
                        SendSwitchData(playObject, playObject.m_nServerIndex);
                        SendChangeServer(playObject, (byte)playObject.m_nServerIndex);
                        playObject = null;
                        return result;
                    }
                    playObject.MapFileName = envir.MapFileName;
                    var nC = 0;
                    while (true)
                    {
                        if (envir.CanWalk(playObject.CurrX, playObject.CurrY, true)) break;
                        playObject.CurrX = (short)(playObject.CurrX - 3 + M2Share.RandomNumber.Random(6));
                        playObject.CurrY = (short)(playObject.CurrY - 3 + M2Share.RandomNumber.Random(6));
                        nC++;
                        if (nC >= 5) break;
                    }
                    if (!envir.CanWalk(playObject.CurrX, playObject.CurrY, true))
                    {
                        _logger.Warn(string.Format(sChangeServerFail2,
                            new object[] { M2Share.ServerIndex, playObject.m_nServerIndex, playObject.MapName }));
                        playObject.MapName = M2Share.Config.HomeMap;
                        envir = M2Share.MapMgr.FindMap(M2Share.Config.HomeMap);
                        playObject.CurrX = M2Share.Config.HomeX;
                        playObject.CurrY = M2Share.Config.HomeY;
                    }
                    playObject.Envir = envir;
                    playObject.OnEnvirnomentChanged();
                    if (playObject.Envir == null)
                    {
                        _logger.Error(sErrorEnvirIsNil);
                        goto ReGetMap;
                    }
                    else
                        playObject.m_boReadyRun = false;
                    playObject.MapFileName = envir.MapFileName;
                }
                else
                {
                    GetHumData(playObject, ref userOpenInfo.HumanRcd);
                    playObject.MapName = switchDataInfo.sMap;
                    playObject.CurrX = switchDataInfo.wX;
                    playObject.CurrY = switchDataInfo.wY;
                    playObject.Abil = switchDataInfo.Abil;
                    playObject.Abil = switchDataInfo.Abil;
                    LoadSwitchData(switchDataInfo, ref playObject);
                    DelSwitchData(switchDataInfo);
                    Envirnoment envir = M2Share.MapMgr.GetMapInfo(M2Share.ServerIndex, playObject.MapName);
                    if (envir != null)
                    {
                        _logger.Warn(string.Format(sChangeServerFail3,
                            new object[] { M2Share.ServerIndex, playObject.m_nServerIndex, playObject.MapName }));
                        playObject.MapName = M2Share.Config.HomeMap;
                        envir = M2Share.MapMgr.FindMap(M2Share.Config.HomeMap);
                        playObject.CurrX = M2Share.Config.HomeX;
                        playObject.CurrY = M2Share.Config.HomeY;
                    }
                    else
                    {
                        if (!envir.CanWalk(playObject.CurrX, playObject.CurrY, true))
                        {
                            _logger.Warn(string.Format(sChangeServerFail4,
                                new object[] { M2Share.ServerIndex, playObject.m_nServerIndex, playObject.MapName }));
                            playObject.MapName = M2Share.Config.HomeMap;
                            envir = M2Share.MapMgr.FindMap(M2Share.Config.HomeMap);
                            playObject.CurrX = M2Share.Config.HomeX;
                            playObject.CurrY = M2Share.Config.HomeY;
                        }
                        playObject.AbilCopyToWAbil();
                        playObject.Envir = envir;
                        playObject.OnEnvirnomentChanged();
                        if (playObject.Envir == null)
                        {
                            _logger.Error(sErrorEnvirIsNil);
                            goto ReGetMap;
                        }
                        else
                        {
                            playObject.m_boReadyRun = false;
                            playObject.m_boLoginNoticeOK = true;
                            playObject.bo6AB = true;
                        }
                    }
                }
                playObject.m_sUserID = userOpenInfo.LoadUser.sAccount;
                playObject.m_sIPaddr = userOpenInfo.LoadUser.sIPaddr;
                playObject.m_sIPLocal = M2Share.GetIPLocal(playObject.m_sIPaddr);
                playObject.m_nSocket = userOpenInfo.LoadUser.nSocket;
                playObject.m_nGSocketIdx = userOpenInfo.LoadUser.nGSocketIdx;
                playObject.m_nGateIdx = userOpenInfo.LoadUser.nGateIdx;
                playObject.m_nSessionID = userOpenInfo.LoadUser.nSessionID;
                playObject.m_nPayMent = userOpenInfo.LoadUser.nPayMent;
                playObject.m_nPayMode = userOpenInfo.LoadUser.nPayMode;
                playObject.m_dwLoadTick = userOpenInfo.LoadUser.dwNewUserTick;
                //PlayObject.m_nSoftVersionDateEx = M2Share.GetExVersionNO(UserOpenInfo.LoadUser.nSoftVersionDate, ref PlayObject.m_nSoftVersionDate);
                playObject.m_nSoftVersionDate = userOpenInfo.LoadUser.nSoftVersionDate;
                playObject.m_nSoftVersionDateEx = userOpenInfo.LoadUser.nSoftVersionDate;//M2Share.GetExVersionNO(UserOpenInfo.LoadUser.nSoftVersionDate, ref PlayObject.m_nSoftVersionDate);
                result = playObject;
            }
            catch (Exception ex)
            {
                _logger.Error(sExceptionMsg);
                _logger.Error(ex.StackTrace);
            }
            return result;
        }

        private void ProcessHumans()
        {
            const string sExceptionMsg1 = "[Exception] TUserEngine::ProcessHumans -> Ready, Save, Load...";
            const string sExceptionMsg3 = "[Exception] TUserEngine::ProcessHumans ClosePlayer.Delete";
            var dwCheckTime = HUtil32.GetTickCount();
            PlayObject playObject;
            if ((HUtil32.GetTickCount() - ProcessLoadPlayTick) > 200)
            {
                ProcessLoadPlayTick = HUtil32.GetTickCount();
                try
                {
                    HUtil32.EnterCriticalSection(LoadPlaySection);
                    try
                    {
                        for (var i = 0; i < LoadPlayList.Count; i++)
                        {
                            UserOpenInfo userOpenInfo;
                            if (!M2Share.FrontEngine.IsFull() && !ProcessHumansIsLogined(LoadPlayList[i].sChrName))
                            {
                                userOpenInfo = LoadPlayList[i];
                                playObject = ProcessHumans_MakeNewHuman(userOpenInfo);
                                if (playObject != null)
                                {
                                    if (playObject.IsRobot)
                                    {
                                        BotPlayObjectList.Add(playObject);
                                    }
                                    else
                                    {
                                        PlayObjectList.Add(playObject);
                                    }
                                    NewHumanList.Add(playObject);
                                    SendServerGroupMsg(Grobal2.ISM_USERLOGON, M2Share.ServerIndex, playObject.CharName);
                                }
                            }
                            else
                            {
                                KickOnlineUser(LoadPlayList[i].sChrName);
                                userOpenInfo = LoadPlayList[i];
                                _mListOfGateIdx.Add(userOpenInfo.LoadUser.nGateIdx);
                                _mListOfSocket.Add(userOpenInfo.LoadUser.nSocket);
                            }
                            LoadPlayList[i] = null;
                        }
                        LoadPlayList.Clear();
                        for (var i = 0; i < _mChangeHumanDbGoldList.Count; i++)
                        {
                            var goldChangeInfo = _mChangeHumanDbGoldList[i];
                            playObject = GetPlayObject(goldChangeInfo.sGameMasterName);
                            if (playObject != null)
                                playObject.GoldChange(goldChangeInfo.sGetGoldUser, goldChangeInfo.nGold);
                            goldChangeInfo = null;
                        }
                        _mChangeHumanDbGoldList.Clear();
                    }
                    finally
                    {
                        HUtil32.LeaveCriticalSection(LoadPlaySection);
                    }
                    for (var i = 0; i < NewHumanList.Count; i++)
                    {
                        playObject = NewHumanList[i];
                        M2Share.GateMgr.SetGateUserList(playObject.m_nGateIdx, playObject.m_nSocket, playObject);
                    }
                    NewHumanList.Clear();
                    for (var i = 0; i < _mListOfGateIdx.Count; i++)
                    {
                        M2Share.GateMgr.CloseUser(_mListOfGateIdx[i], _mListOfSocket[i]);
                    }
                    _mListOfGateIdx.Clear();
                    _mListOfSocket.Clear();
                }
                catch (Exception e)
                {
                    _logger.Error(sExceptionMsg1);
                    _logger.Error(e.Message);
                }
            }

            //人工智障开始登陆
            if (RobotLogonList.Count > 0)
            {
                if (HUtil32.GetTickCount() - RobotLogonTick > 1000)
                {
                    RobotLogonTick = HUtil32.GetTickCount();
                    if (RobotLogonList.Count > 0)
                    {
                        var roBot = RobotLogonList[0];
                        RegenAiObject(roBot);
                        RobotLogonList.RemoveAt(0);
                    }
                }
            }

            try
            {
                for (var i = 0; i < PlayObjectFreeList.Count; i++)
                {
                    playObject = PlayObjectFreeList[i];
                    if ((HUtil32.GetTickCount() - playObject.GhostTick) > M2Share.Config.HumanFreeDelayTime)// 5 * 60 * 1000
                    {
                        PlayObjectFreeList[i] = null;
                        PlayObjectFreeList.RemoveAt(i);
                        break;
                    }
                    if (playObject.m_boSwitchData && playObject.m_boRcdSaved)
                    {
                        if (SendSwitchData(playObject, playObject.m_nServerIndex) || playObject.m_nWriteChgDataErrCount > 20)
                        {
                            playObject.m_boSwitchData = false;
                            playObject.m_boSwitchDataOK = true;
                            playObject.m_boSwitchDataSended = true;
                            playObject.m_dwChgDataWritedTick = HUtil32.GetTickCount();
                        }
                        else
                        {
                            playObject.m_nWriteChgDataErrCount++;
                        }
                    }
                    if (playObject.m_boSwitchDataSended && HUtil32.GetTickCount() - playObject.m_dwChgDataWritedTick > 100)
                    {
                        playObject.m_boSwitchDataSended = false;
                        SendChangeServer(playObject, (byte)playObject.m_nServerIndex);
                    }
                }
            }
            catch
            {
                _logger.Error(sExceptionMsg3);
            }
            ProcessPlayObjectData();
            ProcessHumanLoopTime++;
            M2Share.g_nProcessHumanLoopTime = ProcessHumanLoopTime;
            if (ProcHumIdx == 0)
            {
                ProcessHumanLoopTime = 0;
                M2Share.g_nProcessHumanLoopTime = ProcessHumanLoopTime;
                var dwUsrRotTime = HUtil32.GetTickCount() - M2Share.g_dwUsrRotCountTick;
                M2Share.dwUsrRotCountMin = dwUsrRotTime;
                M2Share.g_dwUsrRotCountTick = HUtil32.GetTickCount();
                if (M2Share.dwUsrRotCountMax < dwUsrRotTime) M2Share.dwUsrRotCountMax = dwUsrRotTime;
            }
            M2Share.g_nHumCountMin = HUtil32.GetTickCount() - dwCheckTime;
            if (M2Share.g_nHumCountMax < M2Share.g_nHumCountMin) M2Share.g_nHumCountMax = M2Share.g_nHumCountMin;
        }

        private void ProcessRobotPlayData()
        {
            const string sExceptionMsg = "[Exception] TUserEngine::ProcessRobotPlayData";
            try
            {
                var dwCurTick = HUtil32.GetTickCount();
                var nIdx = ProcBotHubIdx;
                var boCheckTimeLimit = false;
                var dwCheckTime = HUtil32.GetTickCount();
                while (true)
                {
                    if (BotPlayObjectList.Count <= nIdx) break;
                    var playObject = BotPlayObjectList[nIdx];
                    if (dwCurTick - playObject.RunTick > playObject.RunTime)
                    {
                        playObject.RunTick = dwCurTick;
                        if (!playObject.Ghost)
                        {
                            if (!playObject.m_boLoginNoticeOK)
                            {
                                playObject.RunNotice();
                            }
                            else
                            {
                                if (!playObject.m_boReadyRun)
                                {
                                    playObject.m_boReadyRun = true;
                                    playObject.UserLogon();
                                }
                                else
                                {
                                    if ((HUtil32.GetTickCount() - playObject.SearchTick) > playObject.SearchTime)
                                    {
                                        playObject.SearchTick = HUtil32.GetTickCount();
                                        playObject.SearchViewRange();
                                        playObject.GameTimeChanged();
                                    }
                                    playObject.Run();
                                }
                            }
                        }
                        else
                        {
                            BotPlayObjectList.Remove(playObject);
                            playObject.Disappear();
                            AddToHumanFreeList(playObject);
                            playObject.DealCancelA();
                            SaveHumanRcd(playObject);
                            M2Share.GateMgr.CloseUser(playObject.m_nGateIdx, playObject.m_nSocket);
                            SendServerGroupMsg(Grobal2.SS_202, M2Share.ServerIndex, playObject.CharName);
                            continue;
                        }
                    }
                    nIdx++;
                    if ((HUtil32.GetTickCount() - dwCheckTime) > M2Share.g_dwHumLimit)
                    {
                        boCheckTimeLimit = true;
                        ProcBotHubIdx = nIdx;
                        break;
                    }
                }
                if (!boCheckTimeLimit) ProcBotHubIdx = 0;
            }
            catch (Exception ex)
            {
               _logger.Error(sExceptionMsg);
               _logger.Error(ex.StackTrace);
            }
        }

        private void ProcessPlayObjectData()
        {
            try
            {
                var dwCurTick = HUtil32.GetTickCount();
                var nIdx = ProcHumIdx;
                var boCheckTimeLimit = false;
                var dwCheckTime = HUtil32.GetTickCount();
                while (true)
                {
                    if (PlayObjectList.Count <= nIdx) break;
                    var playObject = PlayObjectList[nIdx];
                    if (playObject == null)
                    {
                        continue;
                    }
                    if ((dwCurTick - playObject.RunTick) > playObject.RunTime)
                    {
                        playObject.RunTick = dwCurTick;
                        if (!playObject.Ghost)
                        {
                            if (!playObject.m_boLoginNoticeOK)
                            {
                                playObject.RunNotice();
                            }
                            else
                            {
                                if (!playObject.m_boReadyRun)
                                {
                                    playObject.m_boReadyRun = true;
                                    playObject.UserLogon();
                                }
                                else
                                {
                                    if ((HUtil32.GetTickCount() - playObject.SearchTick) > playObject.SearchTime)
                                    {
                                        playObject.SearchTick = HUtil32.GetTickCount();
                                        playObject.SearchViewRange();//搜索对像
                                        playObject.GameTimeChanged();//游戏时间改变
                                    }
                                    if ((HUtil32.GetTickCount() - playObject.m_dwShowLineNoticeTick) > M2Share.Config.ShowLineNoticeTime)
                                    {
                                        playObject.m_dwShowLineNoticeTick = HUtil32.GetTickCount();
                                        if (M2Share.LineNoticeList.Count > playObject.m_nShowLineNoticeIdx)
                                        {
                                            var lineNoticeMsg = M2Share.g_ManageNPC.GetLineVariableText(playObject, M2Share.LineNoticeList[playObject.m_nShowLineNoticeIdx]);
                                            switch (lineNoticeMsg[0])
                                            {
                                                case 'R':
                                                    playObject.SysMsg(lineNoticeMsg.Substring(1, lineNoticeMsg.Length - 1), MsgColor.Red, MsgType.Notice);
                                                    break;
                                                case 'G':
                                                    playObject.SysMsg(lineNoticeMsg.Substring(1, lineNoticeMsg.Length - 1), MsgColor.Green, MsgType.Notice);
                                                    break;
                                                case 'B':
                                                    playObject.SysMsg(lineNoticeMsg.Substring(1, lineNoticeMsg.Length - 1), MsgColor.Blue, MsgType.Notice);
                                                    break;
                                                default:
                                                    playObject.SysMsg(lineNoticeMsg, (MsgColor)M2Share.Config.LineNoticeColor, MsgType.Notice);
                                                    break;
                                            }
                                        }
                                        playObject.m_nShowLineNoticeIdx++;
                                        if (M2Share.LineNoticeList.Count <= playObject.m_nShowLineNoticeIdx)
                                        {
                                            playObject.m_nShowLineNoticeIdx = 0;
                                        }
                                    }
                                    playObject.Run();
                                    if (!M2Share.FrontEngine.IsFull() && (HUtil32.GetTickCount() - playObject.m_dwSaveRcdTick) > M2Share.Config.SaveHumanRcdTime)
                                    {
                                        playObject.m_dwSaveRcdTick = HUtil32.GetTickCount();
                                        playObject.DealCancelA();
                                        SaveHumanRcd(playObject);
                                    }
                                }
                            }
                        }
                        else
                        {
                            PlayObjectList.Remove(playObject);
                            playObject.Disappear();
                            AddToHumanFreeList(playObject);
                            playObject.DealCancelA();
                            SaveHumanRcd(playObject);
                            M2Share.GateMgr.CloseUser(playObject.m_nGateIdx, playObject.m_nSocket);
                            SendServerGroupMsg(Grobal2.ISM_USERLOGOUT, M2Share.ServerIndex, playObject.CharName);
                            continue;
                        }
                    }
                    nIdx++;
                    if ((HUtil32.GetTickCount() - dwCheckTime) > M2Share.g_dwHumLimit)
                    {
                        boCheckTimeLimit = true;
                        ProcHumIdx = nIdx;
                        break;
                    }
                }
                if (!boCheckTimeLimit) ProcHumIdx = 0;
            }
            catch (Exception ex)
            {
                _logger.Error("[Exception] TUserEngine::ProcessHumans");
                _logger.Error(ex.StackTrace);
            }
        }

        private void ProcessMerchants()
        {
            var boProcessLimit = false;
            const string sExceptionMsg = "[Exception] TUserEngine::ProcessMerchants";
            var dwRunTick = HUtil32.GetTickCount();
            try
            {
                var dwCurrTick = HUtil32.GetTickCount();
                for (var i = _merchantPosition; i < MerchantList.Count; i++)
                {
                    var merchantNpc = MerchantList[i];
                    if (!merchantNpc.Ghost)
                    {
                        if ((dwCurrTick - merchantNpc.RunTick) > merchantNpc.RunTime)
                        {
                            if ((HUtil32.GetTickCount() - merchantNpc.SearchTick) > merchantNpc.SearchTime)
                            {
                                merchantNpc.SearchTick = HUtil32.GetTickCount();
                                merchantNpc.SearchViewRange();
                            }
                            if ((dwCurrTick - merchantNpc.RunTick) > merchantNpc.RunTime)
                            {
                                merchantNpc.RunTick = dwCurrTick;
                                merchantNpc.Run();
                            }
                        }
                    }
                    else
                    {
                        if ((HUtil32.GetTickCount() - merchantNpc.GhostTick) > 60 * 1000)
                        {
                            merchantNpc = null;
                            MerchantList.RemoveAt(i);
                            break;
                        }
                    }
                    if ((HUtil32.GetTickCount() - dwRunTick) > M2Share.g_dwNpcLimit)
                    {
                        _merchantPosition = i;
                        boProcessLimit = true;
                        break;
                    }
                }
                if (!boProcessLimit)
                {
                    _merchantPosition = 0;
                }
            }
            catch
            {
                _logger.Error(sExceptionMsg);
            }
            ProcessMerchantTimeMin = HUtil32.GetTickCount() - dwRunTick;
            if (ProcessMerchantTimeMin > ProcessMerchantTimeMax)
            {
                ProcessMerchantTimeMax = ProcessMerchantTimeMin;
            }
            if (ProcessNpcTimeMin > ProcessNpcTimeMax)
            {
                ProcessNpcTimeMax = ProcessNpcTimeMin;
            }
        }

        private void ProcessMissions()
        {

        }

        /// <summary>
        /// 取怪物刷新时间
        /// </summary>
        /// <returns></returns>
        public int GetMonstersZenTime(int dwTime)
        {
            int result;
            if (dwTime < 30 * 60 * 1000)
            {
                var d10 = (PlayObjectCount - M2Share.Config.UserFull) / HUtil32._MAX(1, M2Share.Config.ZenFastStep);
                if (d10 > 0)
                {
                    if (d10 > 6) d10 = 6;
                    result = (int)(dwTime - Math.Round(dwTime / 10 * (double)d10));
                }
                else
                {
                    result = dwTime;
                }
            }
            else
            {
                result = dwTime;
            }
            return result;
        }

        /// <summary>
        /// 怪物处理
        /// 刷新、行动、攻击等动作
        /// </summary>
        private void ProcessMonsters()
        {
            bool boCanCreate;
            var dwRunTick = HUtil32.GetTickCount();
            AnimalObject monster = null;
            try
            {
                var boProcessLimit = false;
                var dwCurrentTick = HUtil32.GetTickCount();
                MonGenInfo monGen = null;
                // 刷新怪物开始
                if ((HUtil32.GetTickCount() - RegenMonstersTick) > M2Share.Config.RegenMonstersTime)
                {
                    RegenMonstersTick = HUtil32.GetTickCount();
                    if (CurrMonGenIdx < MonGenList.Count)
                    {
                        monGen = MonGenList[CurrMonGenIdx];
                    }
                    else if (MonGenList.Count > 0)
                    {
                        monGen = MonGenList[0];
                    }
                    if (CurrMonGenIdx < MonGenList.Count - 1)
                    {
                        CurrMonGenIdx++;
                    }
                    else
                    {
                        CurrMonGenIdx = 0;
                    }
                    if (monGen != null && !string.IsNullOrEmpty(monGen.sMonName) && !M2Share.Config.boVentureServer)
                    {
                        var nTemp = HUtil32.GetTickCount() - monGen.dwStartTick;
                        if (monGen.dwStartTick == 0 || nTemp > GetMonstersZenTime(monGen.dwZenTime))
                        {
                            var nGenCount = monGen.nActiveCount; //取已刷出来的怪数量
                            var boRegened = true;
                            var nGenModCount = monGen.nCount / M2Share.Config.MonGenRate * 10;
                            var map = M2Share.MapMgr.FindMap(monGen.sMapName);
                            if (map == null || map.Flag.boNOHUMNOMON && map.HumCount <= 0)
                                boCanCreate = false;
                            else
                                boCanCreate = true;
                            if (nGenModCount > nGenCount && boCanCreate)// 增加 控制刷怪数量比例
                            {
                                boRegened = RegenMonsters(monGen, nGenModCount - nGenCount);
                            }
                            if (boRegened)
                            {
                                monGen.dwStartTick = HUtil32.GetTickCount();
                            }
                        }
                    }
                }
                // 刷新怪物结束
                var dwMonProcTick = HUtil32.GetTickCount();

                MonsterProcessCount = 0;
                var i = 0;
                for (i = MonGenListPosition; i < MonGenList.Count; i++)
                {
                    monGen = MonGenList[i];
                    int nProcessPosition;
                    if (MonGenCertListPosition < monGen.CertList.Count)
                        nProcessPosition = MonGenCertListPosition;
                    else
                        nProcessPosition = 0;
                    MonGenCertListPosition = 0;
                    while (true)
                    {
                        if (nProcessPosition >= monGen.CertList.Count)
                        {
                            break;
                        }
                        monster = (AnimalObject)monGen.CertList[nProcessPosition];
                        if (monster != null)
                        {
                            if (!monster.Ghost)
                            {
                                if ((dwCurrentTick - monster.RunTick) > monster.RunTime)
                                {
                                    monster.RunTick = dwRunTick;
                                    if (monster.Death && monster.CanReAlive && monster.Invisible && (monster.MonGen != null))
                                    {
                                        if ((HUtil32.GetTickCount() - monster.ReAliveTick) > GetMonstersZenTime(monster.MonGen.dwZenTime))
                                        {
                                            if (monster.ReAliveEx(monster.MonGen))
                                            {
                                                monster.ReAliveTick = HUtil32.GetTickCount();
                                            }
                                        }
                                    }
                                    if (!monster.IsVisibleActive && (monster.ProcessRunCount < M2Share.Config.ProcessMonsterInterval))
                                    {
                                        monster.ProcessRunCount++;
                                    }
                                    else
                                    {
                                        if ((dwCurrentTick - monster.SearchTick) > monster.SearchTime)
                                        {
                                            monster.SearchTick = HUtil32.GetTickCount();
                                            if (!monster.Death)
                                            {
                                                monster.SearchViewRange();
                                            }
                                            else
                                            {
                                                monster.SearchViewRangeDeath();
                                            }
                                        }
                                        monster.ProcessRunCount = 0;
                                        monster.Run();
                                    }
                                }
                                MonsterProcessPostion++;
                            }
                            else
                            {
                                if ((HUtil32.GetTickCount() - monster.GhostTick) > 5 * 60 * 1000)
                                {
                                    monGen.CertList.RemoveAt(nProcessPosition);
                                    monGen.CertCount--;
                                    monster = null;
                                    continue;
                                }
                            }
                        }
                        nProcessPosition++;
                        if ((HUtil32.GetTickCount() - dwMonProcTick) > M2Share.g_dwMonLimit)
                        {
                            boProcessLimit = true;
                            MonGenCertListPosition = nProcessPosition;
                            break;
                        }
                    }
                    if (boProcessLimit) break;
                }
                if (MonGenList.Count <= i)
                {
                    MonGenListPosition = 0;
                    _monsterCount = MonsterProcessPostion;
                    MonsterProcessPostion = 0;
                }
                if (!boProcessLimit)
                    MonGenListPosition = 0;
                else
                    MonGenListPosition = i;
            }
            catch (Exception e)
            {
                _logger.Error(e.StackTrace);
            }
        }

        /// <summary>
        /// 获取刷怪数量
        /// </summary>
        /// <param name="monGen"></param>
        /// <returns></returns>
        private int GetGenMonCount(MonGenInfo monGen)
        {
            var nCount = 0;
            for (var i = 0; i < monGen.CertList.Count; i++)
            {
                BaseObject baseObject = monGen.CertList[i];
                if (!baseObject.Death && !baseObject.Ghost)
                {
                    nCount++;
                }
            }
            return nCount;
        }

        private void ProcessNpcs()
        {
            var dwRunTick = HUtil32.GetTickCount();
            var boProcessLimit = false;
            try
            {
                var dwCurrTick = HUtil32.GetTickCount();
                for (var i = NpcPosition; i < QuestNpcList.Count; i++)
                {
                    NormNpc npc = QuestNpcList[i];
                    if (!npc.Ghost)
                    {
                        if ((dwCurrTick - npc.RunTick) > npc.RunTime)
                        {
                            if ((HUtil32.GetTickCount() - npc.SearchTick) > npc.SearchTime)
                            {
                                npc.SearchTick = HUtil32.GetTickCount();
                                npc.SearchViewRange();
                            }
                            if ((dwCurrTick - npc.RunTick) > npc.RunTime)
                            {
                                npc.RunTick = dwCurrTick;
                                npc.Run();
                            }
                        }
                    }
                    else
                    {
                        if ((HUtil32.GetTickCount() - npc.GhostTick) > 60 * 1000)
                        {
                            QuestNpcList.RemoveAt(i);
                            break;
                        }
                    }
                    if ((HUtil32.GetTickCount() - dwRunTick) > M2Share.g_dwNpcLimit)
                    {
                        NpcPosition = i;
                        boProcessLimit = true;
                        break;
                    }
                }
                if (!boProcessLimit) NpcPosition = 0;
            }
            catch
            {
                _logger.Error("[Exceptioin] TUserEngine.ProcessNpcs");
            }
            ProcessNpcTimeMin = HUtil32.GetTickCount() - dwRunTick;
            if (ProcessNpcTimeMin > ProcessNpcTimeMax) ProcessNpcTimeMax = ProcessNpcTimeMin;
        }

        public BaseObject RegenMonsterByName(string sMap, short nX, short nY, string sMonName)
        {
            var nRace = GetMonRace(sMonName);
            var baseObject = AddBaseObject(sMap, nX, nY, nRace, sMonName);
            if (baseObject != null)
            {
                var n18 = MonGenList.Count - 1;
                if (n18 < 0) n18 = 0;
                if (MonGenList.Count > n18)
                {
                    var monGen = MonGenList[n18];
                    monGen.CertList.Add(baseObject);
                    monGen.CertCount++;
                }
                baseObject.Envir.AddObject(baseObject);
                baseObject.AddToMaped = true;
            }
            return baseObject;
        }

        public void Run()
        {
            const string sExceptionMsg = "[Exception] TUserEngine::Run";
            try
            {
                if ((HUtil32.GetTickCount() - ShowOnlineTick) > M2Share.Config.ConsoleShowUserCountTime)
                {
                    ShowOnlineTick = HUtil32.GetTickCount();
                    M2Share.NoticeMgr.LoadingNotice();
                    _logger.Info("在线数: " + PlayObjectCount);
                    M2Share.CastleMgr.Save();
                }
                if ((HUtil32.GetTickCount() - SendOnlineHumTime) > 10000)
                {
                    SendOnlineHumTime = HUtil32.GetTickCount();
                    IdSrvClient.Instance.SendOnlineHumCountMsg(OnlinePlayObject);
                }
            }
            catch (Exception e)
            {
                _logger.Error(sExceptionMsg);
                _logger.Error(e.Message);
            }
        }

        public StdItem GetStdItem(ushort nItemIdx)
        {
            StdItem result = null;
            nItemIdx -= 1;
            if (nItemIdx >= 0 && StdItemList.Count > nItemIdx)
            {
                result = StdItemList[nItemIdx];
                if (result.Name == "") result = null;
            }
            return result;
        }

        public StdItem GetStdItem(string sItemName)
        {
            StdItem result = null;
            if (string.IsNullOrEmpty(sItemName)) return result;
            for (var i = 0; i < StdItemList.Count; i++)
            {
                StdItem stdItem = StdItemList[i];
                if (string.Compare(stdItem.Name, sItemName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result = stdItem;
                    break;
                }
            }
            return result;
        }

        public int GetStdItemWeight(int nItemIdx)
        {
            int result = 0;
            nItemIdx -= 1;
            if (nItemIdx >= 0 && StdItemList.Count > nItemIdx)
            {
                result = StdItemList[nItemIdx].Weight;
            }
            return result;
        }

        public string GetStdItemName(int nItemIdx)
        {
            var result = "";
            nItemIdx -= 1;
            if (nItemIdx >= 0 && StdItemList.Count > nItemIdx)
            {
                result = StdItemList[nItemIdx].Name;
            }
            return result;
        }

        public bool FindOtherServerUser(string sName, ref int nServerIndex)
        {
            if (OtherUserNameList.TryGetValue(sName, out var groupServer))
            {
                nServerIndex = groupServer.nServerIdx;
                M2Share.Log.Info($"玩家在[{nServerIndex}]服务器上.");
                return true;
            }
            return false;
        }

        public void CryCry(short wIdent, Envirnoment pMap, int nX, int nY, int nWide, byte btFColor, byte btBColor, string sMsg)
        {
            PlayObject playObject;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                playObject = PlayObjectList[i];
                if (!playObject.Ghost && playObject.Envir == pMap && playObject.BanShout &&
                    Math.Abs(playObject.CurrX - nX) < nWide && Math.Abs(playObject.CurrY - nY) < nWide)
                    playObject.SendMsg(null, wIdent, 0, btFColor, btBColor, 0, sMsg);
            }
        }

        /// <summary>
        /// 计算怪物掉落物品
        /// 即创建怪物对象的时候已经算好要掉落的物品和属性
        /// </summary>
        /// <returns></returns>
        private void MonGetRandomItems(BaseObject mon)
        {
            IList<TMonItem> itemList = null;
            var itemName = string.Empty;
            for (var i = 0; i < MonsterList.Count; i++)
            {
                var monster = MonsterList[i];
                if (string.Compare(monster.sName, mon.CharName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    itemList = monster.ItemList;
                    break;
                }
            }
            if (itemList != null)
            {
                for (var i = 0; i < itemList.Count; i++)
                {
                    var monItem = itemList[i];
                    if (M2Share.RandomNumber.Random(monItem.MaxPoint) <= monItem.SelPoint)
                    {
                        if (string.Compare(monItem.ItemName, Grobal2.sSTRING_GOLDNAME, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            mon.Gold = mon.Gold + monItem.Count / 2 + M2Share.RandomNumber.Random(monItem.Count);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(itemName)) itemName = monItem.ItemName;
                            UserItem userItem = null;
                            if (CopyToUserItemFromName(itemName, ref userItem))
                            {
                                userItem.Dura = (ushort)HUtil32.Round(userItem.DuraMax / 100 * (20 + M2Share.RandomNumber.Random(80)));
                                var stdItem = GetStdItem(userItem.wIndex);
                                if (stdItem == null) continue;
                                if (M2Share.RandomNumber.Random(M2Share.Config.MonRandomAddValue) == 0) //极品掉落几率
                                {
                                    stdItem.RandomUpgradeItem(userItem);
                                }
                                if (new ArrayList(new byte[] { 15, 19, 20, 21, 22, 23, 24, 26 }).Contains(stdItem.StdMode))
                                {
                                    if (stdItem.Shape == 130 || stdItem.Shape == 131 || stdItem.Shape == 132)
                                    {
                                        stdItem.RandomUpgradeUnknownItem(userItem);
                                    }
                                }
                                mon.ItemList.Add(userItem);
                            }
                        }
                    }
                }
            }
        }

        public bool CopyToUserItemFromName(string sItemName, ref UserItem item)
        {
            if (string.IsNullOrEmpty(sItemName)) return false;
            for (var i = 0; i < StdItemList.Count; i++)
            {
                var stdItem = StdItemList[i];
                if (!stdItem.Name.Equals(sItemName, StringComparison.OrdinalIgnoreCase)) continue;
                if (item == null) item = new UserItem();
                item.wIndex = (ushort)(i + 1);
                item.MakeIndex = M2Share.GetItemNumber();
                item.Dura = stdItem.DuraMax;
                item.DuraMax = stdItem.DuraMax;
                return true;
            }
            return false;
        }

        public void ProcessUserMessage(PlayObject playObject, ClientPacket defMsg, string buff)
        {
            var sMsg = string.Empty;
            if (playObject.OffLineFlag) return;
            if (!string.IsNullOrEmpty(buff)) sMsg = buff;
            switch (defMsg.Ident)
            {
                case Grobal2.CM_SPELL:
                    if (M2Share.Config.SpellSendUpdateMsg) // 使用UpdateMsg 可以防止消息队列里有多个操作
                    {
                        playObject.SendUpdateMsg(playObject, defMsg.Ident, defMsg.Tag, HUtil32.LoWord(defMsg.Recog),
                            HUtil32.HiWord(defMsg.Recog), HUtil32.MakeLong(defMsg.Param, defMsg.Series), "");
                    }
                    else
                    {
                        playObject.SendMsg(playObject, defMsg.Ident, defMsg.Tag, HUtil32.LoWord(defMsg.Recog),
                            HUtil32.HiWord(defMsg.Recog), HUtil32.MakeLong(defMsg.Param, defMsg.Series), "");
                    }
                    break;
                case Grobal2.CM_QUERYUSERNAME:
                    playObject.SendMsg(playObject, defMsg.Ident, 0, defMsg.Recog, defMsg.Param, defMsg.Tag, "");
                    break;
                case Grobal2.CM_DROPITEM:
                case Grobal2.CM_TAKEONITEM:
                case Grobal2.CM_TAKEOFFITEM:
                case Grobal2.CM_1005:
                case Grobal2.CM_MERCHANTDLGSELECT:
                case Grobal2.CM_MERCHANTQUERYSELLPRICE:
                case Grobal2.CM_USERSELLITEM:
                case Grobal2.CM_USERBUYITEM:
                case Grobal2.CM_USERGETDETAILITEM:
                case Grobal2.CM_CREATEGROUP:
                case Grobal2.CM_ADDGROUPMEMBER:
                case Grobal2.CM_DELGROUPMEMBER:
                case Grobal2.CM_USERREPAIRITEM:
                case Grobal2.CM_MERCHANTQUERYREPAIRCOST:
                case Grobal2.CM_DEALTRY:
                case Grobal2.CM_DEALADDITEM:
                case Grobal2.CM_DEALDELITEM:
                case Grobal2.CM_USERSTORAGEITEM:
                case Grobal2.CM_USERTAKEBACKSTORAGEITEM:
                case Grobal2.CM_USERMAKEDRUGITEM:
                case Grobal2.CM_GUILDADDMEMBER:
                case Grobal2.CM_GUILDDELMEMBER:
                case Grobal2.CM_GUILDUPDATENOTICE:
                case Grobal2.CM_GUILDUPDATERANKINFO:
                    playObject.SendMsg(playObject, defMsg.Ident, defMsg.Series, defMsg.Recog, defMsg.Param, defMsg.Tag,
                        sMsg);
                    break;
                case Grobal2.CM_PASSWORD:
                case Grobal2.CM_CHGPASSWORD:
                case Grobal2.CM_SETPASSWORD:
                    playObject.SendMsg(playObject, defMsg.Ident, defMsg.Param, defMsg.Recog, defMsg.Series, defMsg.Tag,
                        sMsg);
                    break;
                case Grobal2.CM_ADJUST_BONUS:
                    playObject.SendMsg(playObject, defMsg.Ident, defMsg.Series, defMsg.Recog, defMsg.Param, defMsg.Tag,
                        sMsg);
                    break;
                case Grobal2.CM_HORSERUN:
                case Grobal2.CM_TURN:
                case Grobal2.CM_WALK:
                case Grobal2.CM_SITDOWN:
                case Grobal2.CM_RUN:
                case Grobal2.CM_HIT:
                case Grobal2.CM_HEAVYHIT:
                case Grobal2.CM_BIGHIT:
                case Grobal2.CM_POWERHIT:
                case Grobal2.CM_LONGHIT:
                case Grobal2.CM_CRSHIT:
                case Grobal2.CM_TWINHIT:
                case Grobal2.CM_WIDEHIT:
                case Grobal2.CM_FIREHIT:
                    if (M2Share.Config.ActionSendActionMsg) // 使用UpdateMsg 可以防止消息队列里有多个操作
                    {
                        playObject.SendActionMsg(playObject, defMsg.Ident, defMsg.Tag, HUtil32.LoWord(defMsg.Recog),
                            HUtil32.HiWord(defMsg.Recog), 0, "");
                    }
                    else
                    {
                        playObject.SendMsg(playObject, defMsg.Ident, defMsg.Tag, HUtil32.LoWord(defMsg.Recog),
                            HUtil32.HiWord(defMsg.Recog), 0, "");
                    }
                    break;
                case Grobal2.CM_SAY:
                    playObject.SendMsg(playObject, Grobal2.CM_SAY, 0, 0, 0, 0, sMsg);
                    break;
                default:
                    playObject.SendMsg(playObject, defMsg.Ident, defMsg.Series, defMsg.Recog, defMsg.Param, defMsg.Tag,
                        sMsg);
                    break;
            }
            if (!playObject.m_boReadyRun) return;
            switch (defMsg.Ident)
            {
                case Grobal2.CM_TURN:
                case Grobal2.CM_WALK:
                case Grobal2.CM_SITDOWN:
                case Grobal2.CM_RUN:
                case Grobal2.CM_HIT:
                case Grobal2.CM_HEAVYHIT:
                case Grobal2.CM_BIGHIT:
                case Grobal2.CM_POWERHIT:
                case Grobal2.CM_LONGHIT:
                case Grobal2.CM_WIDEHIT:
                case Grobal2.CM_FIREHIT:
                case Grobal2.CM_CRSHIT:
                case Grobal2.CM_TWINHIT:
                    playObject.RunTick -= 100;
                    break;
            }
        }

        public void SendServerGroupMsg(int nCode, int nServerIdx, string sMsg)
        {
            if (M2Share.ServerIndex == 0)
            {
                SnapsmService.Instance.SendServerSocket(nCode + "/" + nServerIdx + "/" + sMsg);
            }
            else
            {
                SnapsmClient.Instance.SendSocket(nCode + "/" + nServerIdx + "/" + sMsg);
            }
        }

        public void GetIsmChangeServerReceive(string flName)
        {
            for (var i = 0; i < PlayObjectFreeList.Count; i++)
            {
                PlayObject hum = PlayObjectFreeList[i];
                if (hum.m_sSwitchDataTempFile == flName)
                {
                    hum.m_boSwitchDataOK = true;
                    break;
                }
            }
        }

        public void OtherServerUserLogon(int sNum, string uname)
        {
            var name = string.Empty;
            var apmode = HUtil32.GetValidStr3(uname, ref name, ":");
            OtherUserNameList.Remove(name);
            OtherUserNameList.Add(name, new ServerGruopInfo()
            {
                nServerIdx = sNum,
                sCharName = uname
            });
        }

        public void OtherServerUserLogout(int sNum, string uname)
        {
            var name = string.Empty;
            var apmode = HUtil32.GetValidStr3(uname, ref name, ":");
            OtherUserNameList.Remove(name);
            // for (var i = m_OtherUserNameList.Count - 1; i >= 0; i--)
            // {
            //     if (string.Compare(m_OtherUserNameList[i].sCharName, Name, StringComparison.OrdinalIgnoreCase) == 0 && m_OtherUserNameList[i].nServerIdx == sNum)
            //     {
            //         m_OtherUserNameList.RemoveAt(i);
            //         break;
            //     }
            // }
        }

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <returns></returns>
        private BaseObject AddBaseObject(string sMapName, short nX, short nY, int nMonRace, string sMonName)
        {
            BaseObject result = null;
            BaseObject cert = null;
            int n1C;
            int n20;
            int n24;
            object p28;
            var map = M2Share.MapMgr.FindMap(sMapName);
            if (map == null) return result;
            switch (nMonRace)
            {
                case MonsterConst.SUPREGUARD:
                    cert = new SuperGuard();
                    break;
                case MonsterConst.PETSUPREGUARD:
                    cert = new PetSuperGuard();
                    break;
                case MonsterConst.ARCHER_POLICE:
                    cert = new ArcherPolice();
                    break;
                case MonsterConst.ANIMAL_CHICKEN:
                    cert = new MonsterObject
                    {
                        Animal = true,
                        MeatQuality = (ushort)(M2Share.RandomNumber.Random(3500) + 3000),
                        BodyLeathery = 50
                    };
                    break;
                case MonsterConst.ANIMAL_DEER:
                    if (M2Share.RandomNumber.Random(30) == 0)
                        cert = new ChickenDeer
                        {
                            Animal = true,
                            MeatQuality = (ushort)(M2Share.RandomNumber.Random(20000) + 10000),
                            BodyLeathery = 150
                        };
                    else
                        cert = new MonsterObject()
                        {
                            Animal = true,
                            MeatQuality = (ushort)(M2Share.RandomNumber.Random(8000) + 8000),
                            BodyLeathery = 150
                        };
                    break;
                case MonsterConst.ANIMAL_WOLF:
                    cert = new AtMonster
                    {
                        Animal = true,
                        MeatQuality = (ushort)(M2Share.RandomNumber.Random(8000) + 8000),
                        BodyLeathery = 150
                    };
                    break;
                case MonsterConst.TRAINER:
                    cert = new Trainer();
                    break;
                case MonsterConst.MONSTER_OMA:
                    cert = new MonsterObject();
                    break;
                case MonsterConst.MONSTER_OMAKNIGHT:
                    cert = new AtMonster();
                    break;
                case MonsterConst.MONSTER_SPITSPIDER:
                    cert = new SpitSpider();
                    break;
                case 83:
                    cert = new SlowAtMonster();
                    break;
                case 84:
                    cert = new Scorpion();
                    break;
                case MonsterConst.MONSTER_STICK:
                    cert = new StickMonster();
                    break;
                case 86:
                    cert = new AtMonster();
                    break;
                case MonsterConst.MONSTER_DUALAXE:
                    cert = new DualAxeMonster();
                    break;
                case 88:
                    cert = new AtMonster();
                    break;
                case 89:
                    cert = new AtMonster();
                    break;
                case 90:
                    cert = new GasAttackMonster();
                    break;
                case 91:
                    cert = new MagCowMonster();
                    break;
                case 92:
                    cert = new CowKingMonster();
                    break;
                case MonsterConst.MONSTER_THONEDARK:
                    cert = new ThornDarkMonster();
                    break;
                case MonsterConst.MONSTER_LIGHTZOMBI:
                    cert = new LightingZombi();
                    break;
                case MonsterConst.MONSTER_DIGOUTZOMBI:
                    cert = new DigOutZombi();
                    if (M2Share.RandomNumber.Random(2) == 0) cert.Bo2Ba = true;
                    break;
                case MonsterConst.MONSTER_ZILKINZOMBI:
                    cert = new ZilKinZombi();
                    if (M2Share.RandomNumber.Random(4) == 0) cert.Bo2Ba = true;
                    break;
                case 97:
                    cert = new CowMonster();
                    if (M2Share.RandomNumber.Random(2) == 0) cert.Bo2Ba = true;
                    break;
                case MonsterConst.MONSTER_WHITESKELETON:
                    cert = new WhiteSkeleton();
                    break;
                case MonsterConst.MONSTER_SCULTURE:
                    cert = new ScultureMonster
                    {
                        Bo2Ba = true
                    };
                    break;
                case MonsterConst.MONSTER_SCULTUREKING:
                    cert = new ScultureKingMonster();
                    break;
                case MonsterConst.MONSTER_BEEQUEEN:
                    cert = new BeeQueen();
                    break;
                case 104:
                    cert = new ArcherMonster();
                    break;
                case 105:
                    cert = new GasMothMonster();
                    break;
                case 106: // 楔蛾
                    cert = new GasDungMonster();
                    break;
                case 107:
                    cert = new CentipedeKingMonster();
                    break;
                case 110:
                    cert = new CastleDoor();
                    break;
                case 111:
                    cert = new WallStructure();
                    break;
                case MonsterConst.MONSTER_ARCHERGUARD:
                    cert = new ArcherGuard();
                    break;
                case MonsterConst.MONSTER_ELFMONSTER:
                    cert = new ElfMonster();
                    break;
                case MonsterConst.MONSTER_ELFWARRIOR:
                    cert = new ElfWarriorMonster();
                    break;
                case 115:
                    cert = new BigHeartMonster();
                    break;
                case 116:
                    cert = new SpiderHouseMonster();
                    break;
                case 117:
                    cert = new ExplosionSpider();
                    break;
                case 118:
                    cert = new HighRiskSpider();
                    break;
                case 119:
                    cert = new BigPoisionSpider();
                    break;
                case 120:
                    cert = new SoccerBall();
                    break;
                case 130:
                    cert = new DoubleCriticalMonster();
                    break;
                case 131:
                    cert = new RonObject();
                    break;
                case 132:
                    cert = new SandMobObject();
                    break;
                case 133:
                    cert = new MagicMonObject();
                    break;
                case 134:
                    cert = new BoneKingMonster();
                    break;
                case 200:
                    cert = new ElectronicScolpionMon();
                    break;
                case 201:
                    cert = new CloneMonster();
                    break;
                case 203:
                    cert = new TeleMonster();
                    break;
                case 206:
                    cert = new Khazard();
                    break;
                case 208:
                    cert = new GreenMonster();
                    break;
                case 209:
                    cert = new RedMonster();
                    break;
                case 210:
                    cert = new FrostTiger();
                    break;
                case 214:
                    cert = new FireMonster();
                    break;
                case 215:
                    cert = new FireballMonster();
                    break;
            }

            if (cert != null)
            {
                MonInitialize(cert, sMonName);
                cert.Envir = map;
                cert.MapName = sMapName;
                cert.CurrX = nX;
                cert.CurrY = nY;
                cert.Direction = M2Share.RandomNumber.RandomByte(8);
                cert.CharName = sMonName;
                cert.WAbil = cert.Abil;
                cert.OnEnvirnomentChanged();
                if (M2Share.RandomNumber.Random(100) < cert.CoolEyeCode) cert.CoolEye = true;
                MonGetRandomItems(cert);
                cert.Initialize();
                if (cert.AddtoMapSuccess)
                {
                    p28 = null;
                    if (cert.Envir.Width < 50)
                        n20 = 2;
                    else
                        n20 = 3;
                    if (cert.Envir.Height < 250)
                    {
                        if (cert.Envir.Height < 30)
                            n24 = 2;
                        else
                            n24 = 20;
                    }
                    else
                    {
                        n24 = 50;
                    }

                    n1C = 0;
                    while (true)
                    {
                        if (!cert.Envir.CanWalk(cert.CurrX, cert.CurrY, false))
                        {
                            if (cert.Envir.Width - n24 - 1 > cert.CurrX)
                            {
                                cert.CurrX += (short)n20;
                            }
                            else
                            {
                                cert.CurrX = (short)(M2Share.RandomNumber.Random(cert.Envir.Width / 2) + n24);
                                if (cert.Envir.Height - n24 - 1 > cert.CurrY)
                                    cert.CurrY += (short)n20;
                                else
                                    cert.CurrY = (short)(M2Share.RandomNumber.Random(cert.Envir.Height / 2) + n24);
                            }
                        }
                        else
                        {
                            p28 = cert.Envir.AddToMap(cert.CurrX, cert.CurrY, CellType.MovingObject, cert);
                            break;
                        }

                        n1C++;
                        if (n1C >= 31) break;
                    }

                    if (p28 == null)
                        //Cert.Free;
                        cert = null;
                }
            }

            result = cert;
            return result;
        }

        /// <summary>
        /// 创建怪物对象
        /// 在指定时间内创建完对象，则返加TRUE，如果超过指定时间则返回FALSE
        /// </summary>
        /// <returns></returns>
        private bool RegenMonsters(MonGenInfo monGen, int nCount)
        {
            BaseObject cert;
            const string sExceptionMsg = "[Exception] TUserEngine::RegenMonsters";
            var result = true;
            var dwStartTick = HUtil32.GetTickCount();
            try
            {
                if (monGen.nRace > 0)
                {
                    short nX;
                    short nY;
                    if (M2Share.RandomNumber.Random(100) < monGen.nMissionGenRate)
                    {
                        nX = (short)(monGen.nX - monGen.nRange + M2Share.RandomNumber.Random(monGen.nRange * 2 + 1));
                        nY = (short)(monGen.nY - monGen.nRange + M2Share.RandomNumber.Random(monGen.nRange * 2 + 1));
                        for (var i = 0; i < nCount; i++)
                        {
                            cert = AddBaseObject(monGen.sMapName, (short)(nX - 10 + M2Share.RandomNumber.Random(20)),
                                (short)(nY - 10 + M2Share.RandomNumber.Random(20)), monGen.nRace, monGen.sMonName);
                            if (cert != null)
                            {
                                cert.CanReAlive = true;
                                cert.ReAliveTick = HUtil32.GetTickCount();
                                cert.MonGen = monGen;
                                monGen.nActiveCount++;
                                monGen.CertList.Add(cert);
                            }
                            if ((HUtil32.GetTickCount() - dwStartTick) > M2Share.g_dwZenLimit)
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < nCount; i++)
                        {
                            nX = (short)(monGen.nX - monGen.nRange + M2Share.RandomNumber.Random(monGen.nRange * 2 + 1));
                            nY = (short)(monGen.nY - monGen.nRange + M2Share.RandomNumber.Random(monGen.nRange * 2 + 1));
                            cert = AddBaseObject(monGen.sMapName, nX, nY, monGen.nRace, monGen.sMonName);
                            if (cert != null)
                            {
                                cert.CanReAlive = true;
                                cert.ReAliveTick = HUtil32.GetTickCount();
                                cert.MonGen = monGen;
                                monGen.nActiveCount++;
                                monGen.CertList.Add(cert);
                            }
                            if (HUtil32.GetTickCount() - dwStartTick > M2Share.g_dwZenLimit)
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                _logger.Error(sExceptionMsg);
            }
            return result;
        }

        public PlayObject GetPlayObject(string sName)
        {
            PlayObject result = null;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                if (string.Compare(PlayObjectList[i].CharName, sName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    PlayObject playObject = PlayObjectList[i];
                    if (!playObject.Ghost)
                    {
                        if (!(playObject.m_boPasswordLocked && playObject.ObMode && playObject.AdminMode))
                        {
                            result = playObject;
                        }
                    }
                    break;
                }
            }
            return result;
        }

        public void KickPlayObjectEx(string sName)
        {
            PlayObject playObject;
            HUtil32.EnterCriticalSection(M2Share.ProcessHumanCriticalSection);
            try
            {
                for (var i = 0; i < PlayObjectList.Count; i++)
                {
                    if (string.Compare(PlayObjectList[i].CharName, sName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        playObject = PlayObjectList[i];
                        playObject.m_boEmergencyClose = true;
                        break;
                    }
                }
            }
            finally
            {
                HUtil32.LeaveCriticalSection(M2Share.ProcessHumanCriticalSection);
            }
        }

        public PlayObject GetPlayObjectEx(string sName)
        {
            PlayObject result = null;
            HUtil32.EnterCriticalSection(M2Share.ProcessHumanCriticalSection);
            try
            {
                for (var i = 0; i < PlayObjectList.Count; i++)
                {
                    if (string.Compare(PlayObjectList[i].CharName, sName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        result = PlayObjectList[i];
                        break;
                    }
                }
            }
            finally
            {
                HUtil32.LeaveCriticalSection(M2Share.ProcessHumanCriticalSection);
            }
            return result;
        }

        public object FindMerchant(int merchantId)
        {
            var normNpc = M2Share.ActorMgr.Get(merchantId);
            NormNpc npcObject = null;
            var npcType = normNpc.GetType();
            if (npcType == typeof(Merchant))
            {
                npcObject = (Merchant)Convert.ChangeType(normNpc, typeof(Merchant));
            }
            if (npcType == typeof(GuildOfficial))
            {
                npcObject = (GuildOfficial)Convert.ChangeType(normNpc, typeof(GuildOfficial));
            }
            if (npcType == typeof(NormNpc))
            {
                npcObject = (NormNpc)Convert.ChangeType(normNpc, typeof(NormNpc));
            }
            if (npcType == typeof(CastleOfficial))
            {
                npcObject = (CastleOfficial)Convert.ChangeType(normNpc, typeof(CastleOfficial));
            }
            return npcObject;
        }

        public object FindNpc(int npcId)
        {
            return M2Share.ActorMgr.Get(npcId); ;
        }

        /// <summary>
        /// 获取指定地图范围对象数
        /// </summary>
        /// <returns></returns>
        public int GetMapOfRangeHumanCount(Envirnoment envir, int nX, int nY, int nRange)
        {
            var result = 0;
            PlayObject playObject;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                playObject = PlayObjectList[i];
                if (!playObject.Ghost && playObject.Envir == envir)
                {
                    if (Math.Abs(playObject.CurrX - nX) < nRange && Math.Abs(playObject.CurrY - nY) < nRange)
                    {
                        result++;
                    }
                }
            }
            return result;
        }

        public bool GetHumPermission(string sUserName, ref string sIPaddr, ref byte btPermission)
        {
            var result = false;
            btPermission = (byte)M2Share.Config.StartPermission;
            for (var i = 0; i < AdminList.Count; i++)
            {
                TAdminInfo adminInfo = AdminList[i];
                if (string.Compare(adminInfo.sChrName, sUserName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    btPermission = (byte)adminInfo.nLv;
                    sIPaddr = adminInfo.sIPaddr;
                    result = true;
                    break;
                }
            }
            return result;
        }

        public void AddUserOpenInfo(UserOpenInfo userOpenInfo)
        {
            HUtil32.EnterCriticalSection(LoadPlaySection);
            try
            {
                LoadPlayList.Add(userOpenInfo);
            }
            finally
            {
                HUtil32.LeaveCriticalSection(LoadPlaySection);
            }
        }

        private void KickOnlineUser(string sChrName)
        {
            PlayObject playObject;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                playObject = PlayObjectList[i];
                if (string.Compare(playObject.CharName, sChrName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    playObject.m_boKickFlag = true;
                    break;
                }
            }
        }

        private void SendChangeServer(PlayObject playObject, byte nServerIndex)
        {
            var sIPaddr = string.Empty;
            var nPort = 0;
            const string sMsg = "{0}/{1}";
            if (M2Share.GetMultiServerAddrPort(nServerIndex, ref sIPaddr, ref nPort))
            {
                playObject.m_boReconnection = true;
                playObject.SendDefMessage(Grobal2.SM_RECONNECT, 0, 0, 0, 0, string.Format(sMsg, sIPaddr, nPort));
            }
        }

        public void SaveHumanRcd(PlayObject playObject)
        {
            if (playObject.IsRobot) //Bot玩家不保存数据
            {
                return;
            }
            var saveRcd = new TSaveRcd
            {
                sAccount = playObject.m_sUserID,
                sChrName = playObject.CharName,
                nSessionID = playObject.m_nSessionID,
                PlayObject = playObject,
                HumanRcd = new THumDataInfo()
            };
            saveRcd.HumanRcd.Data.Initialization();
            playObject.MakeSaveRcd(ref saveRcd.HumanRcd);
            M2Share.FrontEngine.AddToSaveRcdList(saveRcd);
        }

        private void AddToHumanFreeList(PlayObject playObject)
        {
            playObject.GhostTick = HUtil32.GetTickCount();
            PlayObjectFreeList.Add(playObject);
        }

        private void GetHumData(PlayObject playObject, ref THumDataInfo humanRcd)
        {
            THumInfoData humData;
            UserItem[] humItems;
            UserItem[] bagItems;
            TMagicRcd[] humMagic;
            SystemModule.Packet.ServerPackets.MagicInfo magicInfo;
            UserMagic userMagic;
            UserItem[] storageItems;
            UserItem userItem;
            humData = humanRcd.Data;
            playObject.CharName = humData.sCharName;
            playObject.MapName = humData.sCurMap;
            playObject.CurrX = humData.wCurX;
            playObject.CurrY = humData.wCurY;
            playObject.Direction = humData.btDir;
            playObject.Hair = humData.btHair;
            playObject.Gender = Enum.Parse<PlayGender>(humData.btSex.ToString());
            playObject.Job = (PlayJob)humData.btJob;
            playObject.Gold = humData.nGold;
            playObject.Abil.Level = humData.Abil.Level;
            playObject.Abil.HP = humData.Abil.HP;
            playObject.Abil.MP = humData.Abil.MP;
            playObject.Abil.MaxHP = humData.Abil.MaxHP;
            playObject.Abil.MaxMP = humData.Abil.MaxMP;
            playObject.Abil.Exp = humData.Abil.Exp;
            playObject.Abil.MaxExp = humData.Abil.MaxExp;
            playObject.Abil.Weight = humData.Abil.Weight;
            playObject.Abil.MaxWeight = humData.Abil.MaxWeight;
            playObject.Abil.WearWeight = humData.Abil.WearWeight;
            playObject.Abil.MaxWearWeight = humData.Abil.MaxWearWeight;
            playObject.Abil.HandWeight = humData.Abil.HandWeight;
            playObject.Abil.MaxHandWeight = humData.Abil.MaxHandWeight;
            playObject.StatusTimeArr = humData.wStatusTimeArr;
            playObject.HomeMap = humData.sHomeMap;
            playObject.HomeX = humData.wHomeX;
            playObject.HomeY = humData.wHomeY;
            playObject.BonusAbil = humData.BonusAbil;
            playObject.BonusPoint = humData.nBonusPoint;
            playObject.m_btCreditPoint = humData.btCreditPoint;
            playObject.m_btReLevel = humData.btReLevel;
            playObject.m_sMasterName = humData.sMasterName;
            playObject.m_boMaster = humData.boMaster;
            playObject.m_sDearName = humData.sDearName;
            playObject.m_sStoragePwd = humData.sStoragePwd;
            if (playObject.m_sStoragePwd != "")
            {
                playObject.m_boPasswordLocked = true;
            }
            playObject.m_nGameGold = humData.nGameGold;
            playObject.m_nGamePoint = humData.nGamePoint;
            playObject.m_nPayMentPoint = humData.nPayMentPoint;
            playObject.PkPoint = humData.nPKPoint;
            if (humData.btAllowGroup > 0)
            {
                playObject.AllowGroup = true;
            }
            else
            {
                playObject.AllowGroup = false;
            }
            playObject.BtB2 = humData.btF9;
            playObject.AttatckMode = (AttackMode)humData.btAttatckMode;
            playObject.IncHealth = humData.btIncHealth;
            playObject.IncSpell = humData.btIncSpell;
            playObject.IncHealing = humData.btIncHealing;
            playObject.FightZoneDieCount = humData.btFightZoneDieCount;
            playObject.m_sUserID = humData.sAccount;
            playObject.m_boLockLogon = humData.boLockLogon;
            playObject.m_wContribution = humData.wContribution;
            playObject.HungerStatus = humData.nHungerStatus;
            playObject.AllowGuildReCall = humData.boAllowGuildReCall;
            playObject.GroupRcallTime = humData.wGroupRcallTime;
            playObject.BodyLuck = humData.dBodyLuck;
            playObject.AllowGroupReCall = humData.boAllowGroupReCall;
            playObject.QuestUnitOpen = humData.QuestUnitOpen;
            playObject.QuestUnit = humData.QuestUnit;
            playObject.QuestFlag = humData.QuestFlag;
            humItems = humanRcd.Data.HumItems;
            playObject.UseItems[Grobal2.U_DRESS] = humItems[Grobal2.U_DRESS];
            playObject.UseItems[Grobal2.U_WEAPON] = humItems[Grobal2.U_WEAPON];
            playObject.UseItems[Grobal2.U_RIGHTHAND] = humItems[Grobal2.U_RIGHTHAND];
            playObject.UseItems[Grobal2.U_NECKLACE] = humItems[Grobal2.U_HELMET];
            playObject.UseItems[Grobal2.U_HELMET] = humItems[Grobal2.U_NECKLACE];
            playObject.UseItems[Grobal2.U_ARMRINGL] = humItems[Grobal2.U_ARMRINGL];
            playObject.UseItems[Grobal2.U_ARMRINGR] = humItems[Grobal2.U_ARMRINGR];
            playObject.UseItems[Grobal2.U_RINGL] = humItems[Grobal2.U_RINGL];
            playObject.UseItems[Grobal2.U_RINGR] = humItems[Grobal2.U_RINGR];
            playObject.UseItems[Grobal2.U_BUJUK] = humItems[Grobal2.U_BUJUK];
            playObject.UseItems[Grobal2.U_BELT] = humItems[Grobal2.U_BELT];
            playObject.UseItems[Grobal2.U_BOOTS] = humItems[Grobal2.U_BOOTS];
            playObject.UseItems[Grobal2.U_CHARM] = humItems[Grobal2.U_CHARM];
            bagItems = humanRcd.Data.BagItems;
            if (bagItems != null)
            {
                for (var i = 0; i < bagItems.Length; i++)
                {
                    if (bagItems[i] == null)
                    {
                        continue;
                    }
                    if (bagItems[i].wIndex > 0)
                    {
                        userItem = bagItems[i];
                        playObject.ItemList.Add(userItem);
                    }
                }
            }
            humMagic = humanRcd.Data.Magic;
            if (humMagic != null)
            {
                for (var i = 0; i < humMagic.Length; i++)
                {
                    if (humMagic[i] == null)
                    {
                        continue;
                    }
                    magicInfo = FindMagic(humMagic[i].wMagIdx);
                    if (magicInfo != null)
                    {
                        userMagic = new UserMagic();
                        userMagic.MagicInfo = magicInfo;
                        userMagic.wMagIdx = humMagic[i].wMagIdx;
                        userMagic.btLevel = humMagic[i].btLevel;
                        userMagic.btKey = humMagic[i].btKey;
                        userMagic.nTranPoint = humMagic[i].nTranPoint;
                        playObject.MagicList.Add(userMagic);
                    }
                }
            }
            storageItems = humanRcd.Data.StorageItems;
            if (storageItems != null)
            {
                for (var i = 0; i < storageItems.Length; i++)
                {
                    if (storageItems[i] == null)
                    {
                        continue;
                    }
                    if (storageItems[i].wIndex > 0)
                    {
                        userItem = storageItems[i];
                        playObject.StorageItemList.Add(userItem);
                    }
                }
            }
        }

        private string GetHomeInfo(PlayJob nJob, ref short nX, ref short nY)
        {
            string result;
            int I;
            if (M2Share.StartPointList.Count > 0)
            {
                if (M2Share.StartPointList.Count > M2Share.Config.StartPointSize)
                    I = M2Share.RandomNumber.Random(M2Share.Config.StartPointSize);
                else
                    I = 0;
                result = M2Share.GetStartPointInfo(I, ref nX, ref nY);
            }
            else
            {
                result = M2Share.Config.HomeMap;
                nX = M2Share.Config.HomeX;
                nX = M2Share.Config.HomeY;
            }
            return result;
        }

        private short GetRandHomeX(PlayObject playObject)
        {
            return (short)(M2Share.RandomNumber.Random(3) + (playObject.HomeX - 2));
        }

        private short GetRandHomeY(PlayObject playObject)
        {
            return (short)(M2Share.RandomNumber.Random(3) + (playObject.HomeY - 2));
        }

        public SystemModule.Packet.ServerPackets.MagicInfo FindMagic(int nMagIdx)
        {
            SystemModule.Packet.ServerPackets.MagicInfo result = null;
            for (var i = 0; i < MagicList.Count; i++)
            {
                SystemModule.Packet.ServerPackets.MagicInfo magic = MagicList[i];
                if (magic.wMagicID == nMagIdx)
                {
                    result = magic;
                    break;
                }
            }
            return result;
        }

        private void MonInitialize(BaseObject baseObject, string sMonName)
        {
            for (var i = 0; i < MonsterList.Count; i++)
            {
                TMonInfo monster = MonsterList[i];
                if (string.Compare(monster.sName, sMonName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    baseObject.Race = monster.btRace;
                    baseObject.RaceImg = monster.btRaceImg;
                    baseObject.Appr = monster.wAppr;
                    baseObject.Abil.Level = (byte)monster.wLevel;
                    baseObject.LifeAttrib = monster.btLifeAttrib;
                    baseObject.CoolEyeCode = (byte)monster.wCoolEye;
                    baseObject.FightExp = monster.dwExp;
                    baseObject.Abil.HP = monster.wHP;
                    baseObject.Abil.MaxHP = monster.wHP;
                    baseObject.MonsterWeapon = HUtil32.LoByte(monster.wMP);
                    baseObject.Abil.MP = 0;
                    baseObject.Abil.MaxMP = monster.wMP;
                    baseObject.Abil.AC = HUtil32.MakeLong(monster.wAC, monster.wAC);
                    baseObject.Abil.MAC = HUtil32.MakeLong(monster.wMAC, monster.wMAC);
                    baseObject.Abil.DC = HUtil32.MakeLong(monster.wDC, monster.wMaxDC);
                    baseObject.Abil.MC = HUtil32.MakeLong(monster.wMC, monster.wMC);
                    baseObject.Abil.SC = HUtil32.MakeLong(monster.wSC, monster.wSC);
                    baseObject.SpeedPoint = (byte)monster.wSpeed;
                    baseObject.HitPoint = (byte)monster.wHitPoint;
                    baseObject.WalkSpeed = monster.wWalkSpeed;
                    baseObject.WalkStep = monster.wWalkStep;
                    baseObject.WalkWait = monster.wWalkWait;
                    baseObject.NextHitTime = monster.wAttackSpeed;
                    baseObject.NastyMode = monster.boAggro;
                    baseObject.NoTame = monster.boTame;
                    break;
                }
            }
        }

        public bool OpenDoor(Envirnoment envir, int nX, int nY)
        {
            var result = false;
            var door = envir.GetDoor(nX, nY);
            if (door != null && !door.Status.boOpened && !door.Status.bo01)
            {
                door.Status.boOpened = true;
                door.Status.dwOpenTick = HUtil32.GetTickCount();
                SendDoorStatus(envir, nX, nY, Grobal2.RM_DOOROPEN, 0, nX, nY, 0, "");
                result = true;
            }
            return result;
        }

        private bool CloseDoor(Envirnoment envir, TDoorInfo door)
        {
            var result = false;
            if (door != null && door.Status.boOpened)
            {
                door.Status.boOpened = false;
                SendDoorStatus(envir, door.nX, door.nY, Grobal2.RM_DOORCLOSE, 0, door.nX, door.nY, 0, "");
                result = true;
            }
            return result;
        }

        private void SendDoorStatus(Envirnoment envir, int nX, int nY, short wIdent, short wX, int nDoorX, int nDoorY,
            int nA, string sStr)
        {
            int n1C = nX - 12;
            int n24 = nX + 12;
            int n20 = nY - 12;
            int n28 = nY + 12;
            for (var n10 = n1C; n10 <= n24; n10++)
            {
                for (var n14 = n20; n14 <= n28; n14++)
                {
                    var cellsuccess = false;
                    var cellInfo = envir.GetCellInfo(n10, n14, ref cellsuccess);
                    if (cellsuccess && cellInfo.IsAvailable)
                    {
                        for (var i = 0; i < cellInfo.Count; i++)
                        {
                            var osObject = cellInfo.ObjList[i];
                            if (osObject != null && osObject.CellType == CellType.MovingObject)
                            {
                                var baseObject = M2Share.ActorMgr.Get(osObject.CellObjId);;
                                if (baseObject != null && !baseObject.Ghost && baseObject.Race == Grobal2.RC_PLAYOBJECT)
                                {
                                    baseObject.SendMsg(baseObject, wIdent, wX, nDoorX, nDoorY, nA, sStr);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProcessMapDoor()
        {
            var dorrList = M2Share.MapMgr.GetDoorMapList();
            for (var i = 0; i < dorrList.Count; i++)
            {
                var envir = dorrList[i];
                for (var j = 0; j < envir.DoorList.Count; j++)
                {
                    TDoorInfo door = envir.DoorList[j];
                    if (door.Status.boOpened)
                    {
                        if ((HUtil32.GetTickCount() - door.Status.dwOpenTick) > 5 * 1000)
                        {
                            CloseDoor(envir, door);
                        }
                    }
                }
            }
        }

        private void ProcessEvents()
        {
            int count;
            for (var i = MagicEventList.Count - 1; i >= 0; i--)
            {
                MagicEvent magicEvent = MagicEventList[i];
                if (magicEvent != null)
                {
                    for (var j = magicEvent.BaseObjectList.Count - 1; j >= 0; j--)
                    {
                        BaseObject baseObject = magicEvent.BaseObjectList[j];
                        if (baseObject.Death || baseObject.Ghost || !baseObject.HolySeize)
                            magicEvent.BaseObjectList.RemoveAt(j);
                    }
                    if (magicEvent.BaseObjectList.Count <= 0 || (HUtil32.GetTickCount() - magicEvent.dwStartTick) > magicEvent.dwTime ||
                        (HUtil32.GetTickCount() - magicEvent.dwStartTick) > 180000)
                    {
                        count = 0;
                        while (true)
                        {
                            if (magicEvent.Events[count] != null) magicEvent.Events[count].Close();
                            count++;
                            if (count >= 8) break;
                        }
                        magicEvent = null;
                        MagicEventList.RemoveAt(i);
                    }
                }
            }
        }

        public SystemModule.Packet.ServerPackets.MagicInfo FindMagic(string sMagicName)
        {
            SystemModule.Packet.ServerPackets.MagicInfo result = null;
            for (var i = 0; i < MagicList.Count; i++)
            {
                SystemModule.Packet.ServerPackets.MagicInfo magic = MagicList[i];
                if (magic.sMagicName.Equals(sMagicName, StringComparison.OrdinalIgnoreCase))
                {
                    result = magic;
                    break;
                }
            }
            return result;
        }

        public int GetMapRangeMonster(Envirnoment envir, int nX, int nY, int nRange, IList<BaseObject> list)
        {
            var result = 0;
            if (envir == null) return result;
            for (var i = 0; i < MonGenList.Count; i++)
            {
                var monGen = MonGenList[i];
                if (monGen == null) continue;
                if (monGen.Envir != null && monGen.Envir != envir) continue;
                for (var j = 0; j < monGen.CertList.Count; j++)
                {
                    var baseObject = monGen.CertList[j];
                    if (!baseObject.Death && !baseObject.Ghost && baseObject.Envir == envir &&
                        Math.Abs(baseObject.CurrX - nX) <= nRange && Math.Abs(baseObject.CurrY - nY) <= nRange)
                    {
                        if (list != null) list.Add(baseObject);
                        result++;
                    }
                }
            }
            return result;
        }

        public void AddMerchant(Merchant merchant)
        {
            MerchantList.Add(merchant);
        }

        public int GetMerchantList(Envirnoment envir, int nX, int nY, int nRange, IList<BaseObject> tmpList)
        {
            for (var i = 0; i < MerchantList.Count; i++)
            {
                var merchant = MerchantList[i];
                if (merchant.Envir == envir && Math.Abs(merchant.CurrX - nX) <= nRange &&
                    Math.Abs(merchant.CurrY - nY) <= nRange) tmpList.Add(merchant);
            }
            return tmpList.Count;
        }

        public int GetNpcList(Envirnoment envir, int nX, int nY, int nRange, IList<BaseObject> tmpList)
        {
            for (var i = 0; i < QuestNpcList.Count; i++)
            {
                var npc = QuestNpcList[i];
                if (npc.Envir == envir && Math.Abs(npc.CurrX - nX) <= nRange &&
                    Math.Abs(npc.CurrY - nY) <= nRange) tmpList.Add(npc);
            }
            return tmpList.Count;
        }

        public void ReloadMerchantList()
        {
            for (var i = 0; i < MerchantList.Count; i++)
            {
                Merchant merchant = MerchantList[i];
                if (!merchant.Ghost)
                {
                    merchant.ClearScript();
                    merchant.LoadNPCScript();
                }
            }
        }

        public void ReloadNpcList()
        {
            for (var i = 0; i < QuestNpcList.Count; i++)
            {
                NormNpc npc = QuestNpcList[i];
                npc.ClearScript();
                npc.LoadNPCScript();
            }
        }

        public int GetMapMonster(Envirnoment envir, IList<BaseObject> list)
        {
            var result = 0;
            if (envir == null) return result;
            for (var i = 0; i < MonGenList.Count; i++)
            {
                MonGenInfo monGen = MonGenList[i];
                if (monGen == null) continue;
                for (var j = 0; j < monGen.CertList.Count; j++)
                {
                    BaseObject baseObject = monGen.CertList[j];
                    if (!baseObject.Death && !baseObject.Ghost && baseObject.Envir == envir)
                    {
                        if (list != null)
                            list.Add(baseObject);
                        result++;
                    }
                }
            }
            return result;
        }

        public void HumanExpire(string sAccount)
        {
            if (!M2Share.Config.KickExpireHuman) return;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                PlayObject playObject = PlayObjectList[i];
                if (string.Compare(playObject.m_sUserID, sAccount, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    playObject.m_boExpire = true;
                    break;
                }
            }
        }

        public int GetMapHuman(string sMapName)
        {
            var result = 0;
            var envir = M2Share.MapMgr.FindMap(sMapName);
            if (envir == null) return result;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                PlayObject playObject = PlayObjectList[i];
                if (!playObject.Death && !playObject.Ghost && playObject.Envir == envir) result++;
            }
            return result;
        }

        public int GetMapRageHuman(Envirnoment envir, int nRageX, int nRageY, int nRage, IList<BaseObject> list)
        {
            var result = 0;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                PlayObject playObject = PlayObjectList[i];
                if (!playObject.Death && !playObject.Ghost && playObject.Envir == envir &&
                    Math.Abs(playObject.CurrX - nRageX) <= nRage && Math.Abs(playObject.CurrY - nRageY) <= nRage)
                {
                    list.Add(playObject);
                    result++;
                }
            }
            return result;
        }

        public ushort GetStdItemIdx(string sItemName)
        {
            ushort result = 0;
            if (string.IsNullOrEmpty(sItemName)) return result;
            for (var i = 0; i < StdItemList.Count; i++)
            {
                var stdItem = StdItemList[i];
                if (stdItem.Name.Equals(sItemName, StringComparison.OrdinalIgnoreCase))
                {
                    result = (ushort)(i + 1);
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// 向每个人物发送消息
        /// </summary>
        public void SendBroadCastMsgExt(string sMsg, MsgType msgType)
        {
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                PlayObject playObject = PlayObjectList[i];
                if (!playObject.Ghost)
                    playObject.SysMsg(sMsg, MsgColor.Red, msgType);
            }
        }

        public void SendBroadCastMsg(string sMsg, MsgType msgType)
        {
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                PlayObject playObject = PlayObjectList[i];
                if (!playObject.Ghost)
                {
                    playObject.SysMsg(sMsg, MsgColor.Red, msgType);
                }
            }
        }

        public void sub_4AE514(TGoldChangeInfo goldChangeInfo)
        {
            var goldChange = goldChangeInfo;
            HUtil32.EnterCriticalSection(LoadPlaySection);
            _mChangeHumanDbGoldList.Add(goldChange);
        }

        public void ClearMonSayMsg()
        {
            for (var i = 0; i < MonGenList.Count; i++)
            {
                MonGenInfo monGen = MonGenList[i];
                for (var j = 0; j < monGen.CertList.Count; j++)
                {
                    BaseObject monBaseObject = monGen.CertList[j];
                    monBaseObject.SayMsgList = null;
                }
            }
        }

        public string GetHomeInfo(ref short nX, ref short nY)
        {
            string result;
            if (M2Share.StartPointList.Count > 0)
            {
                int I;
                if (M2Share.StartPointList.Count > M2Share.Config.StartPointSize)
                    I = M2Share.RandomNumber.Random(M2Share.Config.StartPointSize);
                else
                    I = 0;
                result = M2Share.GetStartPointInfo(I, ref nX, ref nY);
            }
            else
            {
                result = M2Share.Config.HomeMap;
                nX = M2Share.Config.HomeX;
                nX = M2Share.Config.HomeY;
            }
            return result;
        }

        public void StartAi()
        {
           // if (_processRobotTimer.ThreadState != ThreadState.Running)
           // {
           //     _processRobotTimer.Start();
           // }
        }

        public void AddAiLogon(RoBotLogon ai)
        {
            RobotLogonList.Add(ai);
        }

        private bool RegenAiObject(RoBotLogon ai)
        {
            var playObject = AddAiPlayObject(ai);
            if (playObject != null)
            {
                playObject.HomeMap = GetHomeInfo(ref playObject.HomeX, ref playObject.HomeY);
                playObject.m_sUserID = "假人" + ai.sCharName;
                playObject.Start(TPathType.t_Dynamic);
                BotPlayObjectList.Add(playObject);
                return true;
            }
            return false;
        }

        private RobotPlayObject AddAiPlayObject(RoBotLogon ai)
        {
            int n1C;
            int n20;
            int n24;
            object p28;
            var envirnoment = M2Share.MapMgr.FindMap(ai.sMapName);
            if (envirnoment == null)
            {
                return null;
            }
            RobotPlayObject cert = new RobotPlayObject();
            cert.Envir = envirnoment;
            cert.MapName = ai.sMapName;
            cert.CurrX = ai.nX;
            cert.CurrY = ai.nY;
            cert.Direction = (byte)M2Share.RandomNumber.Random(8);
            cert.CharName = ai.sCharName;
            cert.WAbil = cert.Abil;
            if (M2Share.RandomNumber.Random(100) < cert.CoolEyeCode)
            {
                cert.CoolEye = true;
            }
            //Cert.m_sIPaddr = GetIPAddr;// Mac问题
            //Cert.m_sIPLocal = GetIPLocal(Cert.m_sIPaddr);
            cert.m_sConfigFileName = ai.sConfigFileName;
            cert.m_sHeroConfigFileName = ai.sHeroConfigFileName;
            cert.m_sFilePath = ai.sFilePath;
            cert.m_sConfigListFileName = ai.sConfigListFileName;
            cert.m_sHeroConfigListFileName = ai.sHeroConfigListFileName;// 英雄配置列表目录
            cert.Initialize();
            cert.RecalcLevelAbilitys();
            cert.RecalcAbilitys();
            cert.Abil.HP = cert.Abil.MaxHP;
            cert.Abil.MP = cert.Abil.MaxMP;
            if (cert.AddtoMapSuccess)
            {
                p28 = null;
                if (cert.Envir.Width < 50)
                {
                    n20 = 2;
                }
                else
                {
                    n20 = 3;
                }
                if (cert.Envir.Height < 250)
                {
                    if (cert.Envir.Height < 30)
                    {
                        n24 = 2;
                    }
                    else
                    {
                        n24 = 20;
                    }
                }
                else
                {
                    n24 = 50;
                }
                n1C = 0;
                while (true)
                {
                    if (!cert.Envir.CanWalk(cert.CurrX, cert.CurrY, false))
                    {
                        if ((cert.Envir.Width - n24 - 1) > cert.CurrX)
                        {
                            cert.CurrX += (short)n20;
                        }
                        else
                        {
                            cert.CurrX = (byte)(M2Share.RandomNumber.Random(cert.Envir.Width / 2) + n24);
                            if (cert.Envir.Height - n24 - 1 > cert.CurrY)
                            {
                                cert.CurrY += (short)n20;
                            }
                            else
                            {
                                cert.CurrY = (byte)(M2Share.RandomNumber.Random(cert.Envir.Height / 2) + n24);
                            }
                        }
                    }
                    else
                    {
                        p28 = cert.Envir.AddToMap(cert.CurrX, cert.CurrY, CellType.MovingObject, cert);
                        break;
                    }
                    n1C++;
                    if (n1C >= 31)
                    {
                        break;
                    }
                }
                if (p28 == null)
                {
                    cert = null;
                }
            }
            return cert;
        }

        public void SendQuestMsg(string sQuestName)
        {
            PlayObject playObject;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                playObject = PlayObjectList[i];
                if (!playObject.Death && !playObject.Ghost)
                    M2Share.g_ManageNPC.GotoLable(playObject, sQuestName, false);
            }
        }

        public void ClearItemList()
        {
            StdItemList.Reverse();
            ClearMerchantData();
        }

        public void SwitchMagicList()
        {
            if (MagicList.Count > 0)
            {
                _oldMagicList.Add(MagicList);
                MagicList = new List<SystemModule.Packet.ServerPackets.MagicInfo>();
            }
        }

        private void ClearMerchantData()
        {
            for (var i = 0; i < MerchantList.Count; i++)
            {
                MerchantList[i].ClearData();
            }
        }

        public void GuildMemberReGetRankName(GuildInfo guild)
        {
            var nRankNo = 0;
            for (var i = 0; i < PlayObjectList.Count; i++)
            {
                if (PlayObjectList[i].MyGuild == guild)
                {
                    guild.GetRankName(PlayObjectList[i], ref nRankNo);
                }
            }
        }
    }
}