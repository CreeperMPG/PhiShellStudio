using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhigrosArchive;
using PhigrosArchive.Abstractions;
using PhigrosArchive.Save;
using PhigrosShellGUI.Services;

namespace PhigrosShellGUI.ViewModels;

/// <summary>首页 ViewModel</summary>
public partial class HomeViewModel : ViewModelBase
{
    /// <summary>请求跳转到 slot 选择页</summary>
    public event Action<SaveFileInfo[]>? CloudSlotSelected;

    /// <summary>请求直接打开存档详情页（本地文件或单 slot）</summary>
    /// <remarks>tuple: (SaveFile, SaveFileInfo?) — SaveFileInfo 非 null 表示云存档</remarks>
    public event Action<SaveFile, SaveFileInfo?>? SaveFileLoaded;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    // ── 从文件打开 ──

    [RelayCommand]
    private async Task OpenFromFileAsync()
    {
        IsLoading = true;
        StatusMessage = "选择存档文件...";

        try
        {
            var topLevel = MainWindowHelper.Instance != null
                ? TopLevel.GetTopLevel(MainWindowHelper.Instance)
                : TopLevel.GetTopLevel(MainWindowHelper.MainViewInstance);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开 Phigros 存档",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Phigros 存档") { Patterns = ["*.save", "*.zip"] },
                ],
            });

            if (files.Count == 0) return;

            var file = files[0];

            // 读取文件内容
            byte[] zipData;
            await using (var stream = await file.OpenReadAsync())
            using (var ms = new MemoryStream())
            {
                await stream.CopyToAsync(ms);
                zipData = ms.ToArray();
            }

            var saveFile = new SaveFile(zipData, null);
            StatusMessage = $"已加载：{file.Name}";

            SaveFileLoaded?.Invoke(saveFile, null); // 本地文件，无 SaveFileInfo
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── 从云存档打开 ──

    [RelayCommand]
    private async Task OpenFromCloudAsync()
    {
        if (!LoginStateProvider.IsLoggedIn)
        {
            // 未登录 → 由外部处理登录 Overlay
            NeedLogin?.Invoke();
            return;
        }

        IsLoading = true;
        StatusMessage = "正在获取存档列表...";

        try
        {
            var playerInfo = LoginStateProvider.CurrentPlayerInfo!;
            var saveInfos = await playerInfo.FetchSaveInfoAsync();

            if (saveInfos.Length == 0)
            {
                StatusMessage = "没有找到存档";
                return;
            }

            // 检查设置：是否自动进入单存档
            var settings = new SettingsService().Load();
            if (settings.AutoEnterSingleSlot && saveInfos.Length == 1)
            {
                // 只有一个 slot，直接下载进入（SaveFileInfo 传入，供上传/刷新使用）
                var saveInfo = saveInfos[0];
                var saveFile = await saveInfo.FetchSaveAsync(null);
                StatusMessage = "自动进入存档...";
                SaveFileLoaded?.Invoke(saveFile, saveInfo);
            }
            else
            {
                // 多个 slot，进入选择页
                CloudSlotSelected?.Invoke(saveInfos);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"获取存档失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>未登录时触发，由外层（MainWindowViewModel）弹出登录 Overlay</summary>
    public event Action? NeedLogin;
}
