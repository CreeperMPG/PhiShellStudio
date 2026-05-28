using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive;
using PhigrosArchive.Save;

namespace PhigrosShellGUI.ViewModels;

/// <summary>主界面（无选项卡，只有用户信息和存档列表）</summary>
public partial class MainViewModel : ViewModelBase
{
    /// <summary>点击 slot 时触发，传入 SaveFileInfo 用于下载并进入详情页</summary>
    public event Action<SaveFileInfo>? SlotSelected;

    [ObservableProperty]
    private string _nickname = string.Empty;

    [ObservableProperty]
    private string _shortId = string.Empty;

    [ObservableProperty]
    private string _objectId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SaveSlotBrief> _slots = new();

    /// <summary>底层的玩家信息（供后续操作使用）</summary>
    public PhigrosPlayerInfo PlayerInfo { get; private set; } = null!;

    public async Task InitializeAsync(PhigrosPlayerInfo playerInfo)
    {
        PlayerInfo = playerInfo;
        Nickname = playerInfo.Nickname;
        ShortId = playerInfo.ShortID;
        ObjectId = playerInfo.UserObjectID;

        var saveInfos = await playerInfo.FetchSaveInfoAsync();

        Slots.Clear();
        for (int i = 0; i < saveInfos.Length; i++)
        {
            var info = saveInfos[i];
            var summary = info.Summary;
            Slots.Add(new SaveSlotBrief
            {
                SlotIndex = i,
                RKS = summary.RankingScore,
                Challenge = summary.Challenge,
                Avatar = summary.Avatar,
                SaveFileInfo = info
            });
        }
    }

    /// <summary>点击 slot → 通知导航到详情页</summary>
    [RelayCommand]
    private void SelectSlot(SaveSlotBrief? slot)
    {
        if (slot?.SaveFileInfo != null)
            SlotSelected?.Invoke(slot.SaveFileInfo);
    }
}

/// <summary>存档槽位摘要（MainView 列表用）</summary>
public partial class SaveSlotBrief : ViewModelBase
{
    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private float _rKS;

    [ObservableProperty]
    private ushort _challenge;

    [ObservableProperty]
    private string _avatar = string.Empty;

    public SaveFileInfo SaveFileInfo { get; set; } = null!;
}
