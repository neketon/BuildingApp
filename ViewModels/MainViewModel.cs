using Azure;
using BuildingApp.Commands;
using BuildingApp.ViewModels;
using BuildingApp.Data;
using BuildingApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Material = BuildingApp.Models.Material;
using Operation = BuildingApp.Models.Operation;

namespace BuildingApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    // Один контекст БД на всё окно.
    private readonly AppDbContext _context = new();

    public ObservableCollection<Material> Materials { get; }
    public ObservableCollection<Operation> Operations { get; }
    public List<string> OperationTypes { get; } = new() { "Приход", "Расход" };

    // ===== Выбранный материал (двусторонняя привязка к DataGrid) =====
    private Material? _selectedMaterial;
    public Material? SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            if (SetProperty(ref _selectedMaterial, value))
            {
                OnPropertyChanged(nameof(SelectedMaterialName));
                LoadOperations();
            }
        }
    }
    public string SelectedMaterialName => SelectedMaterial?.Name ?? "(выберите материал слева)";

    // ===== Форма новой операции =====
    private string _operationType = "Приход";
    public string OperationType { get => _operationType; set => SetProperty(ref _operationType, value); }

    private string _operationQuantity = "";
    public string OperationQuantity { get => _operationQuantity; set => SetProperty(ref _operationQuantity, value); }

    private string _operationComment = "";
    public string OperationComment { get => _operationComment; set => SetProperty(ref _operationComment, value); }

    // ===== Форма нового материала =====
    private string _newMaterialName = "";
    public string NewMaterialName { get => _newMaterialName; set => SetProperty(ref _newMaterialName, value); }

    private string _newMaterialUnit = "шт";
    public string NewMaterialUnit { get => _newMaterialUnit; set => SetProperty(ref _newMaterialUnit, value); }

    private string _newMaterialMinQuantity = "";
    public string NewMaterialMinQuantity { get => _newMaterialMinQuantity; set => SetProperty(ref _newMaterialMinQuantity, value); }

    // ===== Команды =====
    public ICommand AddOperationCommand { get; }
    public ICommand AddMaterialCommand { get; }
    public ICommand DeleteMaterialCommand { get; }

    public MainViewModel()
    {
        Materials = new ObservableCollection<Material>();
        Operations = new ObservableCollection<Operation>();

        AddOperationCommand = new RelayCommand(_ => AddOperation());
        AddMaterialCommand = new RelayCommand(_ => AddMaterial());
        DeleteMaterialCommand = new RelayCommand(_ => DeleteMaterial());

        try
        {
            RefreshMaterials();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Не удалось подключиться к базе данных.\n\n" +
                "1. Проверьте, что SQL Server запущен.\n" +
                "2. Проверьте строку подключения в Data/AppDbContext.cs.\n" +
                "3. Убедитесь, что БД BuildingMaterialsDB создана " +
                "(скрипт Database/CreateDatabase.sql).\n\n" +
                $"Техническая ошибка:\n{ex.Message}",
                "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // --- Загрузка материалов из БД (с сохранением выделения) ---
    private void RefreshMaterials()
    {
        var selectedId = SelectedMaterial?.Id;

        SelectedMaterial = null;                 // сбрасываем, чтобы потом точно обновилось
        Materials.Clear();

        var list = _context.Materials.OrderBy(m => m.Name).ToList();
        foreach (var m in list) Materials.Add(m);

        if (selectedId.HasValue)
            SelectedMaterial = Materials.FirstOrDefault(m => m.Id == selectedId.Value);
    }

    // --- Загрузка операций по выбранному материалу ---
    private void LoadOperations()
    {
        Operations.Clear();
        if (SelectedMaterial == null) return;

        var ops = _context.Operations
            .Where(o => o.MaterialId == SelectedMaterial.Id)
            .OrderByDescending(o => o.OperationDate)
            .Take(100)
            .ToList();

        foreach (var op in ops) Operations.Add(op);
    }

    // --- Добавить операцию (приход или расход) ---
    private void AddOperation()
    {
        if (SelectedMaterial == null)
        {
            MessageBox.Show("Сначала выберите материал в таблице слева.",
                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!double.TryParse(OperationQuantity, out var qty) || qty <= 0)
        {
            MessageBox.Show("Введите корректное количество (> 0). Например: 10 или 2,5",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Берём "свежую" сущность из контекста и меняем остаток.
        var material = _context.Materials.First(m => m.Id == SelectedMaterial.Id);

        if (OperationType == "Приход")
            material.Quantity += qty;
        else
            material.Quantity -= qty;

        _context.Operations.Add(new Operation
        {
            MaterialId = material.Id,
            OperationType = OperationType,
            Quantity = qty,
            OperationDate = DateTime.Now,
            Comment = OperationComment
        });
        _context.SaveChanges();

        // Сбрасываем форму
        OperationQuantity = "";
        OperationComment = "";

        // Обновляем интерфейс — и остаток, и журнал операций
        RefreshMaterials();
    }

    // --- Добавить новый материал ---
    private void AddMaterial()
    {
        if (string.IsNullOrWhiteSpace(NewMaterialName))
        {
            MessageBox.Show("Введите название материала.",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        double.TryParse(NewMaterialMinQuantity, out var minQty);

        _context.Materials.Add(new Material
        {
            Name = NewMaterialName.Trim(),
            Unit = string.IsNullOrWhiteSpace(NewMaterialUnit) ? "шт" : NewMaterialUnit.Trim(),
            Quantity = 0,
            MinQuantity = minQty
        });
        _context.SaveChanges();

        NewMaterialName = "";
        NewMaterialUnit = "шт";
        NewMaterialMinQuantity = "";

        RefreshMaterials();
    }

    // --- Удалить выбранный материал и все его операции ---
    private void DeleteMaterial()
    {
        if (SelectedMaterial == null)
        {
            MessageBox.Show("Сначала выберите материал в таблице.",
                "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Удалить материал «{SelectedMaterial.Name}» и все его операции?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        var material = _context.Materials.First(m => m.Id == SelectedMaterial.Id);

        // Удаляем связанные операции вручную (надёжнее, чем надеяться на каскад EF)
        var related = _context.Operations.Where(o => o.MaterialId == material.Id).ToList();
        _context.Operations.RemoveRange(related);
        _context.Materials.Remove(material);
        _context.SaveChanges();

        SelectedMaterial = null;
        RefreshMaterials();
    }
}