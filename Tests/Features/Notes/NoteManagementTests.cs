using BusinessLogic.Core.Features.Notes;
using BusinessLogic.Models.Notes;
using BusinessLogic.Shared;

namespace BusinessLogicTests.Features.Notes
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
        public async Task SaveAndRetrieve_GeneralNote_Works()
        {
            var note = new GeneralNote
            {
                Title = "General Note",
                Content = "General content",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };

            await _noteManager.SaveNoteAsync(note);

            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("General Note", notes[0].Title);
            Assert.Equal("General content", notes[0].Content);
            Assert.IsType<GeneralNote>(notes[0]);
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
                Format = NoteFormat.PlainText,
                Stage = IdeaStage.Exploring
            };
            await _noteManager.SaveNoteAsync(note);
            var notes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(notes);
            Assert.Equal("Test Idea", notes[0].Title);
            Assert.Equal("Idea content", notes[0].Content);

            var restoredIdea = Assert.IsType<IdeaNote>(notes[0]);
            Assert.Equal(IdeaStage.Exploring, restoredIdea.Stage);
            Assert.Equal("List 3 approaches and the trade-off for each.", restoredIdea.GetFocusPrompt());
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
        public async Task UpdateNote_CapturesPreviousVersionInHistory()
        {
            var createdAt = DateTime.Now.AddHours(-2);
            var modifiedAt = DateTime.Now.AddHours(-1);

            var original = new IdeaNote
            {
                Title = "HistoryTest",
                Content = "Original content",
                CreatedAt = createdAt,
                ModifiedAt = modifiedAt,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };

            await _noteManager.SaveNoteAsync(original);

            var updated = new IdeaNote
            {
                Title = "HistoryTest",
                Content = "Updated content",
                CreatedAt = original.CreatedAt,
                ModifiedAt = original.ModifiedAt,
                IsPinned = true,
                Tag = NoteTag.Red,
                Format = NoteFormat.Markdown
            };

            var updateResult = await _noteManager.UpdateNoteAsync("HistoryTest", updated);
            Assert.True(updateResult);

            var history = await _noteManager.GetNoteHistoryAsync("HistoryTest");
            var entry = Assert.Single(history);

            Assert.Equal("HistoryTest", entry.Title);
            Assert.Equal("Original content", entry.Content);
            Assert.Equal(createdAt, entry.CreatedAt);
            Assert.Equal(modifiedAt, entry.ModifiedAt);
            Assert.Equal(NoteType.Idea, entry.Type);
            Assert.Equal(NoteFormat.PlainText, entry.Format);
            Assert.False(entry.IsPinned);
            Assert.True(entry.ChangedAtUtc <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateNote_WhenFormatChanges_HistoryRemainsAvailable()
        {
            var original = new IdeaNote
            {
                Title = "HistoryFormatChange",
                Content = "v1",
                CreatedAt = DateTime.Now.AddMinutes(-10),
                ModifiedAt = DateTime.Now.AddMinutes(-5),
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };

            await _noteManager.SaveNoteAsync(original);

            var updated = new IdeaNote
            {
                Title = "HistoryFormatChange",
                Content = "v2",
                CreatedAt = original.CreatedAt,
                ModifiedAt = original.ModifiedAt,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.Markdown
            };

            var updateResult = await _noteManager.UpdateNoteAsync("HistoryFormatChange", updated);
            Assert.True(updateResult);

            var history = await _noteManager.GetNoteHistoryAsync("HistoryFormatChange");
            var entry = Assert.Single(history);

            Assert.Equal("v1", entry.Content);
            Assert.Equal(NoteFormat.PlainText, entry.Format);
        }

        [Fact]
        public async Task RevertNoteToHistory_RestoresPreviousVersion_AndTracksCurrentAsHistory()
        {
            var original = new IdeaNote
            {
                Title = "RevertHistoryTest",
                Content = "v1",
                CreatedAt = DateTime.Now.AddMinutes(-20),
                ModifiedAt = DateTime.Now.AddMinutes(-10),
                IsPinned = false,
                Tag = NoteTag.Blue,
                Format = NoteFormat.PlainText
            };

            await _noteManager.SaveNoteAsync(original);

            var updated = new IdeaNote
            {
                Title = "RevertHistoryTest",
                Content = "v2",
                CreatedAt = original.CreatedAt,
                ModifiedAt = original.ModifiedAt,
                IsPinned = true,
                Tag = NoteTag.Red,
                Format = NoteFormat.Markdown
            };

            var updateResult = await _noteManager.UpdateNoteAsync("RevertHistoryTest", updated);
            Assert.True(updateResult);

            var beforeRevertHistory = await _noteManager.GetNoteHistoryAsync("RevertHistoryTest");
            var targetEntry = Assert.Single(beforeRevertHistory);

            var revertResult = await _noteManager.RevertNoteToHistoryAsync("RevertHistoryTest", targetEntry.ChangedAtUtc);
            Assert.True(revertResult);

            var currentNote = Assert.Single(await _noteManager.RetrieveNotesAsync(), n => n.Title == "RevertHistoryTest");
            Assert.Equal("v1", currentNote.Content);
            Assert.False(currentNote.IsPinned);
            Assert.Equal(NoteTag.Blue, currentNote.Tag);
            Assert.Equal(NoteFormat.PlainText, currentNote.Format);

            var afterRevertHistory = await _noteManager.GetNoteHistoryAsync("RevertHistoryTest");
            Assert.Equal(2, afterRevertHistory.Count);
            Assert.Equal("v2", afterRevertHistory[1].Content);
        }

        [Fact]
        public async Task RevertNoteToHistory_WithUnknownSnapshot_ReturnsFalse()
        {
            var note = new IdeaNote
            {
                Title = "MissingHistoryEntry",
                Content = "Current",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };

            await _noteManager.SaveNoteAsync(note);

            var result = await _noteManager.RevertNoteToHistoryAsync("MissingHistoryEntry", DateTime.UtcNow.AddYears(-1));
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateNote_History_IsCappedAtMaxEntries()
        {
            // Arrange: create one note and apply 55 updates — more than the 50-entry cap.
            var note = new GeneralNote
            {
                Title = "HistoryPruneTest",
                Content = "v0",
                CreatedAt = DateTime.Now.AddHours(-1),
                ModifiedAt = DateTime.Now.AddHours(-1),
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);

            for (var i = 1; i <= 55; i++)
            {
                var update = new GeneralNote
                {
                    Title = "HistoryPruneTest",
                    Content = $"v{i}",
                    CreatedAt = note.CreatedAt,
                    ModifiedAt = DateTime.Now,
                    IsPinned = false,
                    Tag = NoteTag.None,
                    Format = NoteFormat.PlainText
                };
                await _noteManager.UpdateNoteAsync("HistoryPruneTest", update);
            }

            // Assert: no more than 50 entries are stored.
            var history = await _noteManager.GetNoteHistoryAsync("HistoryPruneTest");
            Assert.True(history.Count <= 50, $"Expected at most 50 history entries, but got {history.Count}.");

            // The newest snapshots should be retained (v5–v54 at minimum).
            Assert.Contains(history, e => e.Content.StartsWith("v5"));
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

        [Fact]
        public async Task MoveNote_FromFolderToRoot_Works()
        {
            var folderName = "FolderA";
            _noteManager.CreateFolder(folderName);
            var note = new IdeaNote
            {
                Title = "MoveToRoot",
                Content = "Moving to root",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note, folderName);

            var result = await _noteManager.MoveNoteAsync("MoveToRoot", folderName, null);

            Assert.True(result);
            var rootNotes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Single(rootNotes);
            Assert.Equal("MoveToRoot", rootNotes[0].Title);
            var folderNotes = (await _noteManager.RetrieveNotesAsync(folderName)).ToList();
            Assert.Empty(folderNotes);
        }

        [Fact]
        public async Task MoveNote_FromRootToFolder_Works()
        {
            var folderName = "FolderB";
            _noteManager.CreateFolder(folderName);
            var note = new IdeaNote
            {
                Title = "MoveToFolder",
                Content = "Moving to folder",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);

            var result = await _noteManager.MoveNoteAsync("MoveToFolder", null, folderName);

            Assert.True(result);
            var folderNotes = (await _noteManager.RetrieveNotesAsync(folderName)).ToList();
            Assert.Single(folderNotes);
            Assert.Equal("MoveToFolder", folderNotes[0].Title);
            var rootNotes = (await _noteManager.RetrieveNotesAsync()).ToList();
            Assert.Empty(rootNotes);
        }

        [Fact]
        public async Task MoveNote_RoundTrip_FolderToRootToFolder_Works()
        {
            var folderName = "FolderC";
            _noteManager.CreateFolder(folderName);
            var note = new IdeaNote
            {
                Title = "RoundTrip",
                Content = "Round trip note",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note, folderName);

            // Move to root
            var toRoot = await _noteManager.MoveNoteAsync("RoundTrip", folderName, null);
            Assert.True(toRoot);
            Assert.Single((await _noteManager.RetrieveNotesAsync()).ToList());
            Assert.Empty((await _noteManager.RetrieveNotesAsync(folderName)).ToList());

            // Move back to folder
            var backToFolder = await _noteManager.MoveNoteAsync("RoundTrip", null, folderName);
            Assert.True(backToFolder);
            Assert.Empty((await _noteManager.RetrieveNotesAsync()).ToList());
            Assert.Single((await _noteManager.RetrieveNotesAsync(folderName)).ToList());
        }

        [Fact]
        public async Task MoveNote_NonExistent_ReturnsFalse()
        {
            var folderName = "FolderD";
            _noteManager.CreateFolder(folderName);

            var result = await _noteManager.MoveNoteAsync("NoSuchNote", null, folderName);

            Assert.False(result);
        }

        [Fact]
        public async Task MoveNote_SameFolder_ReturnsFalse()
        {
            var note = new IdeaNote
            {
                Title = "SameFolder",
                Content = "Same folder note",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now,
                IsPinned = false,
                Tag = NoteTag.None,
                Format = NoteFormat.PlainText
            };
            await _noteManager.SaveNoteAsync(note);

            var result = await _noteManager.MoveNoteAsync("SameFolder", null, null);

            Assert.False(result);
        }

        [Fact]
        public async Task MoveNote_EmptyTitle_ReturnsFalse()
        {
            var result = await _noteManager.MoveNoteAsync("", null, "AnyFolder");

            Assert.False(result);
        }
    }
}
