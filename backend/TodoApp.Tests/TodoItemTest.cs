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

    [Fact]
    public void Adding_a_task_with_only_spaces_is_rejected()
    {
        var list = new TodoList();
        Assert.Throws<ArgumentException>(() => list.AddTask("   "));
        Assert.Empty(list.Tasks);
    }

    [Fact]
    public void Editing_a_title_to_only_spaces_is_rejected()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        Assert.Throws<ArgumentException>(() => list.EditTitle(task.Id, "   "));
    }

    [Fact]
    public void Changing_to_an_undefined_priority_is_rejected()
    {
        var list = new TodoList();
        var task = list.AddTask("Buy milk");
        Assert.Throws<ArgumentException>(() => list.ChangePriority(task.Id, (Priority)5));
    }

    [Fact]
    public void Setting_a_due_date_without_a_time()
    {
        var list = new TodoList();
        var task = list.AddTask("Pay the bill");
        var date = new DateOnly(2026, 10, 9);
        list.ChangeDueDate(task.Id, date, null);
        Assert.Equal(date, task.DueDate);
        Assert.Null(task.DueTime);
    }

    [Fact]
    public void Setting_a_due_date_with_a_time()
    {
        var list = new TodoList();
        var task = list.AddTask("Meeting");
        var date = new DateOnly(2026, 10, 9);
        var time = new TimeOnly(15, 0);
        list.ChangeDueDate(task.Id, date, time);
        Assert.Equal(date, task.DueDate);
        Assert.Equal(time, task.DueTime);
    }

    [Fact]
    public void Setting_a_time_without_a_date_is_rejected()
    {
        var list = new TodoList();
        var task = list.AddTask("Meeting");
        Assert.Throws<ArgumentException>(() => list.ChangeDueDate(task.Id, null, new TimeOnly(15, 0)));
    }

    [Fact]
    public void Removing_the_due_date_also_removes_the_time()
    {
        var list = new TodoList();
        var task = list.AddTask("Meeting");
        list.ChangeDueDate(task.Id, new DateOnly(2026, 10, 9), new TimeOnly(15, 0));
        list.ChangeDueDate(task.Id, null, null);
        Assert.Null(task.DueDate);
        Assert.Null(task.DueTime);
    }


}