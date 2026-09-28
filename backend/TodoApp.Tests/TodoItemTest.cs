using TodoApp.Core;
using Xunit;


public class TodoItemTest
{
    [Fact]
    public void Marking_a_task_done()
    {
        var task = new TodoItem("Buy milk");
        task.Complete();
        Assert.True(task.IsDone);
    }

    [Fact]
    public void Reopening_a_task()
    {
        var task = new TodoItem("Buy milk");
        task.Complete();
        task.Reopen();
        Assert.False(task.IsDone);
    }

    [Fact]

    public void Adding_a_task_with_a_empty_title()
    {
        Assert.Throws<ArgumentException>(() => new TodoItem(""));
    }
   
}