﻿using GameSvr.Player;
using SystemModule.Data;

namespace GameSvr.GameCommand.Commands
{
    /// <summary>
    /// 显示物品信息
    /// </summary>
    [Command("ShowUseItem", "显示物品信息", CommandHelp.GameCommandShowUseItemInfoHelpMsg, 10)]
    public class ShowUseItemInfoCommand : Command
    {
        [ExecuteCommand]
        public void ShowUseItem(string[] @Params, PlayObject PlayObject)
        {
            if (@Params == null)
            {
                return;
            }
            var sHumanName = @Params.Length > 0 ? @Params[0] : "";
            if (string.IsNullOrEmpty(sHumanName) || !string.IsNullOrEmpty(sHumanName) && sHumanName[1] == '?')
            {
                PlayObject.SysMsg(GameCommand.ShowHelp, MsgColor.Red, MsgType.Hint);
                return;
            }
            var m_PlayObject = M2Share.WorldEngine.GetPlayObject(sHumanName);
            if (m_PlayObject == null)
            {
                PlayObject.SysMsg(string.Format(CommandHelp.NowNotOnLineOrOnOtherServer, sHumanName), MsgColor.Red, MsgType.Hint);
                return;
            }
            for (var i = 0; i < m_PlayObject.UseItems.Length; i++)
            {
                var UserItem = m_PlayObject.UseItems[i];
                if (UserItem.Index == 0)
                {
                    continue;
                }
                PlayObject.SysMsg(string.Format("%s[%s]IDX[%d]系列号[%d]持久[%d-%d]", M2Share.GetUseItemName(i), M2Share.WorldEngine.GetStdItemName(UserItem.Index), UserItem.Index,
                    UserItem.MakeIndex, UserItem.Dura, UserItem.DuraMax), MsgColor.Blue, MsgType.Hint);
            }
        }
    }
}