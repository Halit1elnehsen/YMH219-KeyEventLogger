namespace YMH219_KeyEventLogger;

public partial class Form1 : Form
{
    private readonly KeyboardRecorder _recorder = new();
    private readonly List<KeyboardEventRecord> _records = [];
    private readonly HashSet<Keys> _downInCurrentRun = [];
    private Label _status = null!;
    private ListView _list = null!;
    private Label _count = null!;
    private Button _start = null!;

    public Form1()
    {
        InitializeComponent(); BuildInterface(); KeyPreview = true;
        KeyDown += CaptureKeyDown; KeyUp += CaptureKeyUp; _recorder.Recorded += AddRecord;
    }

    private void BuildInterface()
    {
        Text = "Tuş İzleri — YMH219"; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(760, 560); ClientSize = new Size(960, 690); BackColor = Color.FromArgb(244, 246, 250);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), RowCount = 4, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 105)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); Controls.Add(root);
        var header = new Panel { Dock = DockStyle.Fill };
        var title = new Label { Text = "TUŞ İZLERİ", Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.FromArgb(35, 49, 77), AutoSize = true, Location = new Point(0, 4) };
        var subtitle = new Label { Text = "Klavye olaylarını yalnızca bu pencere odaktayken kaydeder.", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(104, 119, 143), AutoSize = true, Location = new Point(3, 49) };
        _status = new Label { Text = "●  KAYIT KAPALI", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(116, 129, 145), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(610, 25) };
        header.Controls.AddRange([title, subtitle, _status]); header.Resize += (_, _) => _status.Left = header.ClientSize.Width - _status.Width - 4; root.Controls.Add(header, 0, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 7, 0, 0) };
        _start = Button("KAYDI BAŞLAT", Color.FromArgb(53, 111, 194), 140); _start.Click += (_, _) => StartRecording();
        var stop = Button("DURDUR", Color.FromArgb(78, 92, 116), 108); stop.Click += (_, _) => StopRecording();
        var clear = Button("TEMİZLE", Color.FromArgb(235, 237, 242), 108, Color.FromArgb(63, 75, 96)); clear.Click += (_, _) => ClearRecords();
        var export = Button("DIŞA AKTAR  ↓", Color.FromArgb(235, 237, 242), 142, Color.FromArgb(63, 75, 96)); export.Click += (_, _) => ExportRecords();
        actions.Controls.AddRange([_start, stop, clear, export]); root.Controls.Add(actions, 0, 1);
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18) };
        _list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10), BackColor = Color.White, ForeColor = Color.FromArgb(43, 57, 79) };
        _list.Columns.Add("SAAT", 180); _list.Columns.Add("OLAY", 170); _list.Columns.Add("TUŞ", 240); card.Controls.Add(_list); root.Controls.Add(card, 0, 2);
        _count = new Label { Text = "0 olay  ·  Tekrar basımları bir kez sayılır", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(105, 119, 143), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        root.Controls.Add(_count, 0, 3);
    }

    private static Button Button(string text, Color back, int width, Color? fore = null) => new() { Text = text, Width = width, Height = 42, Margin = new Padding(0, 0, 12, 0), FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore ?? Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };

    private void StartRecording() { _downInCurrentRun.Clear(); _recorder.Start(); _status.Text = "●  KAYIT AÇIK"; _status.ForeColor = Color.FromArgb(36, 151, 104); _start.Enabled = false; }
    private void StopRecording() { _recorder.Stop(); _downInCurrentRun.Clear(); _status.Text = "●  KAYIT KAPALI"; _status.ForeColor = Color.FromArgb(116, 129, 145); _start.Enabled = true; }
    private void ClearRecords() { _records.Clear(); _list.Items.Clear(); RefreshCount(); }
    private void CaptureKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_recorder.IsRecording || e.KeyCode == Keys.Tab) return;
        if (_downInCurrentRun.Add(e.KeyCode)) _recorder.KeyDown(e.KeyCode);
    }
    private void CaptureKeyUp(object? sender, KeyEventArgs e)
    {
        if (!_recorder.IsRecording) return;
        if (_downInCurrentRun.Remove(e.KeyCode)) _recorder.KeyUp(e.KeyCode);
    }
    private void AddRecord(KeyboardEventRecord record)
    {
        _records.Add(record);
        var item = new ListViewItem(record.Timestamp.ToString("HH:mm:ss.fff")); item.SubItems.Add(record.EventName); item.SubItems.Add(record.KeyName); _list.Items.Add(item); item.EnsureVisible(); RefreshCount();
    }
    private void RefreshCount() => _count.Text = $"{_records.Count} olay  ·  Tuşlar bu pencere dışındayken okunmaz";
    private void ExportRecords()
    {
        if (_records.Count == 0) { MessageBox.Show(this, "Dışa aktarılacak kayıt bulunmuyor. Önce kaydı başlatıp tuşlara basın.", "Kayıt yok", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası (*.txt)|*.txt", FileName = $"tus-kayitlari-{DateTime.Now:yyyyMMdd-HHmm}.txt", Title = "Tuş kayıtlarını kaydet" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllLines(dialog.FileName, ["Zaman\tOlay\tTuş", .. _records.Select(r => r.ToFileString())]); MessageBox.Show(this, "Kayıt dosyası başarıyla kaydedildi.", "Dışa aktarma tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (IOException ex) { MessageBox.Show(this, $"Dosya kaydedilemedi: {ex.Message}", "Kayıt hatası", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
