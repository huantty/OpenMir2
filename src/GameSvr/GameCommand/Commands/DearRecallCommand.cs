﻿using GameSvr.Player;
using SystemModule;
using SystemModule.Data;

namespace GameSvr.GameCommand.Commands
{
    /// <summary>
    /// 夫妻传送，将对方传送到自己身边，对方必须允许传送。
    /// </summary>
    [Command("DearRecall", "夫妻传送", "(夫妻传送，将对方传送到自己身边，对方必须允许传送。)", 0)]
    public class DearRecallCommond : Command
    {
        [ExecuteCommand]
        public void DearRecall(PlayObject PlayObject)
        {
            if (PlayObject.m_sDearName == "")
            {
                PlayObject.SysMsg("你没有结婚!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            if (PlayObject.Envir.Flag.boNODEARRECALL)
            {
                PlayObject.SysMsg("本地图禁止夫妻传送!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            if (PlayObject.m_DearHuman == null)
            {
                if (PlayObject.Gender == 0)
                {
                    PlayObject.SysMsg("你的老婆不在线!!!", MsgColor.Red, MsgType.Hint);
                }
                else
                {
                    PlayObject.SysMsg("你的老公不在线!!!", MsgColor.Red, MsgType.Hint);
                }
                return;
            }
            if (HUtil32.GetTickCount() - PlayObject.m_dwDearRecallTick < 10000)
            {
                PlayObject.SysMsg("稍等会才能再次使用此功能!!!", MsgColor.Red, MsgType.Hint);
                return;
            }
            PlayObject.m_dwDearRecallTick = HUtil32.GetTickCount();
            if (PlayObject.m_DearHuman.m_boCanDearRecall)
            {
                PlayObject.RecallHuman(PlayObject.m_DearHuman.ChrName);
            }
            else
            {
                PlayObject.SysMsg(PlayObject.m_DearHuman.ChrName + " 不允许传送!!!", MsgColor.Red, MsgType.Hint);
            }
        }
    }
}