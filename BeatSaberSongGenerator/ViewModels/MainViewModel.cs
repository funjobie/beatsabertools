using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using BeatSaberSongGenerator.Generators;
using BeatSaberSongGenerator.IO;
using Microsoft.Win32;
using NAudio.Wave;

namespace BeatSaberSongGenerator.ViewModels
{
    public class MainViewModel : NotifyPropertyChangedBase
    {
        private string lastDirectory;
        public MainViewModel()
        {
            BrowseAudioCommand = new RelayCommand(BrowseAudio);
            BrowseCoverCommand = new RelayCommand(BrowseCover);
            GenerateCommand = new RelayCommand(GenerateSong, CanGenerateSong);
            BrowseMultipleAudioCommand = new RelayCommand(GenerateAllSongs, CanGenerateAllSongs);
        }

        private float skillLevel = 0.5f;
        public float SkillLevel
        {
            get => skillLevel;
            set
            {
                skillLevel = value;
                OnPropertyChanged();
            }
        }

        private string songName;
        public string SongName
        {
            get => songName;
            set
            {
                songName = value;
                OnPropertyChanged();
            }
        }

        private string author;
        public string Author
        {
            get => author;
            set
            {
                author = value;
                OnPropertyChanged();
            }
        }

        private string audioFilePath;
        public string AudioFilePath
        {
            get => audioFilePath;
            set
            {
                var isReadableAudio = IsSupportedByNAudio(value);// || Path.GetExtension(value)?.ToLowerInvariant() == ".ogg";

                if (!isReadableAudio)
                {
                    MessageBox.Show("Song cannot be read");
                }
                else
                {
                    audioFilePath = value;
                    SongName = Path.GetFileNameWithoutExtension(audioFilePath);
                    TagLib.File file = TagLib.File.Create(audioFilePath);
                    String performers = file.Tag.JoinedPerformers;
                    String composers = file.Tag.JoinedComposers;
                    Author = "Performed by: " + performers + "; Composed by: " + composers;
                }
                OnPropertyChanged();
            }
        }

        private static bool IsSupportedByNAudio(string audioFilePath)
        {
            try
            {
                using (new AudioFileReader(audioFilePath))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private string coverFilePath;
        public string CoverFilePath
        {
            get => coverFilePath;
            set
            {
                if (Path.GetExtension(value)?.ToLowerInvariant() != ".jpg")
                    MessageBox.Show("Cover must be a JPG-image");
                else
                    coverFilePath = value;
                OnPropertyChanged();
            }
        }

        private const string DefaultGenerateButtonText = "Generate";
        private string generateButtonText = DefaultGenerateButtonText;
        public string GenerateButtonText
        {
            get => generateButtonText;
            private set
            {
                generateButtonText = value;
                OnPropertyChanged();
            }
        }

        private string batchProcessingText = "0/0";
        public string BatchProcessingText
        {
            get => batchProcessingText;
            set
            {
                batchProcessingText = value;
                OnPropertyChanged() ;
            }
        }

        private Visibility progressBarVisibility = Visibility.Collapsed;
        public Visibility ProgressBarVisibility
        {
            get => progressBarVisibility;
            private set
            {
                progressBarVisibility = value;
                OnPropertyChanged();
            }
        }


        public ICommand BrowseAudioCommand { get; }
        public ICommand BrowseCoverCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand BrowseMultipleAudioCommand { get; }

        private void BrowseAudio()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = lastDirectory,
                CheckFileExists = true,
                Title = "Select audio file"
            };
            if (openFileDialog.ShowDialog() != true)
                return;
            AudioFilePath = openFileDialog.FileName;
            lastDirectory = Path.GetDirectoryName(openFileDialog.FileName);
        }

        private void BrowseCover()
        {
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = lastDirectory,
                CheckFileExists = true,
                Title = "Select cover image"
            };
            if (openFileDialog.ShowDialog() != true)
                return;
            CoverFilePath = openFileDialog.FileName;
            lastDirectory = Path.GetDirectoryName(openFileDialog.FileName);
        }

        private bool CanGenerateSong()
        {
            return !string.IsNullOrWhiteSpace(SongName)
                && !string.IsNullOrWhiteSpace(Author)
                && File.Exists(AudioFilePath)
                && File.Exists(CoverFilePath);
        }

        private bool CanGenerateAllSongs()
        {
            return File.Exists(CoverFilePath);
        }

        private void GenerateSong()
        {
            Thread newThread = new Thread(this.DoGenerate);
            newThread.Start();

            GenerateButtonText = "Generating...";
            ProgressBarVisibility = Visibility.Visible;
            MessageBox.Show("This may take a few minutes and the application might hang during that time. " + Environment.NewLine
                            + "Unless you get dialogs other than this one you just have to be patient.");

        }

        public void DoGenerate(object data)
        {
            var songGenerator = new SongGenerator(new SongGeneratorSettings
            {
                SkillLevel = SkillLevel,
            });
            var song = songGenerator.Generate(SongName, Author, AudioFilePath, CoverFilePath);
            var songStorer = new SongStorer();
            var outputDirectory = Path.Combine(
                Path.GetDirectoryName(AudioFilePath),
                Path.GetFileNameWithoutExtension(AudioFilePath));
            songStorer.Store(song, outputDirectory);

            MessageBox.Show("Song successfully generated");
            GenerateButtonText = DefaultGenerateButtonText;
            ProgressBarVisibility = Visibility.Collapsed;
        }

        private void GenerateAllSongs()
        {
            if (MessageBox.Show("This may take a few minutes and the application might hang during that time. " + Environment.NewLine
                + "It takes especially long when the ogg files have not been generated yet. " + Environment.NewLine
                + "Unless you get dialogs other than this one you just have to be patient.") == MessageBoxResult.OK)
            {
                var openFilesDialog = new OpenFileDialog
                {
                    InitialDirectory = lastDirectory,
                    CheckFileExists = true,
                    Multiselect = true,
                    Title = "Select files to process"
                };
                if (openFilesDialog.ShowDialog() != true)
                    return;
                string[] files = openFilesDialog.FileNames;
                if (files.Length > 0)
                {
                    GenerateButtonText = "Generating...";
                    ProgressBarVisibility = Visibility.Visible;

                    lastDirectory = Path.GetDirectoryName(files[0]);
                    BatchProcessingText = "0/" + files.Length;
                    Thread newThread = new Thread(this.DoGenerateAll);
                    newThread.Start(files);
                }
            }
        }
        private void DoGenerateAll(object data)
        {
            string[] files = (string[])data;
            for(int i = 0; i < files.Length; ++i)
            {
                BatchProcessingText = (i+1) + "/" + files.Length;
                var songGenerator = new SongGenerator(new SongGeneratorSettings
                {
                    SkillLevel = SkillLevel,
                });

                AudioFilePath = files[i];
                SongName = Path.GetFileNameWithoutExtension(files[i]);
                TagLib.File file = TagLib.File.Create(files[i]);
                String performers = file.Tag.JoinedPerformers;
                String composers = file.Tag.JoinedComposers;
                Author = "Performed by: " + performers + "; Composed by: " + composers;

                var song = songGenerator.Generate(SongName, Author, files[i], CoverFilePath);
                var songStorer = new SongStorer();
                var outputDirectory = Path.Combine(
                    Path.GetDirectoryName(files[i]),
                    Path.GetFileNameWithoutExtension(files[i]));
                songStorer.Store(song, outputDirectory);
            }
            BatchProcessingText = "finished";
            GenerateButtonText = DefaultGenerateButtonText;
            ProgressBarVisibility = Visibility.Collapsed;
            MessageBox.Show("Songs successfully generated");
        }
    }
}
