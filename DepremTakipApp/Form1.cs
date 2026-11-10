using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DepremTakipApp
{
    public partial class Form1 : Form
    {
        private EarthquakeService _earthquakeService;
        private Timer _refreshTimer;

        // UI Bileşenleri
        private DataGridView dgvEarthquakes;
        private Button btnRefresh;
        private ComboBox cmbSort;
        private ComboBox cmbCityFilter;
        private Label lblHeader;
        private Panel topPanel;

        private List<Earthquake> _currentEarthquakes = new List<Earthquake>();

        public Form1()
        {
            InitializeComponent();
            _earthquakeService = new EarthquakeService();

            // 9:16 Dikey Pencere Boyutu ve Estetik Form Ayarları
            this.Width = 540;
            this.Height = 960;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "FleyzTech | Canlı Deprem Monitörü";
            this.DoubleBuffered = true;

            // --- ARKA PLAN RESMİ YÜKLEME ---
            string imagePath = @"C:\Users\Mehmet Akif Balcı\source\repos\Ders1\DersDeneme\DepremTakipApp\DepremTakipApp\Resources\bg1.jpg";
            if (File.Exists(imagePath))
            {
                this.BackgroundImage = Image.FromFile(imagePath);
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            else
            {
                this.BackColor = Color.FromArgb(15, 15, 20);
            }

            InitializeCustomUI();
            ConfigureTimer();
        }

        private void InitializeCustomUI()
        {
            // 1. Üst Panel
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 115;
            topPanel.BackColor = Color.FromArgb(220, 15, 15, 22);
            this.Controls.Add(topPanel);

            // Başlık Label
            lblHeader = new Label();
            lblHeader.Text = "TÜRKİYE ANLIK DEPREM MONİTÖRÜ";
            lblHeader.ForeColor = Color.FromArgb(245, 245, 250);
            lblHeader.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            lblHeader.Location = new Point(15, 12);
            lblHeader.AutoSize = true;
            topPanel.Controls.Add(lblHeader);

            // Yenile Butonu
            btnRefresh = new Button();
            btnRefresh.Text = "🔄 Yenile";
            btnRefresh.BackColor = Color.FromArgb(0, 122, 204);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnRefresh.Location = new Point(15, 52);
            btnRefresh.Size = new Size(95, 34);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += async (s, e) => await LoadDataAndRefreshUI();
            topPanel.Controls.Add(btnRefresh);

            // Sıralama ComboBox'ı
            cmbSort = new ComboBox();
            cmbSort.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSort.Items.AddRange(new object[] { "En Yeni", "En Büyük Ml", "En Küçük Ml" });
            cmbSort.SelectedIndex = 0;
            cmbSort.Location = new Point(120, 56);
            cmbSort.Size = new Size(130, 26);
            cmbSort.Font = new Font("Segoe UI", 9);
            cmbSort.SelectedIndexChanged += (s, e) => ApplyFilterAndSorting();
            topPanel.Controls.Add(cmbSort);

            // Yer Filtreleme ComboBox'ı
            cmbCityFilter = new ComboBox();
            cmbCityFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCityFilter.Items.Add("Tüm Yerler");
            cmbCityFilter.SelectedIndex = 0;
            cmbCityFilter.Location = new Point(258, 56);
            cmbCityFilter.Size = new Size(155, 26);
            cmbCityFilter.Font = new Font("Segoe UI", 9);
            cmbCityFilter.SelectedIndexChanged += (s, e) => ApplyFilterAndSorting();
            topPanel.Controls.Add(cmbCityFilter);

            // 2. DataGridView (Tam oturan, taşma yapmayan estetik tablo)
            dgvEarthquakes = new DataGridView();
            dgvEarthquakes.Location = new Point(10, 125);
            dgvEarthquakes.Size = new Size(506, 790);
            dgvEarthquakes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            dgvEarthquakes.BackgroundColor = Color.FromArgb(20, 20, 28);
            dgvEarthquakes.BorderStyle = BorderStyle.None;
            dgvEarthquakes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvEarthquakes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEarthquakes.ReadOnly = true;
            dgvEarthquakes.RowHeadersVisible = false;
            dgvEarthquakes.AllowUserToAddRows = false;
            dgvEarthquakes.AllowUserToResizeRows = false;
            dgvEarthquakes.EnableHeadersVisualStyles = false;
            dgvEarthquakes.ScrollBars = ScrollBars.Vertical;

            // Tablo Başlık Stili
            dgvEarthquakes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 38);
            dgvEarthquakes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvEarthquakes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvEarthquakes.ColumnHeadersHeight = 44;
            dgvEarthquakes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Satır Stilleri
            dgvEarthquakes.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 30);
            dgvEarthquakes.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 230);
            dgvEarthquakes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(55, 70, 100);
            dgvEarthquakes.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvEarthquakes.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgvEarthquakes.RowTemplate.Height = 40;

            this.Controls.Add(dgvEarthquakes);

            // Çift arabellekleme
            typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(dgvEarthquakes, true, null);

            topPanel.SendToBack();
            dgvEarthquakes.BringToFront();
        }

        private void ConfigureTimer()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 30000; // 30 saniye
            _refreshTimer.Tick += async (sender, e) => await LoadDataAndRefreshUI();
            _refreshTimer.Start();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await LoadDataAndRefreshUI();
        }

        private async Task LoadDataAndRefreshUI()
        {
            btnRefresh.Enabled = false;
            btnRefresh.Text = "Yükleniyor...";

            var data = await _earthquakeService.GetEarthquakesAsync();

            if (data != null && data.Count > 0)
            {
                _currentEarthquakes = data;
                UpdateCityComboBox();
                ApplyFilterAndSorting();
                this.Text = $"Deprem Takip | Güncellendi: {DateTime.Now:HH:mm:ss}";
            }

            btnRefresh.Enabled = true;
            btnRefresh.Text = "🔄 Yenile";
        }

        private void UpdateCityComboBox()
        {
            string selectedBefore = cmbCityFilter.SelectedItem?.ToString() ?? "Tüm Yerler";

            cmbCityFilter.Items.Clear();
            cmbCityFilter.Items.Add("Tüm Yerler");

            var cities = _currentEarthquakes
                .Select(x => x.title.Split('-').Last().Split('(').First().Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            cmbCityFilter.Items.AddRange(cities);

            if (cmbCityFilter.Items.Contains(selectedBefore))
            {
                cmbCityFilter.SelectedItem = selectedBefore;
            }
            else
            {
                cmbCityFilter.SelectedIndex = 0;
            }
        }

        private void ApplyFilterAndSorting()
        {
            if (_currentEarthquakes == null || _currentEarthquakes.Count == 0) return;

            IEnumerable<Earthquake> filteredList = _currentEarthquakes;

            string selectedCity = cmbCityFilter.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedCity) && selectedCity != "Tüm Yerler")
            {
                filteredList = filteredList.Where(x => x.title.Contains(selectedCity));
            }

            if (cmbSort.SelectedIndex == 1) // En büyük Ml
            {
                filteredList = filteredList.OrderByDescending(x => x.mag);
            }
            else if (cmbSort.SelectedIndex == 2) // En küçük Ml
            {
                filteredList = filteredList.OrderBy(x => x.mag);
            }

            dgvEarthquakes.DataSource = filteredList.ToList();

            // Kolon Genişlikleri ve Başlıkları (Tarih sütununun dolması için 'date' property kontrolü)
            if (dgvEarthquakes.Columns["title"] != null)
            {
                dgvEarthquakes.Columns["title"].HeaderText = "Yer";
                dgvEarthquakes.Columns["title"].FillWeight = 130;
            }
            if (dgvEarthquakes.Columns["mag"] != null)
            {
                dgvEarthquakes.Columns["mag"].HeaderText = "Ml";
                dgvEarthquakes.Columns["mag"].FillWeight = 40;
            }
            if (dgvEarthquakes.Columns["depth"] != null)
            {
                dgvEarthquakes.Columns["depth"].HeaderText = "Derinlik";
                dgvEarthquakes.Columns["depth"].FillWeight = 55;
            }
            if (dgvEarthquakes.Columns["date"] != null)
            {
                dgvEarthquakes.Columns["date"].HeaderText = "Tarih ve Saat";
                dgvEarthquakes.Columns["date"].FillWeight = 115;
            }

            if (dgvEarthquakes.Columns["lat"] != null) dgvEarthquakes.Columns["lat"].Visible = false;
            if (dgvEarthquakes.Columns["lng"] != null) dgvEarthquakes.Columns["lng"].Visible = false;

            ColorRowsByMagnitude();
        }

        private void ColorRowsByMagnitude()
        {
            foreach (DataGridViewRow row in dgvEarthquakes.Rows)
            {
                if (row.Cells["mag"].Value != null)
                {
                    double mag = Convert.ToDouble(row.Cells["mag"].Value);

                    if (mag >= 4.0)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(160, 45, 45);
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                    else if (mag >= 3.0)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(140, 110, 30);
                        row.DefaultCellStyle.ForeColor = Color.White;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 30);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 230);
                    }
                }
            }
        }

        private void Form1_Load_1(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    }
}