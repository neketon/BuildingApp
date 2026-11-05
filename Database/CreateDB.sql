-- ============================================================
-- Учет строительных материалов на объекте
-- Запустить в SQL Server Management Studio (SSMS) один раз
-- ============================================================

IF DB_ID('BuildingMaterialsDB') IS NOT NULL
BEGIN
    ALTER DATABASE BuildingMaterialsDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE BuildingMaterialsDB;
END
GO

CREATE DATABASE BuildingMaterialsDB;
GO

USE BuildingMaterialsDB;
GO

-- Справочник материалов
CREATE TABLE Materials (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Unit        NVARCHAR(20)  NOT NULL DEFAULT N'шт',
    Quantity    FLOAT         NOT NULL DEFAULT 0,
    MinQuantity FLOAT         NOT NULL DEFAULT 0
);
GO

-- Журнал операций (приход/расход)
CREATE TABLE Operations (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    MaterialId     INT NOT NULL,
    OperationType  NVARCHAR(10) NOT NULL,   -- 'Приход' или 'Расход'
    Quantity       FLOAT NOT NULL,
    OperationDate  DATETIME NOT NULL DEFAULT GETDATE(),
    Comment        NVARCHAR(200) NULL,
    CONSTRAINT FK_Operations_Materials
        FOREIGN KEY (MaterialId) REFERENCES Materials(Id)
        ON DELETE CASCADE
);
GO

-- Тестовые данные
INSERT INTO Materials (Name, Unit, Quantity, MinQuantity) VALUES
    (N'Цемент М500',      N'кг', 500,  100),
    (N'Песок',            N'кг', 1200, 300),
    (N'Кирпич красный',   N'шт', 800,  200),
    (N'Доска обрезная',   N'м3', 5,    10),  -- специально ниже минимума
    (N'Арматура D12',     N'м',  150,  50);
GO

DECLARE @Cement INT = (SELECT Id FROM Materials WHERE Name = N'Цемент М500');
DECLARE @Brick  INT = (SELECT Id FROM Materials WHERE Name = N'Кирпич красный');

INSERT INTO Operations (MaterialId, OperationType, Quantity, Comment) VALUES
    (@Cement, N'Приход', 200,  N'Поставка от ООО "СтройМат"'),
    (@Cement, N'Расход', 50,   N'Заливка фундамента'),
    (@Brick,  N'Приход', 1000, N'Начальная поставка'),
    (@Brick,  N'Расход', 200,  N'Кладка 1 этажа');
GO

PRINT 'База данных BuildingMaterialsDB успешно создана!';
GO