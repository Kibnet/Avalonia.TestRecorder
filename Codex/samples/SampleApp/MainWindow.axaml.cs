using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace SampleApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainViewModel ViewModel => (MainViewModel?)DataContext ?? throw new InvalidOperationException("DataContext is not set.");

    private void OnHoverEnter(object? sender, PointerEventArgs e)
    {
        ViewModel.SetHover("Курсор над блоком");
    }

    private void OnHoverLeave(object? sender, PointerEventArgs e)
    {
        ViewModel.SetHover("Курсор ушел");
    }
}
