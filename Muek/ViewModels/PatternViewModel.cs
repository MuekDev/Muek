using System.Collections.Generic;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Models;
using Muek.Services;
using Muek.Views;

namespace Muek.ViewModels;

public partial class PatternViewModel : ViewModelBase
{
    [ObservableProperty] private Color _color;
    [ObservableProperty] private string _name;
    [ObservableProperty] private List<PianoRoll.Note> _notes;
    [ObservableProperty] private IBrush _background;

    public PatternViewModel()
    {
        _color = DataStateService.MuekColor;
        _name = "New Pattern";
        _notes = new();
        _background = new SolidColorBrush(Colors.Black, 0);
    }
    
    [RelayCommand]
    public void SelectPattern()
    {
        var pianoRoll =  ViewHelper.GetMainWindow().PianoRollWindow.EditArea;
        var button = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelectButton;
        var pattern = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelection;
        pianoRoll.Pattern = this;
        pianoRoll.InvalidateVisual();
        pianoRoll.SaveNotes();
        button.Content = Name;
    }
    
    [RelayCommand]
    public void RemovePattern()
    {
        var pianoRoll =  ViewHelper.GetMainWindow().PianoRollWindow.EditArea;
        var button = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelectButton;
        var pattern = ViewHelper.GetMainWindow().PianoRollWindow.PatternSelection;
        if(pianoRoll.Pattern == this)
        {
            pianoRoll.Pattern = null;
            pianoRoll.InvalidateVisual();
            pianoRoll.SaveNotes();
            button.Content = "Pattern Undefined";
        }
        pattern.ViewModel.Patterns.Remove(this);
    }
}