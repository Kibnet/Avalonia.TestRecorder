using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SampleApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        // Add some initial items to the dynamic list
        DynamicItems.Add("Initial Item 1");
        DynamicItems.Add("Initial Item 2");
    }

    // Original properties
    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Basic Controls Tab properties
    [ObservableProperty]
    private string _basicStatusMessage = "Form ready";

    // Advanced Controls Tab properties
    [ObservableProperty]
    private double _sliderValue = 50;

    // Interactive Controls Tab properties
    [ObservableProperty]
    private int _selectedCategoryIndex = -1;

    [ObservableProperty]
    private bool _isSubCategoryEnabled = false;

    [ObservableProperty]
    private bool _isSecretVisible = false;

    [ObservableProperty]
    private ObservableCollection<string> _dynamicItems = new();

    // For sub-category items
    [ObservableProperty]
    private ObservableCollection<string> _subCategoryItems = new();

    // Counter for button clicks
    [ObservableProperty]
    private int _clickCounter = 0;

    // Commands
    [RelayCommand]
    private void Login()
    {
        StatusMessage = "Login successful";
    }

    [RelayCommand]
    private void SubmitBasicForm()
    {
        BasicStatusMessage = "Form submitted successfully";
    }

    [RelayCommand]
    private void AddNewItem()
    {
        DynamicItems.Add($"Item {DynamicItems.Count + 1}");
    }

    [RelayCommand]
    private void IncrementCounter()
    {
        ClickCounter++;
    }

    // Handle category selection change
    partial void OnSelectedCategoryIndexChanged(int value)
    {
        IsSubCategoryEnabled = value >= 0;
        
        // Populate sub-categories based on the selected category
        SubCategoryItems.Clear();
        
        switch (value)
        {
            case 0: // Electronics
                SubCategoryItems.Add("Phones");
                SubCategoryItems.Add("Laptops");
                SubCategoryItems.Add("Tablets");
                break;
            case 1: // Clothing
                SubCategoryItems.Add("Shirts");
                SubCategoryItems.Add("Pants");
                SubCategoryItems.Add("Shoes");
                break;
            case 2: // Books
                SubCategoryItems.Add("Fiction");
                SubCategoryItems.Add("Non-Fiction");
                SubCategoryItems.Add("Educational");
                break;
        }
    }
}