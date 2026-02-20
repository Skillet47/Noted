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
        public void SaveAndRetrieve_IdeaNote_Works()
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
            _noteManager.SaveNote(note);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal("Test Idea", notes[0].Title);
            Assert.Equal("Idea content", notes[0].Content);
            Assert.IsType<IdeaNote>(notes[0]);
        }

        [Fact]
        public void SaveAndRetrieve_ReminderNote_Works()
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
            _noteManager.SaveNote(note);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal("Test Reminder", notes[0].Title);
            Assert.Equal("Reminder content", notes[0].Content);
            Assert.True(notes[0].IsPinned);
            Assert.IsType<ReminderNote>(notes[0]);
        }

        [Fact]
        public void SaveAndRetrieve_TaskNote_Works()
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
            _noteManager.SaveNote(note);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal("Test Task", notes[0].Title);
            Assert.Equal(NoteTaskStatus.InProgress, ((TaskNote)notes[0]).Status);
            Assert.IsType<TaskNote>(notes[0]);
        }

        [Fact]
        public void UpdateNote_ChangesContentAndFormat()
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
            _noteManager.SaveNote(note);
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
            var result = _noteManager.UpdateNote("UpdateTest", updated);
            Assert.True(result);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal("New content", notes[0].Content);
            Assert.Equal(NoteFormat.Markdown, notes[0].Format);
        }

        [Fact]
        public void DeleteNote_RemovesFile()
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
            _noteManager.SaveNote(note);
            var deleted = _noteManager.DeleteNote("DeleteMe");
            Assert.True(deleted);
            var notes = _noteManager.RetrieveNotes().ToList();
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
        public void SaveNote_WithEmptyTitle_ShouldReturnFailure()
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
            var result = _noteManager.SaveNote(note);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public void SaveNote_WithInvalidFilenameCharacters_ShouldSanitizeAndSave()
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
            _noteManager.SaveNote(note);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal("Invalid:/\\*?<>|Title", notes[0].Title);
        }

        [Fact]
        public void RetrieveNotes_FromNonExistentFolder_ShouldReturnEmpty()
        {
            var nm = new NoteManagement(Path.Combine(_testFolder, "DoesNotExist"));
            var notes = nm.RetrieveNotes().ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public void DeleteNote_NonExistent_ShouldReturnFalse()
        {
            var result = _noteManager.DeleteNote("NotThere");
            Assert.False(result);
        }

        [Fact]
        public void UpdateNote_NonExistent_ShouldReturnFalse()
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
            var result = _noteManager.UpdateNote("NotThere", note);
            Assert.False(result);
        }

        [Fact]
        public void CreateFolder_WithEmptyName_ShouldReturnFalse()
        {
            var result = _noteManager.CreateFolder("");
            Assert.False(result);
        }

        [Fact]
        public void SaveAndRetrieve_NoteWithEmptyContent_Works()
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
            _noteManager.SaveNote(note);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal(string.Empty, notes[0].Content);
        }

        [Fact]
        public void SaveAndRetrieve_NoteWithMultiLineContent_Works()
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
            _noteManager.SaveNote(note);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Contains("Line2", notes[0].Content);
            Assert.Contains("Line3", notes[0].Content);
        }

        [Fact]
        public void RetrieveNotes_WhenEmptyFolder_ReturnsEmpty()
        {
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Empty(notes);
        }

        [Fact]
        public void MoveNoteToTrash_AndRestore_Works()
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
            _noteManager.SaveNote(note);
            var moved = _noteManager.MoveNoteToTrash("TrashMe", null);
            Assert.True(moved);
            Assert.Empty(_noteManager.RetrieveNotes().ToList());
            // Now restore
            var restored = _noteManager.RestoreNoteFromTrash("TrashMe");
            Assert.True(restored);
            var notes = _noteManager.RetrieveNotes().ToList();
            Assert.Single(notes);
            Assert.Equal("TrashMe", notes[0].Title);
        }

        [Fact]
        public void MoveNoteToTrash_NonExistent_ReturnsFalse()
        {
            var moved = _noteManager.MoveNoteToTrash("NotThere", null);
            Assert.False(moved);
        }

        [Fact]
        public void RestoreNoteFromTrash_NonExistent_ReturnsFalse()
        {
            var restored = _noteManager.RestoreNoteFromTrash("NotThere");
            Assert.False(restored);
        }

        [Fact]
        public void PermanentlyDeleteNoteFromTrash_Works()
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
            _noteManager.SaveNote(note);
            _noteManager.MoveNoteToTrash("PermanentDelete", null);
            var deleted = _noteManager.PermanentlyDeleteNoteFromTrash("PermanentDelete");
            Assert.True(deleted);
            // Try restoring, should fail
            var restored = _noteManager.RestoreNoteFromTrash("PermanentDelete");
            Assert.False(restored);
        }

        [Fact]
        public void PermanentlyDeleteNoteFromTrash_NonExistent_ReturnsFalse()
        {
            var deleted = _noteManager.PermanentlyDeleteNoteFromTrash("NotThere");
            Assert.False(deleted);
        }

        [Fact]
        public void DeleteFolder_WithNotes_MovesNotesToTrashAndDeletesFolder()
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
            _noteManager.SaveNote(note, folderName);
            var deleted = _noteManager.DeleteFolder(folderName);
            Assert.True(deleted);
            // Folder should not exist
            Assert.False(Directory.Exists(Path.Combine(_testFolder, folderName)));
            // Note should be in trash
            var restored = _noteManager.RestoreNoteFromTrash("InFolder");
            Assert.True(restored);
        }

        [Fact]
        public void DeleteFolder_EmptyFolder_DeletesSuccessfully()
        {
            var folderName = "EmptyFolder";
            _noteManager.CreateFolder(folderName);
            var deleted = _noteManager.DeleteFolder(folderName);
            Assert.True(deleted);
            Assert.False(Directory.Exists(Path.Combine(_testFolder, folderName)));
        }

        [Fact]
        public void DeleteFolder_NonExistent_ReturnsFalse()
        {
            var deleted = _noteManager.DeleteFolder("NotThere");
            Assert.False(deleted);
        }

        [Fact]
        public void DeleteFolder_TrashOrEmptyName_ReturnsFalse()
        {
            var trashResult = _noteManager.DeleteFolder("Trash");
            Assert.False(trashResult);
            var emptyResult = _noteManager.DeleteFolder("");
            Assert.False(emptyResult);
        }
    }
}
