// File: Views/MainView.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProjectZetaTeam;





namespace ProjectZetaTeam.Views
{
    public partial class MainView : UserControl
    {

//image file extensions
        private readonly string[] _supportedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

// ── Language support ──────────────────────────────────────────────────────────

        private enum Lang { Ru, En, Zh }
        private Lang _currentLang = Lang.Ru;

        // [label key][ ru / en / zh ]
        private static readonly Dictionary<string, string[]> _strings = new()
        {
            ["ThemeToggle"]  = ["Сменить тему",       "Toggle theme",   "切换主题"],
            ["SelectFile"]   = ["Выберите файл:",      "Select file:",   "选择文件："],
            ["SelectBtn"]    = ["Выбрать",             "Browse",         "浏览"],
            ["SelectMethod"] = ["Выберите метод:",     "Select method:", "选择方法："],
            ["LabelText"]    = ["Текст:",              "Text:",          "文本："],
            ["Watermark"]    = ["Введите текст...",    "Enter text...",  "输入文本…"],
            ["Encrypt"]      = ["Зашифровать",         "Encrypt",        "加密"],
            ["Decrypt"]      = ["Расшифровать",        "Decrypt",        "解密"],
        };

        private string T(string key) => _strings[key][(int)_currentLang];

        private void ApplyLanguage()
        {
            ThemeToggleButton.Content = T("ThemeToggle");
            LabelSelectFile.Text      = T("SelectFile");
            SelectFile.Content        = T("SelectBtn");
            LabelSelectMethod.Text    = T("SelectMethod");
            LabelText.Text            = T("LabelText");
            TextMessage.Watermark     = T("Watermark");
            Start.Content             = T("Encrypt");
            StartDeEncrypt.Content    = T("Decrypt");
        }

        private void LangToggleButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _currentLang = _currentLang switch
            {
                Lang.Ru => Lang.En,
                Lang.En => Lang.Zh,
                _       => Lang.Ru,
            };
            ApplyLanguage();
        }

// ─────────────────────────────────────────────────────────────────────────────

//init ui
//default using lsb
        public MainView()
        {
            InitializeComponent();
            MethodsSelect.SelectedIndex = 0;
        }


//dark/light theme

        private void ThemeToggleButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var currentTheme = Application.Current?.ActualThemeVariant;

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = currentTheme == ThemeVariant.Dark
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark;
            }
        }

//button_select file

        private async void SelectFileButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? selectedImagePath = await OpenImageFileDialogAsync();
                if (!string.IsNullOrEmpty(selectedImagePath))
                {
                    await LoadAndDisplayImageAsync(selectedImagePath);
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка выбора файла: {ex.Message}";
            }
        }
//get filepath or null
        private async Task<string?> OpenImageFileDialogAsync()
        {
            var topLevel = TopLevel.GetTopLevel(this);

            if (topLevel == null) return null;

            var storageProvider = topLevel.StorageProvider;

            var options = new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
                AllowMultiple = false,
                FileTypeFilter = new FilePickerFileType[]
                {
                    new FilePickerFileType("Изображения (*.jpg, *.png, *.webp)")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp" },
                        MimeTypes = new[] { "image/jpeg", "image/png", "image/webp" }
                    },
                    FilePickerFileTypes.All
                }
            };

            IReadOnlyList<IStorageFile> result = await storageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                return result[0].Path.LocalPath;
            }

            return null;
        }


        

//drag file on ui

        private void OnVisualElementDragOver(object? sender, DragEventArgs e)
        {
            if (e.DataTransfer.Contains(DataFormat.File))
                e.DragEffects = DragDropEffects.Copy;
            else
                e.DragEffects = DragDropEffects.None;
        }


//show the pic

        private async Task LoadAndDisplayImageAsync(string filePath)
        {
            if (SelectedImage.Source is IDisposable oldBitmap)
                oldBitmap.Dispose();

            try
            {
                await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var bitmap = new Bitmap(fileStream);
                SelectedImage.Source = bitmap;
                _currentInputFilePath = filePath;
                SelectedFileText.Text = filePath;
                MessageTextBlock.Text = string.Empty;
                TextMessage.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка загрузки: {ex.Message}";
                SelectedImage.Source = null;
                _currentInputFilePath = null;
                SelectedFileText.Text = string.Empty;
            }
        }



//drop the file on ui
        private async void OnVisualElementDrop(object? sender, DragEventArgs e)
        {
            try
            {
                if (e.DataTransfer.Contains(DataFormat.File))
                {
                    var files = e.DataTransfer.TryGetFiles();
                    if (files != null && files.Any())
                    {
                        var firstFile = files.First();
                        string localPath = firstFile.Path.LocalPath;
                        string extension = Path.GetExtension(localPath).ToLower();
                        if (_supportedExtensions.Contains(extension))
                        {
                            await LoadAndDisplayImageAsync(localPath);
                        }
                        else
                        {
                            MessageTextBlock.Text = "Ошибка: Данный формат файла не поддерживается.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageTextBlock.Text = $"Ошибка перетаскивания: {ex.Message}";
            }
        }


//the file path

        private string? _currentInputFilePath;


//button: encrypt,write text into image

        private async void OnEncryptAndSaveButtonClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentInputFilePath))
            {
                MessageTextBlock.Text = "Ошибка: Сначала выберите исходное изображение!";
                return;
            }

            string secretText = TextMessage.Text ?? "";
            if (string.IsNullOrWhiteSpace(secretText))
            {
                MessageTextBlock.Text = "Ошибка: Введите текст для сокрытия.";
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            string originalFileName = Path.GetFileName(_currentInputFilePath);
            string nameNoExt = Path.GetFileNameWithoutExtension(originalFileName);
            string ext = Path.GetExtension(originalFileName);
            string suggestedName = $"{nameNoExt}_stego{ext}";

            var saveOptions = new FilePickerSaveOptions
            {
                Title = "Сохранить изображение",
                SuggestedFileName = suggestedName,
                FileTypeChoices = new[]
                {
                    FilePickerFileTypes.ImagePng,
                    FilePickerFileTypes.ImageWebp
                }
            };

            var targetFile = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);
            if (targetFile == null) return;

            string outputPath = targetFile.Path.LocalPath;

            try
            {
                if (MethodsSelect.SelectedIndex == 0) // LSB
                {
                    await Task.Run(() => LsbSteganography.HideText(_currentInputFilePath, outputPath, secretText));

                    Logger.LogOperation(
                        operation: "ENCRYPT_LSB",
                        fileName: _currentInputFilePath,
                        message: secretText,
                        success: true
                    );
                    MessageTextBlock.Text = "Изображение успешно сохранено!";
                }
                else if (MethodsSelect.SelectedIndex == 1) // EXIF
                {
                    await Task.Run(() => ExifSteganography.HideMessageInExif(_currentInputFilePath, secretText, outputPath));

                    Logger.LogOperation(
                        operation: "ENCRYPT_EXIF",
                        fileName: _currentInputFilePath,
                        message: secretText,
                        success: true
                    );
                    MessageTextBlock.Text = "Сообщение успешно спрятано!";
                }
            }
            catch (Exception ex)
            {
                Logger.LogOperation(
                    operation: "ENCRYPT_ERROR",
                    fileName: _currentInputFilePath ?? "unknown",
                    message: secretText,
                    success: false,
                    error: ex.Message
                );
                MessageTextBlock.Text = $"Ошибка: {ex.Message}";
            }
        }



//read info from image

        private async void OnDecryptButtonClick(object? sender, RoutedEventArgs e)
        {
            if (MethodsSelect.SelectedIndex == 0) // LSB
            {
                try
                {
                    if (string.IsNullOrEmpty(_currentInputFilePath))  //if file name was empty hint a error
                    {
                        MessageTextBlock.Text = "Ошибка: Сначала перетащите или выберите зашифрованный файл!";
                        return;
                    }

                    string hiddenMessage = await Task.Run(() => LsbSteganography.ExtractText(_currentInputFilePath));

                    if (!string.IsNullOrEmpty(hiddenMessage)) // might show up some random symbol,need to fix here
                    {
                        TextMessage.Text = hiddenMessage;
                        MessageTextBlock.Text = "Сообщение успешно извлечено.";

                        Logger.LogOperation(
                            operation: "DECRYPT_LSB",
                            fileName: _currentInputFilePath,
                            message: hiddenMessage,
                            success: true
                        );
                    }
                    else
                    {
                        MessageTextBlock.Text = "Сообщений не найдено.";
                        Logger.LogOperation(
                            operation: "DECRYPT_LSB",
                            fileName: _currentInputFilePath,
                            message: "",
                            success: false,
                            error: "No message found"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogOperation(
                        operation: "DECRYPT_ERROR_LSB",
                        fileName: _currentInputFilePath ?? "unknown",
                        message: "",
                        success: false,
                        error: ex.Message
                    );
                    MessageTextBlock.Text = $"Ошибка дешифровки: {ex.Message}";
                }
            }
            else if (MethodsSelect.SelectedIndex == 1) // EXIF
            {
                try
                {
                    if (string.IsNullOrEmpty(_currentInputFilePath))
                    {
                        MessageTextBlock.Text = "Ошибка: Сначала перетащите или выберите зашифрованный файл!";
                        return;
                    }

                    string message = await Task.Run(() => ExifSteganography.ExtractMessageFromExif(_currentInputFilePath));

                    if (!string.IsNullOrEmpty(message))
                    {
                        TextMessage.Text = message;
                        MessageTextBlock.Text = "Сообщение успешно извлечено.";

                        Logger.LogOperation(
                            operation: "DECRYPT_EXIF",
                            fileName: _currentInputFilePath,
                            message: message,
                            success: true
                        );
                    }
                    else
                    {
                        MessageTextBlock.Text = "Сообщений не найдено.";
                        Logger.LogOperation(
                            operation: "DECRYPT_EXIF",
                            fileName: _currentInputFilePath,
                            message: "",
                            success: false,
                            error: "No message found"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogOperation(
                        operation: "DECRYPT_ERROR_EXIF",
                        fileName: _currentInputFilePath ?? "unknown",
                        message: "",
                        success: false,
                        error: ex.Message
                    );
                    MessageTextBlock.Text = $"Ошибка дешифровки: {ex.Message}";
                }
            }
        }
    }
}
