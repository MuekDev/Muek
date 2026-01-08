using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Neumorphism;
using Avalonia.Themes.Simple;
using Muek.Services;
using Projektanker.Icons.Avalonia;

namespace Muek.Views;

public partial class MuekPlugin : UserControl
{
    public enum MuekPluginType
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

    private MuekPluginType PluginType { get; } = MuekPluginType.Oscillator;

    private readonly BoxShadows _boxShadows = BoxShadows.Parse("0 0 10 #000000");
    private readonly BoxShadows _insetBoxShadow = BoxShadows.Parse("inset 0 0 10 #88000000");
    private readonly CornerRadius _cornerRadius = CornerRadius.Parse("4");

    private readonly IBrush _stroke = Brush.Parse("#88000000");
    private readonly Thickness _thickness = Thickness.Parse("4");
    
    public MuekPlugin()
    {
        InitializeComponent();
        SwitchPluginType();
    }
    public MuekPlugin(MuekPluginType pluginType)
    {
        PluginType = pluginType;
        InitializeComponent();
        SwitchPluginType();
    }

    private void SwitchPluginType()
    {
        switch (PluginType)
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
                    CornerRadius = _cornerRadius,
                    BoxShadow = _boxShadows,
                    Child = waveType
                },
            }
        };
        var adsrWidth = 50;
        var attack = new Slider()
        {
            Width = adsrWidth,
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
            Width = adsrWidth,
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
            Width = adsrWidth * 1.5,
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
            Width = adsrWidth,
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
        var attackText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = attack.Value.ToString("0.0"),
        };
        var decayText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = decay.Value.ToString("0.0"),
        };
        var sustainText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = sustain.Value.ToString("0.0"),
        };
        var releaseText = new TextBox()
        {
            Width = 80,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = release.Value.ToString("0.0"),
        };
        
        attackText.TextChanged += (s, e) =>
        {
            attack.Value = double.Parse(attackText.Text);
            attackText.Text = attack.Value.ToString("0.0");
        };
        decayText.TextChanged += (s, e) =>
        {
            decay.Value = double.Parse(decayText.Text);
            decayText.Text = decay.Value.ToString("0.0");
        };
        sustainText.TextChanged += (s, e) =>
        {
            sustain.Value = double.Parse(sustainText.Text);
            sustainText.Text = sustain.Value.ToString("0.0");
        };
        releaseText.TextChanged += (s, e) =>
        {
            release.Value = double.Parse(releaseText.Text);
            releaseText.Text = release.Value.ToString("0.0");
        };


        var attackBend = new MuekValuer()
        {
            ValuerColor = DataStateService.MuekColorBrush,
            Layout = MuekValuer.LayoutEnum.Knob,
            Width = 15,
            Height = 15,
            Margin = Thickness.Parse("10"),
            Minimum = -1,
            Maximum = 1,
            DefaultValue = 0,
        };
        var decayBend = new MuekValuer()
        {
            ValuerColor = DataStateService.MuekColorBrush,
            Layout = MuekValuer.LayoutEnum.Knob,
            Width = 15,
            Height = 15,
            Margin = Thickness.Parse("10"),
            Minimum = -1,
            Maximum = 1,
            DefaultValue = 0,
        };
        var releaseBend = new MuekValuer()
        {
            ValuerColor = DataStateService.MuekColorBrush,
            Layout = MuekValuer.LayoutEnum.Knob,
            Width = 15,
            Height = 15,
            Margin = Thickness.Parse("10"),
            Minimum = -1,
            Maximum = 1,
            DefaultValue = 0,
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
                    CornerRadius = _cornerRadius,
                    BoxShadow = _boxShadows,
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
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            attack,
                                            attackText,
                                            attackBend
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
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            decay,
                                            decayText,
                                            decayBend
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
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            sustain,
                                            sustainText
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
                                                Width = 50,
                                                VerticalAlignment = VerticalAlignment.Center
                                            },
                                            release,
                                            releaseText,
                                            releaseBend
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
            }
        };
        var waveHeight = 150;
        var adsrWave = new Polyline()
        {
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            Height = waveHeight
        };
        var adsrFill = new Polygon()
        {
            Fill = new SolidColorBrush(DataStateService.MuekColor, 0.1),
            Height = waveHeight
        };


        var radius = 6;

        var attackPoint = new Ellipse()
        {
            Fill = DataStateService.MuekColorBrush,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
            Margin = new Thickness(attack.Value/50-radius/2d,-radius/2d,0,0),
        };

        
        var sustainPoint = new Ellipse()
        {
            Fill = DataStateService.MuekColorBrush,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
            Margin = new Thickness((attack.Value + decay.Value)/50-radius/2d,waveHeight - waveHeight * sustain.Value - radius/2d,0,0),
        };

        var releasePoint = new Ellipse()
        {
            Fill = DataStateService.MuekColorBrush,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
            Margin = new Thickness((attack.Value + decay.Value + release.Value)/50-radius/2d,0,0,-radius/2d),
        };

        var attackPointPressed = false;

        var sustainPointPressed = false;

        var releasePointPressed = false;

        attackPoint.PointerPressed += (sender, args) => { attackPointPressed = true; args.Handled = true; };

        attackPoint.PointerReleased += (sender, args) => { attackPointPressed = false; args.Handled = true; };

        sustainPoint.PointerPressed += (sender, args) => { sustainPointPressed = true; args.Handled = true; };

        sustainPoint.PointerReleased += (sender, args) => { sustainPointPressed = false; args.Handled = true; };

        releasePoint.PointerPressed += (sender, args) => { releasePointPressed = true; args.Handled = true; };

        releasePoint.PointerReleased += (sender, args) => { releasePointPressed = false; args.Handled = true; };

        attackPoint.PointerMoved += (sender, args) =>
        {
            if (!attackPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            attack.Value = position.X * 50;
            args.Handled = true;
        };

        sustainPoint.PointerMoved += (sender, args) =>
        {
            if (!sustainPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            decay.Value = position.X * 50 - attack.Value;
            sustain.Value = (waveHeight - position.Y) / waveHeight;
            args.Handled = true;
        };
        
        releasePoint.PointerMoved += (sender, args) =>
        {
            if (!releasePointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            release.Value = position.X * 50 - attack.Value - decay.Value;
            args.Handled = true;
        };

        attackPoint.PointerEntered += (sender, args) =>
        {
            attackPoint.Fill = Brushes.White;
            args.Handled = true;
        };
        sustainPoint.PointerEntered += (sender, args) =>
        {
            sustainPoint.Fill = Brushes.White;
            args.Handled = true;
        };
        releasePoint.PointerEntered += (sender, args) =>
        {
            releasePoint.Fill = Brushes.White;
            args.Handled = true;
        };
        attackPoint.PointerExited += (sender, args) =>
        {
            attackPoint.Fill = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        sustainPoint.PointerExited += (sender, args) =>
        {
            sustainPoint.Fill = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        releasePoint.PointerExited += (sender, args) =>
        {
            releasePoint.Fill = DataStateService.MuekColorBrush;
            args.Handled = true;
        };

        var attackBendPoint = new Ellipse()
        {
            Fill = Brushes.Transparent,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
        };
        var sustainBendPoint = new Ellipse()
        {
            Fill = Brushes.Transparent,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
        };
        var releaseBendPoint = new Ellipse()
        {
            Fill = Brushes.Transparent,
            Stroke = DataStateService.MuekColorBrush,
            StrokeThickness = 1,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Height = radius,
            Width = radius,
        };

        var attackBendPointPressed = false;
        var sustainBendPointPressed = false;
        var releaseBendPointPressed = false;

        Point attackBendPointPressedPosition = default;
        Point sustainBendPointPressedPosition = default;
        Point releaseBendPointPressedPosition = default;

        double attackBendPressedValue = attackBend.Value;
        double sustainBendPressedValue = decayBend.Value;
        double releaseBendPressedValue = releaseBend.Value;
        
        attackBendPoint.PointerPressed += (sender, args) =>
        {
            attackBendPointPressed = true;
            attackBendPointPressedPosition = args.GetPosition((sender as Visual)!.Parent as Visual);
            attackBendPressedValue = attackBend.Value;
            args.Handled = true;
        };
        sustainBendPoint.PointerPressed += (sender, args) =>
        {
            sustainBendPointPressed = true;
            sustainBendPointPressedPosition = args.GetPosition((sender as Visual)!.Parent as Visual);
            sustainBendPressedValue = decayBend.Value;
            args.Handled = true;
        };
        releaseBendPoint.PointerPressed += (sender, args) =>
        {
            releaseBendPointPressed = true;
            releaseBendPointPressedPosition = args.GetPosition((sender as Visual)!.Parent as Visual);
            releaseBendPressedValue = releaseBend.Value;
            args.Handled = true;
        };
        attackBendPoint.PointerReleased += (sender, args) =>
        {
            attackBendPointPressed = false;
            args.Handled = true;
        };
        sustainBendPoint.PointerReleased += (sender, args) =>
        {
            sustainBendPointPressed = false;
            args.Handled = true;
        };
        releaseBendPoint.PointerReleased += (sender, args) =>
        {
            releaseBendPointPressed = false;
            args.Handled = true;
        };
        
        attackBendPoint.PointerMoved += (sender, args) =>
        {
            if (!attackBendPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            var offset = (position - attackBendPointPressedPosition).Y;
            attackBend.Value = attackBendPressedValue - offset / waveHeight;
            args.Handled = true;
        };
        sustainBendPoint.PointerMoved += (sender, args) =>
        {
            if (!sustainBendPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            var offset = (position - sustainBendPointPressedPosition).Y;
            decayBend.Value = sustainBendPressedValue - offset / waveHeight;
            args.Handled = true;
        };
        releaseBendPoint.PointerMoved += (sender, args) =>
        {
            if (!releaseBendPointPressed) return;
            var position = args.GetPosition((sender as Visual)!.Parent as Visual);
            var offset = (position - releaseBendPointPressedPosition).Y;
            releaseBend.Value = releaseBendPressedValue - offset / waveHeight;
            args.Handled = true;
        };

        attackBendPoint.PointerEntered += (sender, args) =>
        {
            attackBendPoint.Stroke = Brushes.White;
            args.Handled = true;
        };
        sustainBendPoint.PointerEntered += (sender, args) =>
        {
            sustainBendPoint.Stroke = Brushes.White;
            args.Handled = true;
        };
        releaseBendPoint.PointerEntered += (sender, args) =>
        {
            releaseBendPoint.Stroke = Brushes.White;
            args.Handled = true;
        };
        attackBendPoint.PointerExited += (sender, args) =>
        {
            attackBendPoint.Stroke = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        sustainBendPoint.PointerExited += (sender, args) =>
        {
            sustainBendPoint.Stroke = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        releaseBendPoint.PointerExited += (sender, args) =>
        {
            releaseBendPoint.Stroke = DataStateService.MuekColorBrush;
            args.Handled = true;
        };
        

        UpdateAdsr();
        attack.ValueChanged += (sender, args) =>
        {
            UpdateAdsr();
            attackText.Text = attack.Value.ToString("0.0");
        };
        decay.ValueChanged += (sender, args) =>
        {
            UpdateAdsr();
            decayText.Text = decay.Value.ToString("0.0");
        };
        sustain.ValueChanged += (sender, args) =>
        {
            UpdateAdsr(); 
            sustainText.Text = sustain.Value.ToString("0.0");
        };
        release.ValueChanged += (sender, args) =>
        {
            UpdateAdsr(); 
            releaseText.Text = release.Value.ToString("0.0");
        };

        attackBend.ValueChanged += (sender, args) => { UpdateAdsr(); };
        decayBend.ValueChanged += (sender, args) => { UpdateAdsr(); };
        releaseBend.ValueChanged += (sender, args) => { UpdateAdsr(); };


        void UpdateAdsr()
        {
            Point[] points = 
            [
                new(0, waveHeight),
                new(attack.Value / 50, 0),
                new((attack.Value + decay.Value) / 50, waveHeight - waveHeight * sustain.Value),
                new((attack.Value + decay.Value + release.Value) / 50, waveHeight)
            ];

            var bendCount = 20;

            var attackPoints = Enumerable.Range(1, bendCount).Select(i =>
            {
                var index = (double)i /  bendCount;
                var x = index * attack.Value / 50;
                var value = -attackBend.Value * 10;
                var y = value == 0 ? index
                    :(double.Exp(value * index) - 1) / (double.Exp(value) - 1);
                var point = new Point(x, waveHeight - waveHeight * y);
                return point;
            }).ToList();
            var decayPoints = Enumerable.Range(1, bendCount).Select(i =>
            {
                var index = (double)i /  bendCount;
                var x = attack.Value / 50 + index * decay.Value / 50;
                var value = decayBend.Value * 10;
                var y = value == 0 ? index
                    :(double.Exp(value * index) - 1) / (double.Exp(value) - 1);
                var point = new Point(x, (1 - sustain.Value) * waveHeight * y);
                return point;
            }).ToList();
            var releasePoints = Enumerable.Range(1, bendCount).Select(i =>
            {
                var index = (double)i /  bendCount;
                var x = (attack.Value + decay.Value) / 50 + index * release.Value / 50;
                var value = releaseBend.Value * 10;
                var y = value == 0 ? index
                    :(double.Exp(value * index) - 1) / (double.Exp(value) - 1);
                var point = new Point(x, (1 - sustain.Value) * waveHeight + sustain.Value * waveHeight * y);
                return point;
            }).ToList();
            
            adsrWave.Points =
                new List<Point>()
                {
                    points[0]
                }
                .Concat(
                    attackPoints)
                .Concat(new List<Point>()
                {
                    points[1]
                })
                .Concat(
                    decayPoints)
                .Concat(new List<Point>() 
                {
                    points[2] 
                })
                .Concat(
                    releasePoints)
                .Concat(new List<Point>() 
                {
                    points[3] 
                }).ToList();
            adsrFill.Points = adsrWave.Points;
            
            attackPoint.Margin = new Thickness(attack.Value / 50 - radius / 2d, -radius / 2d, 0, 0);
            sustainPoint.Margin = new Thickness((attack.Value + decay.Value) / 50 - radius / 2d,
                waveHeight - waveHeight * sustain.Value - radius/2d, 0, 0);
            releasePoint.Margin =
                new Thickness((attack.Value + decay.Value + release.Value) / 50 - radius / 2d, 0, 0, -radius/2d);

            var attackBendPointPos = attackPoints[attackPoints.Count / 2 - 1];
            var sustainBendPointPos = decayPoints[decayPoints.Count / 2 - 1];
            var releaseBendPointPos = releasePoints[releasePoints.Count / 2 - 1];
            attackBendPoint.Margin =
                new Thickness(attackBendPointPos.X - radius / 2d, attackBendPointPos.Y - radius / 2d, 0, 0);
            sustainBendPoint.Margin = new Thickness(sustainBendPointPos.X - radius / 2d,
                sustainBendPointPos.Y - radius / 2d, 0, 0);
            releaseBendPoint.Margin = new Thickness(releaseBendPointPos.X - radius / 2d,
                releaseBendPointPos.Y - radius / 2d, 0, 0);
        }


        var adsrWaveBorder =
            new Grid()
            {
                Height = 150,
                Children =
                {
                    new Grid()
                    {
                        VerticalAlignment = VerticalAlignment.Top,
                        Children =
                        {
                            adsrWave,
                            adsrFill,
                    
                            attackBendPoint,
                            sustainBendPoint,
                            releaseBendPoint,

                            attackPoint,
                            sustainPoint,
                            releasePoint
                        }
                    }
                }
            };

        var adsrBorder = new Border()
        {
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Background = Brush.Parse("#232323"),
            Padding = _thickness,
            Child = new Grid()
            {
                Children =
                {
                    new Border()
                    {
                        Background = Brush.Parse("#23000000"),
                        Margin = Thickness.Parse("20 0 0 0"),
                        CornerRadius = _cornerRadius,
                        BoxShadow = _insetBoxShadow,
                        ClipToBounds = true,
                        Child = new Border()
                        {
                            VerticalAlignment = VerticalAlignment.Bottom,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Margin = new Thickness(0, 0, 0, radius / 2d),
                            ClipToBounds = false,
                            Child = adsrWaveBorder
                        }
                    },
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
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Minimum = -24,
            Maximum = 24,
            Value = 0,
            Increment = 1,
            Height = 25,
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
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Minimum = -50,
            Maximum = 50,
            Value = 0,
            Increment = 1,
            Height = 25,
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
            Height = 10,
            TickPlacement = TickPlacement.BottomRight,
            TickFrequency = 1,
            Styles =
            {
                new NeumorphismTheme()
            },
            Margin = Thickness.Parse("0 0 0 20"),
            Width = 100,
        };
        var level = new Slider
        {
            Foreground = DataStateService.MuekColorBrush,
            Minimum = 0,
            Maximum = 100,
            Value = 100,
            Height = 10,
            TickPlacement = TickPlacement.BottomRight,
            TickFrequency = 10,
            Styles =
            {
                new NeumorphismTheme()
            },
            Margin = Thickness.Parse("0 0 0 20"),
            Width = 100,
        };
        var panText = new TextBox()
        {
            Width = 50,
            Height = 10,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = pan.Value.ToString("0.0"),
            Margin = Thickness.Parse("0 -20 0 0")
        };
        panText.TextChanged += (_, _) =>
        {
            if (panText.Text == "-")
                panText.Text = "-0";
            pan.Value = double.Parse(panText.Text);
            panText.Text = pan.Value.ToString("0.0");
        };
        var levelText = new TextBox()
        {
            Width = 50,
            Height = 10,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Text = level.Value.ToString("0.0"),
            Margin = Thickness.Parse("0 -20 0 0")
        };
        level.ValueChanged += (_, _) =>
        {
            level.Value = double.Round(level.Value,2);
            levelText.Text = level.Value.ToString("0.0");
        };
        levelText.TextChanged += (_, _) =>
        {
            level.Value = double.Parse(levelText.Text);
            levelText.Text = level.Value.ToString("0.0");
        };

        var settingsBorder = new Border()
        {
            Background = Brush.Parse("#232323"),
            Padding = Thickness.Parse("5"),
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
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
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            pan,
                            panText
                        }
                    },
                    new Label()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 10,
                        Content = "Level",
                    },
                    new StackPanel()
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            level,
                            levelText
                        }
                    },
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
        var panFill = new Rectangle()
        {
            Fill = new LinearGradientBrush()
            {
                Opacity = 0.1,
                StartPoint = new RelativePoint(0,0,RelativeUnit.Relative),
                EndPoint = new RelativePoint(1,0, RelativeUnit.Relative),
                GradientStops = [
                    new GradientStop(DataStateService.MuekColor, 0),
                    new GradientStop(Colors.Transparent, 1)
                ]
            },
            Height = 100,
            Width = 0,
            Margin = Thickness.Parse("50 0 0 0"),
            HorizontalAlignment = HorizontalAlignment.Left,
            RenderTransform = new RotateTransform(0)
        };
        pan.ValueChanged += (_, _) =>
        {
            pan.Value = double.Clamp(pan.Value, -1, 1);
            pan.Value = double.Round(pan.Value, 2);
            panText.Text = pan.Value.ToString("0.0");
            panFill.Width = pan.Value == 0 ? 0 : 50;
            panFill.Fill = new LinearGradientBrush()
            {
                Opacity = 0.1,
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
                [
                    new GradientStop(DataStateService.MuekColor, 0),
                    new GradientStop(Colors.Transparent, double.Abs(pan.Value))
                ]
            };
            panFill.Margin = pan.Value >= 0 ? Thickness.Parse("50 0 0 0") : Thickness.Parse("0");
            panFill.RenderTransform = pan.Value >= 0 ? new RotateTransform(0) : new RotateTransform(180);
        };
        WavetableChange();
        waveType.SelectionChanged += (sender, args) =>
        {
            WavetableChange();
        };
        level.ValueChanged += (sender, args) =>
        {
            WavetableChange();
        };

        void WavetableChange()
        {
            switch (waveType.SelectedIndex)
            {
                case 0:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i, -50 * level.Value / 100d * Math.Sin(2 * Math.PI * i / 100) + 50)
                    ).ToList();
                    break;
                case 1:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i, 100-(i + 50)%100 * level.Value / 100d - 50 * (100-level.Value) / 100d)
                    ).ToList();
                    break;
                case 2:
                    wave.Points = Enumerable.Range(0, 101).Select(i =>
                        new Point(i,i is 0 or 100 ? 50 : i < 50 ? 0 + (50-level.Value/2d) : 50 + level.Value/2d)
                    ).ToList();
                    break;
            }
            waveFill.Points = wave.Points;
            
        }

        var waveBorder = new Border()
        {
            Background = Brush.Parse("#23000000"),
            CornerRadius = _cornerRadius,
            Margin = Thickness.Parse("20 0 0 0"),
            BoxShadow = _insetBoxShadow,
            ClipToBounds = true,
            Child = new Viewbox()
            {
                ClipToBounds = false,
                Height = 180,
                StretchDirection = StretchDirection.UpOnly,
                Stretch = Stretch.Fill,
                Child = new Grid()
                {
                    Children =
                    {
                        panFill,
                        waveFill,
                        wave
                    }
                }
            }
        };

        var waveBorderPressed = false;
        var waveBorderPressedPosition = new Point();
        var waveBorderPressedValue = level.Value;
        waveBorder.PointerPressed += (sender, args) =>
        {
            waveBorderPressed = true;
            waveBorderPressedPosition = args.GetPosition(sender as Visual);
            waveBorderPressedValue = level.Value;
            args.Handled = true;
        };
        waveBorder.PointerMoved += (sender, args) =>
        {
            if (!waveBorderPressed) return;
            var position = args.GetPosition(sender as Visual);
            level.Value = waveBorderPressedValue - position.Y + waveBorderPressedPosition.Y;
            args.Handled = true;
        };
        waveBorder.PointerReleased += (sender, args) =>
        {
            waveBorderPressed = false;
            args.Handled = true;
        };

        var waveReference = new Grid()
        {
            Children =
            {
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("8 94"),
                    EndPoint = Point.Parse("15 94")
                },
                new Label()
                {
                    Content  = "-inf",
                    Margin = Thickness.Parse("0 80 0 0"),
                    Foreground = Brush.Parse("#23ffffff"),
                    FontSize = 8
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("12 49"),
                    EndPoint = Point.Parse("15 49"),
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("12 139"),
                    EndPoint = Point.Parse("15 139"),
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("8 4"),
                    EndPoint = Point.Parse("15 4"),
                },
                new Label()
                {
                    Content  = "-6dB",
                    Margin = Thickness.Parse("-4 4 0 0"),
                    Foreground = Brush.Parse("#23ffffff"),
                    FontSize = 8
                },
                new Line()
                {
                    Stroke = Brush.Parse("#23ffffff"),
                    StartPoint = Point.Parse("8 184"),
                    EndPoint = Point.Parse("15 184"),
                },
                new Label()
                {
                    Content  = "-6dB",
                    Margin = Thickness.Parse("-4 170 0 0"),
                    Foreground = Brush.Parse("#23ffffff"),
                    FontSize = 8
                },
            }
        };
        
        var viewBorder = new Border()
        {
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Background = Brush.Parse("#232323"),
            Padding = _thickness,
            Child = new Grid(){
                Children =
                {
                    waveBorder,
                    waveReference
                }
            }
        };

        viewBorder.SizeChanged += (sender, args) =>
        {
            waveReference.IsVisible = viewBorder.Bounds.Width >= 20;
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
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
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
        point1.PointerPressed += (sender, args) => { point1Pressed = true; args.Handled = true; };
        point2.PointerPressed += (sender, args) => { point2Pressed = true; args.Handled = true; };
        point3.PointerPressed += (sender, args) => { point3Pressed = true; args.Handled = true; };
        point4.PointerPressed += (sender, args) => { point4Pressed = true; args.Handled = true; };
        point5.PointerPressed += (sender, args) => { point5Pressed = true; args.Handled = true; };
        point6.PointerPressed += (sender, args) => { point6Pressed = true; args.Handled = true; };
        
        point1.PointerMoved += (sender, args) =>
        {
            if(!point1Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point1Freq.Value = position.X;
            point1Level.Value = -position.Y + 50;
            args.Handled = true;
        };
        point2.PointerMoved += (sender, args) =>
        {
            if (!point2Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point2Freq.Value = position.X;
            point2Level.Value = -position.Y + 50;
            args.Handled = true;
        };
        point3.PointerMoved += (sender, args) =>
        {
            if (!point3Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point3Freq.Value = position.X;
            point3Level.Value = -position.Y + 50;
            args.Handled = true;
        };
        point4.PointerMoved += (sender, args) =>
        {
            if (!point4Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point4Freq.Value = position.X;
            point4Level.Value = -position.Y + 50;
            args.Handled = true;
        };
        point5.PointerMoved += (sender, args) =>
        {
            if (!point5Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point5Freq.Value = position.X;
            point5Level.Value = -position.Y + 50;
            args.Handled = true;
        };
        point6.PointerMoved += (sender, args) =>
        {
            if (!point6Pressed) return;
            var position = args.GetPosition(curve);
            position = new Point(double.Min(position.X,700-10), double.Min(position.Y,100-10));
            point6Freq.Value = position.X;
            point6Level.Value = -position.Y + 50;
            args.Handled = true;
        };
        
        
        point1.PointerReleased += (sender, args) => { point1Pressed = false; args.Handled = true; };
        point2.PointerReleased += (sender, args) => { point2Pressed = false; args.Handled = true; };
        point3.PointerReleased += (sender, args) => { point3Pressed = false; args.Handled = true; };
        point4.PointerReleased += (sender, args) => { point4Pressed = false; args.Handled = true; };
        point5.PointerReleased += (sender, args) => { point5Pressed = false; args.Handled = true; };
        point6.PointerReleased += (sender, args) => { point6Pressed = false; args.Handled = true; };

        var view = new Border()
        {
            Height = 150,
            Background = Brush.Parse("#232323"),
            Padding = _thickness,
            CornerRadius = _cornerRadius,
            BoxShadow = _boxShadows,
            Child = new Border(){
                Background = Brush.Parse("#23000000"),
                CornerRadius = _cornerRadius,
                BoxShadow = _insetBoxShadow,
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

    private void RemovePlugin(object? sender, RoutedEventArgs e)
    {
        var parent = Parent as StackPanel;
        parent?.Children.Remove(this);
    }
}