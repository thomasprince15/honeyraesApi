\c HoneyRaes

INSERT INTO Customer(Name, Address) VALUES('Bob', '314 crossing road');
INSERT INTO Customer(Name, Address) VALUES('Tim', '567 lost road');
INSERT INTO Customer(Name, Address) VALUES('Harvy', '975 passing street');

INSERT INTO Employee(Name, Specialty) VALUES('Pat', 'Plumber');
INSERT INTO Employee(Name, Specialty) VALUES('Charlie', 'Pest Control');

INSERT INTO ServiceTicket(CustomerId, EmployeeId, Description, Emergency) VALUES(1, 2, 'wasp nest', true);
INSERT INTO ServiceTicket(CustomerId, EmployeeId, Description, Emergency) VALUES(2, 1, 'pipe burst', true);
INSERT INTO ServiceTicket(CustomerId, EmployeeId,  Description, Emergency) VALUES(3, 1, 'ant hill', false);