using TodoApp.Core;
using Xunit;


public class TodoItemTest
{
    [Fact]
    public void Marking_a_task_done()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        list.CompleteTask(task.Id);
        Assert.True(task.IsDone);
    }

    [Fact]
    public void Reopening_a_done_task()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        list.CompleteTask(task.Id);
        list.ReopenTask(task.Id);
        Assert.False(task.IsDone);
    }

    [Fact]
    public void Changing_to_an_allowed_priority()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        list.ChangePriority(task.Id, Priority.Low);
        Assert.Equal(Priority.Low, task.Priority);
    }

    [Fact]

    public void Adding_a_task_to_a_list()
    {
        var list = new TodoList();
        list.AddTask("Buy milk");
        Assert.Single(list.Tasks);
        Assert.False(list.Tasks[0].IsDone);
    }

    [Fact]

    public void removing_a_task_from_a_list()
    {
        var list = new TodoList();
        var milk = list.AddTask("Buy milk");
        var mum = list.AddTask("call mum");

        list.DeleteTask(milk.Id);

        Assert.Single(list.Tasks);
        Assert.DoesNotContain(list.Tasks, t => t.Id == milk.Id);
        Assert.Contains(list.Tasks, t => t.Id == mum.Id);
    }

    [Fact]
    public void Adding_a_task_with_an_empty_title()
    {
        var list = new TodoList();
        Assert.Throws<ArgumentException>(() => list.AddTask(""));
        Assert.Empty(list.Tasks);
    }

    [Fact]
    public void Changing_a_tasks_due_date()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        var newDate = new DateTime(2026, 10, 5);
        list.ChangeDueDate(task.Id, newDate);
        Assert.Equal(newDate, task.DueDate);
    }

    [Fact]
    public void Editing_a_tasks_title()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        list.EditTitle(task.Id, "Buy oat milk");
        Assert.Equal("Buy oat milk", task.Title);
    }

    [Fact]
    public void Editing_a_tasks_title_to_empty_is_rejected()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        Assert.Throws<ArgumentException>(() => list.EditTitle(task.Id, ""));
    }




}