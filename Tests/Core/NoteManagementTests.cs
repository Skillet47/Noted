using BusinessLogic.Core;
using BusinessLogic.Notes;

namespace BusinessLogicTests.Core
{
    public class NoteManagementTests : IDisposable
    {
        private readonly string _testFolder;
        private readonly NoteManagement _noteManager;

        public NoteManagementTests()
        {
            _testFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testFolder);
            _noteManager = new NoteManagement(_testFolder);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testFolder))
                Directory.Delete(_testFolder, true);
        }

        [Fact]
        public async Task SaveAndRetrieve_IdeaNote_Works()
        {
            var note = new IdeaNote
            {
                Title = "Test Idea",
                Content = "Idea content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("Test Idea", notes[0].Title);
            Assert.Equal("Idea content", notes[0].Content);
            Assert.IsType<IdeaNote>(notes[0]);
        }

        [Fact]
        public async Task SaveAndRetrieve_ReminderNote_Works()
        {
            var note = new ReminderNote
            {
                Title = "Test Reminder",
                Content = "Reminder content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = true,
                Tag = NoteTag.None,
                Format = NoteFormat.Markdown,
                ReminderDateTime = DateTime.Now.AddDays(1)
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("Test Reminder", notes[0].Title);
            Assert.Equal("Reminder content", notes[0].Content);
            Assert.True(notes[0].IsPinned);
            Assert.IsType<ReminderNote>(notes[0]);
        }

        [Fact]
        public async Task SaveAndRetrieve_TaskNote_Works()
        {
            var note = new TaskNote
            {
                Title = "Test Task",
                Content = "Task content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.RichText,
                Status = NoteTaskStatus.InProgress
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("Test Task", notes[0].Title);
            Assert.Equal(NoteTaskStatus.InProgress, ((TaskNote)notes[0]).Status);
            Assert.IsType<TaskNote>(notes[0]);
        }

        [Fact]
        public async Task UpdateNote_ChangesContentAndFormat()
        {
            var note = new IdeaNote
            {
                Title = "UpdateTest",
                Content = "Old content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var updated = new IdeaNote
            {
                Title = "UpdateTest",
                Content = "New content",
                CreatedAt = note.CreatedAt,
                ModifiedAt = note.ModifiedAt,
                IsPinned = true,
                Tag = NoteTag.None,
                Format = NoteFormat.Markdown
            };
            var result = await _noteManager.UpdateNoteAsync("UpdateTest", updated);
            Assert.True(result);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("New content", notes[0].Content);
            Assert.Equal(NoteFormat.Markdown, notes[0].Format);
        }

        [Fact]
        public async Task DeleteNote_RemovesFile()
        {
            var note = new IdeaNote
            {
                Title = "DeleteMe",
                Content = "Delete content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var deleted = await _noteManager.DeleteNoteAsync("DeleteMe");
            Assert.True(deleted);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public void CreateFolderAndGetSubfolders_Works()
        {
            var folderName = "SubFolder1";
            var created = _noteManager.CreateFolder(folderName);
            Assert.True(created);
            var subfolders = _noteManager.GetSubfolders().ToList();
            Assert.Contains(folderName, subfolders);
        }

        [Fact]
        public async Task SaveNote_WithEmptyTitle_ShouldReturnFailure()
        {
            var note = new IdeaNote
            {
                Title = string.Empty,
                Content = "No title",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            // Should return failure result and not save a file
            var result = await _noteManager.SaveNoteAsync(note);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public async Task SaveNote_WithInvalidFilenameCharacters_ShouldSanitizeAndSave()
        {
            var note = new IdeaNote
            {
                Title = "Invalid:/\\*?<>|Title",
                Content = "Sanitized title",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("Invalid:/\\*?<>|Title", notes[0].Title);
        }

        [Fact]
        public async Task RetrieveNotes_FromNonExistentFolder_ShouldReturnEmpty()
        {
            var nm = new NoteManagement(Path.Combine(_testFolder, "DoesNotExist"));
            var notes = (await nm.RetrieveNotesAsync()).ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public async Task DeleteNote_NonExistent_ShouldReturnFalse()
        {
            var result = await _noteManager.DeleteNoteAsync("NotThere");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNote_NonExistent_ShouldReturnFalse()
        {
            var note = new IdeaNote
            {
                Title = "NotThere",
                Content = "Nothing",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            var result = await _noteManager.UpdateNoteAsync("NotThere", note);
            Assert.False(result);
        }

        [Fact]
        public void CreateFolder_WithEmptyName_ShouldReturnFalse()
        {
            var result = _noteManager.CreateFolder("");
            Assert.False(result);
        }

        [Fact]
        public async Task SaveAndRetrieve_NoteWithEmptyContent_Works()
        {
            var note = new IdeaNote
            {
                Title = "EmptyContent",
                Content = string.Empty,
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal(string.Empty, notes[0].Content);
        }

        [Fact]
        public async Task SaveAndRetrieve_NoteWithMultiLineContent_Works()
        {
            var note = new IdeaNote
            {
                Title = "MultiLineContent",
                Content = "Line1\nLine2\nLine3",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Contains("Line2", notes[0].Content);
            Assert.Contains("Line3", notes[0].Content);
        }

        [Fact]
        public async Task RetrieveNotes_WhenEmptyFolder_ReturnsEmpty()
        {
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public async Task MoveNoteToTrash_AndRestore_Works()
        {
            var note = new IdeaNote
            {
                Title = "TrashMe",
                Content = "Trash content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            var moved = await _noteManager.MoveNoteToTrashAsync("TrashMe", null);
            Assert.True(moved);
            Assert.Empty((await _noteManager.RetrieveNotesAsync()).ToList());
            // Now restore
            var restored = await _noteManager.RestoreNoteFromTrashAsync("TrashMe");
            Assert.True(restored);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("TrashMe", notes[0].Title);
        }

        [Fact]
        public async Task MoveNoteToTrash_NonExistent_ReturnsFalse()
        {
            var moved = await _noteManager.MoveNoteToTrashAsync("NotThere", null);
            Assert.False(moved);
        }

        [Fact]
        public async Task RestoreNoteFromTrash_NonExistent_ReturnsFalse()
        {
            var restored = await _noteManager.RestoreNoteFromTrashAsync("NotThere");
            Assert.False(restored);
        }

        [Fact]
        public async Task PermanentlyDeleteNoteFromTrash_Works()
        {
            var note = new IdeaNote
            {
                Title = "PermanentDelete",
                Content = "To be deleted",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);
            await _noteManager.MoveNoteToTrashAsync("PermanentDelete", null);
            var deleted = await _noteManager.PermanentlyDeleteNoteFromTrashAsync("PermanentDelete");
            Assert.True(deleted);
            // Try restoring, should fail
            var restored = await _noteManager.RestoreNoteFromTrashAsync("PermanentDelete");
            Assert.False(restored);
        }

        [Fact]
        public async Task PermanentlyDeleteNoteFromTrash_NonExistent_ReturnsFalse()
        {
            var deleted = await _noteManager.PermanentlyDeleteNoteFromTrashAsync("NotThere");
            Assert.False(deleted);
        }

        [Fact]
        public async Task DeleteFolder_WithNotes_MovesNotesToTrashAndDeletesFolder()
        {
            var folderName = "FolderToDelete";
            _noteManager.CreateFolder(folderName);
            var note = new IdeaNote
            {
                Title = "InFolder",
                Content = "In folder",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note, folderName);
            var deleted = await _noteManager.DeleteFolderAsync(folderName);
            Assert.True(deleted);
            // Folder should not exist
            Assert.False(Directory.Exists(Path.Combine(_testFolder, folderName)));
            // Note should be in trash
            var restored = await _noteManager.RestoreNoteFromTrashAsync("InFolder");
            Assert.True(restored);
        }

        [Fact]
        public async Task DeleteFolder_EmptyFolder_DeletesSuccessfully()
        {
            var folderName = "EmptyFolder";
            _noteManager.CreateFolder(folderName);
            var deleted = await _noteManager.DeleteFolderAsync(folderName);
            Assert.True(deleted);
            Assert.False(Directory.Exists(Path.Combine(_testFolder, folderName)));
        }

        [Fact]
        public async Task DeleteFolder_NonExistent_ReturnsFalse()
        {
            var deleted = await _noteManager.DeleteFolderAsync("NotThere");
            Assert.False(deleted);
        }

        [Fact]
        public async Task DeleteFolder_TrashOrEmptyName_ReturnsFalse()
        {
            var trashResult = await _noteManager.DeleteFolderAsync("Trash");
            Assert.False(trashResult);
            var emptyResult = await _noteManager.DeleteFolderAsync("");
            Assert.False(emptyResult);
        }
    }
}
