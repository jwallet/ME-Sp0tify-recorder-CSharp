﻿using EspionSpotify.Models;
using Xunit;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Collections.Generic;
using EspionSpotify.AudioSessions;
using System.IO;
using NAudio.Lame;
using NAudio.Wave;
using System;
using EspionSpotify.Enums;

namespace EspionSpotify.Tests
{
    public class RecorderTests
    {
        private readonly IFrmEspionSpotify _formMock;
        private IFileSystem _fileSystem;
        private readonly UserSettings _userSettings;
        private readonly IMainAudioSession _audioSession;

        public RecorderTests()
        {
            _formMock = new Moq.Mock<IFrmEspionSpotify>().Object;
            _audioSession = new Moq.Mock<IMainAudioSession>().Object;
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>());
            _userSettings = new UserSettings();
        }

        [Fact]
        internal void IsSkipTrackActive_FalsyWhenTrackNotFound()
        {
            var userSettings = new UserSettings
            {
                RecordRecordingsStatus = Enums.RecordRecordingsStatus.Skip,
                OutputPath = @"C:\path",
                TrackTitleSeparator = "_",
                MediaFormat = Enums.MediaFormat.Mp3
            };
            var watcherTrackNotFound = new Recorder(
                _formMock,
                _audioSession,
                userSettings,
                new Track() { Artist = "Artist", Title = "Title" },
                _fileSystem);

            Assert.False(watcherTrackNotFound.IsSkipTrackActive);
        }

        [Fact]
        internal void IsSkipTrackActive_FalsyWhenTrackFoundButDuplicateEnabled()
        {
            var userSettingsCanDuplicate = new UserSettings
            {
                RecordRecordingsStatus = Enums.RecordRecordingsStatus.Duplicate,
                OutputPath = @"C:\path",
                TrackTitleSeparator = "_",
                MediaFormat = Enums.MediaFormat.Mp3
            };
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\path\Artist_-_Dont_Overwrite_Me.mp3", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });
            var watcherTrackFoundCanDuplicate = new Recorder(
                _formMock,
                _audioSession,
                userSettingsCanDuplicate,
                new Track() { Artist = "Artist", Title = "Dont Overwrite Me" },
                _fileSystem);

            Assert.False(watcherTrackFoundCanDuplicate.IsSkipTrackActive);
        }

        [Fact]
        internal void IsSkipTrackActive_FalsyWhenTrackFoundButOverwriteEnabled()
        {
            var userSettingsCanDuplicate = new UserSettings
            {
                RecordRecordingsStatus = Enums.RecordRecordingsStatus.Overwrite,
                OutputPath = @"C:\path",
                TrackTitleSeparator = "_",
                MediaFormat = Enums.MediaFormat.Mp3
            };
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\path\Artist_-_Dont_Overwrite_Me.mp3", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });
            var watcherTrackFoundCanDuplicate = new Recorder(
                _formMock,
                _audioSession,
                userSettingsCanDuplicate,
                new Track() { Artist = "Artist", Title = "Dont Overwrite Me" },
                _fileSystem);

            Assert.False(watcherTrackFoundCanDuplicate.IsSkipTrackActive);
        }

        [Fact]
        internal void IsTrackExists_TruthyWhenTrackFoundPlaying()
        {
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\path\Artist_-_Existing_Track.mp3", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) }
            });
            var userSettings = new UserSettings { OutputPath = @"C:\path", TrackTitleSeparator = "_", MediaFormat = Enums.MediaFormat.Mp3 };
            var watcherTrackFound = new Recorder(
                _formMock,
                _audioSession,
                userSettings,
                new Track() { Artist = "Artist", Title = "Existing Track", Playing = true },
                _fileSystem);

            Assert.True(watcherTrackFound.IsSkipTrackActive);
        }

        [Fact]
        internal void GetMediaFileWriter_WithWavFormat_ReturnsWaveFileWriter()
        {
            var userSettings = new UserSettings { MediaFormat = Enums.MediaFormat.Wav };

            var result = new Recorder(
                _formMock,
                _audioSession,
                userSettings,
                new Track(),
                _fileSystem).GetMediaFileWriter(new MemoryStream(), new WaveFormat());

            Assert.IsType<WaveFileWriter>(result);
        }

        [Fact]
        internal void GetMediaFileWriter_WithMp3Format_ReturnsLameMP3FileWriter()
        {
            var userSettings = new UserSettings { MediaFormat = Enums.MediaFormat.Mp3, Bitrate = LAMEPreset.ABR_160 };

            var result = new Recorder(
                _formMock,
                _audioSession,
                userSettings,
                new Track(),
                _fileSystem).GetMediaFileWriter(new MemoryStream(), new WaveFormat());

            Assert.IsType<LameMP3FileWriter>(result);
        }

        [Fact]
        internal void GetMediaFileWriter_WithUnknownFormat_ThrowsException()
        {
            var userSettings = new UserSettings { MediaFormat = (MediaFormat)Int16.MinValue, Bitrate = LAMEPreset.ABR_160 };

            var exception = Assert.Throws<Exception>(() => new Recorder(
                _formMock,
                _audioSession,
                userSettings,
                new Track(),
                _fileSystem).GetMediaFileWriter(new MemoryStream(), new WaveFormat()));

            Assert.Equal("Failed to get FileWriter", exception.Message);
        }
    }
}
