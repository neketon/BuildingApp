namespace BuildingApp.Models;

public class Operation
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string OperationType { get; set; } = "Приход";
    public double Quantity { get; set; }
    public DateTime OperationDate { get; set; }
    public string Comment { get; set; } = "";
}