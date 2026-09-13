using TodoApi.Dtos;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
    var todo = todos.FirstOrDefault(todo => todo.Id == id);

    return todo is null
        ? Results.NotFound()
        : Results.Ok(todo);
});

app.MapPost("/api/todos", (TodoPostDto request) =>
{
    var nextId = todos.Count == 0 ? 1 : todos.Max(todo => todo.Id) + 1;
    var todo = new TodoGetDto(nextId, request.Title, request.IsCompleted);

    todos.Add(todo);

    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.MapPut("/api/todos/{id}", (int id, TodoPutDto request) =>
{
    try
    {
        var todoIndex = todos.FindIndex(todo => todo.Id == id);

        if (todoIndex == -1)
        {
            return Results.NotFound();
        }

        todos[todoIndex] = new TodoGetDto(id, request.Title, request.IsCompleted);

        return Results.Ok(todos[todoIndex]);
    }
    catch
    {
        return Results.Problem("Unable to update the todo item.");
    }
});

app.Run();