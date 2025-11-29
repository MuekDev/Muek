using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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


    public const int NoteRangeMax = 9;
    public const int NoteRangeMin = 0;

    public const int Temperament = 12;

    private static readonly string[] NotePrefixes = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];


    // private double _renderSize = 2000;


    private readonly IBrush _noteColor1 = Brushes.White;
    private readonly IBrush _noteColor2 = Brushes.Black;
    private Color NoteColor3 => Pattern?.Color ?? DataStateService.MuekColor;

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

    private int _editingNote = -1;

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

    private PatternViewModel? _pattern = null;

    public PatternViewModel? Pattern
    {
        get => _pattern;
        set
        {
            _pattern = value;
            ViewHelper.GetMainWindow().PianoRollWindow.ChannelSelectionDisable.IsVisible = _pattern is null;
            if(_pattern is null)
                ViewHelper.GetMainWindow().PianoRollWindow.Channel.UnselectAll();
            OnPropertyChanged(nameof(Pattern));
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (Pattern != null)
        {
            ViewHelper.GetMainWindow().PianoRollWindow.WindowCover.IsVisible = false;
        }
        else
        {
            ViewHelper.GetMainWindow().PianoRollWindow.WindowCover.IsVisible = true;
        }
        var patterns = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelection.ViewModel.Patterns;
        foreach (var pattern in patterns)
        {
            if (pattern == Pattern) pattern.Background = new SolidColorBrush(pattern.Color);
            else pattern.Background = new SolidColorBrush(Colors.Black, 0);
        }
    }

    private int CurrentChannel
    {
        get => (int)(ViewHelper.GetMainWindow().PianoRollWindow.Channel.SelectedItem ?? 1);
        set
        {
            ViewHelper.GetMainWindow().PianoRollWindow.Channel.SelectedItem = int.Clamp(value, 1, 16);
            InvalidateVisual();
        }
    }
    

    public List<Note> Notes
    {
        get => Pattern == null ? [] : Pattern.Notes[CurrentChannel - 1];
        set { if (Pattern != null) Pattern.Notes[CurrentChannel - 1] = value; }
    }

    public Point ScalingSensitivity = new Point(5, 2);

    public int channel = 1;


    //框选音符
    private Rect? _selectFrame;

    public List<Note> SelectedNotes = new();

    // private bool _isShowingOptions = false;
    public double Magnet = 1.0;
    public readonly double LengthIncreasement = 128d;


    private Pen _whitePen = new Pen(new SolidColorBrush(Colors.White, .1));
    private IBrush _whiteBrush = new SolidColorBrush(Colors.White, .05);
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
            Width = LengthIncreasement*_widthOfBeat;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // context.FillRectangle(Brushes.DimGray, new Rect(0, 0, Width, Height));

        //左侧钢琴Bar
        if (IsPianoBar)
        {
            var noteColor = _noteColor1;
            var noteName = -1;

            //左侧钢琴
            {
                //十二平均律
                for(int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
                // for (int i = NoteRangeMin; i <= NoteRangeMax; i++)
                {
                    // for (int note = 0; note < Temperament; note++)
                    {
                        // noteName = i * Temperament + note;
                        noteName = i;
                        noteColor = NoteNameToBrush(IndexToNoteName(noteName));

                        // Console.WriteLine(noteName);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (noteName + 1) * NoteHeight, Width * .9, NoteHeight),
                            5);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (noteName + 1) * NoteHeight, Width * .8, NoteHeight));
                        // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                        context.DrawText(new FormattedText(IndexToNoteName(noteName),
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                NoteHeight * .6, (noteColor == _noteColor2 ? _noteColor1 : _noteColor2)),
                            new Point(0, Height - (noteName + 1) * NoteHeight)
                        );

                        if (noteName.Equals(_currentHoverNote))
                        {
                            context.FillRectangle(_noteHoverColor,
                                new Rect(0, Height - (noteName + 1) * NoteHeight, Width * .9,
                                    NoteHeight));
                        }
                    }
                }
            }
        }

        //绘制区域
        else
        {
            var noteColor = _noteColor1;
            int noteName = -1;
            // for (int i = NoteRangeMin; i <= NoteRangeMax; i++)
            for(int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
            {
                // for (int note = 0; note < Temperament; note++)
                {
                    // noteName = i * Temperament + note;
                    noteName = i;
                    //绘制编辑区
                    // Console.WriteLine(noteName);
                    context.DrawRectangle(Brush.Parse("#40232323"),
                        _whitePen,
                        new Rect(ClampValue, Height - (noteName + 1) * NoteHeight, Width,
                            NoteHeight));
                    // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                    if (!IndexToNoteName(noteName).Contains('#'))
                    {
                        context.DrawRectangle(_whiteBrush, null,
                            new Rect(ClampValue, Height - (noteName + 1) * NoteHeight, Width,
                                NoteHeight));
                    }

                    //绘制Hover区
                    if (noteName.Equals(_currentHoverNote))
                    {
                        context.FillRectangle(new SolidColorBrush(NoteColor3,.1),
                            new Rect(ClampValue, Height - (noteName + 1) * NoteHeight, Width,
                                NoteHeight));
                        // Console.WriteLine($"HOVERING: {_currentHoverNote}");
                    }
                }
            }

            var whiteGridPen = new Pen(new SolidColorBrush(Colors.White, .2), _widthOfBeat * .005);
            var purpleGridPen = new Pen(new SolidColorBrush(Colors.MediumPurple, .5));
            var blueGridPen = new Pen(new SolidColorBrush(Colors.LightSkyBlue, .5));
            if (_widthOfBeat > 20)
            {
                for (double i = 0; i < Width / _widthOfBeat; i += double.Clamp(Magnet,1/8d,2d))
                {
                    if (i % 4 == 0) continue;
                    // var gridLinePen = new Pen(new SolidColorBrush(Colors.White, .1), _widthOfBeat * .005);
                    if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                    {
                        context.DrawLine(whiteGridPen,
                            new Point(i * _widthOfBeat, 0),
                            new Point(i * _widthOfBeat, Height));
                    }
                }

                if (Magnet <= 1 / 6.0)
                {
                    purpleGridPen.Thickness = 1;
                    // gridLinePen = new Pen(new SolidColorBrush(Colors.MediumPurple,.5));
                    for (int i = 1; i < Width / _widthOfBeat; i += 2)
                    {
                        if (_widthOfBeat < 50) purpleGridPen.Thickness = .5;
                        if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                        {
                            context.DrawLine(purpleGridPen,
                                new Point(i * _widthOfBeat, 0),
                                new Point(i * _widthOfBeat, Height));
                        }
                    }
                }

                if (Magnet <= 1 / 3.0)
                {
                    blueGridPen.Thickness = 1;
                    // gridLinePen = new Pen(new SolidColorBrush(Colors.LightSkyBlue,.5));
                    for (int i =2; i < Width / _widthOfBeat; i += 4)
                    {
                        if (_widthOfBeat < 50) blueGridPen.Thickness = .5;
                        if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                        {
                            context.DrawLine(blueGridPen,
                                new Point(i * _widthOfBeat, 0),
                                new Point(i * _widthOfBeat, Height));
                        }
                    }
                }
            }

            
            for (int i = 0; i < Width / _widthOfBeat; i++)
            {
                var gridLinePen = new Pen(new SolidColorBrush(Colors.White, .5),1,new DashStyle([NoteHeight,NoteHeight*.5],0));
                if(_widthOfBeat < 20)
                {
                    gridLinePen.Thickness = .5;
                    gridLinePen.Brush = new SolidColorBrush(Colors.White,.2);
                }
                IBrush textColor = new SolidColorBrush(Colors.White, .2);
                if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                {
                    if(i%16==0)
                    {
                        gridLinePen.Brush = new SolidColorBrush(NoteColor3);
                        if(_widthOfBeat < 20)  gridLinePen.Thickness = .5;
                        textColor = Brushes.White;
                    }
                    if (i % 4 == 0)
                    {
                        //小节数
                        if(_widthOfBeat > 20)
                        {
                            // context.DrawText(new FormattedText(i%16==0 ? $"{i / 16}" : $"{i / 16} : {(1 + (i / 4) % 4)}",
                            //         CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, i%16==0?15:12,
                            //         textColor),
                            //     new Point(6 + i * _widthOfBeat + 1, ScrollOffset));
                            context.DrawText(new FormattedText(i%16==0 ? $"{i / 16 + 1}" : $"{i / 16 + 1} : {(1 + (i / 4) % 4)}",
                                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, i%16==0?15:12,
                                    textColor),
                                new Point(6 + i * _widthOfBeat + 1, ScrollOffset));
                        }
                        else
                        {
                            if (i % 16 == 0)
                            {
                                context.DrawText(new FormattedText($"{i / 16 + 1}",
                                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 15,
                                        textColor),
                                    new Point(6 + i * _widthOfBeat + 1, ScrollOffset));
                            }
                        }
                        context.DrawLine(gridLinePen,
                            new Point(i * _widthOfBeat, 0),
                            new Point(i * _widthOfBeat, Height));
                    }
                }
            }
            
            
            //绘制位置
            if (_currentHoverNote != -1)
            {
                var pen = new Pen(new SolidColorBrush(NoteColor3),.5);
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
                             _widthOfBeat * Magnet),
                            Height)
                    );
                }
            }

            var rightSideCursor = false;
            var whitePen = new Pen(Brushes.White);
            var redPen = new Pen(Brushes.Red, 2);
            var orangePen = new Pen(Brushes.Orange);
            var blackPen = new Pen(Brushes.Black);
            
            var left = ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X;
            var right = ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X +
                        ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width;
            
            // for (int i = NoteRangeMin; i <= NoteRangeMax; i++)
            for(int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
            {
                // for (int note = 0; note < Temperament; note++)
                {
                    // noteName = i * Temperament + note;
                    noteName = i;
                    //渲染音符
                    foreach (Note existNote in Notes)
                        if (existNote.EndTime * _widthOfBeat > left
                            && existNote.StartTime * _widthOfBeat < right)
                    {
                        var start = existNote.StartTime * _widthOfBeat;
                        var end = existNote.EndTime * _widthOfBeat;
                        // var color = existNote.Color;
                        var velocity = existNote.Velocity;
                        if (noteName.Equals(existNote.Name) && (!_isDragging || !SelectedNotes.Contains(existNote)))
                        {
                            if(SelectedNotes.Contains(existNote))
                                context.DrawRectangle(Brushes.Black, whitePen,
                                    new Rect(start, Height - (noteName + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                            else
                            {
                                context.DrawRectangle(new SolidColorBrush(NoteColor3, velocity / 127.0),
                                    blackPen,
                                    new Rect(start, Height - (noteName + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                            }

                            
                            if ((existNote.EndTime - existNote.StartTime) * _widthOfBeat > NoteHeight * .8)
                                if((existNote.EndTime - existNote.StartTime)*_widthOfBeat > NoteHeight * 3)
                                    if (SelectedNotes.Contains(existNote))
                                        context.DrawText(new FormattedText(
                                                $" {IndexToNoteName(existNote.Name)}  vel:{existNote.Velocity}",
                                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                                NoteHeight * .6,
                                                Brushes.White),
                                            new Point(start, Height - (noteName + 1) * NoteHeight));
                                    else
                                        context.DrawText(new FormattedText(
                                                $" {IndexToNoteName(existNote.Name)}  vel:{existNote.Velocity}",
                                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                                NoteHeight * .6,
                                                Brushes.Black),
                                            new Point(start, Height - (noteName + 1) * NoteHeight));
                                else if((existNote.EndTime - existNote.StartTime)*_widthOfBeat > NoteHeight * 2)
                                    if (SelectedNotes.Contains(existNote))
                                        context.DrawText(new FormattedText(
                                                $" {IndexToNoteName(existNote.Name)} {existNote.Velocity}",
                                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                                NoteHeight * .6,
                                                Brushes.White),
                                            new Point(start, Height - (noteName + 1) * NoteHeight));
                                    else
                                        context.DrawText(new FormattedText(
                                                $" {IndexToNoteName(existNote.Name)} {existNote.Velocity}",
                                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                                NoteHeight * .6,
                                                Brushes.Black),
                                            new Point(start, Height - (noteName + 1) * NoteHeight));
                                else
                                    if (SelectedNotes.Contains(existNote))
                                        context.DrawText(new FormattedText($" {IndexToNoteName(existNote.Name)}",
                                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                                NoteHeight * .6,
                                                Brushes.White),
                                            new Point(start, Height - (noteName + 1) * NoteHeight));
                                    else
                                        context.DrawText(new FormattedText($" {IndexToNoteName(existNote.Name)}",
                                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                                NoteHeight * .6,
                                                Brushes.Black),
                                            new Point(start, Height - (noteName + 1) * NoteHeight));


                            //Hover音符
                            if (existNote.Name.Equals(_currentHoverNote) &&
                                _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                                _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                            {
                                if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                                {
                                    //更改音符长度
                                    // Cursor = new Cursor(StandardCursorType.RightSide);
                                    rightSideCursor = true;
                                    context.DrawLine(redPen,
                                        new Point(end - 1, Height - (noteName + 1) * NoteHeight),
                                        new Point(end - 1,
                                            Height - (noteName + 1) * NoteHeight + NoteHeight * .9));
                                }
                                else
                                {
                                    // Cursor = new Cursor(StandardCursorType.Arrow);
                                    context.DrawRectangle(null, new Pen(Brushes.White),
                                        new Rect(start, Height - (noteName + 1) * NoteHeight,
                                            end - start, NoteHeight * .9));
                                }
                            }
                            // else
                            // {
                            //     Cursor = new Cursor(StandardCursorType.Arrow);
                            // }
                        }
                    }

                    //渲染绘制中的音符
                    if ((_isDrawing || _isDragging || _isEditing) && SelectedNotes.Count == 0)
                    {
                        var start = _currentNoteStartTime * _widthOfBeat;
                        var end = _currentNoteEndTime * _widthOfBeat;
                        var color = Colors.DimGray;
                        if (!_isEditing)
                        {
                            if (noteName.Equals(_currentHoverNote))
                            {
                                context.FillRectangle(new SolidColorBrush(color),
                                    new Rect(start, Height - (noteName + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                            }
                        }
                        else
                        {
                            if (noteName.Equals(_editingNote))
                            {
                                context.FillRectangle(new SolidColorBrush(color),
                                    new Rect(start, Height - (noteName + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                                context.DrawLine(new Pen(Brushes.Orange, 1),
                                    new Point(
                                        _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                                        _widthOfBeat * Magnet, 0),
                                    new Point(
                                        _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                                        _widthOfBeat * Magnet, Width));
                            }
                        }
                    }
                }
            }
            Cursor = rightSideCursor ? new Cursor(StandardCursorType.RightSide) : new Cursor(StandardCursorType.Arrow);

            //渲染框
            if (_selectFrame != null)
            {
                context.DrawRectangle(null, new Pen(Brushes.White),
                    (Rect)_selectFrame);
            }
            
            for(int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
            // for (int i = NoteRangeMin; i <= NoteRangeMax; i++)
            {
                // for (int note = 0; note < Temperament; note++)
                {
                    // noteName = i * Temperament + note;
                    noteName = i;
                    //渲染选中音符
                    // foreach (Note existNote in SelectedNotes)
                    // {
                    //     var start = existNote.StartTime * _widthOfBeat;
                    //     var end = existNote.EndTime * _widthOfBeat;
                    //     if (noteName.Equals(existNote.Name))
                    //     {
                    //         context.DrawRectangle(Brushes.Black, whitePen,
                    //             new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight, end - start,
                    //                 NoteHeight * .9));
                    //
                    //         context.DrawText(new FormattedText(IndexToNoteName(existNote.Name),
                    //                 CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                    //                 NoteHeight * .6,
                    //                 Brushes.White),
                    //             new Point(start, Height - (i * _temperament + note + 1) * NoteHeight));
                    //
                    //         //Hover音符
                    //         if (existNote.Name.Equals(_currentHoverNote) &&
                    //             _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                    //             _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                    //         {
                    //             if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                    //             {
                    //                 context.DrawLine(redPen,
                    //                     new Point(end - 1, Height - (i * _temperament + note + 1) * NoteHeight),
                    //                     new Point(end - 1,
                    //                         Height - (i * _temperament + note + 1) * NoteHeight + NoteHeight * .9));
                    //             }
                    //             else
                    //             {
                    //                 context.DrawRectangle(null, new Pen(Brushes.White, 1),
                    //                     new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight,
                    //                         end - start, NoteHeight * .9));
                    //             }
                    //         }
                    //     }
                    // }

                    //渲染拖动的选中音符
                    if (_isDrawing || _isDragging || _isEditing && SelectedNotes.Count != 0)
                    {
                        // var start = _currentNoteStartTime * _widthOfBeat;
                        // var end = _currentNoteEndTime * _widthOfBeat;
                        var color = Colors.DimGray;
                        if (!_isEditing)
                        {
                            List<Note> dragedSelectedNotes = new List<Note>();
                            foreach (Note selectedNote in SelectedNotes)
                            {
                                var dragRelativePos =
                                    ((_currentMousePosition.X - _dragPos.X) -
                                     (_currentMousePosition.X - _dragPos.X) %
                                     _widthOfBeat) / _widthOfBeat;
                                var dragNoteName = (int)(-(_currentMousePosition.Y - _dragPos.Y) / NoteHeight +
                                                         selectedNote.Name);
                                if (dragNoteName > (NoteRangeMax * (Temperament + 1) + 2))
                                {
                                    dragNoteName = NoteRangeMax * (Temperament + 1) + 2;
                                }

                                if (dragNoteName < 0)
                                {
                                    dragNoteName = 0;
                                }

                                dragedSelectedNotes.Add(new Note
                                {
                                    StartTime = selectedNote.StartTime + dragRelativePos,
                                    EndTime = dragRelativePos + selectedNote.EndTime,
                                    Name =
                                        dragNoteName,
                                    // Color = NoteColor3
                                    Velocity = selectedNote.Velocity,
                                });
                            }
                            var solidColorBrush = new SolidColorBrush(color);
                            foreach (Note selectedNote in dragedSelectedNotes)
                            {
                                if (selectedNote.Name.Equals(noteName))
                                {
                                    context.FillRectangle(solidColorBrush,
                                        new Rect(selectedNote.StartTime * _widthOfBeat,
                                            Height - (noteName + 1) * NoteHeight,
                                            (selectedNote.EndTime - selectedNote.StartTime) * _widthOfBeat,
                                            NoteHeight * .9));
                                }
                            }

                            dragedSelectedNotes.Clear();
                        }
                        else
                        {
                            context.DrawLine(orangePen,
                                new Point(
                                    _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                                    _widthOfBeat, 0),
                                new Point(
                                    _currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet) +
                                    _widthOfBeat, Width));
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

            for(int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
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
                    Magnet = ViewHelper.GetMainWindow().PianoRollWindow.MagnetSettingsWindow.SelectedGrid.Value;
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


                    _selectFrame = new Rect(selectStartX, selectStartY,
                        selectWidth, selectHeight);

                    for(int i = NoteRangeMin; i < (NoteRangeMax + 1) * Temperament; i++)
                    // for (int i = NoteRangeMin; i <= NoteRangeMax; i++)
                    {
                        // for (int note = 0; note < Temperament; note++)
                        {
                            foreach (Note existNote in Notes)
                            {
                                if (
                                    (((Rect)_selectFrame).X < existNote.StartTime * _widthOfBeat &&
                                     ((Rect)_selectFrame).X + ((Rect)_selectFrame).Width >
                                     existNote.EndTime * _widthOfBeat ||
                                     ((Rect)_selectFrame).X > existNote.StartTime * _widthOfBeat &&
                                     ((Rect)_selectFrame).X < existNote.EndTime * _widthOfBeat ||
                                     ((Rect)_selectFrame).X + ((Rect)_selectFrame).Width >
                                     existNote.StartTime * _widthOfBeat &&
                                     ((Rect)_selectFrame).X + ((Rect)_selectFrame).Width <
                                     existNote.EndTime * _widthOfBeat) &&
                                    ((Rect)_selectFrame).Y < (Height / NoteHeight - existNote.Name) * NoteHeight &&
                                    ((Rect)_selectFrame).Y + ((Rect)_selectFrame).Height >
                                    (Height / NoteHeight - existNote.Name) * NoteHeight
                                )
                                {
                                    if (!SelectedNotes.Contains(existNote))
                                    {
                                        SelectedNotes.Add(existNote);
                                    }
                                }
                                else
                                {
                                    if (SelectedNotes.Contains(existNote))
                                    {
                                        SelectedNotes.Remove(existNote);
                                    }
                                }
                            }
                        }
                    }

                    InvalidateVisual();
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
                        _isEditing = true;
                        _selectFrame = null;
                        _currentNoteStartTime = existNote.StartTime;
                        removedNote = existNote;
                        _editingNote = existNote.Name;
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
                        if (e.Properties.IsLeftButtonPressed)
                        {
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
                            _currentNoteStartTime =  existNote.StartTime;
                            _currentNoteEndTime = existNote.EndTime;
                            _dragNoteVelocity = existNote.Velocity;
                            
                            //这傻逼钢琴窗就是他妈的一坨沟史
                            removedNote = existNote;
                            e.Handled = true;
                            break;
                        }
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
                Note draggingNote = new Note();
                foreach (Note note in Notes)
                {
                    if (Notes.Contains(note) && _currentHoverNote.Equals(note.Name))
                    {
                        if (_currentNoteStartTime > note.StartTime && _currentNoteStartTime < note.EndTime ||
                            _currentNoteEndTime > note.StartTime && _currentNoteEndTime < note.EndTime ||
                            note.StartTime > _currentNoteStartTime && note.StartTime < _currentNoteEndTime ||
                            note.EndTime > _currentNoteStartTime && note.EndTime < _currentNoteEndTime)
                        {
                            draggingNote = note;
                            break;
                        }
                    }
                }

                List<Note> draggingNotes = new List<Note>();
                foreach (Note selectedNote in SelectedNotes)
                {
                    if (Notes.Contains(selectedNote))
                    {
                        draggingNotes.Add(selectedNote);
                    }
                }

                foreach (Note draggingNote1 in draggingNotes)
                {
                    Notes.Remove(draggingNote1);
                }
                Notes.Remove(draggingNote);
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
                            List<Note> dragedSelectedNotes = new List<Note>();
                            foreach (Note selectedNote in SelectedNotes)
                            {
                                var dragRelativePos =
                                    ((e.GetPosition(this).X - _dragPos.X) -
                                     (e.GetPosition(this).X - _dragPos.X) %
                                     _widthOfBeat) / _widthOfBeat;

                                var noteName = (int)(-(e.GetPosition(this).Y - _dragPos.Y) / NoteHeight +
                                                     selectedNote.Name);
                                if (noteName > (NoteRangeMax * (Temperament + 1) + 2))
                                {
                                    noteName = NoteRangeMax * (Temperament + 1) + 2;
                                }

                                if (noteName < 0)
                                {
                                    noteName = 0;
                                } 
                                dragedSelectedNotes.Add(new Note
                                {
                                    StartTime = selectedNote.StartTime + dragRelativePos >= 0
                                        ? selectedNote.StartTime + dragRelativePos
                                        : 0,
                                    EndTime = dragRelativePos + selectedNote.EndTime,
                                    Name =
                                        noteName,
                                    // Color = NoteColor3
                                    Velocity = selectedNote.Velocity
                                });
                            }

                            SelectedNotes.Clear();
                            foreach (Note dragedSelectedNote in dragedSelectedNotes)
                            {
                                Notes.Add(dragedSelectedNote);
                                SelectedNotes.Add(dragedSelectedNote);
                            }

                            // Console.WriteLine($"Notes:{Notes.Count}");
                            // Console.WriteLine($"SelectedNotes:{SelectedNotes.Count}");
                            dragedSelectedNotes.Clear();
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
                                Name = _editingNote,
                                // Color = NoteColor3
                                Velocity = _dragNoteVelocity
                            });
                        }
                        else
                        {
                            List<Note> dragedSelectedNotes = new List<Note>();
                            foreach (Note selectedNote in SelectedNotes)
                            {
                                dragedSelectedNotes.Add(new Note
                                {
                                    StartTime = selectedNote.StartTime >= 0 ? selectedNote.StartTime : 0,
                                    EndTime = (e.GetPosition(this).X - e.GetPosition(this).X % (_widthOfBeat * Magnet) +
                                               _widthOfBeat) / _widthOfBeat,
                                    Name = selectedNote.Name,
                                    // Color =NoteColor3
                                    Velocity = selectedNote.Velocity
                                });
                            }

                            SelectedNotes.Clear();
                            foreach (Note dragedSelectedNote in dragedSelectedNotes)
                            {
                                Notes.Add(dragedSelectedNote);
                                SelectedNotes.Add(dragedSelectedNote);
                            }

                            // Console.WriteLine($"Notes:{Notes.Count}");
                            // Console.WriteLine($"SelectedNotes:{SelectedNotes.Count}");
                            dragedSelectedNotes.Clear();
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
                // _isShowingOptions = true;
                // _ = ShowOptions();
                ShowOptions();
            }
            else
            {
                // _isShowingOptions = false;
                // _ = HideOptions();
                HideOptions();
            }
            SaveNotes();
        }
        InvalidateVisual();
        e.Handled = true;
        GC.Collect();
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

    // private async Task ShowOptions()
    // {
    //     var positionLeft = SelectedNotes[0].StartTime * _widthOfBeat;
    //     var positionTop = Height - SelectedNotes[0].Name * NoteHeight;
    //     foreach (Note selectedNote in SelectedNotes)
    //     {
    //         positionLeft = double.Min(positionLeft, selectedNote.StartTime * _widthOfBeat);
    //         positionTop = double.Min(positionTop, Height - selectedNote.Name * NoteHeight);
    //     }
    //
    //     Options.Margin = new Thickness(positionLeft, positionTop, 0, 0);
    //         
    //     while (Options.Opacity < 1 && _isShowingOptions)
    //     {
    //         Options.IsVisible = true;
    //         await Task.Delay(50);
    //         lock(Options) Options.Opacity += .5;
    //         Console.WriteLine("IsShowingOptions");
    //     }
    //     await Task.CompletedTask;
    //     Console.WriteLine("ShowOptionsCompleted");
    // }
    // private async Task HideOptions()
    // {
    //     
    // while (Options.Opacity > 0 && !_isShowingOptions)
    // {
    //     
    //     await Task.Delay(50);
    //     lock(Options) Options.Opacity -= .5;
    //     Console.WriteLine("IsHidingOptions");
    // }
    // await Task.CompletedTask;
    // Options.IsVisible = false;
    // Console.WriteLine("HidingOptionsCompleted");
    // }


    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width >= Width)
        {
            ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                Width - ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width,
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y);
        }
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                double currentPosition = e.GetPosition(this).Y / NoteHeight;
                NoteHeight = double.Clamp(NoteHeight + e.Delta.Y * NoteHeight / 20d * ScalingSensitivity.Y, 10, 30);
                Height = NoteHeight * (NoteRangeMax - NoteRangeMin + 1) * Temperament;
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X,
                    currentPosition * NoteHeight -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll).Y);


                Console.WriteLine($"CurrentPositionY: {currentPosition}");

                Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset);
                Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll));
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
                    double.Max(1, ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width / trackEnd), 500);

                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    currentPosition * _widthOfBeat -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll).X,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y);

                // Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset);
                // Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll));
            }

            // _ = ShowOptions();
            // ShowOptions();
            SaveNotes();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    // private string IndexToNoteName(int index)
    // {
    //     string noteName = "";
    //     for (int i = NoteRangeMin - 1; i <= NoteRangeMax + 1; i++)
    //     {
    //         for (int note = 0; note < Temperament; note++)
    //         {
    //             switch (note)
    //             {
    //                 case 0:
    //                     noteName = $"C{i}";
    //                     break;
    //                 case 1:
    //                     noteName = $"C#{i}";
    //                     break;
    //                 case 2:
    //                     noteName = $"D{i}";
    //                     break;
    //                 case 3:
    //                     noteName = $"D#{i}";
    //                     break;
    //                 case 4:
    //                     noteName = $"E{i}";
    //                     break;
    //                 case 5:
    //                     noteName = $"F{i}";
    //                     break;
    //                 case 6:
    //                     noteName = $"F#{i}";
    //                     break;
    //                 case 7:
    //                     noteName = $"G{i}";
    //                     break;
    //                 case 8:
    //                     noteName = $"G#{i}";
    //                     break;
    //                 case 9:
    //                     noteName = $"A{i}";
    //                     break;
    //                 case 10:
    //                     noteName = $"A#{i}";
    //                     break;
    //                 case 11:
    //                     noteName = $"B{i}";
    //                     break;
    //             }
    //
    //             if (index.Equals(i * Temperament + note))
    //             {
    //                 return noteName;
    //             }
    //         }
    //     }
    //
    //     return "";
    // }
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
        if(!IsPianoBar)
        {
            double trackEnd = 0;
            foreach (var note in Notes)
            {
                trackEnd = double.Max(trackEnd, note.EndTime);
            }
            trackEnd += LengthIncreasement;
            Width = trackEnd * _widthOfBeat;
            // Console.WriteLine($"Scroll:{ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X+ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width} Width:{Width}");
            if (ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X+ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width > Width)
            {
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                    Width - ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y);
            }

            _widthOfBeat = double.Max(_widthOfBeat,
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Width / trackEnd);
        }

        ViewHelper.GetMainWindow().PianoRollWindow.PatternPreview.InvalidateVisual();
        ViewHelper.GetMainWindow().PianoRollWindow.NoteVelocity.InvalidateVisual();
        ViewHelper.GetMainWindow().PianoRollWindow.PianoScroller.InvalidateVisual();
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
                Notes.Clear();
                for (int i = 1; i < midi.Data.Tracks; i++)
                {
                    CurrentChannel = i;
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
                ViewHelper.GetMainWindow().PianoRollWindow.PatternPreview.ScrollToNoteFirst();
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
                    1,MidiCommandCode.NoteOn,
                    note.Name,
                    note.Velocity
                    ));
                midi.Data[1].Add(new NoteEvent((int)(note.EndTime * midi.Data.DeltaTicksPerQuarterNote),
                    1,MidiCommandCode.NoteOff,
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