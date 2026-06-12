using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OctoFixFlow
{
    /// <summary>
    /// WellSelectionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WellSelectionWindow : Window
    {
        public List<(int Row, int Col)> SelectedWells { get; set; } = new List<(int Row, int Col)>();
        // 96孔板配置
        private const int Rows = 8;
        private const int Cols = 12;
        private readonly string[] _rowLabels = { "A", "B", "C", "D", "E", "F", "G", "H" };

        // 框选状态变量
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private readonly HashSet<Button> _tempSelectedButtons = new HashSet<Button>();
        // 全选按钮引用
        private Button _selectAllBtn;
        public WellSelectionWindow()
        {
            InitializeComponent();
            Initialize96WellPlate();
        }
        // 初始化96孔板界面
        private void Initialize96WellPlate()
        {
            // 添加列定义（第一列是行标签，然后12列孔位）
            WellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            for (int i = 0; i < Cols; i++)
            {
                WellGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            }

            // 添加行定义（第一行是列标签，然后8行孔位）
            WellGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            for (int i = 0; i < Rows; i++)
            {
                WellGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(55) });
            }

            _selectAllBtn = new Button
            {
                Content = "⭕",
                Background = Brushes.Teal,
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Tag = "select_all"
            };
            _selectAllBtn.Click += SelectAllBtn_Click;
            Grid.SetRow(_selectAllBtn, 0);
            Grid.SetColumn(_selectAllBtn, 0);
            WellGrid.Children.Add(_selectAllBtn);

            // 1. 添加列标签按钮（1-12）
            for (int col = 0; col < Cols; col++)
            {
                var colBtn = CreateHeaderButton((col + 1).ToString(), $"col_{col}");
                colBtn.Click += ColumnHeader_Click;
                Grid.SetRow(colBtn, 0);
                Grid.SetColumn(colBtn, col + 1);
                WellGrid.Children.Add(colBtn);
            }

            // 2. 添加行标签按钮（A-H）
            for (int row = 0; row < Rows; row++)
            {
                var rowBtn = CreateHeaderButton(_rowLabels[row], $"row_{row}");
                rowBtn.Click += RowHeader_Click;
                Grid.SetRow(rowBtn, row + 1);
                Grid.SetColumn(rowBtn, 0);
                WellGrid.Children.Add(rowBtn);
            }

            // 3. 添加孔位按钮
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    var wellBtn = new Button
                    {
                        Content = $"{_rowLabels[row]}{col + 1}",
                        Background = Brushes.White,
                        Foreground = Brushes.Black,
                        FontSize = 12,
                        Margin = new Thickness(2),
                        BorderBrush = Brushes.Red,
                        BorderThickness = new Thickness(1),
                        Tag = (Row: row + 1, Col: col + 1) // 存储行号和列号（从1开始）
                    };

                    // 绑定鼠标事件
                    wellBtn.PreviewMouseLeftButtonDown += WellButton_PreviewMouseLeftButtonDown;
                    wellBtn.MouseMove += WellButton_MouseMove;
                    wellBtn.PreviewMouseLeftButtonUp += WellButton_PreviewMouseLeftButtonUp;

                    Grid.SetRow(wellBtn, row + 1);
                    Grid.SetColumn(wellBtn, col + 1);
                    WellGrid.Children.Add(wellBtn);
                }
            }

            // 回显已选中的孔位
            UpdateAllButtonsState();
        }
        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            // 判断是否已经全选
            bool isAllSelected = Enumerable.Range(1, Rows)
                .All(row => Enumerable.Range(1, Cols)
                    .All(col => SelectedWells.Contains((row, col))));

            if (isAllSelected)
            {
                // 全不选
                SelectedWells.Clear();
            }
            else
            {
                // 全选
                SelectedWells.Clear();
                for (int row = 1; row <= Rows; row++)
                {
                    for (int col = 1; col <= Cols; col++)
                    {
                        SelectedWells.Add((row, col));
                    }
                }
            }

            UpdateAllButtonsState();
        }

        // 创建头部按钮（行/列标签）
        private Button CreateHeaderButton(string content, string tag)
        {
            return new Button
            {
                Content = content,
                Background = Brushes.Teal,
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                Tag = tag
            };
        }

        // 列头点击：选择/取消整列
        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag.ToString().StartsWith("col_"))
            {
                int colIndex = int.Parse(btn.Tag.ToString().Split('_')[1]);
                int colNumber = colIndex + 1;

                // 判断该列是否已全部选中
                bool isAllSelected = Enumerable.Range(1, Rows)
                    .All(row => SelectedWells.Contains((row, colNumber)));

                // 切换整列状态
                for (int row = 1; row <= Rows; row++)
                {
                    if (isAllSelected)
                    {
                        SelectedWells.Remove((row, colNumber));
                    }
                    else
                    {
                        if (!SelectedWells.Contains((row, colNumber)))
                        {
                            SelectedWells.Add((row, colNumber));
                        }
                    }
                }

                UpdateAllButtonsState();
            }
        }

        // 行头点击：选择/取消整行
        private void RowHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag.ToString().StartsWith("row_"))
            {
                int rowIndex = int.Parse(btn.Tag.ToString().Split('_')[1]);
                int rowNumber = rowIndex + 1;

                // 判断该行是否已全部选中
                bool isAllSelected = Enumerable.Range(1, Cols)
                    .All(col => SelectedWells.Contains((rowNumber, col)));

                // 切换整行状态
                for (int col = 1; col <= Cols; col++)
                {
                    if (isAllSelected)
                    {
                        SelectedWells.Remove((rowNumber, col));
                    }
                    else
                    {
                        if (!SelectedWells.Contains((rowNumber, col)))
                        {
                            SelectedWells.Add((rowNumber, col));
                        }
                    }
                }

                UpdateAllButtonsState();
            }
        }

        // 孔位按钮鼠标按下
        private void WellButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(WellGrid);
            _tempSelectedButtons.Clear();

            if (sender is Button btn)
            {
                var wellPos = ((int Row, int Col))btn.Tag;

                // Ctrl+点击：切换单个孔位状态
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (SelectedWells.Contains(wellPos))
                    {
                        SelectedWells.Remove(wellPos);
                    }
                    else
                    {
                        SelectedWells.Add(wellPos);
                    }
                    UpdateAllButtonsState();
                }
                else
                {
                    // 普通点击：开始框选
                    _tempSelectedButtons.Add(btn);
                }
            }

            e.Handled = true;
        }

        // 孔位按钮鼠标移动（框选）
        private void WellButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(WellGrid);

                // 计算框选矩形
                Rect selectionRect = new Rect(
                    Math.Min(_dragStartPoint.X, currentPoint.X),
                    Math.Min(_dragStartPoint.Y, currentPoint.Y),
                    Math.Abs(currentPoint.X - _dragStartPoint.X),
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y));

                // 先恢复所有孔位的正常状态
                foreach (var btn in WellGrid.Children.OfType<Button>()
                    .Where(b => b.Tag is ValueTuple<int, int>))
                {
                    var pos = ((int Row, int Col))btn.Tag;
                    btn.Background = SelectedWells.Contains(pos) ? Brushes.LightGreen : Brushes.White;
                }

                // 标记框选范围内的孔位
                _tempSelectedButtons.Clear();
                foreach (var btn in WellGrid.Children.OfType<Button>()
                    .Where(b => b.Tag is ValueTuple<int, int>))
                {
                    Point btnTopLeft = btn.TransformToAncestor(WellGrid).Transform(new Point(0, 0));
                    Rect btnRect = new Rect(btnTopLeft.X, btnTopLeft.Y, btn.ActualWidth, btn.ActualHeight);

                    if (selectionRect.IntersectsWith(btnRect))
                    {
                        _tempSelectedButtons.Add(btn);
                        btn.Background = Brushes.LightBlue; // 临时选中颜色
                    }
                }
            }
        }

        // 孔位按钮鼠标松开（完成框选）
        private void WellButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                // 如果没有按住Ctrl，先清空原有选择
                if (Keyboard.Modifiers != ModifierKeys.Control)
                {
                    SelectedWells.Clear();
                }

                // 将框选的孔位添加到选中列表
                foreach (var btn in _tempSelectedButtons)
                {
                    var pos = ((int Row, int Col))btn.Tag;
                    if (!SelectedWells.Contains(pos))
                    {
                        SelectedWells.Add(pos);
                    }
                }

                UpdateAllButtonsState();
            }
        }

        // 更新所有按钮的选中状态
        private void UpdateAllButtonsState()
        {
            // 1. 更新孔位按钮
            foreach (var btn in WellGrid.Children.OfType<Button>()
                .Where(b => b.Tag is ValueTuple<int, int>))
            {
                var pos = ((int Row, int Col))btn.Tag;
                btn.Background = SelectedWells.Contains(pos) ? Brushes.LightGreen : Brushes.White;
            }

            // 2. 更新行头按钮状态（全选时变橙色）
            for (int row = 0; row < Rows; row++)
            {
                var rowBtn = WellGrid.Children.OfType<Button>()
                    .FirstOrDefault(b => b.Tag.ToString() == $"row_{row}");

                if (rowBtn != null)
                {
                    bool isAllSelected = Enumerable.Range(1, Cols)
                        .All(col => SelectedWells.Contains((row + 1, col)));

                    rowBtn.Background = isAllSelected ? Brushes.Orange : Brushes.Teal;
                }
            }

            // 3. 更新列头按钮状态（全选时变橙色）
            for (int col = 0; col < Cols; col++)
            {
                var colBtn = WellGrid.Children.OfType<Button>()
                    .FirstOrDefault(b => b.Tag.ToString() == $"col_{col}");

                if (colBtn != null)
                {
                    bool isAllSelected = Enumerable.Range(1, Rows)
                        .All(row => SelectedWells.Contains((row, col + 1)));

                    colBtn.Background = isAllSelected ? Brushes.Orange : Brushes.Teal;
                }
            }
        }

        // 清空选择
        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectedWells.Clear();
            UpdateAllButtonsState();
        }

        // 确定按钮
        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
