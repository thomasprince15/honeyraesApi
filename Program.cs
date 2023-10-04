using Npgsql;
using System.Text.Json.Serialization;
using HoneyRaesAPI.Models;
using Microsoft.AspNetCore.Http.Json;

var connectionString = "Host=localhost;Port=5432;Username=postgres;Password=Myhouse95;Database=HoneyRaes";

List<Customer> customers = new List<Customer> 
{
    new Customer()
    {
        Id = 1,
        Name = "brunhilda",
        Address = "7733 this place way"
    },
    new Customer()
    {
        Id = 2,
        Name = "danny",
        Address = "1265 this way place"
    },
    new Customer()
    {
        Id = 3,
        Name = "delores",
        Address = "748 wrong way place"
    }
};
List<Employee> employees = new List<Employee>
{
    new Employee()
    {
        Id = 1,
        Name = "jerold",
        Specialty = "fixing toilets",
    },
    new Employee()
    {
        Id = 2,
        Name = "tina",
        Specialty = "eating cake",
    }
};
List<ServiceTicket> serviceTickets = new List<ServiceTicket> 
{
    new ServiceTicket()
    {
        Id = 1,
        CustomerId = 3,
        EmployeeId = 2,
        Description = "cake needs eating",
        Emergency = true,
        DateCompleted = new DateTime(2023, 4, 2)
    },
    new ServiceTicket()
    {
        Id = 2,
        CustomerId = 1,
        EmployeeId = 1,
        Description = "fix stuff in the toilet",
        Emergency = false,
        DateCompleted = new DateTime(2023, 2, 5)
    },
    new ServiceTicket()
    {
        Id = 3,
        CustomerId = 2,
        Description = "im baking cake",
        Emergency = false,
    },
    new ServiceTicket()
    {
        Id = 4,
        CustomerId = 2,
        EmployeeId = 1,
        Description = "stuff",
        Emergency = true,
    },
    new ServiceTicket()
    {
        Id = 5,
        CustomerId = 3,
        Description = "and then there will be cake",
        Emergency = false,
    }
};
var builder = WebApplication.CreateBuilder(args);
// Set the JSON serializer options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/servicetickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    serviceTicket.Customer = customers.FirstOrDefault(c => c.Id == serviceTicket.CustomerId);
    return Results.Ok(serviceTicket);
});

app.MapPost("/servicetickets", (ServiceTicket serviceTicket) =>
{
    // creates a new id (When we get to it later, our SQL database will do this for us like JSON Server did!)
    serviceTicket.Id = serviceTickets.Count > 0 ?serviceTickets.Max(st => st.Id) + 1 : 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapDelete("/servicetickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    serviceTickets.Remove(serviceTicket);
});
// add a new endpoint with mapput for updating the service tickets with two parameters id and serviceticket
app.MapPut("/servicetickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    // create a new variable for the serviceticket we want to update
    ServiceTicket ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id);
    // add an int for the index of the chosen service ticket
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate);
    // if no ticket is selected return not found
    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }
    //the id in the request route doesn't match the id from the ticket in the request body. That's a bad request!
    if (id != ticketToUpdate.Id)
    {
        return Results.BadRequest();
    }
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

app.MapPost("/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket ticketToComplete = serviceTickets.FirstOrDefault(st => st.Id == id);
    ticketToComplete.DateCompleted = DateTime.Today;
    return ticketToComplete;
});

app.MapGet("/employees", () =>
{
    // create an empty list of employees to add to. 
    List<Employee> employees = new List<Employee>();
    //make a connection to the PostgreSQL database using the connection string
    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
    //open the connection
    connection.Open();
    // create a sql command to send to the database
    using NpgsqlCommand command = connection.CreateCommand();
    command.CommandText = "SELECT * FROM Employee";
    //send the command. 
    using NpgsqlDataReader reader = command.ExecuteReader();
    //read the results of the command row by row
    while (reader.Read()) // reader.Read() returns a boolean, to say whether there is a row or not, it also advances down to that row if it's there. 
    {
        //This code adds a new C# employee object with the data in the current row of the data reader 
        employees.Add(new Employee
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")), //find what position the Id column is in, then get the integer stored at that position
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Specialty = reader.GetString(reader.GetOrdinal("Specialty"))
        });
    }
    //once all the rows have been read, send the list of employees back to the client as JSON
    return employees;
});

app.MapPost("/employees", (Employee employee) =>
{
    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
    connection.Open();
    using NpgsqlCommand command = connection.CreateCommand();
    command.CommandText = @"
        INSERT INTO Employee (Name, Specialty)
        VALUES (@name, @specialty)
        RETURNING Id
    ";
    command.Parameters.AddWithValue("@name", employee.Name);
    command.Parameters.AddWithValue("@specialty", employee.Specialty);

    //the database will return the new Id for the employee, add it to the C# object
    employee.Id = (int)command.ExecuteScalar();

    return employee;
});

app.MapPut("/employees/{id}", (int id, Employee employee) =>
{
    if (id != employee.Id)
    {
        return Results.BadRequest();
    }
    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
    connection.Open();
    using NpgsqlCommand command = connection.CreateCommand();
    command.CommandText = @"
        UPDATE Employee 
        SET Name = @name,
            Specialty = @specialty
        WHERE Id = @id
    ";
    command.Parameters.AddWithValue("@name", employee.Name);
    command.Parameters.AddWithValue("@specialty", employee.Specialty);
    command.Parameters.AddWithValue("@id", id);

    command.ExecuteNonQuery();
    return Results.NoContent();
});

app.MapGet("/employees/{id}", (int id) =>
{
    Employee employee = null;
    using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
    connection.Open();
    using NpgsqlCommand command = connection.CreateCommand();
    command.CommandText = @"
        SELECT 
            e.Id,
            e.Name, 
            e.Specialty, 
            st.Id AS serviceTicketId, 
            st.CustomerId,
            st.Description,
            st.Emergency,
            st.DateCompleted 
        FROM Employee e
        LEFT JOIN ServiceTicket st ON st.EmployeeId = e.Id
        WHERE e.Id = @id";
    // use command parameters to add the specific Id we are looking for to the query
    command.Parameters.AddWithValue("@id", id);
    using NpgsqlDataReader reader = command.ExecuteReader();
    // We are only expecting one row back, so we don't need a loop!
    while (reader.Read())
    {
        if (employee == null)
        {
            employee = new Employee
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Specialty = reader.GetString(reader.GetOrdinal("Specialty")),
                ServiceTickets = new List<ServiceTicket>() //empty List to add service tickets to
            };
        }
        // reader.IsDBNull checks if a column in a particular position is null
        if (!reader.IsDBNull(reader.GetOrdinal("serviceTicketId")))
        {
            employee.ServiceTickets.Add(new ServiceTicket
            {
                Id = reader.GetInt32(reader.GetOrdinal("serviceTicketId")),
                CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId")),
                //we don't need to get this from the database, we already know it
                EmployeeId = id,
                Description = reader.GetString(reader.GetOrdinal("Description")),
                Emergency = reader.GetBoolean(reader.GetOrdinal("Emergency")),
                // Npgsql can't automatically convert NULL in the database to C# null, so we have to check whether it's null before trying to get it
                DateCompleted = reader.IsDBNull(reader.GetOrdinal("DateCompleted")) ? null : reader.GetDateTime(reader.GetOrdinal("DateCompleted"))
            });
        }
    }
     // Return 404 if the employee is never set (meaning, that reader.Read() immediately returned false because the id did not match an employee)
    // otherwise 200 with the employee data
    return employee == null ? Results.NotFound() : Results.Ok(employee);
});

app.MapGet("/customers", () => 
{
    return customers;
});

app.MapGet("/customers/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.ServiceTickets = serviceTickets.Where(st => st.CustomerId == id).ToList();
    return Results.Ok(customer);
});

app.Run();