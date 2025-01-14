﻿using GameSvr.Player;
using SystemModule;
using SystemModule.Data;
using SystemModule.Packets.ClientPackets;

namespace GameSvr.GameCommand.Commands
{
    /// <summary>
    /// 调整指定玩家技能等级
    /// </summary>
    [Command("TrainingSkill", "调整指定玩家技能等级", "人物名称  技能名称 修炼等级(0-3)", 10)]
    public class TrainingSkillCommand : Command
    {
        [ExecuteCommand]
        public void TrainingSkill(string[] @Params, PlayObject PlayObject)
        {
            if (@Params == null)
            {
                return;
            }
            var sHumanName = @Params.Length > 0 ? @Params[0] : "";
            var sSkillName = @Params.Length > 1 ? @Params[1] : "";
            var nLevel = @Params.Length > 2 ? int.Parse(@Params[2]) : 0;
            UserMagic UserMagic;
            if (string.IsNullOrEmpty(sHumanName) || sSkillName == "" || nLevel <= 0)
            {
                PlayObject.SysMsg(GameCommand.ShowHelp, MsgColor.Red, MsgType.Hint);
                return;
            }
            nLevel = HUtil32._MIN(3, nLevel);
            var m_PlayObject = M2Share.WorldEngine.GetPlayObject(sHumanName);
            if (m_PlayObject == null)
            {
                PlayObject.SysMsg($"{sHumanName}不在线，或在其它服务器上!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            for (var i = 0; i < m_PlayObject.MagicList.Count; i++)
            {
                UserMagic = m_PlayObject.MagicList[i];
                //if (string.Compare(UserMagic.MagicInfo.GetMagicName(), sSkillName, true) == 0)
                //{
                //    UserMagic.btLevel = (byte)nLevel;
                //    m_PlayObject.SendMsg(m_PlayObject, Grobal2.RM_MAGIC_LVEXP, 0, UserMagic.MagicInfo.wMagicId, UserMagic.btLevel, UserMagic.nTranPoint, "");
                //    m_PlayObject.SysMsg(string.Format("{0}的修改炼等级为{1}", sSkillName, nLevel), TMsgColor.c_Green, TMsgType.t_Hint);
                //    PlayObject.SysMsg(string.Format("{0}的技能{1}修炼等级为{2}", sHumanName, sSkillName, nLevel), TMsgColor.c_Green, TMsgType.t_Hint);
                //    break;
                //}
            }
        }
    }
}