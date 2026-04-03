using BusinessLogic.Features.NoteTemplates;
using BusinessLogic.Shared;

namespace BusinessLogicTests.Features.NoteTemplates;

public sealed class NoteTemplateManagementTests : IDisposable
{
    private readonly string _testFolder;
    private readonly NoteTemplateManagement _templateManager;

    public NoteTemplateManagementTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFolder);
        _templateManager = new NoteTemplateManagement(_testFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
            Directory.Delete(_testFolder, true);
    }

    [Fact]
    public async Task GetTemplatesAsync_FirstRun_SeedsGroceriesTemplate()
    {
        var templates = await _templateManager.GetTemplatesAsync();

        var groceries = Assert.Single(templates, t => t.Name == "Groceries List");
        Assert.Equal("Groceries", groceries.Title);
        Assert.Equal(NoteType.Task, groceries.NoteType);
        Assert.Equal(NoteFormat.Markdown, groceries.Format);
    }

    [Fact]
    public async Task SaveTemplateAsync_NewTemplate_CanBeRetrieved()
    {
        var template = new NoteTemplate
        {
            Name = "Standup",
            Title = "Daily Standup",
            Content = "Yesterday:\nToday:\nBlockers:",
            NoteType = NoteType.Idea,
            Format = NoteFormat.Markdown,
            Tag = NoteTag.Blue
        };

        var saveResult = await _templateManager.SaveTemplateAsync(template);
        Assert.True(saveResult.Success);

        var loaded = await _templateManager.GetTemplateAsync("Standup");
        Assert.NotNull(loaded);
        Assert.Equal("Daily Standup", loaded!.Title);
        Assert.Equal(NoteTag.Blue, loaded.Tag);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ExistingTemplate_RemovesTemplate()
    {
        await _templateManager.SaveTemplateAsync(new NoteTemplate
        {
            Name = "Quick Note",
            Title = "Quick",
            Content = "Template body"
        });

        var deleteResult = await _templateManager.DeleteTemplateAsync("Quick Note");
        Assert.True(deleteResult.Success);

        var loaded = await _templateManager.GetTemplateAsync("Quick Note");
        Assert.Null(loaded);
    }
}
