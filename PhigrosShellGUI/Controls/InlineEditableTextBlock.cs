using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace PhigrosShellGUI.Controls;

/// <summary>
/// 内联可编辑文本控件。
/// 默认显示为 TextBlock + 编辑按钮，点击后切换为 TextBox + 确认按钮。
/// 失焦 / Esc → 取消编辑（恢复原值）。
/// 提交时执行确认命令，若 IsValid 为 false 则留在编辑态。
///
/// 非编辑态下自动紧凑布局，编辑态展开以提升编辑体验。
/// </summary>
public class InlineEditableTextBlock : Decorator
{
    // ── 模板部件 ──
    private TextBlock? _displayText;
    private TextBox? _editBox;
    private Button? _toggleButton;
    private PathIcon? _editIcon;
    private PathIcon? _confirmIcon;
    private Border? _rootBorder;
    private Grid? _grid;

    // ── 编辑前暂存原始值，用于取消恢复 ──
    private string? _originalText;

    // ── 标记确认按钮被按下，防止 LostFocus 抢先取消编辑 ──
    private bool _isTogglePressed;

    // ── 图标路径（共享资源，同 IconGeometries）──
    private static readonly Geometry EditIconGeometry = IconGeometries.EditIcon;
    private static readonly Geometry ConfirmIconGeometry = IconGeometries.ConfirmIcon;

    // ════════════════════════════════════════
    //  依赖属性
    // ════════════════════════════════════════

    /// <summary>显示的文本，TwoWay 绑定。仅在确认成功时更新。</summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<InlineEditableTextBlock, string>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>当前是否处于编辑态。</summary>
    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<InlineEditableTextBlock, bool>(nameof(IsEditing));

    /// <summary>当前值是否有效。VM 在命令中设为 false 可使控件留在编辑态。</summary>
    public static readonly StyledProperty<bool> IsValidProperty =
        AvaloniaProperty.Register<InlineEditableTextBlock, bool>(nameof(IsValid), true);

    /// <summary>TextBox 的占位提示文本。</summary>
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<InlineEditableTextBlock, string>(nameof(PlaceholderText), "输入...");

    /// <summary>Enter 是否插入换行（文本类字段用 true，数字类用 false）。</summary>
    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<InlineEditableTextBlock, bool>(nameof(AcceptsReturn), false);

    /// <summary>确认命令，参数为新输入的文本 (string)。</summary>
    public static readonly StyledProperty<System.Windows.Input.ICommand?> ConfirmCommandProperty =
        AvaloniaProperty.Register<InlineEditableTextBlock, System.Windows.Input.ICommand?>(nameof(ConfirmCommand));

    // ════════════════════════════════════════
    //  CLR 属性
    // ════════════════════════════════════════

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public bool IsValid
    {
        get => GetValue(IsValidProperty);
        set => SetValue(IsValidProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public System.Windows.Input.ICommand? ConfirmCommand
    {
        get => GetValue(ConfirmCommandProperty);
        set => SetValue(ConfirmCommandProperty, value);
    }

    // ── 构造 ──

    static InlineEditableTextBlock()
    {
        IsEditingProperty.Changed.AddClassHandler<InlineEditableTextBlock>((o, e) => o.OnIsEditingChanged((bool)e.NewValue!));
        IsValidProperty.Changed.AddClassHandler<InlineEditableTextBlock>((o, e) => o.OnIsValidChanged((bool)e.NewValue!));
    }

    public InlineEditableTextBlock()
    {
        BuildVisualTree();
    }

    // ── 构建视觉树 ──

    private void BuildVisualTree()
    {
        _rootBorder = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
        };

        // 双列 Grid：内容区 | 按钮
        _grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            // 默认非编辑态：紧凑
            Margin = new Thickness(0),
        };

        // 显示态——TextBlock
        _displayText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 1),   // 紧凑
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        _displayText.Bind(TextBlock.TextProperty, new Binding("Text") { Source = this, Mode = BindingMode.OneWay });
        _displayText.Tapped += OnDisplayTextTapped;
        Grid.SetColumn(_displayText, 0);
        _grid.Children.Add(_displayText);

        // 编辑态——TextBox
        _editBox = new TextBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 2),
            IsVisible = false,
        };
        _editBox.Bind(TextBox.TextProperty, new Binding("Text") { Source = this, Mode = BindingMode.TwoWay });
        _editBox.Bind(TextBox.PlaceholderTextProperty, new Binding("PlaceholderText") { Source = this });
        _editBox.LostFocus += OnEditBoxLostFocus;
        _editBox.KeyDown += OnEditBoxKeyDown;
        Grid.SetColumn(_editBox, 0);
        _grid.Children.Add(_editBox);

        // 切换按钮
        _toggleButton = new Button
        {
            Width = 22,
            Height = 22,
            Margin = new Thickness(2, 0, 0, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(3),
            Content = BuildButtonContent(),
        };
        _toggleButton.AddHandler(PointerPressedEvent, (s, e) =>
        {
            if (IsEditing) _isTogglePressed = true;
        }, RoutingStrategies.Tunnel);
        _toggleButton.Click += OnToggleButtonClick;
        Grid.SetColumn(_toggleButton, 1);
        _grid.Children.Add(_toggleButton);

        _rootBorder.Child = _grid;
        Child = _rootBorder;
    }

    /// <summary>创建按钮内部面板（编辑图标 + 确认图标，按状态切换可见性）</summary>
    private Panel BuildButtonContent()
    {
        var panel = new Panel();

        _editIcon = new PathIcon
        {
            Data = EditIconGeometry,
            Width = 12,
            Height = 12,
        };
        panel.Children.Add(_editIcon);

        _confirmIcon = new PathIcon
        {
            Data = ConfirmIconGeometry,
            Width = 12,
            Height = 12,
            Foreground = this.FindResource("SystemAccentColor") as IBrush ?? Brushes.DodgerBlue,
            IsVisible = false,
        };
        panel.Children.Add(_confirmIcon);

        return panel;
    }

    // ── 事件处理 ──

    private void OnDisplayTextTapped(object? sender, TappedEventArgs e) => EnterEditMode();

    private void OnToggleButtonClick(object? sender, RoutedEventArgs e)
    {
        if (!IsEditing) EnterEditMode();
        else TryConfirm();
    }

    private void OnEditBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (!IsEditing) return;

        if (_isTogglePressed)
        {
            _isTogglePressed = false;
            return;
        }

        CancelEdit();
    }

    private void OnEditBoxKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                CancelEdit();
                e.Handled = true;
                break;

            case Key.Enter:
                if (AcceptsReturn)
                {
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    {
                        TryConfirm();
                        e.Handled = true;
                    }
                }
                else
                {
                    TryConfirm();
                    e.Handled = true;
                }
                break;
        }
    }

    // ── 核心逻辑 ──

    private void EnterEditMode()
    {
        _originalText = Text;

        if (_editBox != null)
        {
            _editBox.AcceptsReturn = AcceptsReturn;
            _editBox.TextWrapping = AcceptsReturn ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }

        IsEditing = true;
        RefreshVisualState();

        SubscribeTopLevelPointer();

        if (_editBox != null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _editBox.Focus();
                _editBox.SelectAll();
            }, DispatcherPriority.Background);
        }
    }

    private void TryConfirm()
    {
        var newText = _editBox?.Text ?? string.Empty;

        if (ConfirmCommand is { } cmd && cmd.CanExecute(newText))
        {
            cmd.Execute(newText);

            if (IsValid)
            {
                Text = newText;
                IsEditing = false;
                UnsubscribeTopLevelPointer();
                RefreshVisualState();
            }
        }
        else
        {
            Text = newText;
            IsEditing = false;
            UnsubscribeTopLevelPointer();
            RefreshVisualState();
        }
    }

    private void CancelEdit()
    {
        if (_editBox != null)
            _editBox.Text = _originalText ?? string.Empty;

        IsEditing = false;
        UnsubscribeTopLevelPointer();
        RefreshVisualState();
    }

    // ── 顶层指针监听（点击外部取消编辑） ──

    private void SubscribeTopLevelPointer()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
            topLevel.PointerPressed += OnTopLevelPointerPressed;
    }

    private void UnsubscribeTopLevelPointer()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null)
            topLevel.PointerPressed -= OnTopLevelPointerPressed;
    }

    private void OnTopLevelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEditing) return;

        var point = e.GetPosition(this);
        if (point.X < 0 || point.X > Bounds.Width || point.Y < 0 || point.Y > Bounds.Height)
        {
            CancelEdit();
        }
    }

    // ── UI 状态更新 ──

    private void OnIsEditingChanged(bool isEditing)
    {
        RefreshVisualState();
    }

    private void OnIsValidChanged(bool isValid)
    {
        if (isValid && IsEditing)
        {
            Text = _editBox?.Text ?? Text;
            IsEditing = false;
            UnsubscribeTopLevelPointer();
            RefreshVisualState();
        }
    }

    private void RefreshVisualState()
    {
        PseudoClasses.Set(":editing", IsEditing);
        PseudoClasses.Set(":invalid", !IsValid);

        if (_displayText != null)
        {
            _displayText.IsVisible = !IsEditing;
            _displayText.Padding = IsEditing
                ? new Thickness(0)       // 编辑态隐藏，无 padding
                : new Thickness(4, 1);   // 显示态紧凑
        }

        if (_editBox != null)
        {
            _editBox.IsVisible = IsEditing;
            _editBox.PlaceholderText = PlaceholderText;
        }

        // 按钮：编辑态大一点好点，显示态小巧
        if (_toggleButton != null)
        {
            _toggleButton.Width = IsEditing ? 28 : 20;
            _toggleButton.Height = IsEditing ? 28 : 20;
            _toggleButton.Margin = IsEditing
                ? new Thickness(4, 0, 0, 0)
                : new Thickness(2, 0, 0, 0);
        }

        // Grid 容器间距：显示态紧凑，编辑态宽松
        if (_grid != null)
        {
            _grid.Margin = IsEditing
                ? new Thickness(4, 2)
                : new Thickness(0);
        }

        if (_editIcon != null)
        {
            _editIcon.IsVisible = !IsEditing;
            _editIcon.Width = IsEditing ? 14 : 11;
            _editIcon.Height = IsEditing ? 14 : 11;
        }
        if (_confirmIcon != null)
        {
            _confirmIcon.IsVisible = IsEditing;
            _confirmIcon.Width = IsEditing ? 14 : 11;
            _confirmIcon.Height = IsEditing ? 14 : 11;
        }

        // 边框：仅编辑态显示，无效态变红
        if (_rootBorder != null)
        {
            if (!IsEditing)
            {
                _rootBorder.BorderThickness = new Thickness(0);
            }
            else if (!IsValid)
            {
                _rootBorder.BorderThickness = new Thickness(1);
                _rootBorder.BorderBrush = Brushes.Red;
            }
            else
            {
                _rootBorder.BorderThickness = new Thickness(1);
                _rootBorder.BorderBrush = this.FindResource("SystemAccentColor") as IBrush
                    ?? new SolidColorBrush(Color.Parse("#1976D2"));
            }
        }
    }
}
