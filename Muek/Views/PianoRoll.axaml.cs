using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Muek.Models;

namespace Muek.Views;

public partial class PianoRoll : UserControl
{

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

    
    private int _noteRangeMax = 9;
    private int _noteRangeMin = 0;

    private int _Temperament = 12;

    
    //TODO 需要获取目前窗口大小
    private double _renderSize = 2000;
    
    
    
    private IBrush _noteColor1;
    private IBrush _noteColor2;
    private IBrush _noteColor3;

    private IBrush _noteHoverColor;

    private string _currentHoverNote = "";
    
    
    public PianoRoll()
    {
        InitializeComponent();
        
        NoteHeight = 15;
        ClampValue = 0;
        
        
        // IsVisible = true;
        Height = NoteHeight * (_noteRangeMax - _noteRangeMin + 1) * _Temperament;
        Console.WriteLine(Height);

        
        
        _noteColor1 = Brushes.White;
        _noteColor2 = Brushes.Black;
        _noteColor3 = Brushes.YellowGreen;
        
        
        _noteHoverColor = Brush.Parse("#40232323");

        _widthOfBeat = 50;
        if (!IsPianoBar)
        {
            Width = 20000 + 2;
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
            var noteName = "";

            //左侧钢琴
            {
                //十二平均律
                for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
                {
                    for (int note = 0; note < _Temperament; note++)
                    {
                        switch (note)
                        {
                            case 0:
                                noteName = $"C{i}";
                                noteColor = _noteColor3;
                                break;
                            case 1:
                                noteName = $"C#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 2:
                                noteName = $"D{i}";
                                noteColor = _noteColor1;
                                break;
                            case 3:
                                noteName = $"D#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 4:
                                noteName = $"E{i}";
                                noteColor = _noteColor1;
                                break;
                            case 5:
                                noteName = $"F{i}";
                                noteColor = _noteColor1;
                                break;
                            case 6:
                                noteName = $"F#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 7:
                                noteName = $"G{i}";
                                noteColor = _noteColor1;
                                break;
                            case 8:
                                noteName = $"G#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 9:
                                noteName = $"A{i}";
                                noteColor = _noteColor1;
                                break;
                            case 10:
                                noteName = $"A#{i}";
                                noteColor = _noteColor2;
                                break;
                            case 11:
                                noteName = $"B{i}";
                                noteColor = _noteColor1;
                                break;
                        }

                        // Console.WriteLine(noteName);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .9, NoteHeight * .9),5F);
                        context.FillRectangle(noteColor,
                            new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .4, NoteHeight * .9));
                        // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                        context.DrawText(new FormattedText(noteName,
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default,
                                10, (noteColor == _noteColor2 ? _noteColor1 : _noteColor2)),
                            new Point(0, Height - (i * _Temperament + note + 1) * NoteHeight)
                        );

                        if (noteName.Equals(_currentHoverNote))
                        {
                            context.FillRectangle(_noteHoverColor,
                                new Rect(0, Height - (i * _Temperament + note + 1) * NoteHeight, Width * .9,
                                    NoteHeight * .9));
                        }
                    }
                }
            }
        }
        else
        {
            
            
            var noteColor = _noteColor1;
            var noteName = "";
            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _Temperament; note++)
                {
                    switch (note)
                    {
                        case 0:
                            noteName = $"C{i}";
                            noteColor = _noteColor3;
                            break;
                        case 1:
                            noteName = $"C#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 2:
                            noteName = $"D{i}";
                            noteColor = _noteColor1;
                            break;
                        case 3:
                            noteName = $"D#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 4:
                            noteName = $"E{i}";
                            noteColor = _noteColor1;
                            break;
                        case 5:
                            noteName = $"F{i}";
                            noteColor = _noteColor1;
                            break;
                        case 6:
                            noteName = $"F#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 7:
                            noteName = $"G{i}";
                            noteColor = _noteColor1;
                            break;
                        case 8:
                            noteName = $"G#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 9:
                            noteName = $"A{i}";
                            noteColor = _noteColor1;
                            break;
                        case 10:
                            noteName = $"A#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 11:
                            noteName = $"B{i}";
                            noteColor = _noteColor1;
                            break;
                    }
                    // Console.WriteLine(noteName);
                    context.FillRectangle(Brush.Parse("#40232323"),
                        new Rect(ClampValue + 2, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize, NoteHeight * .9));
                    // Console.WriteLine(new Rect(0, Height - (i * _Temperament + note +1) * NoteHeight,NoteWidth,NoteHeight));
                    
                    if (noteName.Equals(_currentHoverNote))
                    {
                        context.FillRectangle(_noteHoverColor,
                            new Rect(ClampValue + 2, Height - (i * _Temperament + note + 1) * NoteHeight, _renderSize,
                                NoteHeight * .9));
                    }
                }
            }
            
            
            for (int i = 0; i < Width / _widthOfBeat; i++)
            {
                var gridLinePen = new Pen(Brushes.White, 1);
                if(i * _widthOfBeat > ClampValue && i *  _widthOfBeat < _renderSize + ClampValue)
                {
                    if (i % 4 == 0)
                    {
                        gridLinePen.Brush = Brushes.White;
                
                        //小节数
                        context.DrawText(new FormattedText(i.ToString(),
                                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 15,
                                Brushes.White),
                            new Point(6 + i * _widthOfBeat, ScrollOffset));
                    }
                    else
                    {
                        gridLinePen.Brush = Brushes.DimGray;
                    }
                
                    context.DrawLine(gridLinePen,
                        new Point(2 + i * _widthOfBeat, 0),
                        new Point(2 + i * _widthOfBeat, Height));
                }
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        
        {
            var noteColor = _noteColor1;
            var noteName = "";

            for (int i = _noteRangeMin; i <= _noteRangeMax; i++)
            {
                for (int note = 0; note < _Temperament; note++)
                {
                    switch (note)
                    {
                        case 0:
                            noteName = $"C{i}";
                            noteColor = _noteColor1;
                            break;
                        case 1:
                            noteName = $"C#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 2:
                            noteName = $"D{i}";
                            noteColor = _noteColor1;
                            break;
                        case 3:
                            noteName = $"D#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 4:
                            noteName = $"E{i}";
                            noteColor = _noteColor1;
                            break;
                        case 5:
                            noteName = $"F{i}";
                            noteColor = _noteColor1;
                            break;
                        case 6:
                            noteName = $"F#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 7:
                            noteName = $"G{i}";
                            noteColor = _noteColor1;
                            break;
                        case 8:
                            noteName = $"G#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 9:
                            noteName = $"A{i}";
                            noteColor = _noteColor1;
                            break;
                        case 10:
                            noteName = $"A#{i}";
                            noteColor = _noteColor2;
                            break;
                        case 11:
                            noteName = $"B{i}";
                            noteColor = _noteColor1;
                            break;
                    }

                    var relativePos = e.GetPosition(this) -
                                      new Point(0, Height - (i * _Temperament + note + 1) * NoteHeight);
                    Console.WriteLine(relativePos);
                    
                    if (relativePos.Y > 0 && relativePos.Y < NoteHeight)
                    {
                        _currentHoverNote = noteName;
                        InvalidateVisual();
                        Console.WriteLine(_currentHoverNote);
                    }
                }
            }
        }
        
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _currentHoverNote =  null;
        InvalidateVisual();
    }
}