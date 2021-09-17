﻿using SystemModule;
using System.Collections.Generic;
using GameSvr.CommandSystem;

namespace GameSvr
{
    [GameCommand("ClearBagItem", "清理包裹物品", 10)]
    public class ClearBagItemCommand : BaseCommond
    {
        [DefaultCommand]
        public void ClearBagItem(string[] @Params, TPlayObject PlayObject)
        {
            var sHumanName = @Params.Length > 0 ? Params[0] : "";
            TUserItem UserItem;
            IList<TDeleteItem> DelList = null;
            if (sHumanName == "" || sHumanName != "" && sHumanName[0] == '?')
            {
                PlayObject.SysMsg(string.Format(M2Share.g_sGameCommandParamUnKnow, this.Attributes.Name, "人物名称"), TMsgColor.c_Red, TMsgType.t_Hint);
                return;
            }
            var m_PlayObject = M2Share.UserEngine.GetPlayObject(sHumanName);
            if (m_PlayObject == null)
            {
                PlayObject.SysMsg(string.Format(M2Share.g_sNowNotOnLineOrOnOtherServer, sHumanName), TMsgColor.c_Red, TMsgType.t_Hint);
                return;
            }
            if (m_PlayObject.m_ItemList.Count > 0)
            {
                for (var i = m_PlayObject.m_ItemList.Count - 1; i >= 0; i--)
                {
                    UserItem = m_PlayObject.m_ItemList[i];
                    if (DelList == null)
                    {
                        DelList = new List<TDeleteItem>();
                    }
                    DelList.Add(new TDeleteItem()
                    {
                        sItemName = M2Share.UserEngine.GetStdItemName(UserItem.wIndex),
                        MakeIndex = UserItem.MakeIndex
                    });
                    UserItem = null;
                    m_PlayObject.m_ItemList.RemoveAt(i);
                }
                m_PlayObject.m_ItemList.Clear();
            }
            if (DelList != null)
            {
                var ObjectId = HUtil32.Sequence();
                M2Share.ObjectSystem.AddOhter(ObjectId, DelList);
                m_PlayObject.SendMsg(m_PlayObject, Grobal2.RM_SENDDELITEMLIST, 0, ObjectId, 0, 0, "");
            }
        }
    }
}