using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Neumorphism;
using Avalonia.Themes.Simple;
using Muek.Services;
using Projektanker.Icons.Avalonia;

namespace Muek.Views;

public partial class MuekPlugin : UserControl
{
    enum MuekPluginType
    {
        Empty,
        Oscillator,
        Sampler,
        Granular,
        Equalizer,
        Filter,
        Compressor,
        Limiter,
        Clipper,
        MultibandCompressor,
        MultibandLimiter,
        MultibandClipper,
        Delay,
        Reverb,
        Phaser,
        Flanger,
        Resonator,
        PitchShifter,
        FrequencyShifter,
        Vocoder,
        Distortion,
        Amplifier,
        Disperser,
        Stereo,
        Channel,
        Modulator,
        Meter
    }

    private readonly MuekPluginType _pluginType = MuekPluginType.Equalizer;
    
    public MuekPlugin()
    {
        InitializeComponent();
        switch (_pluginType)
        {
            case MuekPluginType.Empty:
                InitEmpty();
                break;
            case MuekPluginType.Oscillator:
                InitOscillator();
                break;
            case MuekPluginType.Sampler:
                InitSampler();
                break;
            case MuekPluginType.Granular:
                InitGranular();
                break;
            case MuekPluginType.Equalizer:
                InitEqualizer();
                break;
        }
    }

    private void InitEmpty()
    {
        PluginColor.Background = Brushes.White;
        PluginName.Text = "Empty";
        var text = new TextBlock()
        {
            Text = "This is an empty plugin which will bypass the audio signal",
            Foreground = Brushes.DimGray
        };
        PluginContent.Child = text;
    }


    private void InitOscillator()
    {
        PluginColor.Background = DataStateService.MuekColorBrush;
        PluginName.Text = "Oscillator";
        var waveType = new ListBox()
        {
            SelectionMode = SelectionMode.AlwaysSelected,
            Background = Brushes.Transparent,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Items =
            {
                new Icon()
                {
                    Value = "mdi-sine-wave",
                },
                new Icon()
                {
                    Value = "mdi-sawtooth-wave"
                },
                new Icon()
                {
                    Value = "mdi-square-wave"
                }
            }
        };
        var osc = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("auto,auto,*"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = 8,
            Children =
            {
                new Border
                {
                    Width = 45,
                    Padding = Thickness.Parse("5"),
                    Background = Brush.Parse("#232323"),
                    Child = waveType
                },
            }
        };
        var attack = new Slider()
        {
            Width = 50,
            Height = 10,
            Minimum = 0,
            Maximum = 5000,
            Value = 500,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var decay = new Slider()
        {
            Width = 50,
            Height = 10,
            Minimum = 0,
            Maximum = 5000,
            Value = 1000,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var sustain = new Slider()
        {
            Width = 50,
            Height = 10,
            Minimum = 0,
            Maximum = 1,
            Value = 0.8,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var release = new Slider()
        {
            Width = 50,
            Height = 10,
            Minimum = 0,
            Maximum = 5000,
            Value = 500,
            Foreground = DataStateService.MuekColorBrush,
            Styles =
            {
                new NeumorphismTheme()
            }
        };
        var adsr = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("auto,*"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ColumnSpacing = 8,
            Children =
            {
                new Border
                {
                    Padding = Thickness.Parse("5"),
                    Background = Brush.Parse("#232323"),
                    Child = new StackPanel()
                    {
                        Children =
                        {
                            new StackPanel()
                            {
                                Orientation = Orientation.Vertical,
                                Spacing = 10,
                                Children =
                                {
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Attack",
                                                FontSize = 10,
                                                Width = 50
                                            },
                                            attack
                                        }
                                    },
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Decay",
                                                FontSize = 10,
                                                Width = 50
                                            },
                                            decay
                                        }
                                    },
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Sustain",
                                                FontSize = 10,
                                                Width = 50
                                            },
                                            sustain
                                        }
                                    },
                                    new StackPanel()
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Label()
                                            {
                                                Content = "Release",
                                                FontSize = 10,
                                                Width = 50
                                            },
                                            release
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            }
        };
        var adsrWave = new Polyline()
        {
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            MinWidth = 200,
        };
        var adsrFill = new Polygon()
        {
            Fill = new SolidColorBrush(DataStateService.MuekColor, 0.1),
            MinWidth = 200,
        };
        
        UpdateAdsr();
        attack.ValueChanged += (sender, args) => { UpdateAdsr(); };
        decay.ValueChanged += (sender, args) => { UpdateAdsr(); };
        sustain.ValueChanged += (sender, args) => { UpdateAdsr(); };
        release.ValueChanged += (sender, args) => { UpdateAdsr(); };

        void UpdateAdsr()
        {
            adsrWave.Points =
            [
                new Point(0, 80),
                new Point(attack.Value / 50, 0),
                new Point((attack.Value + decay.Value) / 50, 80 - 80 * sustain.Value),
                new Point((attack.Value + decay.Value + release.Value) / 50, 80)
            ];
            adsrFill.Points = adsrWave.Points;
        }
        
        var adsrBorder = new Border()
        {
            BorderBrush = Brush.Parse("#232323"),
            BorderThickness = Thickness.Parse("4"),
            Child = new Viewbox()
            {
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new Grid()
                {
                    Children =
                    {
                        new Line()
                        {
                            StartPoint  = new Point(1000/50d,0),
                            EndPoint = new Point(1000/50d,80),
                            Stroke = Brush.Parse("#15ffffff"),
                            StrokeThickness = 1,
                            StrokeDashArray = [10,5]
                        },
                        new Line()
                        {
                            StartPoint  = new Point(2000/50d,0),
                            EndPoint = new Point(2000/50d,80),
                            Stroke = Brush.Parse("#15ffffff"),
                            StrokeThickness = 1,
                            StrokeDashArray = [10,5]
                        },
                        new Line()
                        {
                            StartPoint  = new Point(5000/50d,0),
                            EndPoint = new Point(5000/50d,80),
                            Stroke = Brush.Parse("#15ffffff"),
                            StrokeThickness = 1,
                            StrokeDashArray = [10,5]
                        },
                        adsrWave,
                        adsrFill,
                        
                    }
                }
            }
        };
        var grid = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("auto,auto"),
            RowSpacing = 8,
            
        };
        adsr.Children.Add(adsrBorder);
        Grid.SetColumn(adsrBorder,1);
        grid.Children.Add(osc);
        grid.Children.Add(adsr);
        Grid.SetRow(adsr, 1);
        var pitch = new NumericUpDown()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Minimum = -24,
            Maximum = 24,
            Value = 0,
            Increment = 1,
            Height = 25,
            Width = 100,
            Padding = Thickness.Parse("0"),
            BorderBrush = Brushes.Transparent,
            Styles =
            {
                new SimpleTheme()
            },
            CornerRadius = CornerRadius.Parse("5")
        };
        var cents = new NumericUpDown()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Minimum = -50,
            Maximum = 50,
            Value = 0,
            Increment = 1,
            Height = 25,
            Width = 100,
            Padding = Thickness.Parse("0"),
            BorderBrush = Brushes.Transparent,
            Styles =
            {
                new SimpleTheme()
            },
            CornerRadius = CornerRadius.Parse("5")
        };
        var pan = new Slider
        {
            Foreground = DataStateService.MuekColorBrush,
            Minimum = -1,
            Maximum = 1,
            Value = 0,
            Width = 100,
            Height = 8,
            TickPlacement = TickPlacement.TopLeft,
            TickFrequency = 1,
            Styles =
            {
                new NeumorphismTheme()
            },
            Margin = Thickness.Parse("0 0 0 20")
        };
        var volume = new Slider
        {
            Foreground = DataStateService.MuekColorBrush,
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            Width = 100,
            Height = 8,
            TickPlacement = TickPlacement.TopLeft,
            TickFrequency = 10,
            Styles =
            {
                new NeumorphismTheme()
            },
            Margin = Thickness.Parse("0 0 0 20")
        };
        var settingsBorder = new Border()
        {
            Background = Brush.Parse("#232323"),
            Padding = Thickness.Parse("5"),
            Child = new StackPanel()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Pitch",
                    },
                    pitch,
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Cents",
                    },
                    cents,
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Pan",
                    },
                    pan,
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Volume",
                    },
                    volume,
                }
            }
        };
        var wave = new Polyline()
        {
            Height = 100,
            Width = 100,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
        };
        var waveFill = new Polygon()
        {
            Fill = new SolidColorBrush(DataStateService.MuekColor, 0.1),
            Height = 100,
            Width = 100,
        };
        WavetableChange();
        waveType.SelectionChanged += (sender, args) =>
        {
            WavetableChange();
        };
        volume.ValueChanged += (sender, args) =>
        {
            WavetableChange();
        };

        void WavetableChange()
        {
            switch (waveType.SelectedIndex)
            {
                case 0:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i, -50 * volume.Value / 100d * Math.Sin(2 * Math.PI * i / 100) + 50)
                    ).ToList();
                    break;
                case 1:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i, 100-(i + 50)%100 * volume.Value / 100d - 50 * (100-volume.Value) / 100d)
                    ).ToList();
                    break;
                case 2:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i,i is 0 or 100 ? 50 : i < 50 ? 0 + (50-volume.Value/2d) : 50 + volume.Value/2d)
                    ).ToList();
                    break;
            }
            waveFill.Points = wave.Points;
            
        }

        var viewBorder = new Border()
        {
            BorderThickness = Thickness.Parse("4"),
            BorderBrush = Brush.Parse("#232323"),
            Child = new Viewbox()
            {
                ClipToBounds = false,
                Height = 120,
                StretchDirection = StretchDirection.UpOnly,
                Stretch = Stretch.Fill,
                Child = new Grid()
                {
                    Children =
                    {
                        new Line()
                        {
                            StartPoint = new Point(0,50),
                            EndPoint = new Point(100,50),
                            Stroke = Brush.Parse("#15ffffff"),
                            StrokeThickness = 0.5,
                            StrokeDashArray = [20,5]
                        },
                        new Line()
                        {
                            StartPoint = new Point(0,0),
                            EndPoint = new Point(100,0),
                            Stroke = Brush.Parse("#15ffffff"),
                            StrokeThickness = 0.5,
                            StrokeDashArray = [20,5]
                        },
                        new Line()
                        {
                            StartPoint = new Point(0,100),
                            EndPoint = new Point(100,100),
                            Stroke = Brush.Parse("#15ffffff"),
                            StrokeThickness = 0.5,
                            StrokeDashArray = [20,5]
                        },
                        waveFill,
                        wave
                    }
                }
            }
        };
        osc.Children.Add(settingsBorder);
        osc.Children.Add(viewBorder);
        Grid.SetColumn(settingsBorder, 1);
        Grid.SetColumn(viewBorder, 2);
        PluginContent.Child = grid;
    }

    private void InitSampler()
    {
        PluginName.Text = "Sampler";
        var grid = new Grid()
        {
            ColumnDefinitions = ColumnDefinitions.Parse("auto,auto,*")
        };
        var border = new Border()
        {
            Height = 50,
            Width = 50,
            Background = Brush.Parse("#232323"),
            Margin = Thickness.Parse("0 0 8 0"),
        };
        grid.Children.Add(border);
        PluginContent.Child = grid;
        throw new NotImplementedException();
    }

    private void InitGranular()
    {
        throw new NotImplementedException();
    }
    
    
    private void InitEqualizer()
    {
        PluginName.Text = "Equalizer";
        PluginColor.Background = Brushes.DeepSkyBlue;
        var maximum = 48;
        var minimum = -maximum;
        var point1Level = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Height = 100,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = 0,
            Width = 5,
        };
        var point2Level = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Height = 100,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = 0,
            Width = 5,
        };
        var point3Level = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Height = 100,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = 0,
            Width = 5,
        };
        var point4Level = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Height = 100,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = 0,
            Width = 5,
        };
        var point5Level = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Height = 100,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = 0,
            Width = 5,
        };
        var point6Level = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Slider,
            Height = 100,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = 0,
            Width = 5,
        };
        var point1Freq = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Knob,
            Minimum = 0,
            Maximum = 700,
            Width = 15,
            Height = 15,
            DefaultValue = 100,
        };
        var point2Freq = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Knob,
            Minimum = 0,
            Maximum = 700,
            Width = 15,
            Height = 15,
            DefaultValue = 200,
        };
        var point3Freq = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Knob,
            Minimum = 0,
            Maximum = 700,
            Width = 15,
            Height = 15,
            DefaultValue = 300,
        };
        var point4Freq = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Knob,
            Minimum = 0,
            Maximum = 700,
            Width = 15,
            Height = 15,
            DefaultValue = 400,
        };
        var point5Freq = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Knob,
            Minimum = 0,
            Maximum = 700,
            Width = 15,
            Height = 15,
            DefaultValue = 500,
        };
        var point6Freq = new MuekValuer()
        {
            ValuerColor = Brushes.DeepSkyBlue,
            Layout = MuekValuer.LayoutEnum.Knob,
            Minimum = 0,
            Maximum = 700,
            Width = 15,
            Height = 15,
            DefaultValue = 600,
        };
        var parameters = new Border()
        {
            Padding = Thickness.Parse("8"),
            Background = Brush.Parse("#232323"),
            Child = new StackPanel()
            {
                Children =
                {
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 40,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            new StackPanel()
                            { 
                                Spacing = 12,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children = { 
                                       point1Level,
                                       point1Freq
                                   }
                            },
                            new StackPanel()
                            { 
                                Spacing = 12,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children = { 
                                    point2Level,
                                    point2Freq
                                }
                            },
                            new StackPanel()
                            { 
                                Spacing = 12,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children = { 
                                    point3Level,
                                    point3Freq
                                }
                            },
                            new StackPanel()
                            { 
                                Spacing = 12,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children = { 
                                    point4Level,
                                    point4Freq
                                }
                            },
                            new StackPanel()
                            { 
                                Spacing = 12,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children = { 
                                    point5Level,
                                    point5Freq
                                }
                            },
                            new StackPanel()
                            { 
                                Spacing = 12,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Children = { 
                                    point6Level,
                                    point6Freq
                                }
                            },
                        }
                    }
                }
            }
        };
        
        var curve = new Polyline()
        {
            Stroke = Brushes.DeepSkyBlue,
            Height = 100,
        };
        var point1 = new Ellipse()
        {
            Fill = Brushes.DeepSkyBlue,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var point2 = new Ellipse()
        {
            Fill = Brushes.DeepSkyBlue,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var point3 = new Ellipse()
        {
            Fill = Brushes.DeepSkyBlue,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var point4 = new Ellipse()
        {
            Fill = Brushes.DeepSkyBlue,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var point5 = new Ellipse()
        {
            Fill = Brushes.DeepSkyBlue,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var point6 = new Ellipse()
        {
            Fill = Brushes.DeepSkyBlue,
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };

        UpdateCurve();
        point1Level.ValueChanged += (sender, args) => { UpdateCurve(); };
        point2Level.ValueChanged += (sender, args) => { UpdateCurve(); };
        point3Level.ValueChanged += (sender, args) => { UpdateCurve(); };
        point4Level.ValueChanged += (sender, args) => { UpdateCurve(); };
        point5Level.ValueChanged += (sender, args) => { UpdateCurve(); };
        point6Level.ValueChanged += (sender, args) => { UpdateCurve(); };
        point1Freq.ValueChanged += (sender, args) => { UpdateCurve(); };
        point2Freq.ValueChanged += (sender, args) => { UpdateCurve(); };
        point3Freq.ValueChanged += (sender, args) => { UpdateCurve(); };
        point4Freq.ValueChanged += (sender, args) => { UpdateCurve(); };
        point5Freq.ValueChanged += (sender, args) => { UpdateCurve(); };
        point6Freq.ValueChanged += (sender, args) => { UpdateCurve(); };

        var point1Pressed = false;
        var point2Pressed = false;
        var point3Pressed = false;
        var point4Pressed = false;
        var point5Pressed = false;
        var point6Pressed = false;
        point1.PointerPressed += (sender, args) => { point1Pressed = true; };
        point2.PointerPressed += (sender, args) => { point2Pressed = true; };
        point3.PointerPressed += (sender, args) => { point3Pressed = true; };
        point4.PointerPressed += (sender, args) => { point4Pressed = true; };
        point5.PointerPressed += (sender, args) => { point5Pressed = true; };
        point6.PointerPressed += (sender, args) => { point6Pressed = true; };
        
        point1.PointerMoved += (sender, args) =>
        {
            if(!point1Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point1Freq.Value = position.X;
            point1Level.Value = -position.Y + 50;
        };
        point2.PointerMoved += (sender, args) =>
        {
            if (!point2Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point2Freq.Value = position.X;
            point2Level.Value = -position.Y + 50;
        };
        point3.PointerMoved += (sender, args) =>
        {
            if (!point3Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point3Freq.Value = position.X;
            point3Level.Value = -position.Y + 50;
        };
        point4.PointerMoved += (sender, args) =>
        {
            if (!point4Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point4Freq.Value = position.X;
            point4Level.Value = -position.Y + 50;
        };
        point5.PointerMoved += (sender, args) =>
        {
            if (!point5Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point5Freq.Value = position.X;
            point5Level.Value = -position.Y + 50;
        };
        point6.PointerMoved += (sender, args) =>
        {
            if (!point6Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point6Freq.Value = position.X;
            point6Level.Value = -position.Y + 50;
        };
        
        
        point1.PointerReleased += (sender, args) => { point1Pressed = false; };
        point2.PointerReleased += (sender, args) => { point2Pressed = false; };
        point3.PointerReleased += (sender, args) => { point3Pressed = false; };
        point4.PointerReleased += (sender, args) => { point4Pressed = false; };
        point5.PointerReleased += (sender, args) => { point5Pressed = false; };
        point6.PointerReleased += (sender, args) => { point6Pressed = false; };

        var view = new Border()
        {
            Height = 150,
            BorderBrush = Brush.Parse("#232323"),
            BorderThickness = Thickness.Parse("4"),
            Child = new Viewbox()
            {
                ClipToBounds = false,
                Child = new Grid()
                {
                    Children =
                    {
                        curve,
                        point1,
                        point2,
                        point3,
                        point4,
                        point5,
                        point6,
                    }
                }
            },
            
        };
        
        void UpdateCurve()
        {
            var q = 100;
            var points = Enumerable.Range(0, 700)
                .Select(i =>
                {
                    var pos = 50d;
                    if(Math.Abs(i - point1Freq.Value) < q)
                        pos -= point1Level.Value * (q - Math.Abs(i - point1Freq.Value)) / q;
                    if (Math.Abs(i - point2Freq.Value) < q)
                        pos -=  point2Level.Value * (q - Math.Abs(i - point2Freq.Value)) / q;
                    if (Math.Abs(i - point3Freq.Value) < q)
                        pos -=  point3Level.Value * (q - Math.Abs(i - point3Freq.Value)) / q;
                    if (Math.Abs(i - point4Freq.Value) < q)
                        pos -= point4Level.Value * (q - Math.Abs(i - point4Freq.Value)) / q;
                    if (Math.Abs(i - point5Freq.Value) < q)
                        pos -= point5Level.Value * (q - Math.Abs(i - point5Freq.Value)) / q;
                    if (Math.Abs(i - point6Freq.Value) < q)
                        pos -= point6Level.Value * (q - Math.Abs(i - point6Freq.Value)) / q;
                    return new Point(i, pos);
                }).ToList();
            point1.Margin = new Thickness(point1Freq.Value-10, -point1Level.Value+40, 0, 0);
            point2.Margin = new Thickness(point2Freq.Value-10, -point2Level.Value+40, 0, 0);
            point3.Margin = new Thickness(point3Freq.Value-10, -point3Level.Value+40, 0, 0);
            point4.Margin = new Thickness(point4Freq.Value-10, -point4Level.Value+40, 0, 0);
            point5.Margin = new Thickness(point5Freq.Value-10, -point5Level.Value+40, 0, 0);
            point6.Margin = new Thickness(point6Freq.Value-10, -point6Level.Value+40, 0, 0);
            curve.Points = points;
        }
        PluginContent.Child = new Grid()
        {
            RowDefinitions = RowDefinitions.Parse("*,auto"),
            RowSpacing = 8,
            Children =
            {
                view,
                parameters
            }
        };
        Grid.SetRow(parameters, 1);
    }
}