using TodoApi.Dtos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var todos = new List<TodoGetDto>
{
    new(1, "Learn Minimal API", false),
    new(2, "Learn Vue", false)
};

app.MapGet("/", () => "Hello Todo API");

app.MapGet("/api/todos", () =>
    Results.Ok(todos));

app.MapGet("/api/todos/{id}", (int id) =>
{
    var todo = todos.FirstOrDefault(x => x.Id == id);

    return todo is null
        ? Results.NotFound()
        : Results.Ok(todo);
});

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
