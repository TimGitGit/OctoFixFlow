using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OctoFixFlow
{
    public enum CanvasSelectionMode
    {
        SingleCell,    // 单通道：选单个单元格
        EntireColumn   // 八通道：选整列
    }
    public class ConsumableCanvas : Canvas
    {
        //选中的列集合（排序去重）
        //private SortedSet<int> _selectedColumns = new SortedSet<int>();
        private SortedSet<(int Row, int Col)> _selectedCells = new SortedSet<(int, int)>();
        // 画布选择模式（单通道选单元格/八通道选整列）

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
            var selectedCellPen = new Pen(Brushes.Red, 1.5);
            var holePen = new Pen(Brushes.DarkGray, 1);

            double scaleX = ActualWidth / (ConsData.labL + 20);
            double scaleY = ActualHeight / (ConsData.labW + 20);
            double scale = Math.Min(scaleX, scaleY);
            double offsetX = (ActualWidth - ConsData.labL * scale) / 2;
            double offsetY = (ActualHeight - ConsData.labW * scale) / 2;

            //绘制带缺口的耗材外框
            //DrawConsumableOutline(dc, borderPen, scale, offsetX, offsetY);
            var outlineGeometry = DrawConsumableOutline(scale, offsetX, offsetY);
            dc.DrawGeometry(Brushes.White, null, outlineGeometry);
            dc.DrawGeometry(null, borderPen, outlineGeometry);

            //绘制孔
            DrawAllHoles(dc, holePen, selectedCellPen, scale, offsetX, offsetY);

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

            // 核心参数（与Qt变量对应）
            int m_cols = ConsData.numColumns;
            int m_rows = ConsData.numRows;
            double m_a1Distance = ConsData.distanceRowY; // 对应Qt的m_a1Distance
            double m_gap = ConsData.distanceColumnX;     // 对应Qt的m_gap
            double colSpacing = ConsData.distanceColumn * scale; // 列间距（缩放后）
            double rowSpacing = ConsData.distanceRow * scale;   // 行间距（缩放后）
            Brush selectedFillBrush = Brushes.Orange;       // 选中列的孔填充色
            Brush normalFillBrush = Brushes.Transparent;    // 未选中列的孔填充色（透明不遮挡背景）
            // 1. TIP类型耗材（对应Qt的m_Type == 4）
            if (ConsData.type == 4)
            {
                double tipRadius = ConsData.TIPMAXRadius * scale;
                for (int row = 0; row < m_rows; row++)
                {
                    for (int col = 0; col < m_cols; col++)
                    {
                        // 计算孔中心坐标（与Qt逻辑完全对应）
                        double centerX = offsetX + m_a1Distance * scale + col * colSpacing;
                        double centerY = offsetY + m_gap * scale + row * rowSpacing;

                        // 选中列的画笔
                        //bool isColumnSelected = _selectedColumns.Contains(col + 1);
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
            if (ConsData.topShape == 0) // 圆孔（对应Qt的nowShope == 0）
            {
                double radius = ConsData.topRadius * scale;
                for (int row = 0; row < m_rows; row++)
                {
                    for (int col = 0; col < m_cols; col++)
                    {
                        double centerX = offsetX + m_a1Distance * scale + col * colSpacing;
                        double centerY = offsetY + m_gap * scale + row * rowSpacing;
                        // 判断选中状态，切换填充和画笔
                        //bool isColumnSelected = _selectedColumns.Contains(col + 1);
                        bool isCellSelected = _selectedCells.Contains((row + 1, col + 1));
                        Brush currentFillBrush = isCellSelected ? selectedFillBrush : normalFillBrush;
                        Pen currentPen = isCellSelected ? selectedPen : normalPen;
                        dc.DrawEllipse(currentFillBrush, currentPen, new Point(centerX, centerY), radius, radius);
                    }
                }
            }
            else if (ConsData.topShape == 1) // 矩形孔（对应Qt的nowShope == 1）
            {
                double rectWidth = ConsData.topUpperX * scale;
                double rectHeight = ConsData.topUpperY * scale;
                for (int row = 0; row < m_rows; row++)
                {
                    for (int col = 0; col < m_cols; col++)
                    {
                        // 计算矩形中心坐标（与Qt逻辑一致）
                        double centerX = offsetX + m_a1Distance * scale + col * colSpacing;
                        double centerY = offsetY + m_gap * scale + row * rowSpacing;

                        // 计算左上角坐标（中心 - 半宽/半高，解决偏移问题）
                        double left = centerX - rectWidth / 2;
                        double top = centerY - rectHeight / 2;

                        //bool isColumnSelected = _selectedColumns.Contains(col + 1);
                        bool isCellSelected = _selectedCells.Contains((row + 1, col + 1));
                        Brush currentFillBrush = isCellSelected ? selectedFillBrush : normalFillBrush;
                        Pen currentPen = isCellSelected ? selectedPen : normalPen;

                        // 绘制矩形孔
                        dc.DrawRectangle(currentFillBrush, currentPen, new Rect(left, top, rectWidth, rectHeight));

                    }
                }
            }
        }


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (!IsInteractive || ConsData == null || ConsData.numColumns <= 0)
                return;

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
            double colEndX = colStartX + ConsData.numColumns * colSpacing;

            double rowStartY = offsetY + ConsData.distanceColumnX * scale; // 行起点Y（对应原有m_gap）
            double rowSpacing = ConsData.distanceRow * scale;             // 行间距
            double rowEndY = rowStartY + ConsData.numRows * rowSpacing;

            // 检查点击是否在有效列区域内
            //if (mousePos.X >= startX - colSpacing * 0.1 && mousePos.X <= endX + colSpacing * 0.1)
            //{
            // 计算选中的列（核心修正：使用Math.Round避免浮点数精度问题）
            //double rawColumn = (mousePos.X - startX) / colSpacing;
            //int column = (int)Math.Round(rawColumn) + 1; // 四舍五入减少误差
            //                                             // 强制限制列号在有效范围内（1 ~ 最大列数）
            //column = Math.Clamp(column, 1, ConsData.numColumns);
            double rawCol = (mousePos.X - colStartX) / colSpacing;
            int col = Math.Clamp((int)Math.Round(rawCol) + 1, 1, ConsData.numColumns);
            double rawRow = (mousePos.Y - rowStartY) / rowSpacing;
            int row = Math.Clamp((int)Math.Round(rawRow) + 1, 1, ConsData.numRows);
            // 单选逻辑：先清空所有选中列，再添加当前列
            _selectedCells.Clear();
            //_selectedCells.Add((row, col));
            if (CurrentSelectionMode == CanvasSelectionMode.EntireColumn)
            {
                // 八通道：选中整列（当前列的所有行）
                for (int r = 1; r <= ConsData.numRows; r++)
                {
                    _selectedCells.Add((r, col));
                }
            }
            else
            {
                // 单通道：选中单个单元格
                _selectedCells.Add((row, col));
            }
            InvalidateVisual(); // 刷新绘制
            SelectedColumnsChanged?.Invoke(PlateId, FormatSelectedColumns());
            //}
        }

        private string FormatSelectedColumns()
        {
            if (_selectedCells.Count == 0)
                return "";

            var cells = _selectedCells.ToList();
            if (!cells.Any())
                return "";
            var rows = cells.Select(c => c.Row).Distinct().OrderBy(r => r).ToList();
            var cols = cells.Select(c => c.Col).Distinct().OrderBy(c => c).ToList();

            string rowText = FormatRange(rows);
            string colText = FormatRange(cols);

            return $"{ResourceHelper.Instance.StepDetailRowPrefix}{rowText} {ResourceHelper.Instance.StepDetailColumnPrefix}{colText}";

            //var ranges = new List<string>();
            //int start = columns[0];
            //int end = columns[0];

            //for (int i = 1; i < columns.Count; i++)
            //{
            //    if (columns[i] == end + 1)
            //    {
            //        end = columns[i];
            //    }
            //    else
            //    {
            //        ranges.Add(start == end ? $"{start}" : $"{start}~{end}");
            //        start = end = columns[i];
            //    }
            //}

            //ranges.Add(start == end ? $"{start}" : $"{start}~{end}");

            //return $"{ResourceHelper.Instance.StepDetailColumnPrefix}{string.Join("；", ranges)}";
        }
        // 将数字列表格式化为“X”或“X~Y”范围字符串
        private string FormatRange(List<int> numbers)
        {
            if (numbers.Count == 0)
                return "";
            if (numbers.Count == 1)
                return numbers[0].ToString();

            var ranges = new List<string>();
            int start = numbers[0];
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
            InvalidateVisual();
        }
        //根据列集合更新选中状态
        //public void SetSelectedColumns(IEnumerable<int> columns)
        //{
        //    _selectedColumns.Clear();
        //    if (columns != null)
        //    {
        //        foreach (var col in columns)
        //            _selectedColumns.Add(col);
        //    }
        //    InvalidateVisual();
        //}
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
            }
            InvalidateVisual();
        }
    }
}