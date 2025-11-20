using System;
using System.Collections.Generic;
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

    public static readonly StyledProperty<bool> IsPianoBarProperty = AvaloniaProperty.Register<PianoRoll, bool>(
        nameof(IsPianoBar));


    //为了同步一些属性，把两边的钢琴写在一个类里了
    public bool IsPianoBar
    {
        get => GetValue(IsPianoBarProperty);
        set => SetValue(IsPianoBarProperty, value);
    }

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

    public double WidthOfBeat => _widthOfBeat;


    private int _noteRangeMax = 9;
    private int _noteRangeMin = 0;

    private int _temperament = 12;

    
    // private double _renderSize = 2000;


    private IBrush _noteColor1;
    private IBrush _noteColor2;
    private IBrush _noteColor3;

    private IBrush _noteHoverColor;

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
        public Color Color = default;
    }

    private Note _currentHoverDrawedNote = new Note();

    private Point _currentMousePosition;
    private Point _pressedMousePosition;

    public List<Note> Notes = new();

    public Point ScalingSensitivity = new Point(5, 2);


    //框选音符
    private Rect? _selectFrame;

    public List<Note> SelectedNotes = new();

    // private bool _isShowingOptions = false;
    public double Magnet = 1.0;

    public PianoRoll()
    {
        InitializeComponent();

        NoteHeight = 15;
        ClampValue = 0;


        // IsVisible = true;
        Height = NoteHeight * (_noteRangeMax - _noteRangeMin + 1) * _temperament;
        // Console.WriteLine(Height);


        _noteColor1 = Brushes.White;
        _noteColor2 = Brushes.Black;
        _noteColor3 = new SolidColorBrush(DataStateService.MuekColor);


        _noteHoverColor = new SolidColorBrush(DataStateService.MuekColor, .1);

        _widthOfBeat = 50;
        if (!IsPianoBar)
        {
            Width = 32*_widthOfBeat;
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // context.FillRectangle(Brushes.DimGray, new Rect(0, 0, Width, Height));

        //左侧钢琴Bar
        if (IsPianoBar)
        {
            _noteHoverColor = new SolidColorBrush(Colors.Black, .5);

            var noteColor = _noteColor1;
            var noteName = -1;

            //左侧钢琴
            {
                //十二平均律
                for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
                {
                    for (int note = 0; note < _temperament; note++)
                    {
                        noteName = i * _temperament + note;
                        noteColor = NoteNameToBrush(IndexToNoteName(noteName));

                        // Console.WriteLine(noteName);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _temperament + note + 1) * NoteHeight, Width * .9, NoteHeight),
                            5);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _temperament + note + 1) * NoteHeight, Width * .8, NoteHeight));
                        // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                        context.DrawText(new FormattedText(IndexToNoteName(noteName),
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                NoteHeight * .6, (noteColor == _noteColor2 ? _noteColor1 : _noteColor2)),
                            new Point(0, Height - (i * _temperament + note + 1) * NoteHeight)
                        );

                        if (noteName.Equals(_currentHoverNote))
                        {
                            context.FillRectangle(_noteHoverColor,
                                new Rect(0, Height - (i * _temperament + note + 1) * NoteHeight, Width * .9,
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
            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _temperament; note++)
                {
                    noteName = i * _temperament + note;

                    //绘制编辑区
                    // Console.WriteLine(noteName);
                    context.DrawRectangle(Brush.Parse("#40232323"),
                        new Pen(new SolidColorBrush(new HslColor(1, 0, 0, 0).ToRgb(), .1)),
                        new Rect(ClampValue, Height - (i * _temperament + note + 1) * NoteHeight, Width,
                            NoteHeight));
                    // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                    if (!IndexToNoteName(noteName).Contains('#'))
                    {
                        context.DrawRectangle(new SolidColorBrush(Colors.White, .05), null,
                            new Rect(ClampValue, Height - (i * _temperament + note + 1) * NoteHeight, Width,
                                NoteHeight));
                    }

                    //绘制Hover区
                    if (noteName.Equals(_currentHoverNote))
                    {
                        context.FillRectangle(_noteHoverColor,
                            new Rect(ClampValue, Height - (i * _temperament + note + 1) * NoteHeight, Width,
                                NoteHeight));
                        // Console.WriteLine($"HOVERING: {_currentHoverNote}");
                    }
                }
            }

            if (_widthOfBeat > 20)
            {
                for (double i = 0; i < Width / _widthOfBeat; i += Magnet)
                {
                    var gridLinePen = new Pen(new SolidColorBrush(Colors.White, .2), _widthOfBeat * .01);
                    if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                    {
                        context.DrawLine(gridLinePen,
                            new Point(i * _widthOfBeat, 0),
                            new Point(i * _widthOfBeat, Height));
                    }
                }

                if (Magnet <= 1 / 6.0)
                {
                    for (int i = 0; i < Width / _widthOfBeat; i += 1)
                    {
                        var gridLinePen = new Pen(new SolidColorBrush(Colors.MediumPurple), _widthOfBeat * .02);
                        if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                        {
                            context.DrawLine(gridLinePen,
                                new Point(i * _widthOfBeat, 0),
                                new Point(i * _widthOfBeat, Height));
                        }
                    }
                }

                if (Magnet <= 1 / 3.0)
                {
                    for (int i = 0; i < Width / _widthOfBeat; i += 2)
                    {
                        var gridLinePen = new Pen(new SolidColorBrush(Colors.LightSkyBlue), _widthOfBeat * .02);
                        if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                        {
                            context.DrawLine(gridLinePen,
                                new Point(i * _widthOfBeat, 0),
                                new Point(i * _widthOfBeat, Height));
                        }
                    }
                }
            }

            for (int i = 0; i < Width / _widthOfBeat; i++)
            {
                var gridLinePen = new Pen(Brushes.White, _widthOfBeat * .02);
                if (i * _widthOfBeat > ClampValue && i * _widthOfBeat < Width + ClampValue)
                {
                    if (i % 4 == 0)
                    {
                        //小节数
                        context.DrawText(new FormattedText((i / 4).ToString(),
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 15,
                                Brushes.White),
                            new Point(6 + i * _widthOfBeat, ScrollOffset));
                        context.DrawLine(gridLinePen,
                            new Point(i * _widthOfBeat, 0),
                            new Point(i * _widthOfBeat, Height));
                    }
                }
            }

            if (_currentHoverNote != -1)
            {
                if (!_isDrawing && !_isEditing && !_isDragging)
                {
                    context.DrawLine(new Pen(_noteColor3, 1.5),
                        new Point((_currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet)), 0),
                        new Point((_currentMousePosition.X - _currentMousePosition.X % (_widthOfBeat * Magnet)),
                            Height)
                    );
                }
                else if (!_isDragging)
                {
                    context.DrawLine(new Pen(_noteColor3, 1.5),
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

            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _temperament; note++)
                {
                    noteName = i * _temperament + note;

                    //渲染音符
                    foreach (Note existNote in Notes)
                    {
                        var start = existNote.StartTime * _widthOfBeat;
                        var end = existNote.EndTime * _widthOfBeat;
                        var color = existNote.Color;
                        var velocity = existNote.Velocity;
                        if (noteName.Equals(existNote.Name))
                        {
                            context.DrawRectangle(new SolidColorBrush(color, velocity / 127.0),
                                new Pen(Brushes.Black, 1),
                                new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight, end - start,
                                    NoteHeight * .9));

                            context.DrawText(new FormattedText(IndexToNoteName(existNote.Name),
                                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                    NoteHeight * .6,
                                    Brushes.Black),
                                new Point(start, Height - (i * _temperament + note + 1) * NoteHeight));


                            //Hover音符
                            if (existNote.Name.Equals(_currentHoverNote) &&
                                _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                                _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                            {
                                if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                                {
                                    //更改音符长度
                                    Cursor = new Cursor(StandardCursorType.RightSide);
                                    context.DrawLine(new Pen(Brushes.Red, 2),
                                        new Point(end - 1, Height - (i * _temperament + note + 1) * NoteHeight),
                                        new Point(end - 1,
                                            Height - (i * _temperament + note + 1) * NoteHeight + NoteHeight * .9));
                                }
                                else
                                {
                                    Cursor = new Cursor(StandardCursorType.Arrow);
                                    context.DrawRectangle(null, new Pen(Brushes.White, 1),
                                        new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight,
                                            end - start, NoteHeight * .9));
                                }
                            }
                            else
                            {
                                Cursor = new Cursor(StandardCursorType.Arrow);
                            }
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
                                    new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight, end - start,
                                        NoteHeight * .9));
                            }
                        }
                        else
                        {
                            if (noteName.Equals(_editingNote))
                            {
                                context.FillRectangle(new SolidColorBrush(color),
                                    new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight, end - start,
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

            //渲染框
            if (_selectFrame != null)
            {
                context.DrawRectangle(null, new Pen(Brushes.White),
                    (Rect)_selectFrame);
            }

            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _temperament; note++)
                {
                    noteName = i * _temperament + note;

                    //渲染选中音符
                    foreach (Note existNote in SelectedNotes)
                    {
                        var start = existNote.StartTime * _widthOfBeat;
                        var end = existNote.EndTime * _widthOfBeat;
                        if (noteName.Equals(existNote.Name))
                        {
                            context.DrawRectangle(Brushes.Black, new Pen(Brushes.White, 1),
                                new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight, end - start,
                                    NoteHeight * .9));

                            context.DrawText(new FormattedText(IndexToNoteName(existNote.Name),
                                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                    NoteHeight * .6,
                                    Brushes.White),
                                new Point(start, Height - (i * _temperament + note + 1) * NoteHeight));

                            //Hover音符
                            if (existNote.Name.Equals(_currentHoverNote) &&
                                _currentMousePosition.X > existNote.StartTime * _widthOfBeat &&
                                _currentMousePosition.X < existNote.EndTime * _widthOfBeat)
                            {
                                if (double.Abs(_currentMousePosition.X - existNote.EndTime * _widthOfBeat) < 5)
                                {
                                    context.DrawLine(new Pen(Brushes.Red, 2),
                                        new Point(end - 1, Height - (i * _temperament + note + 1) * NoteHeight),
                                        new Point(end - 1,
                                            Height - (i * _temperament + note + 1) * NoteHeight + NoteHeight * .9));
                                }
                                else
                                {
                                    context.DrawRectangle(null, new Pen(Brushes.White, 1),
                                        new Rect(start, Height - (i * _temperament + note + 1) * NoteHeight,
                                            end - start, NoteHeight * .9));
                                }
                            }
                        }
                    }

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
                                if (dragNoteName > (_noteRangeMax * (_temperament + 1) + 2))
                                {
                                    dragNoteName = _noteRangeMax * (_temperament + 1) + 2;
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
                                    Color = DataStateService.MuekColor
                                });
                            }

                            foreach (Note selectedNote in dragedSelectedNotes)
                            {
                                if (selectedNote.Name.Equals(noteName))
                                {
                                    context.FillRectangle(new SolidColorBrush(color),
                                        new Rect(selectedNote.StartTime * _widthOfBeat,
                                            Height - (i * _temperament + note + 1) * NoteHeight,
                                            (selectedNote.EndTime - selectedNote.StartTime) * _widthOfBeat,
                                            NoteHeight * .9));
                                }
                            }

                            dragedSelectedNotes.Clear();
                        }
                        else
                        {
                            context.DrawLine(new Pen(Brushes.Orange, 1),
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

            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _temperament; note++)
                {
                    noteName = i * _temperament + note;

                    var relativePos = e.GetPosition(this) -
                                      new Point(0, Height - (i * _temperament + note + 1) * NoteHeight);
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
                if (e.KeyModifiers == KeyModifiers.Control && _isDragging)
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
                        _currentHoverDrawedNote.StartTime = note.StartTime;
                        _currentHoverDrawedNote.EndTime = note.EndTime;
                        _currentHoverDrawedNote.Name = _currentHoverNote;
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


                    for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
                    {
                        for (int note = 0; note < _temperament; note++)
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
                if (!_isEditing)
                {
                    if (SelectedNotes.Count == 0)
                    {
                        Notes.Add(new Note
                        {
                            StartTime = _currentNoteStartTime >= 0 ? _currentNoteStartTime : 0,
                            EndTime = _currentNoteEndTime,
                            Name = _currentHoverNote,
                            Color = DataStateService.MuekColor
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
                            if (noteName > (_noteRangeMax * (_temperament + 1) + 2))
                            {
                                noteName = _noteRangeMax * (_temperament + 1) + 2;
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
                                Color = DataStateService.MuekColor
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
                            Color = DataStateService.MuekColor
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
                                Color = DataStateService.MuekColor
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

        if (!IsPianoBar)
        {
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
        }

        SaveNotes();
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
            if (note.Name + 12 > _noteRangeMax * (_temperament + 1) + 2)
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
        if (ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width >= Width)
        {
            ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset = new Vector(
                Width - ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width,
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.Y);
        }
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                double currentPosition = e.GetPosition(this).Y / NoteHeight;
                NoteHeight = double.Clamp(NoteHeight + e.Delta.Y * ScalingSensitivity.Y, 10, 30);
                Height = NoteHeight * (_noteRangeMax - _noteRangeMin + 1) * _temperament;
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset = new Vector(
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.X,
                    currentPosition * NoteHeight -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight).Y);


                Console.WriteLine($"CurrentPositionY: {currentPosition}");

                Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset);
                Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight));
            }
            else
            {
                double trackEnd = 0;
                foreach (var note in Notes)
                {
                    trackEnd = double.Max(trackEnd, note.EndTime);
                }
                trackEnd += 32;
                double currentPosition = e.GetPosition(this).X / _widthOfBeat;

                _widthOfBeat = double.Clamp(_widthOfBeat + e.Delta.Y * ScalingSensitivity.X,
                    double.Max(10, ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width / trackEnd), Width * .1);

                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset = new Vector(
                    currentPosition * _widthOfBeat -
                    e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight).X,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.Y);

                // Console.WriteLine(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset);
                // Console.WriteLine(e.GetPosition(ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight));
            }

            // _ = ShowOptions();
            // ShowOptions();
            SaveNotes();
            InvalidateVisual();
            e.Handled = true;
        }
    }

    public int NoteNameToIndex(string name)
    {
        string noteName = "";
        for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
        {
            for (int note = 0; note < _temperament; note++)
            {
                switch (note)
                {
                    case 0:
                        noteName = $"C{i}";
                        break;
                    case 1:
                        noteName = $"C#{i}";
                        break;
                    case 2:
                        noteName = $"D{i}";
                        break;
                    case 3:
                        noteName = $"D#{i}";
                        break;
                    case 4:
                        noteName = $"E{i}";
                        break;
                    case 5:
                        noteName = $"F{i}";
                        break;
                    case 6:
                        noteName = $"F#{i}";
                        break;
                    case 7:
                        noteName = $"G{i}";
                        break;
                    case 8:
                        noteName = $"G#{i}";
                        break;
                    case 9:
                        noteName = $"A{i}";
                        break;
                    case 10:
                        noteName = $"A#{i}";
                        break;
                    case 11:
                        noteName = $"B{i}";
                        break;
                }

                if (name.Equals(noteName))
                {
                    return (i * _temperament + note);
                }
            }
        }

        return -1;
    }

    private string IndexToNoteName(int index)
    {
        string noteName = "";
        for (int i = _noteRangeMin - 1; i <= _noteRangeMax + 1; i++)
        {
            for (int note = 0; note < _temperament; note++)
            {
                switch (note)
                {
                    case 0:
                        noteName = $"C{i}";
                        break;
                    case 1:
                        noteName = $"C#{i}";
                        break;
                    case 2:
                        noteName = $"D{i}";
                        break;
                    case 3:
                        noteName = $"D#{i}";
                        break;
                    case 4:
                        noteName = $"E{i}";
                        break;
                    case 5:
                        noteName = $"F{i}";
                        break;
                    case 6:
                        noteName = $"F#{i}";
                        break;
                    case 7:
                        noteName = $"G{i}";
                        break;
                    case 8:
                        noteName = $"G#{i}";
                        break;
                    case 9:
                        noteName = $"A{i}";
                        break;
                    case 10:
                        noteName = $"A#{i}";
                        break;
                    case 11:
                        noteName = $"B{i}";
                        break;
                }

                if (index.Equals(i * _temperament + note))
                {
                    return noteName;
                }
            }
        }

        return "";
    }

    private IBrush NoteNameToBrush(string name)
    {
        if (name.Contains('#'))
        {
            return _noteColor2;
        }

        if (name[0] == 'C')
        {
            return _noteColor3;
        }

        return _noteColor1;
    }

    public void SaveNotes()
    {
        ViewHelper.GetMainWindow().PianoRollWindow.PatternPreview.Notes = Notes;
        {
            double trackEnd = 0;
            foreach (var note in Notes)
            {
                trackEnd = double.Max(trackEnd, note.EndTime);
            }
            trackEnd += 32;
            Width = trackEnd * _widthOfBeat;
            // Console.WriteLine($"Scroll:{ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.X+ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width} Width:{Width}");
            if (ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.X+ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width > Width)
            {
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset = new Vector(
                    Width - ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Offset.Y);
            }

            _widthOfBeat = double.Max(_widthOfBeat,
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRight.Bounds.Width / trackEnd);
        }
        ViewHelper.GetMainWindow().PianoRollWindow.PatternPreview.InvalidateVisual();
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
                    Color = SelectedNotes[i].Color,
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
                    Color = SelectedNotes[i].Color,
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
                    foreach (var note in midi.Data[i])
                    {
                        if (note.GetType() == typeof(NoteOnEvent))
                        {
                            Notes.Add(new Note()
                            {
                                Name = ((NoteOnEvent)note).NoteNumber,
                                Color = DataStateService.MuekColor,
                                StartTime = ((NoteOnEvent)note).AbsoluteTime /
                                    (double)midi.Data.DeltaTicksPerQuarterNote * 4,
                                EndTime = (((NoteOnEvent)note).AbsoluteTime + ((NoteOnEvent)note).NoteLength) /
                                    (double)midi.Data.DeltaTicksPerQuarterNote * 4,
                                Velocity = ((NoteOnEvent)note).Velocity
                            });
                        }
                    }

                Console.WriteLine($"IMPORT Notes: {Notes.Count}");
                // foreach (var note in Notes)
                // {
                //     Console.WriteLine($"Start: {note.StartTime}; End: {note.EndTime}; Name: {note.Name}");
                // }
                SaveNotes();
                ViewHelper.GetMainWindow().PianoRollWindow.PatternPreview.ScrollToNoteFirst();
                InvalidateVisual();
        }
        Console.WriteLine($"Notes: {Notes.Count}");
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
                midi.Data[1].Add(new NoteEvent((long)(note.StartTime * midi.Data.DeltaTicksPerQuarterNote / 4.0),
                    1,MidiCommandCode.NoteOn,
                    note.Name,
                    note.Velocity
                    ));
                midi.Data[1].Add(new NoteEvent((int)(note.EndTime * midi.Data.DeltaTicksPerQuarterNote / 4.0),
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