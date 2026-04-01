using System.Text.Json;
using BusinessLogic.Core;
using BusinessLogic.Core.Enums;
using BusinessLogic.Notes;

namespace BusinessLogic.NoteTemplates;

public sealed class NoteTemplateManagement : INoteTemplateManagement
{
    private const string TemplateFileName = "templates.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _templateFilePath;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public NoteTemplateManagement(string templateFolderPath)
    {
        if (string.IsNullOrWhiteSpace(templateFolderPath))
            throw new ArgumentException("Template folder path cannot be empty.", nameof(templateFolderPath));

        Directory.CreateDirectory(templateFolderPath);
        _templateFilePath = Path.Combine(templateFolderPath, TemplateFileName);
    }

    public async Task<IReadOnlyList<NoteTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var templateFileExists = File.Exists(_templateFilePath);
            var templates = await ReadTemplatesInternalAsync(cancellationToken).ConfigureAwait(false);

            if (!templateFileExists && templates.Count == 0)
            {
                templates.Add(CreateDefaultGroceriesTemplate());
                await WriteTemplatesInternalAsync(templates, cancellationToken).ConfigureAwait(false);
            }

            return templates.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<NoteTemplate?> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return null;

        var templates = await GetTemplatesAsync(cancellationToken).ConfigureAwait(false);
        return templates.FirstOrDefault(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<OperationResult> SaveTemplateAsync(NoteTemplate template, CancellationToken cancellationToken = default)
    {
        if (template is null)
            return OperationResult.Fail("Template cannot be null.");

        if (string.IsNullOrWhiteSpace(template.Name))
            return OperationResult.Fail("Template name cannot be empty.");

        if (string.IsNullOrWhiteSpace(template.Content))
            return OperationResult.Fail("Template content cannot be empty.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var templates = await ReadTemplatesInternalAsync(cancellationToken).ConfigureAwait(false);

            var name = template.Name.Trim();
            var existingIndex = templates.FindIndex(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            var normalizedTemplate = new NoteTemplate
            {
                Name = name,
                Title = template.Title,
                Content = template.Content,
                NoteType = template.NoteType,
                Tag = template.Tag,
                Format = template.Format,
                TaskStatus = template.TaskStatus,
                Recurrence = template.Recurrence
            };

            if (existingIndex >= 0)
                templates[existingIndex] = normalizedTemplate;
            else
                templates.Add(normalizedTemplate);

            await WriteTemplatesInternalAsync(templates, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to save template: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult> DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return OperationResult.Fail("Template name cannot be empty.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var templates = await ReadTemplatesInternalAsync(cancellationToken).ConfigureAwait(false);

            var removed = templates.RemoveAll(t => t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
                return OperationResult.Fail($"Template '{templateName}' was not found.");

            await WriteTemplatesInternalAsync(templates, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Failed to delete template: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<NoteTemplate>> ReadTemplatesInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_templateFilePath))
            return [];

        var json = await File.ReadAllTextAsync(_templateFilePath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<NoteTemplate>>(json, SerializerOptions) ?? [];
    }

    private async Task WriteTemplatesInternalAsync(IEnumerable<NoteTemplate> templates, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(templates, SerializerOptions);
        await File.WriteAllTextAsync(_templateFilePath, json, cancellationToken).ConfigureAwait(false);
    }

    private static NoteTemplate CreateDefaultGroceriesTemplate()
    {
        return new NoteTemplate
        {
            Name = "Groceries List",
            Title = "Groceries",
            Content = "## Groceries\n- [ ] Milk\n- [ ] Eggs\n- [ ] Bread\n- [ ] Fruit\n- [ ] Vegetables\n\n## Notes\n",
            NoteType = NoteType.Task,
            Format = NoteFormat.Markdown,
            Tag = NoteTag.Green,
            TaskStatus = NoteTaskStatus.NotStarted,
            Recurrence = RecurrencePattern.None
        };
    }
}