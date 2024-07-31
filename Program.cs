using HoneyRaesAPI.Models;
using System.Text.Json.Serialization;

List<Customer> customers = new List<Customer> { 
new Customer ()
{
    Id = 1,
    Name = "Taylor",
    Address = "123 River Dr"
},
new Customer ()
{
    Id = 2,
    Name = "Ross",
    Address = "123 Lake Dr"
},
new Customer ()
{
    Id = 3,
    Name = "Derek",
    Address = "123 City Dr"
}
};
List<Employee> employees = new List<Employee> {
new Employee ()
{
    Id = 1,
    Name = "Andrew",
    Specialty = "Orthopedics"
},
new Employee ()
{
    Id = 2,
    Name = "Odie",
    Specialty = "Coding"
}
};
List<ServiceTicket> serviceTickets = new List<ServiceTicket> {
new ServiceTicket ()
{
    Id = 1,
    CustomerId = 1,
    Description = "Broken Leg",
    Emergency = false,
    DateCompleted = new DateTime(2024, 07, 01)
},
new ServiceTicket ()
{
    Id = 2,
    CustomerId = 2,
    EmployeeId = 2,
    Description = "Broken Code",
    Emergency = true
},
new ServiceTicket ()
{
    Id = 3,
    CustomerId = 3,
    Description = "Neck Pain",
    Emergency = false,
    DateCompleted = new DateTime(2023, 07, 03)
},
new ServiceTicket ()
{
    Id = 4,
    CustomerId = 1,
    EmployeeId = 2,
    Description = "API Calls",
    Emergency = false
},
new ServiceTicket ()
{
    Id = 5,
    CustomerId = 2,
    EmployeeId = 1,
    Description = "Blah Blah Blah",
    Emergency = false,
    DateCompleted = new DateTime(2024, 07, 02)
}
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/api/servicetickets", () =>
{
    return serviceTickets;
});

app.MapGet("/api/servicetickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
    {
        return Results.NotFound();
    }
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    serviceTicket.Customer = customers.FirstOrDefault(e => e.Id == serviceTicket.CustomerId);
    return Results.Ok(serviceTicket);
});

app.MapGet("/api/employees", () =>
{
    return employees;
});

app.MapGet("/api/employees/{id}", (int id) =>
{
    Employee employee = employees.FirstOrDefault(e => e.Id == id);
    if (employee == null)
    {
        return Results.NotFound();
    }
    employee.ServiceTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    return Results.Ok(employee);
});

app.MapGet("/api/customers", () =>
{
    return customers;
});

app.MapGet("/api/customers/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(e => e.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    customer.ServiceTickets = serviceTickets.Where(st => st.CustomerId == id).ToList();
    return Results.Ok(customer);
});

app.MapPost("/api/servicetickets", (ServiceTicket serviceTicket) =>
{
    // creates a new id (When we get to it later, our SQL database will do this for us like JSON Server did!)
    serviceTicket.Id = serviceTickets.Max(st => st.Id) + 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapDelete("/api/servicetickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    serviceTickets.Remove(serviceTicket);
});

app.MapPut("/api/servicetickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    ServiceTicket ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id);
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate);
    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }
    //the id in the request route doesn't match the id from the ticket in the request body. That's a bad request!
    if (id != serviceTicket.Id)
    {
        return Results.BadRequest();
    }
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

app.MapPost("/api/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket ticketToComplete = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (ticketToComplete != null)
    {
        ticketToComplete.DateCompleted = DateTime.Today;
    }
    else
    {
    }
});

app.MapGet("/api/servicetickets/emergencies", () =>
{
    List<ServiceTicket> emergencies = serviceTickets.Where(st => st.Emergency == true && st.DateCompleted == null).ToList();

    return Results.Ok(emergencies);
});

app.MapGet("/api/servicetickets/unassigned", () =>
{
    List<ServiceTicket> unassignedTickets = serviceTickets.Where(st => st.EmployeeId == null).ToList();

    return Results.Ok(unassignedTickets);
});

app.MapGet("/api/customers/inactive", () =>
{
    var inactiveCustomers = customers.Where(c => !serviceTickets.Any(s => s.CustomerId == c.Id && s.DateCompleted.HasValue && s.DateCompleted.Value > DateTime.Now.AddYears(-1))
    ).ToList();

    return Results.Ok(inactiveCustomers);
});

app.MapGet("/api/employees/available", () =>
{
    var availableEmployees = serviceTickets.Where(st => st.DateCompleted != null).Select(st => st.EmployeeId).ToList();
    var available = employees.Where(st => !availableEmployees.Contains(st.Id)).ToList();
    return Results.Ok(available);
});

app.MapGet("/api/employeeofthemonth", () =>
{
    var lastMonth = DateTime.Now.AddMonths(-1);
    var employeeOfTheMonth = employees
        .OrderByDescending(e => serviceTickets.Count(st => st.EmployeeId == e.Id && st.DateCompleted.HasValue && st.DateCompleted.Value.Month == lastMonth.Month))
        .FirstOrDefault();

    return Results.Ok(employeeOfTheMonth);
});

app.MapGet("/api/ticketreview", () =>
{
    var completedTickets = serviceTickets
        .Where(st => st.DateCompleted.HasValue)
        .OrderBy(st => st.DateCompleted).ToList();

    foreach (var ticket in completedTickets)
    { 
        ticket.Customer = customers.FirstOrDefault(c => c.Id == ticket.CustomerId);
        ticket.Employee = employees.FirstOrDefault(e => e.Id == ticket.EmployeeId);
    }
    return Results.Ok(completedTickets);
});

app.MapGet("/api/prioritizedtickets", () =>
{
    var prioritizedTickets = serviceTickets
        .Where(st => !st.DateCompleted.HasValue)
        .OrderByDescending(st => st.Emergency)
        .ThenBy(st => st.EmployeeId.HasValue)
        .ToList();
    
    foreach (var ticket in prioritizedTickets)
    {
        ticket.Customer = customers.FirstOrDefault(c => c.Id == ticket.CustomerId);
        ticket.Employee = employees.FirstOrDefault(e => e.Id == ticket.EmployeeId);
    }
    return Results.Ok(prioritizedTickets);

});

app.MapPatch("/api/servicetickets/{id}/assign", (int id, AssignEmployeeRequest request) =>
{
    var ticket = serviceTickets.FirstOrDefault(st => st.Id == id);

    if (ticket == null)
    {
        return Results.NotFound($"Service ticket with ID {id} not found.");
    }

    // Assign the employee to the service ticket
    ticket.EmployeeId = request.EmployeeId;

    return Results.Ok($"Employee {request.EmployeeId} assigned to service ticket {id} successfully.");
});

app.Run();
