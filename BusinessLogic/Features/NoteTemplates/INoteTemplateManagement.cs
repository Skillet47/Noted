using BusinessLogic.Models;

namespace BusinessLogic.Features.NoteTemplates;

public interface INoteTemplateManagement
{
    Task<IReadOnlyList<NoteTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<NoteTemplate?> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default);
    Task<OperationResult> SaveTemplateAsync(NoteTemplate template, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default);
}