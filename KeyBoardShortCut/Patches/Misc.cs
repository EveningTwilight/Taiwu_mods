﻿using Harmony12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityModManagerNet;
using System.Threading.Tasks;

namespace KeyBoardShortCut
{
    // 测试
    [HarmonyPatch(typeof(ui_SystemSetting), "OnInit")]
    public static class Test_Patch
    {
        private static void Postfix(ui_SystemSetting __instance)
        {
            Main.Logger.Error("ui_SystemSetting OnInit");
            Utils.isUIActive("ui_SystemSetting");
        }
    }

    // 通用选择框：确认延迟
    [HarmonyPatch(typeof(YesOrNoWindow), "ShowYesOrNoWindow")]
    public static class YesOrNoWindow_Confirm_Wait_Patch
    {
        private static void Prefix(bool show, bool backMask, bool canClose)
        {
            if (!Main.on) return;
            if (show && !YesOrNoWindow.instance.yesOrNoIsShow) Wait();
        }

        public static async void Wait()
        {
            YesOrNoWindow_Confirm_Patch.canYes = false;
            await Task.Delay(300);
            YesOrNoWindow_Confirm_Patch.canYes = true;
        }
    }

    // 通用选择框：确认
    [HarmonyPatch(typeof(YesOrNoWindow), "Awake")]
    public static class YesOrNoWindow_Confirm_Patch
    {
        public static bool canYes = true;
        private static void Postfix(YesOrNoWindow __instance)
        {
            if (!Main.on) return;
            Utils.ButtonConfirm(__instance.yes, (_) => 
                canYes && 
                __instance.yesOrNoIsShow && 
                __instance.yesOrNoWindow.gameObject.activeInHierarchy
            );
        }
    }

    // 事件选择窗口 右键默认选项
    [HarmonyPatch(typeof(ui_MessageWindow), "Awake")]
    public static class MessageWindow_Close_Patch
    {
        private static void Postfix(ui_MessageWindow __instance)
        {
            if (!Main.on) return;
            __instance.gameObject.AddComponent<ActionsComponent>()
                .OnCheck(CHECK_TYPE.CLOSE)
                .OnCheck((_) => ui_MessageWindow.Exists)
                .OnCheck((_) => ui_MessageWindow.Instance.gameObject.activeInHierarchy)
                .AddAction(() => {
                    var holder = __instance.chooseHolder;
                    var count = holder.childCount;
                    var child = holder.GetChild(count- 1);
                    var button = child.gameObject.GetComponent<Button>();
                    button.onClick.Invoke();
                });
        }
    }

    // 设置快捷键关闭 太吾/本地 产地   关闭技能建筑浏览
    [HarmonyPatch(typeof(HomeSystemWindow), "Awake")]
    public static class HomeSystemWindow_Awake_Patch
    {
        private static void Postfix(HomeSystemWindow __instance)
        {
            if (!Main.on) return;
             __instance.gameObject.AddComponent<ActionsComponent>()
                .OnCheck(CHECK_TYPE.CLOSE)
                .AddAction(() => {
                    if (HomeSystemWindow.Instance.skillView.activeInHierarchy)
                    {
                        var homeViewButtom = Utils.GetUI("ui_HomeViewBottom");
                        if (homeViewButtom)
                        {
                            HomeSystemWindow.Instance.ShowSkillView();
                            Traverse.Create(homeViewButtom).Method("ToggleZoomButtons").GetValue();
                        }
                    } else
                    {
                        Utils.CloseHomeSystemWindow();
                    }
                });
        }
    }

    // 设置快捷键打开 太吾/本地 产地
    [HarmonyPatch(typeof(WorldMapSystem), "Update")]
    public static class WorldMapSystem_Update_Patch
    {
        private static void Postfix(WorldMapSystem __instance)
        {
            if (!Main.enabled || Main.binding_key) return;
            if (UIManager.Instance.curState == UIState.MainWorld)
            {
                if (Main.GetKeyDown(HK_TYPE.VILLAGE_LOCAL))
                {
                    Utils.ShowLocalHomeSystem();
                }
                else if (Main.GetKeyDown(HK_TYPE.VILLAGE))
                {
                    Utils.ShowHomeSystem();
                }
            } else if (UIManager.Instance.curState == UIState.HomeSystem)
            {
                if (Main.GetKeyDown(HK_TYPE.VILLAGE_LOCAL) || Main.GetKeyDown(HK_TYPE.VILLAGE))
                {
                    Utils.CloseHomeSystemWindow();
                }
            }
        }
    }

    // 建筑界面 新建 升级 人力调整
    [HarmonyPatch(typeof(BuildingWindow), "Start")]
    public static class BuildingWindow_Up_Patch
    {
        private static void Postfix(BuildingWindow __instance)
        {
            if (!Main.enabled || Main.binding_key) return;
            Refers component = BuildingWindow.instance.GetComponent<Refers>();

            // 新建 人力调整
            Utils.ButtonHK(component.CGet<Button>("NewBuildingManpowerDownButton"), HK_TYPE.DECREASE);
            Utils.ButtonHK(component.CGet<Button>("NewBuildingManpowerUpButton"), HK_TYPE.INCREASE);

            // 新建 确定
            Utils.ButtonConfirm(component.CGet<Button>("NewBuildingButton"));

            // 升级 人力调整
            Utils.ButtonHK(component.CGet<Button>("UpBuildingManpowerDownButton"), HK_TYPE.DECREASE);
            Utils.ButtonHK(component.CGet<Button>("UpBuildingManpowerUpButton"), HK_TYPE.INCREASE);

            // 升级 确定
            Utils.ButtonConfirm(component.CGet<Button>("UpBuildingButton"));

            // 移除 人力调整
            Utils.ButtonHK(component.CGet<Button>("RemoveBuildingManpowerDownButton"), HK_TYPE.DECREASE);
            Utils.ButtonHK(component.CGet<Button>("RemoveBuildingManpowerUpButton"), HK_TYPE.INCREASE);

            // 移除 确定
            Utils.ButtonConfirm(component.CGet<Button>("RemoveBuildingButton"));
        }
    }

    // 功法书籍选择确认
    [HarmonyPatch(typeof(BuildingWindow), "Start")]
    public static class BuildingWindow_ChooseBookConfirm_Patch
    {
        private static void Postfix(BuildingWindow __instance)
        {
            if (!Main.on) return;
            // 研读
            Utils.ButtonConfirm(__instance.chooseBookButton);
            // 修习， 突破
            Utils.ButtonConfirm(__instance.setGongFaButton);
        }
    }

    // 移除 功法书籍
    [HarmonyPatch(typeof(BuildingWindow), "Start")]
    public static class BuildingWindow_RemoveStudyItem_Patch
    {
        private static void Postfix(BuildingWindow __instance)
        {
            if (!Main.on) return;
            var studyChooseTyp = Traverse.Create(BuildingWindow.instance).Field("studyChooseTyp");
            // 修习
            Utils.ButtonHK(__instance.removeGongFaButton, HK_TYPE.REMOVE_ITEM, (b) => {
                return 0 == studyChooseTyp.GetValue<int>();
            });
            // 突破
            Utils.ButtonHK(__instance.removeLevelUPButton, HK_TYPE.REMOVE_ITEM, (b) => {
                return 1 == studyChooseTyp.GetValue<int>();
            });
            // 研读
            Utils.ButtonHK(__instance.removeReadBookButton, HK_TYPE.REMOVE_ITEM, (b) => {
                return 2 == studyChooseTyp.GetValue<int>();
            });
        }
    }

    // 建筑 功能 菜单切换
    [HarmonyPatch(typeof(BuildingWindow), "Start")]
    public static class BuildingWindow_Toggle_Type_Patch
    {
        private static void Postfix(BuildingWindow __instance)
        {
            if (!Main.on) return;
            Utils.ToggleSwitch(__instance.buildingTypHolder);
        }
    }

    // 更新信息窗口
    [HarmonyPatch(typeof(MainMenu), "Awake")]
    public static class MainMenu_StartMessage_Confirm_Patch
    {
        private static void Postfix(MainMenu __instance)
        {
            if (!Main.on) return;
            Transform welcome = __instance.transform.Find("WelcomeDialog");
            Refers refer = welcome.GetComponent<Refers>();
            Utils.ButtonConfirm(refer.CGet<CButton>("ConfirmBtn"));
        }
    }

    // 商店确认
    [HarmonyPatch(typeof(ShopSystem), "Start")]
    public static class ShopSystem_Confirm_Patch
    {
        private static void Postfix(ShopSystem __instance)
        {
            if (!Main.on) return;
            Utils.ButtonConfirm(__instance.shopOkButton);
        }
    }


    // 过月事件窗口：确认
    [HarmonyPatch(typeof(ui_TurnChange), "Awake")]
    public static class UI_TurnChange_ConfirmTrunChangeWindow_Patch
    {
        private static void Postfix(ui_TurnChange __instance)
        {
            if (!Main.on) return;
            Utils.ButtonConfirm(Traverse.Create(__instance).Field<CButton>("closeBtn").Value);
        }
    }

    // 人物界面 打开
    [HarmonyPatch(typeof(ui_BottomLeft), "Awake")]
    public static class ActorMenu_Open_Patch
    {
        private static void Postfix(ui_BottomLeft __instance)
        {
            if (!Main.on) return;
            Utils.ButtonHK(__instance.CGet<CButton>("PlayerFaceButton"), HK_TYPE.ACTORMENU);
        }
    }

    // 人物界面 切换
    [HarmonyPatch(typeof(ActorMenu), "Awake")]
    public static class ActorMenu_Toggle_Type_Patch
    {
        private static void Postfix(ActorMenu __instance)
        {
            if (!Main.on) return;
            Utils.ToggleSwitch(__instance.actorTeamToggle.group);
        }
    }

    // 人物界面 功法确认
    [HarmonyPatch(typeof(ActorMenu), "Awake")]
    public static class ActorMenu_Gongfa_Confirm_Patch
    {
        private static void Postfix(ActorMenu __instance)
        {
            if (!Main.on) return;
            Utils.ButtonConfirm(__instance.equipGongFaViewButton.GetComponent<Button>());
            Utils.ButtonHK(__instance.removeGongFaViewButton.GetComponent<Button>(), HK_TYPE.REMOVE_ITEM);
        }
    }

    // 较艺界面：结束确认
    [HarmonyPatch(typeof(SkillBattleSystem), "Awake")]
    public static class SkillBattleSystem_ConfirmEnd_Patch
    {
        private static void Postfix(SkillBattleSystem __instance)
        {
            if (!Main.on) return;
            Utils.ButtonConfirm(__instance.closeBattleButton.GetComponent<Button>());
        }
    }


    // 制作确认得到物品
    [HarmonyPatch(typeof(MakeSystem), "Awake")]
    public static class MakeSystem_ConfirmEnd_Patch
    {
        private static void Postfix(MakeSystem __instance)
        {
            if (!Main.on) return;
            Refers component = __instance.GetComponent<Refers>();
            Utils.ButtonConfirm(component.CGet<Button>("StartMakeButton"));
            Utils.ButtonConfirm(component.CGet<Button>("StartFixButton"));
            Utils.ButtonConfirm(component.CGet<Button>("GetItemButton"));
        }
    }

    // 战斗界面：结束确认
    [HarmonyPatch(typeof(BattleEndWindow), "Awake")]
    public static class BattleEndWindow_ConfirmEnd_Patch
    {
        private static void Postfix(BattleEndWindow __instance)
        {
            if (!Main.on) return;
            Utils.ButtonConfirm(__instance.closeBattleEndWindowButton);
        }
    }

    // 书籍 购买确认
    [HarmonyPatch(typeof(BookShopSystem), "Start")]
    public static class BookShopSystem_Ok_Patch
    {
        private static void Postfix(BookShopSystem __instance)
        {
            if (!Main.on) return;
            Refers component = __instance.GetComponent<Refers>();
            Utils.ButtonConfirm(component.CGet<Button>("ShopOkButton"));
        }
    }

    // 进入奇遇
    [HarmonyPatch(typeof(ChoosePlaceWindow), "Awake")]
    public static class ChoosePlaceWindow_ToStory_Patch
    {
        private static void Postfix(ChoosePlaceWindow __instance)
        {
            if (!Main.on) return;
            Utils.ButtonHK(__instance.openToStoryButton, HK_TYPE.STORY);
        }
    }

    // 进入人物搜索
    [HarmonyPatch(typeof(ui_MiniMap), "Awake")]
    public static class ui_MiniMap_ToNameScan_Patch
    {
        private static void Postfix(ui_MiniMap __instance)
        {
            if (!Main.on) return;
            Utils.ButtonHK(Traverse.Create(__instance).Field<CButton>("SearchNpc").Value, HK_TYPE.NAME_SCAN);
        }
    }

    // 进入世界地图
    [HarmonyPatch(typeof(ui_MiniMap), "Awake")]
    public static class ui_MiniMap_ToWorldMap_Patch
    {
        private static void Postfix(ui_MiniMap __instance)
        {
            if (!Main.on) return;
            Utils.ButtonHK(Traverse.Create(__instance).Field<CButton>("ShowMap").Value, HK_TYPE.WORLD_MAP);
        }
    }

    // 奇遇前选择菜单
    [HarmonyPatch(typeof(ToStoryMenu), "Awake")]
    public static class ToStoryMenu_Comfirm_Patch
    {
        private static void Postfix(ToStoryMenu __instance)
        {
            if (!Main.on) return;
            // 选择物品
            Utils.ButtonConfirm(__instance.useItemButton, (_) => ToStoryMenu.toStoryIsShow);
            // 进入奇遇
            Utils.ButtonConfirm(__instance.openStoryButton, (_) => ToStoryMenu.toStoryIsShow);
            // 移除物品
            Utils.ButtonRemove(__instance.removeItemButton, (_) => ToStoryMenu.toStoryIsShow);
        }
    }
    
    // 修复 奇遇菜单中可以过月
    [HarmonyPatch(typeof(UIDate), "ChangeTrunButton")]
    public static class UIDate_Month_Change_Fix_Patch
    {
        private static bool Prefix(UIDate __instance)
        {
            if (!Main.on) return true;
            if (ToStoryMenu.toStoryIsShow) return false;
            return true;
        }
    }
}
