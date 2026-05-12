using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OctoFixFlow
{
    public enum CanvasSelectionMode
    {
        SingleCell,    // 单通道：选单个单元格
        EntireColumn,   // 八通道：选整列
        EntirePlate    // 96通道：选整板（所有行+所有列）
    }
    public class ConsumableCanvas : Canvas
    {
        //选中的孔集合
        private SortedSet<(int Row, int Col)> _selectedCells = new SortedSet<(int, int)>();

        // 8通道模式下的行偏移量（错位插枪头）
        private int _columnRowOffset = 0;
        private int _selectedRowCount = 8;
        public CanvasSelectionMode CurrentSelectionMode { get; set; } = CanvasSelectionMode.SingleCell;

        public bool IsInteractive { get; set; } = false;

        //当前关联的板位ID
        public string PlateId { get; set; }

        //选中列变更事件（用于通知主窗口更新孔位输入框）
        public event Action<string, string> SelectedColumnsChanged;

        private ConsSettings _previousConsData;

        public ConsSettings ConsData
        {
            get => (ConsSettings)GetValue(ConsDataProperty);
            set => SetValue(ConsDataProperty, value);
        }

        public static readonly DependencyProperty ConsDataProperty =
            DependencyProperty.Register("ConsData", typeof(ConsSettings), typeof(ConsumableCanvas),
                new PropertyMetadata(null, OnConsDataChanged));

        private static void OnConsDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = (ConsumableCanvas)d;

            if (canvas._previousConsData != null)
            {
                canvas._previousConsData.PropertyChanged -= canvas.OnConsSettingsPropertyChanged;
            }

            canvas._previousConsData = e.NewValue as ConsSettings;
            if (canvas._previousConsData != null)
            {
                canvas._previousConsData.PropertyChanged += canvas.OnConsSettingsPropertyChanged;
            }

            canvas.InvalidateVisual();
        }

        private void OnConsSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (ConsData == null)
                return;

            var borderPen = new Pen(Brushes.Black, 2);
            var primaryBrush = (SolidColorBrush)FindResource("PrimaryColor");
            var selectedCellPen = new Pen(primaryBrush, 1.5);
            var holePen = new Pen(Brushes.DarkGray, 1);

            double scaleX = ActualWidth / (ConsData.labL + 20);
            double scaleY = ActualHeight / (ConsData.labW + 20);
            double scale = Math.Min(scaleX, scaleY);
            double offsetX = (ActualWidth - ConsData.labL * scale) / 2;
            double offsetY = (ActualHeight - ConsData.labW * scale) / 2;

            //绘制带缺口的耗材外框
            var outlineGeometry = DrawConsumableOutline(scale, offsetX, offsetY);
            dc.DrawGeometry(Brushes.White, null, outlineGeometry);
            dc.DrawGeometry(null, borderPen, outlineGeometry);

            //绘制孔
            DrawAllHoles(dc, holePen, selectedCellPen, scale, offsetX, offsetY);

            if (ConsData.numRows > 0 && ConsData.numColumns > 0)
            {
                double dpiScale = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                var typeface = new Typeface("Arial");
                var textBrush = Brushes.Black;

                double firstColCenterX = offsetX + ConsData.distanceRowY * scale;
                double firstRowCenterY = offsetY + ConsData.distanceColumnX * scale;
                double colSpacing = ConsData.distanceColumn * scale;
                double rowSpacing = ConsData.distanceRow * scale;


                double leftEdgeX = offsetX;
                double topEdgeY = offsetY;

                double firstHoleLeftX;
                if (ConsData.type == 4 || ConsData.topShape == 0)
                {
                    firstHoleLeftX = firstColCenterX - ConsData.topRadius * scale;
                }
                else
                {
                    firstHoleLeftX = firstColCenterX - ConsData.topUpperX * scale / 2;
                }
                double firstHoleTopY;
                if (ConsData.type == 4 || ConsData.topShape == 0)
                {
                    firstHoleTopY = firstRowCenterY - ConsData.topRadius * scale;
                }
                else
                {
                    firstHoleTopY = firstRowCenterY - ConsData.topUpperY * scale / 2;
                }
                double availableWidth = firstHoleLeftX - leftEdgeX - 4 * scale;
                double availableHeight = firstHoleTopY - topEdgeY - 4 * scale;
                double fontSize = Math.Clamp(Math.Min(availableWidth, availableHeight) * 0.8, 6, 14);

                double labelBaseX = leftEdgeX + 3 * scale;

                for (int row = 0; row < ConsData.numRows; row++)
                {
                    char rowLetter = (char)('A' + row);
                    var formattedText = new FormattedText(
                        rowLetter.ToString(),
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        textBrush,
                        dpiScale);

                    double holeCenterY = firstRowCenterY + row * rowSpacing;
                    double labelY = holeCenterY - formattedText.Height / 2;
                    dc.DrawText(formattedText, new Point(labelBaseX, labelY));
                }
                double labelBaseY = topEdgeY + 3 * scale;
                for (int col = 0; col < ConsData.numColumns; col++)
                {
                    string colNumber = (col + 1).ToString();
                    var formattedText = new FormattedText(
                        colNumber,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        fontSize,
                        textBrush,
                        dpiScale);

                    double holeCenterX = firstColCenterX + col * colSpacing;
                    double labelX = holeCenterX - formattedText.Width / 2;
                    dc.DrawText(formattedText, new Point(labelX, labelBaseY));
                }
            }
        }

        private PathGeometry DrawConsumableOutline(double scale, double offsetX, double offsetY)
        {
            double width = ConsData.labL * scale;
            double height = ConsData.labW * scale;
            double notchSize = 10 * scale; // 缺口大小

            var outline = new PathGeometry();
            var figure = new PathFigure();

            // 计算起点（考虑缺口）
            Point startPoint = new Point(offsetX, offsetY);

            if (ConsData.NW == 1) // 左上角有缺口
            {
                startPoint = new Point(offsetX, offsetY + notchSize);
            }

            figure.StartPoint = startPoint;

            // 上边线（考虑左右缺口）
            if (ConsData.NW == 1) // 左上角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + notchSize, offsetY), true));
            }

            if (ConsData.NE == 1) // 右上角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + width - notchSize, offsetY), true));
                figure.Segments.Add(new LineSegment(new Point(offsetX + width, offsetY + notchSize), true));
            }
            else
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + width, offsetY), true));
            }

            // 右边线（考虑上下缺口）
            if (ConsData.NE == 1) // 右上角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + width, offsetY + notchSize), true));
            }

            if (ConsData.SE == 1) // 右下角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + width, offsetY + height - notchSize), true));
                figure.Segments.Add(new LineSegment(new Point(offsetX + width - notchSize, offsetY + height), true));
            }
            else
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + width, offsetY + height), true));
            }

            // 下边线（考虑左右缺口）
            if (ConsData.SE == 1) // 右下角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + width - notchSize, offsetY + height), true));
            }

            if (ConsData.SW == 1) // 左下角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX + notchSize, offsetY + height), true));
                figure.Segments.Add(new LineSegment(new Point(offsetX, offsetY + height - notchSize), true));
            }
            else
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX, offsetY + height), true));
            }

            // 左边线（考虑上下缺口）
            if (ConsData.SW == 1) // 左下角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX, offsetY + height - notchSize), true));
            }

            if (ConsData.NW == 1) // 左上角缺口
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX, offsetY + notchSize), true));
            }
            else
            {
                figure.Segments.Add(new LineSegment(new Point(offsetX, offsetY), true));
            }

            figure.IsClosed = true;
            outline.Figures.Add(figure);

            return outline;
        }

        private void DrawAllHoles(DrawingContext dc, Pen normalPen, Pen selectedPen, double scale, double offsetX, double offsetY)
        {
            if (ConsData.numRows <= 0 || ConsData.numColumns <= 0)
                return;

            int m_cols = ConsData.numColumns;
            int m_rows = ConsData.numRows;
            double m_a1Distance = ConsData.distanceRowY; // 对应Qt的m_a1Distance
            double m_gap = ConsData.distanceColumnX;     // 对应Qt的m_gap
            double colSpacing = ConsData.distanceColumn * scale; // 列间距（缩放后）
            double rowSpacing = ConsData.distanceRow * scale;   // 行间距（缩放后）
            var primaryBrush = (SolidColorBrush)FindResource("SuspendColor");
            Brush selectedFillBrush = primaryBrush;       // 选中列的孔填充色
            Brush normalFillBrush = Brushes.Transparent;    // 未选中列的孔填充色（透明）
            //TIP类型耗材
            if (ConsData.type == 2)
            {
                double tipRadius = ConsData.TIPMAXRadius * scale;
                for (int row = 0; row < m_rows; row++)
                {
                    for (int col = 0; col < m_cols; col++)
                    {
                        // 计算孔中心坐标
                        double centerX = offsetX + m_a1Distance * scale + col * colSpacing;
                        double centerY = offsetY + m_gap * scale + row * rowSpacing;

                        // 选中列的画笔
                        bool isCellSelected = _selectedCells.Contains((row + 1, col + 1));

                        Brush currentFillBrush = isCellSelected ? selectedFillBrush : normalFillBrush;
                        Pen currentPen = isCellSelected ? selectedPen : normalPen;
                        // 绘制圆孔
                        dc.DrawEllipse(currentFillBrush, currentPen, new Point(centerX, centerY), tipRadius, tipRadius);
                    }
                }
                return;
            }

            // 2. 非TIP类型耗材（区分圆孔和矩形孔）
            if (ConsData.topShape == 0) // 圆孔
            {
                double radius = ConsData.topRadius * scale;
                for (int row = 0; row < m_rows; row++)
                {
                    for (int col = 0; col < m_cols; col++)
                    {
                        double centerX = offsetX + m_a1Distance * scale + col * colSpacing;
                        double centerY = offsetY + m_gap * scale + row * rowSpacing;
                        // 判断选中状态，切换填充和画笔
                        bool isCellSelected = _selectedCells.Contains((row + 1, col + 1));
                        Brush currentFillBrush = isCellSelected ? selectedFillBrush : normalFillBrush;
                        Pen currentPen = isCellSelected ? selectedPen : normalPen;
                        dc.DrawEllipse(currentFillBrush, currentPen, new Point(centerX, centerY), radius, radius);
                    }
                }
            }
            else if (ConsData.topShape == 1) // 矩形孔
            {
                double rectWidth = ConsData.topUpperX * scale;
                double rectHeight = ConsData.topUpperY * scale;
                for (int row = 0; row < m_rows; row++)
                {
                    for (int col = 0; col < m_cols; col++)
                    {
                        // 计算矩形中心坐标
                        double centerX = offsetX + m_a1Distance * scale + col * colSpacing;
                        double centerY = offsetY + m_gap * scale + row * rowSpacing;

                        // 计算左上角坐标（中心 - 半宽/半高，解决偏移问题）
                        double left = centerX - rectWidth / 2;
                        double top = centerY - rectHeight / 2;

                        bool isCellSelected = _selectedCells.Contains((row + 1, col + 1));
                        Brush currentFillBrush = isCellSelected ? selectedFillBrush : normalFillBrush;
                        Pen currentPen = isCellSelected ? selectedPen : normalPen;

                        // 绘制矩形孔
                        dc.DrawRectangle(currentFillBrush, currentPen, new Rect(left, top, rectWidth, rectHeight));
                    }
                }
            }
        }

        // 【新增】重写鼠标滚轮事件，处理8通道错位
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            // 只有在8通道模式且当前有选中的列时才响应滚轮
            if (CurrentSelectionMode != CanvasSelectionMode.EntireColumn ||
                _selectedCells.Count == 0 ||
                ConsData == null) return;

            int currentCol = _selectedCells.First().Col;

            // 计算该耗材下的“满通道数”（如果耗材超过8行就按8行算，没超过就按实际行数算）
            int fullChannelCount = Math.Min(8, ConsData.numRows);

            // 滚轮向上 (e.Delta > 0)：想往上看 / 减少选中行数
            if (e.Delta > 0)
            {
                // 1. 优先尝试减少偏移量（如果还在错位状态，先回正）
                if (_columnRowOffset > 0)
                {
                    _columnRowOffset--;
                }
                // 2. 偏移量已经是0了，开始减少选中的行数（例如 1~8 变成 1~7）
                else
                {
                    if (_selectedRowCount > 1) // 最少保留1行
                    {
                        _selectedRowCount--;
                    }
                }
            }
            // 滚轮向下 (e.Delta < 0)：想往下看 / 增加选中行数
            else
            {
                // 1. 优先尝试恢复行数（如果之前行数被减少了，先补回8行）
                if (_selectedRowCount < fullChannelCount)
                {
                    _selectedRowCount++;
                }
                // 2. 行数已经满了，开始增加偏移量（例如 1~8 变成 2~9）
                else
                {
                    // 【核心边界限制】计算最大偏移量：
                    // 公式：(起始行1 + 偏移量) + (总行数 - 1) <= 耗材总行数
                    // 简化：偏移量 <= 耗材总行数 - 当前选中行数
                    //int maxAllowOffset = ConsData.numRows - _selectedRowCount;

                    if (_columnRowOffset < (ConsData.numRows - 1))
                    {
                        _columnRowOffset++;
                    }

                }
            }
            Debug.WriteLine(_columnRowOffset + "[]" + _selectedRowCount);
            // 重新生成选中的孔
            _selectedCells.Clear();
            for (int r = 1; r <= _selectedRowCount; r++)
            {
                int targetRow = r + _columnRowOffset;
                // 这里的二次保险虽然有了maxAllowOffset，但为了安全还是保留
                if (targetRow >= 1 && targetRow <= ConsData.numRows)
                {
                    _selectedCells.Add((targetRow, currentCol));
                }
            }

            InvalidateVisual();
            SelectedColumnsChanged?.Invoke(PlateId, FormatSelectedColumns(ConsData.numRows, ConsData.numColumns));

            //// 获取当前选中的列
            //int currentCol = _selectedCells.First().Col;

            //// 计算偏移量：滚轮向上(e.Delta>0)，偏移量减小(往上错)；反之增大
            //// 注意：WPF默认滚轮向上是正，向下是负，可根据实际手感调整
            //int delta = e.Delta > 0 ? -1 : 1;
            //int newOffset = _columnRowOffset + delta;

            //// 计算最大允许偏移量 (假设是8通道，总行数 - 8)
            //// 如果耗材不是8行，这里动态计算
            ////int channelCount = 8;
            ////int maxOffset = ConsData.numRows - channelCount;
            //int maxOffset = ConsData.numRows;
            //// 限制偏移量范围：0 <= offset <= maxOffset
            //if (newOffset < 0) newOffset = 0;
            //if (newOffset > maxOffset) newOffset = maxOffset;

            //// 如果偏移量没变化，不执行操作
            //if (newOffset == _columnRowOffset) return;

            //_columnRowOffset = newOffset;

            //// 重新生成选中的孔
            //_selectedCells.Clear();
            //for (int r = 1; r <= maxOffset; r++)
            //{
            //    int targetRow = r + _columnRowOffset;
            //    // 确保行号不越界
            //    if (targetRow >= 1 && targetRow <= ConsData.numRows)
            //    {
            //        _selectedCells.Add((targetRow, currentCol));
            //    }
            //}

            //InvalidateVisual();
            //SelectedColumnsChanged?.Invoke(PlateId, FormatSelectedColumns());
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (!IsInteractive || ConsData == null || ConsData.numColumns <= 0)
                return;

            // 点击时重置偏移量（每次重新点击列，都从第1行开始）
            _columnRowOffset = 0;
            _selectedRowCount = Math.Min(8, ConsData.numRows);

            // 计算缩放因子（与绘制时完全一致）
            double scaleX = ActualWidth / (ConsData.labL + 20);
            double scaleY = ActualHeight / (ConsData.labW + 20);
            double scale = Math.Min(scaleX, scaleY);

            // 鼠标位置转换（相对控件坐标）
            var mousePos = e.GetPosition(this);
            double offsetX = (ActualWidth - ConsData.labL * scale) / 2;
            double offsetY = (ActualHeight - ConsData.labW * scale) / 2;

            // 计算有效点击区域（与绘制时的列间距完全对应）
            double colStartX = offsetX + ConsData.distanceRowY * scale; // 对应m_a1Distance的X起点
            double colSpacing = ConsData.distanceColumn * scale; // 与绘制时的列间距一致

            double rowStartY = offsetY + ConsData.distanceColumnX * scale; // 行起点Y（对应原有m_gap）
            double rowSpacing = ConsData.distanceRow * scale;             // 行间距

            double rawCol = (mousePos.X - colStartX) / colSpacing;
            int col = Math.Clamp((int)Math.Round(rawCol) + 1, 1, ConsData.numColumns);
            double rawRow = (mousePos.Y - rowStartY) / rowSpacing;
            int row = Math.Clamp((int)Math.Round(rawRow) + 1, 1, ConsData.numRows);

            // 单选逻辑：先清空所有选中列，再添加当前列
            _selectedCells.Clear();
            if (CurrentSelectionMode == CanvasSelectionMode.EntirePlate)
            {
                // 96通道：选中所有行+所有列（整板）
                for (int r = 1; r <= ConsData.numRows; r++)
                {
                    for (int c = 1; c <= ConsData.numColumns; c++)
                    {
                        _selectedCells.Add((r, c));
                    }
                }
            }
            else if (CurrentSelectionMode == CanvasSelectionMode.EntireColumn)
            {
                // 八通道：选中整列
                //for (int r = 1; r <= ConsData.numRows; r++)
                //{
                //    _selectedCells.Add((r, col));
                //}
                int channelCount = 8; // 默认8通道
                for (int r = 1; r <= channelCount; r++)
                {
                    // 实际行号 = 循环序号(1-8) + 偏移量(0)
                    int targetRow = r + _columnRowOffset;
                    if (targetRow >= 1 && targetRow <= ConsData.numRows)
                    {
                        _selectedCells.Add((targetRow, col));
                    }
                }

            }
            else
            {
                // 单通道：选中单个单元格
                _selectedCells.Add((row, col));

            }
            InvalidateVisual(); // 刷新绘制
            SelectedColumnsChanged?.Invoke(PlateId, FormatSelectedColumns(ConsData.numRows, ConsData.numColumns));
            //}
        }

        private string FormatSelectedColumns(int consRows, int consCols)
        {
            if (_selectedCells.Count == 0)
                return "";

            var cells = _selectedCells.ToList();

            if (!cells.Any())
                return "";

            var rows = cells.Select(c => c.Row).Distinct().OrderBy(r => r).ToList();
            var cols = cells.Select(c => c.Col).Distinct().OrderBy(c => c).ToList();

            string rowText = FormatRange(rows, consRows);
            string colText = FormatRange(cols, consCols);

            return $"{ResourceHelper.Instance.StepDetailRowPrefix}{rowText} {ResourceHelper.Instance.StepDetailColumnPrefix}{colText}";
        }
        // 将数字列表格式化为“X”或“X~Y”范围字符串
        private string FormatRange(List<int> numbers, int consNums)
        {
            if (numbers.Count == 0)
                return "";
            if (numbers.Count == 1)
                return numbers[0].ToString();

            int lastNumber = numbers[numbers.Count - 1];

            var ranges = new List<string>();
            int start = numbers[0];
            //if (lastNumber == consNums)
            //{
            //    ranges.Add($"{start}");

            //}
            //else
            //{
            //    ranges.Add($"{lastNumber - consNums}");
            //}
            int end = numbers[0];

            for (int i = 1; i < numbers.Count; i++)
            {
                if (numbers[i] == end + 1)
                {
                    end = numbers[i];
                }
                else
                {
                    ranges.Add(start == end ? $"{start}" : $"{start}~{end}");
                    start = end = numbers[i];
                }
            }
            ranges.Add(start == end ? $"{start}" : $"{start}~{end}");

            return string.Join("；", ranges);
        }
        //清空选中状态
        public void ClearSelection()
        {
            _selectedCells.Clear();
            _columnRowOffset = 0;
            InvalidateVisual();
        }

        public void SetSelectedCells(IEnumerable<(int Row, int Col)> cells)
        {
            _selectedCells.Clear();

            if (cells != null)
            {
                foreach (var cell in cells)
                {
                    // 校验行列号有效性
                    if (cell.Row >= 1 && cell.Row <= ConsData?.numRows &&
                        cell.Col >= 1 && cell.Col <= ConsData?.numColumns)
                    {
                        _selectedCells.Add(cell);
                    }
                }
                if (_selectedCells.Count > 0)
                {
                    var rows = _selectedCells.Select(c => c.Row).OrderBy(r => r).ToList();

                    // 计算选中了多少行
                    _selectedRowCount = rows.Last();

                    // 计算偏移量：最小的行号 - 1
                    // 例如：选中了 3,4,5,6,7,8 -> 最小行是3 -> Offset = 3 - 1 = 2
                    _columnRowOffset = rows.First() - 1;
                }
            }
            else
            {
                _columnRowOffset = 0;
                _selectedRowCount = Math.Min(8, ConsData?.numRows ?? 8);
            }
            //Debug.WriteLine(":::" + _columnRowOffset + ";;;" + _selectedRowCount);
            InvalidateVisual();
        }
    }
}