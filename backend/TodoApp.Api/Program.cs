using TodoApp.Core;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var list = new TodoList();

app.MapGet("/tasks", () => list.Tasks);

app.Run();
