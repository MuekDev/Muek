using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Muek.Models;
using Muek.Services;

namespace Muek.ViewModels;

public partial class PatternSelectionViewModel : ViewModelBase
{
    public ObservableCollection<PatternViewModel> Patterns { get; set; } = [];

    [RelayCommand]
    public void AddPattern()
    {
        Patterns.Add(new PatternViewModel());
    }
}