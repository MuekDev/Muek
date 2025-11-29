using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Muek.Models;

namespace Muek.Views;

//这玩意就是把PatternPreview拿过来瞎改了改

public partial class PianoScroller : UserControl
{
    
    public PianoScroller()
    {
        InitializeComponent();
    }
    
    //一点用都没有，留着回家过年
        // public static readonly StyledProperty<IBrush> BackgroundColorProperty =
    //     AvaloniaProperty.Register<PatternPreview, IBrush>(
    //         nameof(BackgroundColor));

    // public IBrush BackgroundColor => ViewHelper.GetMainWindow().PianoRollWindow.PatternColor.Background;
    // {
    //     get => GetValue(BackgroundColorProperty);
    //     set => SetValue(BackgroundColorProperty, value);
    // }

    private List<PianoRoll.Note> Notes => ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Notes;

    private bool _isHover = false;
    private bool _isPressed = false;
    private bool _isDragging = false;

    private readonly Pen _whitePen = new Pen(Brushes.White);
    
    private RenderTargetBitmap _notesCache;
    private bool _isCacheDirty = true;
    private int _lastNotesCount = 0;
    private double _lastBoundsWidth = 0;
    private double _lastBoundsHeight = 0;
    

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }

        if (!ViewHelper.GetMainWindow().PianoRollWindow.IsShowing)
        {
            // context.DrawRectangle(BackgroundColor, null, new Rect(0, 0, Bounds.Width, Bounds.Height));
            if (_isHover)
            {
                context.DrawRectangle(new SolidColorBrush(Colors.Black, .1), null,
                    new Rect(0, 0, Bounds.Width, Bounds.Height), 10D, 15D);
            }
        }
        else
        {
            context.DrawRectangle(new SolidColorBrush(Colors.White, .05), null,
                new Rect( 0,ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y /
                            ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height
                            * Bounds.Height
                    ,
                    Bounds.Width,
                    ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Height /
                    ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height
                    * Bounds.Height));
            if (_isHover)
            {
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.White)),
                    new Rect( 0,ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y /
                                ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height
                                * Bounds.Height
                       ,
                        Bounds.Width ,
                        ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Bounds.Height /
                        ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height
                        * Bounds.Height));
            }
        }

        // 使用缓存的音符渲染
        RenderNotesWithCache(context);
    }

    private void RenderNotesWithCache(DrawingContext context)
    {
        if (Notes.Count == 0)
        {
            // 如果没有音符，清理缓存
            if (_notesCache != null)
            {
                _notesCache.Dispose();
                _notesCache = null;
            }
            _lastNotesCount = 0;
            return;
        }

        // 检查是否需要更新缓存
        bool needsUpdate = _isCacheDirty || 
                          _notesCache == null ||
                          _lastNotesCount != Notes.Count ||
                          System.Math.Abs(_lastBoundsWidth - Bounds.Width) > 0.1 ||
                          System.Math.Abs(_lastBoundsHeight - Bounds.Height) > 0.1;

        if (needsUpdate)
        {
            UpdateNotesCache();
            _isCacheDirty = false;
            _lastNotesCount = Notes.Count;
            _lastBoundsWidth = Bounds.Width;
            _lastBoundsHeight = Bounds.Height;
        }

        // 绘制缓存
        if (_notesCache != null)
        {
            context.DrawImage(_notesCache, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
    }

    private void UpdateNotesCache()
    {
        // 释放之前的缓存
        _notesCache?.Dispose();
        
        // 检查有效尺寸
        if (Bounds.Width <= 0 || Bounds.Height <= 0 || double.IsNaN(Bounds.Width) || double.IsNaN(Bounds.Height))
        {
            _notesCache = null;
            return;
        }

        try
        {
            // 创建新的渲染目标位图
            var pixelSize = new PixelSize((int)System.Math.Ceiling(Bounds.Width), (int)System.Math.Ceiling(Bounds.Height));
            if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
            {
                _notesCache = null;
                return;
            }

            _notesCache = new RenderTargetBitmap(pixelSize);
            
            using (var cacheContext = _notesCache.CreateDrawingContext())
            {
                // 绘制音符线条
                foreach (var note in Notes)
                {
                    cacheContext.DrawLine(_whitePen,
                        new Point(0, (1 - (float)note.Name /
                            ((PianoRoll.NoteRangeMax - PianoRoll.NoteRangeMin + 1) *
                                PianoRoll.Temperament - 1)) * Bounds.Height),
                        new Point(Bounds.Width, (1 - (float)note.Name /
                            ((PianoRoll.NoteRangeMax - PianoRoll.NoteRangeMin + 1) *
                                PianoRoll.Temperament - 1)) * Bounds.Height));
                }
            }
        }
        catch (System.Exception ex)
        {
            // 如果创建缓存失败，回退到直接渲染
            System.Diagnostics.Debug.WriteLine($"Failed to create notes cache: {ex.Message}");
            _notesCache = null;
        }
    }

    private double _pressedPosition;
    private double _pressedScroll;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }
        
        // Console.WriteLine("Pattern Notes:");
        // foreach (var note in Notes)
        // {
        // Console.WriteLine(note.Name);
        // }
        // if (!ViewHelper.GetMainWindow().PianoRollWindow.IsShowing)
        // {
        //     ScrollToNoteFirst();
        // }

        _pressedPosition = e.GetPosition(this).Y;
        _pressedScroll = ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.Y;
        _isPressed = true;
        if (ViewHelper.GetMainWindow().PianoRollWindow.IsShowing)
        {
            e.Handled = true;
        }
    }

    // public void ScrollToNoteFirst()
    // {
    //     if (Notes.Count > 0)
    //     {
    //         PianoRoll.Note noteFirst = Notes[0];
    //         foreach (var note in Notes)
    //         {
    //             var position = note.Name;
    //             // noteMin = int.Min(position, noteMin); //最低的音符
    //             // noteMax = int.Max(position, noteMax); //最高的音符
    //             // noteFirst = double.Min(noteFirst, note.StartTime); //最左边的音符
    //             // noteLast = double.Max(noteLast, note.EndTime); //最右边的音符
    //             noteFirst = noteFirst.StartTime < note.StartTime ? noteFirst : note;
    //         }
    //
    //         ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
    //             noteFirst.StartTime * ViewHelper.GetMainWindow().PianoRollWindow.EditArea.WidthOfBeat,
    //             ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height - (noteFirst.Name + 1) *
    //             ViewHelper.GetMainWindow().PianoRollWindow.EditArea.NoteHeight);
    //     }
    // }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        e.Handled = true;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHover = true;
        InvalidateVisual();

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }
        
        if (_isPressed)
        {
            ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset = new Vector(
                ViewHelper.GetMainWindow().PianoRollWindow.PianoRollRightScroll.Offset.X,
                (e.GetPosition(this).Y - _pressedPosition) / Bounds.Height
                * ViewHelper.GetMainWindow().PianoRollWindow.EditArea.Height + _pressedScroll
                
            );
            if (!_isDragging)
            {
            }

            _isDragging = true;
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHover = false;
        InvalidateVisual();
        e.Handled = true;
    }
}