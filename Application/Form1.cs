using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Application
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient Http = new HttpClient();
        public string path = "";
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Map files (*.map)|*.map";
                openFileDialog.Title = "Выберите файл";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFilePath = openFileDialog.FileName;
                    listBox1.Items.Add($"Выбран файл:");
                    listBox1.Items.Add($" {selectedFilePath}");
                    path = selectedFilePath;
                }
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (path == string.Empty)
            {
                listBox1.Items.Add($"Ошибка. Файл карты не выбран!");
                return;
            }
            await UploadMap();
        }
        private async Task UploadMap()
        {
            if (!System.IO.File.Exists(path))
            {
                listBox1.Items.Add($"Ошибка. Файл карты не найден:");
                listBox1.Items.Add(" " + path);
                return;
            }

            try
            {
                using FileStream fs = System.IO.File.OpenRead(path);
                string responseUrl = await UploadMapImpl(fs, Path.GetFileName(path));
                if (responseUrl != null)
                {
                    listBox1.Items.Add($"Успех. Карта успешно загружена по адресу:");
                    listBox1.Items.Add(" " + responseUrl);
                }
            }
            catch (Exception ex)
            {
                listBox1.Items.Add($"Ошибка. Ошибка загрузки карты:");
                listBox1.Items.Add(" " + ex.Message);
            }
        }

        private async Task<string> UploadMapImpl(Stream stream, string mapFileName)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (string.IsNullOrWhiteSpace(mapFileName))
            {
                throw new ArgumentNullException(nameof(mapFileName));
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException("Поток должен поддерживать чтение и поиск.", nameof(stream));
            }

            string requestUri = "https://api.facepunch.com/api/public/rust-map-upload/" + mapFileName;
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    stream.Seek(0L, SeekOrigin.Begin);
                    using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUri);
                    request.Content = new StreamContent(stream);
                    using HttpResponseMessage response = await Http.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(responseContent) || !responseContent.StartsWith("http"))
                        {
                            throw new Exception("Некорректный ответ от сервера при загрузке карты.");
                        }

                        return responseContent;
                    }

                    int statusCode = (int)response.StatusCode;
                    if (statusCode >= 400 && statusCode <= 499)
                    {
                        listBox1.Items.Add($"Ошибка. Сервер отклонил запрос на загрузку карты:");
                        listBox1.Items.Add(" " + await response.Content.ReadAsStringAsync());
                        return null;
                    }

                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    listBox1.Items.Add($"Предупреждение. Ошибка загрузки файла карты (попытка {i + 1} из 10):");
                    listBox1.Items.Add(" " + ex.Message);
                    await Task.Delay(1000 + i * 5000);
                }
            }

            listBox1.Items.Add("Ошибка. Не удалось загрузить файл карты!");
            return null;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string selectedText = listBox1.SelectedItem.ToString();
                Clipboard.SetText(selectedText);
            }
        }
    }
}

