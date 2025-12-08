using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows.Input;

namespace SampleApp;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private string _status = "Готов к поиску";
    private string _hoverStatus = "Наведи курсор на синий блок";
    private string? _selectedItem;
    private string _recorderOutput = "./RecordedTests";

    public MainViewModel()
    {
        Items = new ObservableCollection<string>(Enumerable.Range(1, 40).Select(i => $"Элемент {i}"));
        SubmitCommand = new SimpleCommand(_ => Submit());
        ClearCommand = new SimpleCommand(_ => Clear());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> Items { get; }

    public ICommand SubmitCommand { get; }
    public ICommand ClearCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (value != _searchText)
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (value != _status)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public string HoverStatus
    {
        get => _hoverStatus;
        set
        {
            if (value != _hoverStatus)
            {
                _hoverStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public string? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value != _selectedItem)
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }
    }

    public string RecorderOutput
    {
        get => _recorderOutput;
        set
        {
            if (value != _recorderOutput)
            {
                _recorderOutput = value;
                OnPropertyChanged();
            }
        }
    }

    public void SetHover(string text) => HoverStatus = text;

    private void Submit()
    {
        Status = string.IsNullOrWhiteSpace(SearchText)
            ? "Введите текст перед поиском"
            : $"Поиск: {SearchText}";
    }

    private void Clear()
    {
        SearchText = string.Empty;
        Status = "Очищено";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal sealed class SimpleCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public SimpleCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
