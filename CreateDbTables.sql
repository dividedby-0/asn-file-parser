CREATE TABLE Boxes
(
    Id                  INT IDENTITY PRIMARY KEY,
    SupplierIdentifier  NVARCHAR(50),
    CartonBoxIdentifier NVARCHAR(50)
);

CREATE TABLE BoxContents
(
    Id       INT IDENTITY PRIMARY KEY,
    BoxId    INT FOREIGN KEY REFERENCES Boxes(Id),
    PoNumber NVARCHAR(50),
    ISBN     NVARCHAR(50),
    Quantity INT
);

CREATE TABLE FileProcessingLog
(
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    FileName      VARCHAR(255) NOT NULL,
    ProcessedDate DATETIME DEFAULT GETDATE()
);