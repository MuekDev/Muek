using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Muek.Models;
using Muek.Services;

namespace Muek.Views;

public partial class PianoRollWindow : UserControl
{
    private bool _isShowing = false;
    public bool IsShowing => _isDetached || _isShowing;
    private double _maxSize = 400.0;
    private bool _isDragging = false;
    private bool _velocityIsDragging = false;
    private bool _isDetached = false;
    
    public PianoRollWindow()
    {
        InitializeComponent();
        ClipToBounds = false;
        ResizePanel.PointerPressed += (sender, args) =>
        {
            if (_isDetached) return;
            _isDragging = true;
            if (_isShowing)
            {
                CloseButton.IsVisible = true;
                OpenButton.IsVisible = false;
            }
            else
            {
                _maxSize = 90;
            }

            args.Handled = true;
        };
        ResizePanel.PointerMoved += (sender, args) =>
        {
            if (_isDetached) return;
            if (_isDragging)
            {
                _maxSize = double.Clamp(Height - args.GetPosition(this).Y,90,ViewHelper.GetMainWindow().Height * .8);
                
                if(VelocityWindow.Height > MainBar.Bounds.Height)
                    VelocityWindow.Height = double.Clamp(MainBar.Bounds.Height,20, double.Max(PianoRollRightScroll.Bounds.Height,200));
                
                Height = _maxSize;
                args.Handled = true;
                if(_maxSize > 150)
                    Show();
                else
                {
                    _isShowing = false;
                }
            }
            else
            {
                if (_maxSize <= 90)
                {
                    CloseButton.IsVisible = false;
                    OpenButton.IsVisible = true;
                    _isShowing = false;
                    _maxSize = 400;
                }
                else
                if (_maxSize < 150)
                {
                    Hide();
                    _maxSize = 400;
                }
            }
        };
        ResizePanel.PointerReleased += (sender, args) =>
        {
            if (_isDetached) return;
            _isDragging = false;
            ResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
            
        };
        ResizePanel.PointerEntered += (sender, args) =>
        {
            if (_isDetached) return;
            ResizeBorder.IsVisible = true;
            Cursor = new Cursor(StandardCursorType.TopSide);
        };
        ResizePanel.PointerExited += (sender, args) =>
        {
            if (_isDragging) return;
            ResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
        };
        DropDisplay.IsVisible = false;
        DropDisplay.Background = new SolidColorBrush(Colors.Black, .5);
        EditArea.SetValue(DragDrop.AllowDropProperty, true);
        EditArea.AddHandler(DragDrop.DragOverEvent,((sender, args) =>
        {
            DropDisplay.IsVisible = true;
        }));
        // EditArea.AddHandler(DragDrop.DropEvent, MidiDragDrop());
        DropDisplay.SetValue(DragDrop.AllowDropProperty, true);
        DropDisplay.AddHandler(DragDrop.DragOverEvent, MidiDragOver());
        DropDisplay.AddHandler(DragDrop.DragLeaveEvent, (sender, args) =>
        {
            DropDisplay.IsVisible = false;
        });
        DropDisplay.AddHandler(DragDrop.DropEvent, MidiDragDrop());
        OpenButton.Background = new SolidColorBrush(DataStateService.MuekColor);
        WindowCover.Background = new SolidColorBrush(Colors.Black, .5);
        PatternColor.Background = new SolidColorBrush(DataStateService.MuekColor);
        
        
        VelocityResizePanel.PointerPressed += (sender, args) =>
        {
            _velocityIsDragging = true;
            args.Handled = true;
        };
        VelocityResizePanel.PointerMoved += (sender, args) =>
        {
            if (_velocityIsDragging)
            {
                VelocityWindow.Height = double.Clamp(Height - args.GetPosition(this).Y - 40,20, double.Max(PianoRollRightScroll.Bounds.Height,200));
                args.Handled = true;
            }
        };
        VelocityResizePanel.PointerReleased += (sender, args) =>
        {
            _velocityIsDragging = false;
            VelocityResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
            
        };
        VelocityResizePanel.PointerEntered += (sender, args) =>
        {
            VelocityResizeBorder.IsVisible = true;
            Cursor = new Cursor(StandardCursorType.TopSide);
        };
        VelocityResizePanel.PointerExited += (sender, args) =>
        {
            if(_velocityIsDragging) return;
            VelocityResizeBorder.IsVisible = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
        };
        Channel.ItemsSource = new[]
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
        };
    }
    
    private EventHandler<DragEventArgs> MidiDragDrop()
    {
        return (sender, args) =>
        {
            if (args.Data.Contains(DataFormats.Files))
            {
                #pragma warning disable CS0618
                var files = args.Data.GetFileNames()?.ToList();
                #pragma warning restore CS0618
                if (files == null) return;
                foreach (var file in files)
                {
                    if (!Path.Exists(file)) continue;
                    if (Path.GetExtension(file).ToLower() != ".mid") continue;
                    EditArea.ImportMidi(file);
                }
            }
            DropDisplay.IsVisible = false;
        };
    }

    private EventHandler<DragEventArgs>? MidiDragOver()
    {
        return (sender, args)=>
        {
            args.DragEffects = DragDropEffects.Copy;
            DropDisplay.IsVisible = true;
            args.Handled = true;
            InvalidateVisual();
        };
    }
    
    private void Hide()
    {
        if (_isDetached) return;
        if(Height >= _maxSize)
        {
            _isShowing = false;
            CloseButton.IsVisible = false;
            OpenButton.IsVisible = true;
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(500),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = HeightProperty,
                                Value = PatternPreview.Height + TopBar.Height
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
        else
        {
            _isShowing = false;
            CloseButton.IsVisible = false;
            OpenButton.IsVisible = true;
            _maxSize = 400;
        }
        PatternPreview.InvalidateVisual();
        // e.Handled = true;
    }
    
    public void Show()
    {
        if (_isDetached) return;
        if (_isDragging)
        {
            _isShowing = true;
            CloseButton.IsVisible = true;
            OpenButton.IsVisible = false;
        }
        else if(Height <= PatternPreview.Height + TopBar.Height)
        {
            _isShowing = true;
            CloseButton.IsVisible = true;
            OpenButton.IsVisible = false;
            new Animation
            {
                Duration = TimeSpan.FromMilliseconds(500),
                FillMode = FillMode.Forward,
                Easing = Easing.Parse("CubicEaseOut"),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter
                            {
                                Property = HeightProperty,
                                Value = _maxSize
                            }
                        }
                    }
                }
            }.RunAsync(this);
        }
        PatternPreview.InvalidateVisual();
        // e.Handled = true;
    }

    private void ScrollChange(object? sender, ScrollChangedEventArgs e)
    {
        if (e.Source != null && e.Source.Equals(PianoRollLeftScroll))
        {
            EditArea.Height = PianoBar.Height;
            PianoRollRightScroll.Offset = new Vector(PianoRollRightScroll.Offset.X,PianoRollLeftScroll.Offset.Y);
            EditArea.NoteHeight = PianoBar.NoteHeight;
        }

        if (e.Source != null && e.Source.Equals(PianoRollRightScroll))
        {
            PianoBar.Height = EditArea.Height;
            PianoRollLeftScroll.Offset = new Vector(0,PianoRollRightScroll.Offset.Y);
            PianoBar.NoteHeight = EditArea.NoteHeight;

            NoteVelocityScroll.Offset = new Vector(PianoRollRightScroll.Offset.X, 0);

        }

        if (e.Source != null && e.Source.Equals(NoteVelocityScroll))
        {
            NoteVelocity.Width = EditArea.Width;
            PianoRollRightScroll.Offset = new Vector(NoteVelocityScroll.Offset.X, PianoRollRightScroll.Offset.Y);
        }
        EditArea.ScrollOffset = PianoRollRightScroll.Offset.Y;
        EditArea.ClampValue = PianoRollRightScroll.Offset.X;
        PatternPreview.InvalidateVisual();
        PianoScroller.InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            PianoRollRightScroll.Offset = new Vector(PianoRollRightScroll.Offset.X - e.Delta.Y * 44,PianoRollRightScroll.Offset.Y );
            PianoRollRightScroll.Offset = new Vector(Math.Max(0, PianoRollRightScroll.Offset.X),PianoRollRightScroll.Offset.Y) ; // 不允许左滚超过0
            
            InvalidateVisual();
            e.Handled = true;
        }
    }

    private void ImportMidiFile(object? sender, RoutedEventArgs e)
    {
        var files = new Window().StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Midi file",
            FileTypeFilter =
            [
                new FilePickerFileType("MIDI File")
                {
                    Patterns = ["*.mid"]
                }
            ],
            AllowMultiple = false,
        }).GetAwaiter().GetResult();

        if (files.Count >= 1)
        {
            var file = files[0].Path.LocalPath;
            EditArea.ImportMidi(file);
        }

        e.Handled = true;
    }

    private void MagnetPropertyChange(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        EditArea.Magnet = MagnetSettingsWindow.SelectedGrid.Value;
        EditArea.InvalidateVisual();
    }

    private void ExportMidiFile(object? sender, RoutedEventArgs e)
    {
        
        EditArea.ExportMidi();
    }

    private void SelectChannel(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            // EditArea.CurrentTrack = (int)(e.AddedItems[0] ?? 1);
            EditArea.SaveNotes();
            PatternPreview.ScrollToNoteFirst();
        }
        catch (Exception @exception)
        {
            Console.Error.WriteLine(@exception.Message);
        }
    }

    private void ShowButtonClicked(object? sender, RoutedEventArgs e)
    {
        Show();
    }

    private void HideButtonClicked(object? sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void ShowSingleWindow(object? sender, RoutedEventArgs e)
    {
        var mainWindow = ViewHelper.GetMainWindow();
        if(mainWindow.MainGrid.Children.Contains(this))
        {
            mainWindow.MainGrid.Children.Remove(this);
            var window = new Window
            {
                ExtendClientAreaToDecorationsHint = true,
                SystemDecorations = SystemDecorations.BorderOnly,
                ExtendClientAreaTitleBarHeightHint = 0,
                Content = this,
                MinHeight = 200,
                MinWidth = 1000
            };
            PointerPressed += (_, args) =>
            {
                if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    window.BeginMoveDrag(args);
                }
            };
            Height = window.Height;
            window.Show();
            _isDetached = true;
            CloseButton.IsVisible = false;
            OpenButton.IsVisible = false;
            window.Closed += (o, args) =>
            {
                window.Content = null;
                mainWindow.MainGrid.Children.Add(this);
                Hide();
                Height = 90;
                _isDetached = false;
                OpenButton.IsVisible = true;
                CloseButton.IsVisible = false;
            };
        }
        else
        {
            try
            {
                if (Parent is Window window)
                    window.Close();
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}