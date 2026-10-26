using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 存档根对象。纯数据、纯 C#：Core 不引用 UnityEngine，JSON 的读写放在 Game 层
    /// （<c>SaveSystem</c> 用 <c>JsonUtility</c>），这里只负责「状态 ⇄ 数据」的搬运。
    ///
    /// 刻意不用字典、不用属性、不用可空值类型：Unity 的序列化器只认公开字段与数组/List，
    /// 字典必须摊平成平行数组（见 <see cref="CommsSave"/> 与 <see cref="QuestSave"/>）。
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>存档格式版本。任何字段语义变化都必须 +1，旧档会被明确拒绝而不是读出半个状态。</summary>
        public int Version;

        public string SavedAtUtc;

        public CampaignSave Campaign;
        public MineSave Mine;
        public PodSave Pod;
        public LevelBagSave Craft;
        public LevelBagSave Cpu;
        public LevelSave Insurance;
        public LevelSave Rack;
        public CommsSave Comms;
        public MerchantSave Merchant;
        public NpcMarketSave NpcMarket;
        public QuestSave Quests;
        public BagSave Stash;
        public BagSave PlayerBag;
        public BagSave Secure;

        /// <summary>
        /// 所有容器（零件盒、武器箱、密钥盒…）的内容物摊平在这张表里，由
        /// <see cref="ItemSave.ContainerId"/> 引用。**不能**把容器做成嵌套的
        /// <c>ItemSave[]</c>：Unity 的序列化器有 10 层深度上限，自引用类型会在
        /// 序列化时直接报错并截断数据。
        /// </summary>
        public ContainerSave[] Containers;
    }

    [Serializable]
    public sealed class CampaignSave
    {
        public int Week;
        public int Day;
        public int TotalPaid;
        public int Status;
        public int Ending;
    }

    [Serializable]
    public sealed class LevelSave
    {
        public int Level;
    }

    [Serializable]
    public sealed class LevelBagSave
    {
        public int Level;
        public BagSave Bag;
    }

    [Serializable]
    public sealed class PodSave
    {
        public int Level;
        public string SelectedKeyItemId;
        public BagSave Bag;
    }

    [Serializable]
    public sealed class CommsSave
    {
        public int Level;
        public BagSave Bag;

        /// <summary>被任务占用的链路实例，与 <see cref="LockedQuestIds"/> 一一对应。</summary>
        public int[] LockedInstanceIds;
        public string[] LockedQuestIds;
    }

    [Serializable]
    public sealed class MineSave
    {
        public int Level;
        public bool[] Filled;
        public int FilledWeek;
        public int FilledDay;
    }

    /// <summary>
    /// 网格快照。尺寸必须与设施等级推导出的尺寸一致，否则判定为不兼容存档——
    /// 宁可报错也不要把物品悄悄塞进一个更小的网格里。
    /// </summary>
    [Serializable]
    public sealed class BagSave
    {
        public int Width;
        public int Height;
        public ItemSave[] Items;
    }

    /// <summary>
    /// 一件物品。<see cref="ContainerId"/> 为 0 表示没有容器；否则指向
    /// <see cref="SaveData.Containers"/> 里的某一项。容器的格子尺寸来自物品定义
    /// （<see cref="ContainerSpec"/>），存档里冗余存一份用于校验。
    /// </summary>
    [Serializable]
    public sealed class ItemSave
    {
        public int InstanceId;
        public string DefinitionId;
        public int X;
        public int Y;
        public int Rotation;
        public bool Rotatable;
        public int Count;
        public int Durability;
        public bool ExpeditionBound;
        public int ContainerId;
    }

    [Serializable]
    public sealed class ContainerSave
    {
        public int Id;
        public int Width;
        public int Height;
        public ItemSave[] Items;
    }

    [Serializable]
    public sealed class MerchantSave
    {
        public int Week;
        public int Day;
        public BagSave Stock;
        public OfferSave[] Offers;
    }

    [Serializable]
    public sealed class OfferSave
    {
        public string ItemId;
        public int Price;
        public int Remaining;
        public int Count;
        public int InstanceId;
        public bool IsBuyback;
    }

    [Serializable]
    public sealed class NpcMarketSave
    {
        public NpcStockSave[] Entries;
    }

    [Serializable]
    public sealed class NpcStockSave
    {
        public string NpcId;
        public int LastRefreshDay;
        public BagSave Stock;
    }

    [Serializable]
    public sealed class QuestSave
    {
        public string[] AcceptedOrder;
        public QuestEntrySave[] Entries;
        public QuestCooldownSave[] Cooldowns;
    }

    [Serializable]
    public sealed class QuestEntrySave
    {
        public string QuestId;
        public int State;
        public int Count;
        public int AcceptedDay;
        public int DeadlineDay;
    }

    [Serializable]
    public sealed class QuestCooldownSave
    {
        public string NpcId;
        public int UntilDay;
    }
}
