using System.ComponentModel.DataAnnotations.Schema;

namespace BuildingApp.Models;

public class Material
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "шт";
    public double Quantity { get; set; }
    public double MinQuantity { get; set; }

    // Вычисляемое свойство — НЕ колонка в БД.
    // Используется для подсветки строк в интерфейсе.
    [NotMapped]
    public bool IsLowStock => MinQuantity > 0 && Quantity < MinQuantity;

}
