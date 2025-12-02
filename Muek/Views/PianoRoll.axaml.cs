using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Muek.Models;
using Muek.Services;
using Muek.ViewModels;
using NAudio.Midi;

namespace Muek.Views;

public partial class PianoRoll : UserControl
{
    public int Index = 0;

    public static readonly StyledProperty<double> NoteHeightProperty = AvaloniaProperty.Register<PianoRoll, double>(
        nameof(NoteHeight));

    public double NoteHeight
    {
        get => GetValue(NoteHeightProperty);
        set => SetValue(NoteHeightProperty, value);
    }

    //为了同步一些属性，把两边的钢琴写在一个类里了
    public bool IsPianoBar { get; set; } = false;

    public static readonly StyledProperty<double> ScrollOffsetProperty = AvaloniaProperty.Register<PianoRoll, double>(
        nameof(ScrollOffset));

    public double ScrollOffset
    {
        get => GetValue(ScrollOffsetProperty);
        set => SetValue(ScrollOffsetProperty, value);
    }

    public static readonly StyledProperty<double> ClampValueProperty = AvaloniaProperty.Register<PianoRoll, double>(
        nameof(ClampValue));

    public double ClampValue
    {
        get => GetValue(ClampValueProperty);
        set => SetValue(ClampValueProperty, value);
    }

    private double _widthOfBeat;

    public double WidthOfBeat
    {
        get => _widthOfBeat;
        set => _widthOfBeat = value;
    }

    private MainWindow? _mainWindow;
    private MainWindow MainWindow => _mainWindow ??= ViewHelper.GetMainWindow();

    public const int NoteRangeMax = 9;
    public const int NoteRangeMin = 0;

    public const int Temperament = 12;

    private static readonly string[] NotePrefixes = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];


    // private double _renderSize = 2000;


    private readonly IBrush _noteColor1 = Brushes.White;
    private readonly IBrush _noteColor2 = Brushes.Black;

    private static Color _noteColor3 => _pattern?.Color ?? DataStateService.MuekColor;
    private Color NoteColor3 => _pattern?.Color ?? DataStateService.MuekColor;

    private IBrush _noteHoverColor = new SolidColorBrush(Colors.Black, .5);

    private int _currentHoverNote = -1;

    private bool _isDrawing = false;
    private bool _isDragging = false;
    private bool _isEditing = false;

    private double _currentNoteStartTime;
    private double _currentNoteEndTime;
    private Point _dragPos;
    private double _dragStartTime;
    private double _dragEndTime;

    private Note _editingNote = new Note();

    public record struct Note()
    {
        public int Name = 0;
        public double StartTime = 0;
        public double EndTime = 0;

        public int Velocity = 127;
        // public Color Color = default;
    }

    private Note _currentHoverDrawnNote = new();

    private Point _currentMousePosition;
    private Point _pressedMousePosition;

    private static PatternViewModel? _pattern = null;

    public PatternViewModel? Pattern
    {
        get => _pattern;
        set
        {
            _pattern = value;
            MainWindow.PianoRollWindow.ChannelSelectionDisable.IsVisible = _pattern is null;
            if (_pattern is null)
                MainWindow.PianoRollWindow.Channel.UnselectAll();
            OnPropertyChanged(nameof(Pattern));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (Pattern != null)
        {
            MainWindow.PianoRollWindow.WindowCover.IsVisible = false;
        }
        else
        {
            MainWindow.PianoRollWindow.WindowCover.IsVisible = true;
        }

        var patterns = MainWindow.PianoRollWindow.PatternSelection.ViewModel.Patterns;
        foreach (var pattern in patterns)
        {
            if (pattern == Pattern) pattern.Background = new SolidColorBrush(pattern.Color);
            else pattern.Background = new SolidColorBrush(Colors.Black, 0);
        }
    }

    private static readonly List<Note> EmptyNotes = [];

    private int CurrentChannel
    {
        get => (int)(MainWindow.PianoRollWindow.Channel.SelectedItem ?? 1);
        set
        {
            MainWindow.PianoRollWindow.Channel.SelectedItem = int.Clamp(value, 1, 16);
            InvalidateVisual();
        }
    }


    public List<Note> Notes
    {
        get => Pattern == null ? EmptyNotes : Pattern.Notes[CurrentChannel - 1];
        set
        {
            if (Pattern != null) Pattern.Notes[CurrentChannel - 1] = value;
        }
    }

    public Point ScalingSensitivity = new Point(5, 2);


    //框选音符
    private Rect? _selectFrame;

    public List<Note> SelectedNotes = new();

    // private bool _isShowingOptions = false;
    public double Magnet = 1.0;
    public readonly double LengthIncreasement = 128d;


    private static readonly Pen WhitePen = new Pen(new SolidColorBrush(Colors.White, .1));
    private static readonly IBrush WhiteBrush = new SolidColorBrush(Colors.White, .05);
    private static readonly Pen WhiteGridPen = new Pen(new SolidColorBrush(Colors.White, .2), .1);
    private static readonly Pen PurpleGridPen = new Pen(new SolidColorBrush(Colors.MediumPurple, .5));
    private static readonly Pen BlueGridPen = new Pen(new SolidColorBrush(Colors.LightSkyBlue, .5));

    private static readonly Pen NoteWhitePen = new Pen(Brushes.White);
    private static readonly Pen RedPen = new Pen(Brushes.Red, 2);
    private static readonly Pen OrangePen = new Pen(Brushes.Orange);
    private static readonly Pen BlackPen = new Pen(Brushes.Black);

    private static readonly Pen GridLinePenNormal = new Pen(new SolidColorBrush(Colors.White, .5));
    private static readonly Pen GridLinePenThin = new Pen(new SolidColorBrush(Colors.White, .5), .5);
    private static Pen GridLinePenColor => new(new SolidColorBrush(_noteColor3));
    private static Pen GridLinePenColorThin => new(new SolidColorBrush(_noteColor3), .5);
    private static Pen GridLinePenColorThinner => new(new SolidColorBrush(_noteColor3), .2);
    private static readonly SolidColorBrush GridLineTextColor = new SolidColorBrush(Colors.White);
    private static readonly SolidColorBrush GridLineTextColorTranslucent = new SolidColorBrush(Colors.White, .2);

    private int _dragNoteVelocity;


    public PianoRoll()
    {
        InitializeComponent();

        NoteHeight = 15;
        ClampValue = 0;


        // IsVisible = true;
        Height = NoteHeight * (NoteRangeMax - NoteRangeMin + 1) * Temperament;
        // Console.WriteLine(Height);


        // _noteHoverColor = new SolidColorBrush(DataStateService.MuekColor, .1);

        _widthOfBeat = 50;
        if (!IsPianoBar)
        {
            Width = LengthIncreasement * _widthOfBeat;
        }
    }

    //AI写的并行，可能出事，但是性能提升非常大
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // context.FillRectangle(Brushes.DimGray, new Rect(0, 0, Width, Height));

        //左侧钢琴Bar
        if (IsPianoBar)
        {
            // 预计算所有需要的颜色和文本
            var noteData = new List<(int noteName, IBrush color, string displayText, double yPosition)>();
            for (int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
            {
                var noteName = i;
                var displayText = IndexToNoteName(noteName);
                var color = NoteNameToBrush(displayText);
                var yPosition = Height - (noteName + 1) * NoteHeight;

                noteData.Add((noteName, color, displayText, yPosition));
            }

            // 串行绘制
            foreach (var (noteName, color, displayText, yPosition) in noteData)
            {
                context.FillRectangle(color,
                    new Rect(0, yPosition, Width * .9, NoteHeight),
                    5);
                context.FillRectangle(color,
                    new Rect(0, yPosition, Width * .8, NoteHeight));

                context.DrawText(new FormattedText(displayText,
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                        NoteHeight * .6, (color == _noteColor2 ? _noteColor1 : _noteColor2)),
                    new Point(0, yPosition)
                );

                if (noteName.Equals(_currentHoverNote))
                {
                    context.FillRectangle(_noteHoverColor,
                        new Rect(0, yPosition, Width * .9, NoteHeight));
                }
            }
        }

        //绘制区域
        else
        {
            var left = MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.X;
            var right = MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.X +
                        MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width;
            var visibleRect = new Rect(left, 0, right - left, Bounds.Height);
            using (context.PushClip(visibleRect))
            {
                // 预计算背景网格数据
                var backgroundRects = new List<(Rect rect, bool isWhiteKey)>();
                for (int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
                {
                    var noteName = i;
                    var yPosition = Height - (noteName + 1) * NoteHeight;
                    var rect = new Rect(ClampValue, yPosition, Width, NoteHeight);
                    var isWhiteKey = !IndexToNoteName(noteName).Contains('#');

                    backgroundRects.Add((rect, isWhiteKey));
                }

                // 批量绘制背景
                foreach (var (rect, isWhiteKey) in backgroundRects)
                {
                    context.DrawRectangle(Brush.Parse("#40232323"), WhitePen, rect);

                    if (isWhiteKey)
                    {
                        context.DrawRectangle(WhiteBrush, null, rect);
                    }

                    // 检查hover状态（这里需要特殊处理，因为_currentHoverNote可能变化）
                    var noteName = (int)((Height - rect.Y) / NoteHeight - 1);
                    if (noteName.Equals(_currentHoverNote))
                    {
                        context.FillRectangle(new SolidColorBrush(NoteColor3, .1), rect);
                    }
                }

                // 网格线绘制（保持原有逻辑）

                if (_widthOfBeat > 20)
                {
                    for (double i = 0; i < Width / _widthOfBeat; i += double.Clamp(Magnet, 1 / 8d, 2d))
                    {
                        if (i % 4 == 0) continue;
                        if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                        {
                            context.DrawLine(WhiteGridPen,
                                new Point(i * _widthOfBeat, 0),
                                new Point(i * _widthOfBeat, Height));
                        }
                    }

                    if (Magnet <= 1 / 6.0)
                    {
                        PurpleGridPen.Thickness = 1;
                        for (int i = 1; i < Width / _widthOfBeat; i += 2)
                        {
                            if (_widthOfBeat < 50) PurpleGridPen.Thickness = .5;
                            if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                            {
                                context.DrawLine(PurpleGridPen,
                                    new Point(i * _widthOfBeat, 0),
                                    new Point(i * _widthOfBeat, Height));
                            }
                        }
                    }

                    if (Magnet <= 1 / 3.0)
                    {
                        BlueGridPen.Thickness = 1;
                        for (int i = 2; i < Width / _widthOfBeat; i += 4)
                        {
                            if (_widthOfBeat < 50) BlueGridPen.Thickness = .5;
                            if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                            {
                                context.DrawLine(BlueGridPen,
                                    new Point(i * _widthOfBeat, 0),
                                    new Point(i * _widthOfBeat, Height));
                            }
                        }
                    }
                }

                // 小节线和文本绘制 - 优化版本
                var beatWidth = _widthOfBeat;
                var clampVal = ClampValue;
                var visibleWidth = Width + clampVal;
                var scrollOffset = ScrollOffset;

                // 预计算循环边界，避免重复除法
                int totalBeats = (int)(Width / beatWidth) + 1;
                int startBeat = Math.Max(0, (int)(clampVal / beatWidth));
                int endBeat = Math.Min(totalBeats, (int)(visibleWidth / beatWidth));

                // 预定义格式化参数
                var culture = CultureInfo.CurrentCulture;
                var flowDirection = FlowDirection.LeftToRight;
                var typeface = Typeface.Default;

                // 批量收集绘制操作
                var measureLines = new List<(Point start, Point end, IPen pen)>();
                var beatLines = new List<(Point start, Point end, IPen pen)>();
                var measureTexts = new List<(string text, Point position, double fontSize, IBrush color)>();
                var beatTexts = new List<(string text, Point position, double fontSize, IBrush color)>();

                for (int i = startBeat; i < endBeat; i++)
                {
                    double xPos = i * beatWidth;

                    // 每16拍（一个小节）绘制小节线
                    if (i % 16 == 0)
                    {
                        IPen pen = beatWidth < 20
                            ? (beatWidth < 5 ? GridLinePenColorThinner : GridLinePenColorThin)
                            : GridLinePenColor;

                        measureLines.Add((new Point(xPos, 0), new Point(xPos, Height), pen));

                        // 小节号文本
                        double fontSize = beatWidth < 10 ? (beatWidth < 2 ? 5 : 10) : 15;
                        measureTexts.Add(($"{i / 16 + 1}", new Point(6 + xPos + 1, scrollOffset), fontSize,
                            GridLineTextColor));
                    }

                    // 每4拍绘制次强拍线
                    if (i % 4 == 0 && i % 16 != 0)
                    {
                        if (beatWidth > 20)
                        {
                            // 拍号文本
                            string text = $"{i / 16 + 1} : {(1 + (i / 4) % 4)}";
                            beatTexts.Add((text, new Point(6 + xPos + 1, scrollOffset), 10,
                                GridLineTextColorTranslucent));
                        }

                        if (beatWidth > 5)
                        {
                            IPen pen = beatWidth < 20 ? GridLinePenThin : GridLinePenNormal;
                            beatLines.Add((new Point(xPos, 0), new Point(xPos, Height), pen));
                        }
                    }
                }

                // 批量绘制线条
                foreach (var line in measureLines)
                {
                    context.DrawLine(line.pen, line.start, line.end);
                }

                foreach (var line in beatLines)
                {
                    context.DrawLine(line.pen, line.start, line.end);
                }

                // 批量绘制文本
                foreach (var textData in measureTexts)
                {
                    var formattedText = new FormattedText(textData.text, culture, flowDirection,
                        typeface, textData.fontSize, textData.color);
                    context.DrawText(formattedText, textData.position);
                }

                foreach (var textData in beatTexts)
                {
                    var formattedText = new FormattedText(textData.text, culture, flowDirection,
                        typeface, textData.fontSize, textData.color);
                    context.DrawText(formattedText, textData.position);
                }

                // 当前位置指示线
                if (_currentHoverNote != -1)
                {
                    var pen = new Pen(new SolidColorBrush(NoteColor3), .5);
                    if (!_isDrawing && !_isEditing && !_isDragging)
                    {
                        context.DrawLine(pen,
                            new Point((_currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet)), 0),
                            new Point((_currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet)),
                                Height)
                        );
                    }
                    else if (!_isDragging)
                    {
                        context.DrawLine(pen,
                            new Point(
                                (_currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                                 _widthOfBeat * Magnet), 0),
                            new Point(
                                (_currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                                 _widthOfBeat * Magnet), Height)
                        );
                    }
                }

                var rightSideCursor = false;


                var pattern = Pattern;
                var notes = Notes;

                // 预计算音符绘制数据
                // 预计算音符绘制数据 - 使用结构体数组避免堆分配
                var noteCount = pattern?.Notes.Sum(pNotes => pNotes.Count) ?? 0;
                var noteDrawData =
                    new (Note note, double start, double end, double yPosition, int noteName, bool isInNotes, bool
                        isSelected, bool isHovered, double noteWidth)[noteCount];

                if (pattern != null)
                {
                    var index = 0;
                    var leftTime = left / _widthOfBeat;
                    var rightTime = right / _widthOfBeat;
                    var notesSet = new HashSet<Note>(notes); // 使用HashSet提高Contains性能
                    var selectedNotesSet = new HashSet<Note>(SelectedNotes);

                    foreach (var pNotes in pattern.Notes)
                    {
                        foreach (Note existNote in pNotes)
                        {
                            if (existNote.StartTime < rightTime && existNote.EndTime > leftTime)
                            {
                                var start = existNote.StartTime * _widthOfBeat;
                                var end = existNote.EndTime * _widthOfBeat;
                                var yPosition = Height - (existNote.Name + 1) * NoteHeight;
                                var noteWidth = end - start;
                                var isInNotes = notesSet.Contains(existNote);
                                var isSelected = selectedNotesSet.Contains(existNote);
                                var isHovered = existNote.Name.Equals(_currentHoverNote) &&
                                                _currentMousePosition.X > start &&
                                                _currentMousePosition.X < end;

                                noteDrawData[index++] = (existNote, start, end, yPosition, existNote.Name, isInNotes,
                                    isSelected, isHovered, noteWidth);
                            }
                        }
                    }

                    // 如果实际数量小于预分配，调整数组大小
                    if (index < noteCount)
                    {
                        Array.Resize(ref noteDrawData, index);
                    }
                }

                // 预定义格式化参数避免重复创建
                var textColorWhite = Brushes.White;
                var textColorBlack = Brushes.Black;
                var translucentWhite = Brush.Parse("#22ffffff");
                var noteHeight90Percent = NoteHeight * .9;
                var noteHeight80Percent = NoteHeight * .8;
                var noteHeight60Percent = NoteHeight * .6;

                // 批量绘制音符
                foreach (var (existNote, start, end, yPosition, noteName, isInNotes, isSelected, isHovered, noteWidth)
                         in noteDrawData)
                    if (!(_isEditing && SelectedNotes.Contains(existNote)))
                    {
                        var velocity = existNote.Velocity;

                        if (!isInNotes)
                        {
                            if (noteName.Equals(existNote.Name))
                            {
                                context.DrawRectangle(translucentWhite, null,
                                    new Rect(start, yPosition, noteWidth, noteHeight90Percent));
                            }
                        }
                        else
                        {
                            if (noteName.Equals(existNote.Name) && (!_isDragging || !isSelected))
                            {
                                if (isSelected)
                                {
                                    context.DrawRectangle(textColorBlack, NoteWhitePen,
                                        new Rect(start, yPosition, noteWidth, noteHeight90Percent));
                                }
                                else
                                {
                                    context.DrawRectangle(new SolidColorBrush(NoteColor3, velocity / 127.0),
                                        noteWidth > 3 ? BlackPen : null,
                                        new Rect(start, yPosition, noteWidth, noteHeight90Percent));
                                }

                                // 音符文本 - 预计算文本内容避免重复字符串分配
                                if (noteWidth > noteHeight80Percent)
                                {
                                    var textColor = isSelected ? textColorWhite : textColorBlack;
                                    string text;

                                    if (noteWidth > NoteHeight * 3)
                                        text = $" {IndexToNoteName(existNote.Name)}  vel:{existNote.Velocity}";
                                    else if (noteWidth > NoteHeight * 2)
                                        text = $" {IndexToNoteName(existNote.Name)} {existNote.Velocity}";
                                    else
                                        text = $" {IndexToNoteName(existNote.Name)}";

                                    context.DrawText(new FormattedText(text, culture, flowDirection,
                                            typeface, noteHeight60Percent, textColor),
                                        new Point(start, yPosition));
                                }

                                // Hover效果
                                if (isHovered)
                                {
                                    if (Math.Abs(_currentMousePosition.X - end) < 5)
                                    {
                                        rightSideCursor = true;
                                        context.DrawLine(RedPen,
                                            new Point(end - 1, yPosition),
                                            new Point(end - 1, yPosition + noteHeight90Percent));
                                    }
                                    else
                                    {
                                        context.DrawRectangle(null, new Pen(textColorWhite),
                                            new Rect(start, yPosition, noteWidth, noteHeight90Percent));
                                    }
                                }
                            }
                        }
                    }

                // 绘制中的音符
                if ((_isDrawing || _isDragging || _isEditing) && SelectedNotes.Count == 0)
                {
                    var start = _currentNoteStartTime * _widthOfBeat;
                    var end = _currentNoteEndTime * _widthOfBeat;
                    var color = Colors.DimGray;

                    if (!_isEditing)
                    {
                        var yPosition = Height - (_currentHoverNote + 1) * NoteHeight;
                        context.FillRectangle(new SolidColorBrush(color),
                            new Rect(start, yPosition, end - start, NoteHeight * .9));
                    }
                    else
                    {
                        var yPosition = Height - (_editingNote.Name + 1) * NoteHeight;
                        context.FillRectangle(new SolidColorBrush(color),
                            new Rect(start, yPosition, end - start, NoteHeight * .9));
                        // context.DrawLine(new Pen(Brushes.Orange, 1),
                        //     new Point(
                        //         _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                        //         _widthOfBeat * Magnet, 0),
                        //     new Point(
                        //         _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                        //         _widthOfBeat * Magnet, Width));
                    }
                }

                Cursor = rightSideCursor
                    ? new Cursor(StandardCursorType.RightSide)
                    : new Cursor(StandardCursorType.Arrow);

                // 选择框
                if (_selectFrame != null)
                {
                    context.DrawRectangle(null, new Pen(Brushes.White), (Rect)_selectFrame);
                }

                // 拖动的选中音符
                if ((_isDrawing || _isDragging || _isEditing) && SelectedNotes.Count != 0)
                {
                    var color = Colors.DimGray;

                    if (!_isEditing)
                    {
                        var dragRelativePos =
                            ((_currentMousePosition.X - _dragPos.X) -
                             (_currentMousePosition.X - _dragPos.X) % _widthOfBeat) / _widthOfBeat;

                        foreach (Note selectedNote in SelectedNotes)
                        {
                            var dragNoteName =
                                (int)(-(_currentMousePosition.Y - _dragPos.Y) / NoteHeight + selectedNote.Name);
                            dragNoteName = Math.Clamp(dragNoteName, 0, NoteRangeMax * (Temperament + 1) + 2);

                            var draggedNote = new Note
                            {
                                StartTime = selectedNote.StartTime + dragRelativePos,
                                EndTime = dragRelativePos + selectedNote.EndTime,
                                Name = dragNoteName,
                                Velocity = selectedNote.Velocity,
                            };

                            var yPosition = Height - (dragNoteName + 1) * NoteHeight;
                            context.FillRectangle(new SolidColorBrush(color),
                                new Rect(draggedNote.StartTime * _widthOfBeat, yPosition,
                                    (draggedNote.EndTime - draggedNote.StartTime) * _widthOfBeat, NoteHeight * .9));
                        }
                    }
                    else
                    {
                        // context.DrawLine(OrangePen,
                        //     new Point(
                        //         _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) + _widthOfBeat,
                        //         0),
                        //     new Point(
                        //         _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) + _widthOfBeat,
                        //         Width));

                        var editingNotes = new List<Note>();
                        foreach (var note in SelectedNotes)
                        {
                            var noteEnd = _currentNoteEndTime - _currentNoteStartTime + note.EndTime -
                                          (_editingNote.EndTime - _editingNote.StartTime);
                            editingNotes.Add(note with { EndTime = noteEnd });
                        }


                        foreach (Note selectedNote in editingNotes)
                        {
                            var yPosition = Height - (selectedNote.Name + 1) * NoteHeight;
                            context.FillRectangle(new SolidColorBrush(color),
                                new Rect(selectedNote.StartTime * _widthOfBeat, yPosition,
                                    (selectedNote.EndTime - selectedNote.StartTime) * _widthOfBeat,
                                    NoteHeight * .9));
                        }
                    }
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        {
            var noteName = -1;

            for (int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
                // for (int i = NoteRangeMin; i <= NoteRangeMax; i++)
            {
                // for (int note = 0; note < Temperament; note++)
                {
                    // noteName = i * Temperament + note;
                    noteName = i;
                    var relativePos = e.GetPosition(this) -
                                      new Point(0, Height - (noteName + 1) * NoteHeight);
                    // Console.WriteLine(relativePos);

                    if (relativePos.Y > 0 && relativePos.Y < NoteHeight)
                    {
                        _currentHoverNote = noteName;
                        InvalidateVisual();
                        // Console.WriteLine(_currentHoverNote);
                    }
                }
            }
        }
        {
            if (!IsPianoBar)
            {
                //TODO 应该实现按住Alt可以自由拖动音符
                if (e.KeyModifiers == KeyModifiers.Alt && _isDragging)
                {
                    Magnet = 1 / 32f;
                }
                else
                {
                    Magnet = MainWindow.PianoRollWindow.MagnetSettingsWindow.SelectedGrid.Value;
                }

                _currentMousePosition = e.GetPosition(this);
                if (_isDrawing)
                {
                    _currentNoteEndTime =
                        (e.GetPosition(this).X - (e.GetPosition(this).X) % (_widthOfBeat * Magnet) +
                         _widthOfBeat * Magnet) / _widthOfBeat;
                }

                if (_isDragging && SelectedNotes.Count == 0)
                {
                    _currentNoteStartTime = _dragStartTime + ((e.GetPosition(this).X - _dragPos.X) -
                                                              (e.GetPosition(this).X - _dragPos.X) %
                                                              (_widthOfBeat * Magnet)) / _widthOfBeat;
                    _currentNoteEndTime = _dragEndTime + ((e.GetPosition(this).X - _dragPos.X) -
                                                          (e.GetPosition(this).X - _dragPos.X) %
                                                          (_widthOfBeat * Magnet)) / _widthOfBeat;
                }

                if (_isEditing)
                {
                    _currentNoteEndTime =
                        (e.GetPosition(this).X - (e.GetPosition(this).X) % (_widthOfBeat * Magnet) +
                         _widthOfBeat * Magnet) / _widthOfBeat;
                }

                foreach (Note note in Notes)
                {
                    if (e.GetPosition(this).X > note.StartTime * _widthOfBeat &&
                        e.GetPosition(this).X < note.EndTime * _widthOfBeat)
                    {
                        _currentHoverDrawnNote.StartTime = note.StartTime;
                        _currentHoverDrawnNote.EndTime = note.EndTime;
                        _currentHoverDrawnNote.Name = _currentHoverNote;
                    }
                }

                //框选逻辑
                if (_selectFrame != null)
                {
                    Rect _tempSelectFrame = (Rect)_selectFrame;

                    double selectStartX = _tempSelectFrame.X;
                    double selectStartY = _tempSelectFrame.Y;
                    double selectWidth = double.Abs(selectStartX - e.GetPosition(this).X);
                    double selectHeight = double.Abs(selectStartY - e.GetPosition(this).Y);

                    if (e.GetPosition(this).X < _pressedMousePosition.X)
                    {
                        selectStartX = e.GetPosition(this).X;
                        selectWidth = _tempSelectFrame.Right - selectStartX;
                    }

                    if (e.GetPosition(this).Y < _pressedMousePosition.Y)
                    {
                        selectStartY = e.GetPosition(this).Y;
                        selectHeight = _tempSelectFrame.Bottom - selectStartY;
                    }

                    _selectFrame = new Rect(selectStartX, selectStartY, selectWidth, selectHeight);

                    // 优化后的选择逻辑 - 修复并行版本
                    Rect selectFrameRect = (Rect)_selectFrame;

                    // 预计算选择框边界和UI相关值（在UI线程中）
                    double frameLeft = selectFrameRect.X;
                    double frameRight = selectFrameRect.X + selectFrameRect.Width;
                    double frameTop = selectFrameRect.Y;
                    double frameBottom = selectFrameRect.Y + selectFrameRect.Height;

                    // 预计算所有UI相关值
                    double currentHeight = Height;
                    double currentNoteHeight = NoteHeight;
                    double currentWidthOfBeat = _widthOfBeat;

                    // 使用并行处理计算选中的音符
                    var newSelectedNotes = new System.Collections.Concurrent.ConcurrentBag<Note>();

                    // 并行处理音符选择检测
                    Parallel.ForEach(Notes, existNote =>
                    {
                        // 使用预计算的UI值，避免在并行线程中访问UI属性
                        double noteStartX = existNote.StartTime * currentWidthOfBeat;
                        double noteEndX = existNote.EndTime * currentWidthOfBeat;
                        double noteY = currentHeight - (existNote.Name + 1) * currentNoteHeight;
                        double noteBottom = noteY + currentNoteHeight;

                        // 简化的碰撞检测
                        bool xOverlap = frameLeft < noteEndX && frameRight > noteStartX;
                        bool yOverlap = frameTop < noteBottom && frameBottom > noteY;

                        if (xOverlap && yOverlap)
                        {
                            newSelectedNotes.Add(existNote);
                        }
                    });

                    // 将并发包转换为哈希集以便快速查找
                    var newSelectedSet = new HashSet<Note>(newSelectedNotes);

                    // 批量更新选择状态
                    bool selectionChanged = false;

                    // 移除不在新选择集中的音符
                    for (int i = SelectedNotes.Count - 1; i >= 0; i--)
                    {
                        if (!newSelectedSet.Contains(SelectedNotes[i]))
                        {
                            SelectedNotes.RemoveAt(i);
                            selectionChanged = true;
                        }
                    }

                    // 添加新选择的音符
                    foreach (var note in newSelectedSet)
                    {
                        if (!SelectedNotes.Contains(note))
                        {
                            SelectedNotes.Add(note);
                            selectionChanged = true;
                        }
                    }

                    if (selectionChanged)
                    {
                        InvalidateVisual();
                    }
                }
            }
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _currentHoverNote = -1;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressedMousePosition = e.GetPosition(this);
        if (!IsPianoBar)
        {
            if (e.Properties.IsLeftButtonPressed)
            {
                _selectFrame = new Rect(e.GetPosition(this).X, e.GetPosition(this).Y, 0, 0);
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    _currentNoteStartTime =
                        (e.GetPosition(this).X - (e.GetPosition(this).X) % (_widthOfBeat * Magnet)) / _widthOfBeat;
                    _isDrawing = true;
                    _selectFrame = null;
                    SelectedNotes.Clear();
                }
            }

            Note? removedNote = null;
            foreach (Note existNote in Notes)
            {
                if (existNote.Name.Equals(_currentHoverNote) &&
                    _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                    _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                {
                    if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                    {
                        if (e.Properties.IsRightButtonPressed)
                        {
                            removedNote = existNote;
                            SelectedNotes.Clear();
                            e.Handled = true;
                            break;
                        }

                        if (!e.Properties.IsLeftButtonPressed) continue;
                        _isEditing = true;
                        _selectFrame = null;
                        _currentNoteStartTime = existNote.StartTime;
                        removedNote = existNote;
                        _editingNote = existNote;
                        _dragNoteVelocity = existNote.Velocity;
                        e.Handled = true;
                    }
                    else
                    {
                        //删除单个音符
                        if (e.Properties.IsRightButtonPressed)
                        {
                            removedNote = existNote;
                            SelectedNotes.Clear();
                            e.Handled = true;
                            break;
                        }

                        //拖动单个音符
                        if (!e.Properties.IsLeftButtonPressed) continue;
                        if (!SelectedNotes.Contains(existNote))
                        {
                            SelectedNotes.Clear();
                        }

                        _isDragging = true;
                        _selectFrame = null;
                        _dragPos = e.GetPosition(this);
                        _dragStartTime = existNote.StartTime;
                        _dragEndTime = existNote.EndTime;
                        _dragNoteVelocity = existNote.Velocity;
                        _currentNoteStartTime = existNote.StartTime;
                        _currentNoteEndTime = existNote.EndTime;
                        _dragNoteVelocity = existNote.Velocity;

                        //这傻逼钢琴窗就是他妈的一坨沟史
                        removedNote = existNote;
                        e.Handled = true;
                        break;
                    }
                }
            }

            if (removedNote != null)
            {
                Notes.Remove((Note)removedNote);
            }
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!IsPianoBar)
        {
            if (e.GetPosition(this) == _pressedMousePosition)
            {
                SelectedNotes.Clear();
            }

            if (_currentNoteStartTime < _currentNoteEndTime && (_isDrawing || _isDragging || _isEditing))
            {
                // 并行查找draggingNote
                Note draggingNote = new Note();
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
                var found = false;

                Parallel.ForEach(Notes, parallelOptions, (note, loopState) =>
                {
                    if (Notes.Contains(note) && _currentHoverNote.Equals(note.Name))
                    {
                        if (_currentNoteStartTime > note.StartTime && _currentNoteStartTime < note.EndTime ||
                            _currentNoteEndTime > note.StartTime && _currentNoteEndTime < note.EndTime ||
                            note.StartTime > _currentNoteStartTime && note.StartTime < _currentNoteEndTime ||
                            note.EndTime > _currentNoteStartTime && note.EndTime < _currentNoteEndTime)
                        {
                            draggingNote = note;
                            found = true;
                            loopState.Stop(); // 找到第一个匹配项就停止
                        }
                    }
                });

                // 并行构建draggingNotes列表
                List<Note> draggingNotes = new List<Note>();
                if (SelectedNotes.Count > 0)
                {
                    var concurrentDraggingNotes = new ConcurrentBag<Note>();

                    Parallel.ForEach(SelectedNotes, parallelOptions, selectedNote =>
                    {
                        if (Notes.Contains(selectedNote))
                        {
                            concurrentDraggingNotes.Add(selectedNote);
                        }
                    });

                    draggingNotes = concurrentDraggingNotes.ToList();
                }

                // 批量移除操作（保持原有逻辑不变）
                foreach (Note draggingNote1 in draggingNotes)
                {
                    Notes.Remove(draggingNote1);
                }

                if (found)
                {
                    Notes.Remove(draggingNote);
                }

                draggingNotes.Clear();

                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    if (_isDrawing) _dragNoteVelocity = 127;
                    if (!_isEditing)
                    {
                        if (SelectedNotes.Count == 0)
                        {
                            Notes.Add(new Note
                            {
                                StartTime = _currentNoteStartTime >= 0 ? _currentNoteStartTime : 0,
                                EndTime = _currentNoteEndTime,
                                Name = _currentHoverNote,
                                // Color = NoteColor3
                                Velocity = _dragNoteVelocity
                            });
                        }
                        else
                        {
                            // 并行处理音符拖拽计算
                            var concurrentdraggedNotes = new ConcurrentBag<Note>();

                            var mouseX = e.GetPosition(this).X;
                            var mouseY = e.GetPosition(this).Y;
                            var dragPosX = _dragPos.X;
                            var dragPosY = _dragPos.Y;

                            var cachedNoteHeight = NoteHeight; 
                            var cachedWidthOfBeat = _widthOfBeat; 

                            var maxNoteName = NoteRangeMax * (Temperament + 1) + 2;

                            Parallel.ForEach(SelectedNotes, parallelOptions, selectedNote =>
                            {
                                var dragRelativePos =
                                    ((mouseX - dragPosX) -
                                     (mouseX - dragPosX) %
                                     cachedWidthOfBeat) / cachedWidthOfBeat;

                                var noteName = (int)(-(mouseY - dragPosY) / cachedNoteHeight +
                                                     selectedNote.Name);
                         
                                if (noteName > maxNoteName)
                                {
                                    noteName = maxNoteName;
                                }

                                if (noteName < 0)
                                {
                                    noteName = 0;
                                } 
    
                                concurrentdraggedNotes.Add(new Note
                                {
                                    StartTime = selectedNote.StartTime + dragRelativePos >= 0
                                        ? selectedNote.StartTime + dragRelativePos
                                        : 0,
                                    EndTime = dragRelativePos + selectedNote.EndTime,
                                    Name = noteName,
                                    Velocity = selectedNote.Velocity
                                });
                            }); 
                            
                            SelectedNotes.Clear();
                            var draggedSelectedNotes = concurrentdraggedNotes.ToList();

                            foreach (Note draggedSelectedNote in draggedSelectedNotes)
                            {
                                Notes.Add(draggedSelectedNote);
                                SelectedNotes.Add(draggedSelectedNote);
                            }

                            draggedSelectedNotes.Clear();
                        }
                    }
                    else
                    {
                        if (SelectedNotes.Count == 0)
                        {
                            Notes.Add(new Note
                            {
                                StartTime = _currentNoteStartTime >= 0 ? _currentNoteStartTime : 0,
                                EndTime = _currentNoteEndTime,
                                Name = _editingNote.Name,
                                // Color = NoteColor3
                                Velocity = _dragNoteVelocity
                            });
                        }
                        else
                        {
                            // 并行处理编辑模式下的音符
                            var concurrentEditingNotes = new ConcurrentBag<Note>();

                            Parallel.ForEach(SelectedNotes, parallelOptions, note =>
                            {
                                var noteEnd = _currentNoteEndTime - _currentNoteStartTime + note.EndTime -
                                              (_editingNote.EndTime - _editingNote.StartTime);
                                concurrentEditingNotes.Add(note with { EndTime = noteEnd });
                            });

                            SelectedNotes.Clear();
                            var editingNotes = concurrentEditingNotes.ToList();

                            foreach (Note draggedSelectedNote in editingNotes)
                            {
                                Notes.Add(draggedSelectedNote);
                                SelectedNotes.Add(draggedSelectedNote);
                            }

                            editingNotes.Clear();
                        }
                    }
                }
            }

            _isDrawing = false;
            _isDragging = false;
            _isEditing = false;
            _selectFrame = null;

            if (SelectedNotes.Count > 0)
            {
                ShowOptions();
            }
            else
            {
                HideOptions();
            }

            SaveNotes();
        }

        InvalidateVisual();
        e.Handled = true;
    }

    private void ShowOptions()
    {
        var positionLeft = SelectedNotes[0].StartTime * _widthOfBeat;
        var positionTop = Height - SelectedNotes[0].Name * NoteHeight;
        foreach (Note selectedNote in SelectedNotes)
        {
            positionLeft = double.Min(positionLeft, selectedNote.StartTime * _widthOfBeat);
            positionTop = double.Min(positionTop, Height - selectedNote.Name * NoteHeight);
        }

        Options.Margin = new Thickness(positionLeft, positionTop, 0, 0);
        Options.IsVisible = true;
        MoveUpButton.IsEnabled = true;
        MoveDownButton.IsEnabled = true;
        foreach (var note in SelectedNotes)
        {
            if (note.Name + 12 > NoteRangeMax * (Temperament + 1) + 2)
            {
                MoveUpButton.IsEnabled = false;
            }

            if (note.Name - 12 < 0)
            {
                MoveDownButton.IsEnabled = false;
            }
        }
    }

    private void HideOptions()
    {
        Options.IsVisible = false;
    }


    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width >= Width)
        {
            MainWindow.PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                Width - MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width,
                MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.Y);
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                double currentPosition = e.GetPosition(this).Y / NoteHeight;
                NoteHeight = double.Clamp(NoteHeight + e.Delta.Y * NoteHeight / 20d * ScalingSensitivity.Y, 10, 30);
                Height = NoteHeight * (NoteRangeMax - NoteRangeMin + 1) * Temperament;
                MainWindow.PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.X,
                    currentPosition * NoteHeight -
                    e.GetPosition(MainWindow.PianoRollWindow.PianoRollRightScroll).Y);


                Console.WriteLine($"CurrentPositionY: {currentPosition}");

                Console.WriteLine(MainWindow.PianoRollWindow.PianoRollRightScroll.Offset);
                Console.WriteLine(e.GetPosition(MainWindow.PianoRollWindow.PianoRollRightScroll));
            }
            else
            {
                double trackEnd = 0;
                foreach (var note in Notes)
                {
                    trackEnd = double.Max(trackEnd, note.EndTime);
                }

                trackEnd += LengthIncreasement;
                double currentPosition = e.GetPosition(this).X / _widthOfBeat;

                _widthOfBeat = double.Clamp(_widthOfBeat + e.Delta.Y * _widthOfBeat / 50d * ScalingSensitivity.X,
                    double.Max(1, MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width / trackEnd), 500);

                MainWindow.PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    currentPosition * _widthOfBeat -
                    e.GetPosition(MainWindow.PianoRollWindow.PianoRollRightScroll).X,
                    MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.Y);

                // Console.WriteLine(MainWindow.PianoRollWindow.PianoRollRightScroll.Offset);
                // Console.WriteLine(e.GetPosition(MainWindow.PianoRollWindow.PianoRollRightScroll));
            }

            // _ = ShowOptions();
            // ShowOptions();
            SaveNotes();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private string IndexToNoteName(int index)
    {
        // 边界校验：计算有效索引范围
        int minIndex = NoteRangeMin * Temperament;
        int maxIndex = (NoteRangeMax + 1) * Temperament + (Temperament - 1);
        if (index < minIndex || index > maxIndex)
        {
            return "";
        }

        // 直接计算音高组和音名索引
        int octave = index / Temperament;
        int noteIndex = index % Temperament;

        // 从数组获取前缀并拼接结果
        return $"{NotePrefixes[noteIndex]}{octave}";
    }

    private IBrush NoteNameToBrush(string name)
    {
        if (name.Contains('#'))
        {
            return _noteColor2;
        }

        if (name[0] == 'C')
        {
            return new SolidColorBrush(NoteColor3);
        }

        return _noteColor1;
    }

    public void SaveNotes()
    {
        if (!IsPianoBar)
        {
            double trackEnd = 0;
            foreach (var note in Notes)
            {
                trackEnd = double.Max(trackEnd, note.EndTime);
            }

            trackEnd += LengthIncreasement;
            Width = trackEnd * _widthOfBeat;
            // Console.WriteLine($"Scroll:{MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.X+MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width} Width:{Width}");
            if (MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.X +
                MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width > Width)
            {
                MainWindow.PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    Width - MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width,
                    MainWindow.PianoRollWindow.PianoRollRightScroll.Offset.Y);
            }

            _widthOfBeat = double.Max(_widthOfBeat,
                MainWindow.PianoRollWindow.PianoRollRightScroll.Bounds.Width / trackEnd);
        }

        MainWindow.PianoRollWindow.PatternPreview.InvalidateVisual();
        MainWindow.PianoRollWindow.NoteVelocity.InvalidateVisual();
        MainWindow.PianoRollWindow.PianoScroller.InvalidateVisual();
    }

    private void SelectedNotesMoveUp(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < SelectedNotes.Count; i++)
        {
            if (Notes.Contains(SelectedNotes[i]))
            {
                Notes.Remove(SelectedNotes[i]);
                SelectedNotes[i] = new Note
                {
                    // Color = SelectedNotes[i].Color,
                    StartTime = SelectedNotes[i].StartTime,
                    EndTime = SelectedNotes[i].EndTime,
                    Name = SelectedNotes[i].Name + 12,
                    Velocity = SelectedNotes[i].Velocity
                };
                // Console.WriteLine(SelectedNotes[i].Name);
                Notes.Add(SelectedNotes[i]);
            }
        }

        // _ = ShowOptions();
        ShowOptions();
        InvalidateVisual();
    }

    private void SelectedNotesMoveDown(object? sender, RoutedEventArgs e)
    {
        for (int i = 0; i < SelectedNotes.Count; i++)
        {
            if (Notes.Contains(SelectedNotes[i]))
            {
                Notes.Remove(SelectedNotes[i]);
                SelectedNotes[i] = new Note
                {
                    // Color = SelectedNotes[i].Color,
                    StartTime = SelectedNotes[i].StartTime,
                    EndTime = SelectedNotes[i].EndTime,
                    Name = SelectedNotes[i].Name - 12,
                    Velocity = SelectedNotes[i].Velocity
                };
                // Console.WriteLine(SelectedNotes[i].Name);
                Notes.Add(SelectedNotes[i]);
            }
        }

        // _ = ShowOptions();
        ShowOptions();
        InvalidateVisual();
    }

    public void ImportMidi(string file)
    {
        {
            var midi = new MidiService();
            midi.ImportMidi(file);
            for (int i = 1; i < midi.Data.Tracks; i++)
            {
                CurrentChannel = i;
                Notes.Clear();
                for (var index = 0; index < midi.Data[i].Count; index++)
                {
                    var note = midi.Data[i][index];
                    if (note.GetType() == typeof(NoteOnEvent))
                    {
                        try
                        {
                            Notes.Add(new Note()
                            {
                                Name = ((NoteOnEvent)note).NoteNumber,
                                // Color = NoteColor3,
                                StartTime = ((NoteOnEvent)note).AbsoluteTime /
                                    (double)midi.Data.DeltaTicksPerQuarterNote * 4,
                                EndTime = (((NoteOnEvent)note).AbsoluteTime + ((NoteOnEvent)note).NoteLength) /
                                    (double)midi.Data.DeltaTicksPerQuarterNote * 4,
                                Velocity = ((NoteOnEvent)note).Velocity
                            });
                        }
                        catch (Exception @exception)
                        {
                            Console.Error.WriteLine(@exception.Message);
                        }
                    }
                }
            }

            CurrentChannel = 1;

            Console.WriteLine($"IMPORT Notes: {Notes.Count}");
            // foreach (var note in Notes)
            // {
            //     Console.WriteLine($"Start: {note.StartTime}; End: {note.EndTime}; Name: {note.Name}");
            // }
            SaveNotes();
            MainWindow.PianoRollWindow.PatternPreview.ScrollToNoteFirst();
            InvalidateVisual();
        }
        // Console.WriteLine($"Notes: {Notes.Count}");
    }

    public void ExportMidi()
    {
        {
            var file = new Window().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Export Midi file",
                FileTypeChoices =
                [
                    new FilePickerFileType("MIDI File")
                    {
                        Patterns = ["*.mid"]
                    }
                ]
            }).GetAwaiter().GetResult();
            var midi = new MidiService();
            midi.Data.AddTrack();
            foreach (var note in Notes)
            {
                midi.Data[1].Add(new NoteEvent((long)(note.StartTime * midi.Data.DeltaTicksPerQuarterNote),
                    1, MidiCommandCode.NoteOn,
                    note.Name,
                    note.Velocity
                ));
                midi.Data[1].Add(new NoteEvent((int)(note.EndTime * midi.Data.DeltaTicksPerQuarterNote),
                    1, MidiCommandCode.NoteOff,
                    note.Name,
                    note.Velocity));
            }

            for (int i = 1; i < midi.Data.Tracks; i++)
            {
                var endTime = 0;
                if (midi.Data[i].Count != 0) endTime = (int)midi.Data[i][^1].AbsoluteTime;
                midi.Data[i].Add(new MetaEvent(MetaEventType.EndTrack, 0, endTime));
            }

            if (file != null) midi.ExportMidi(file.Path.LocalPath);
        }
    }
}