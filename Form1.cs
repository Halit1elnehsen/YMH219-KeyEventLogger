namespace YMH219_KeyEventLogger;

public partial class Form1 : Form
{
    private readonly KeyboardRecorder _recorder = new();
    private readonly List<KeyboardEventRecord> _records = [];
    private readonly HashSet<Keys> _downInCurrentRun = [];
    private Label _status = null!;
    private ListView _list = null!;
    private Label _count = null!;
    private Label _lastKey = null!, _lastEvent = null!;
    private Label _emptyState = null!;
    private Button _start = null!;

    public Form1()
    {
        InitializeComponent(); BuildInterface(); KeyPreview = true;
        KeyDown += CaptureKeyDown; KeyUp += CaptureKeyUp; _recorder.Recorded += AddRecord;
    }

    private void BuildInterface()
    {
        Text = "Tuş İzleri — YMH219"; StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(800, 620); ClientSize = new Size(1020, 760); BackColor = Color.FromArgb(235, 240, 244); Font = new Font("Segoe UI", 9.5f);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); Controls.Add(root);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(27, 38, 54), Padding = new Padding(20, 12, 20, 10) };
        var title = new Label { Text = "TUŞ İZLERİ", Font = new Font("Segoe UI", 21, FontStyle.Bold), ForeColor = Color.FromArgb(245, 249, 252), AutoSize = true, Location = new Point(20, 8) };
        var subtitle = new Label { Text = "Bu pencereye ait klavye olayları", Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(171, 189, 204), AutoSize = true, Location = new Point(23, 49) };
        _status = new Label { Text = "●  KAYIT KAPALI", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(175, 192, 205), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(700, 28) };
        header.Controls.AddRange([title, subtitle, _status]); header.Resize += (_, _) => _status.Left = header.ClientSize.Width - _status.Width - 22; root.Controls.Add(header, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 8, 0, 0) };
        _start = ActionButton("Kaydı başlat", Color.FromArgb(22, 139, 126), Color.White, 150); _start.Click += (_, _) => StartRecording();
        var stop = ActionButton("Durdur", Color.FromArgb(52, 67, 84), Color.White, 118); stop.Click += (_, _) => StopRecording();
        var clear = ActionButton("Temizle", Color.FromArgb(220, 228, 233), Color.FromArgb(49, 66, 81), 118); clear.Click += (_, _) => ClearRecords();
        var export = ActionButton("Dosyaya aktar", Color.FromArgb(234, 190, 87), Color.FromArgb(46, 53, 62), 158); export.Click += (_, _) => ExportRecords();
        actions.Controls.AddRange([_start, stop, clear, export]); root.Controls.Add(actions, 0, 1);

        var keyPanel = new SurfacePanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(227, 242, 241), Padding = new Padding(18), CornerRadius = 14, OutlineColor = Color.FromArgb(200, 222, 220) };
        var keycap = new Panel { BackColor = Color.FromArgb(29, 43, 59), Size = new Size(102, 64), Location = new Point(18, 17), Padding = new Padding(8) };
        keycap.Paint += (_, e) => { using var edge = new Pen(Color.FromArgb(85, 205, 192), 2); e.Graphics.DrawRectangle(edge, 2, 2, keycap.Width - 5, keycap.Height - 6); };
        _lastKey = new Label { Text = "—", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Font = new Font("Segoe UI", 18, FontStyle.Bold) }; keycap.Controls.Add(_lastKey);
        var keyTitle = new Label { Text = "SON TUŞ", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.FromArgb(58, 105, 104), AutoSize = true, Location = new Point(138, 19) };
        _lastEvent = new Label { Text = "Kayıt başlatıldığında burada görünür.", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(43, 69, 77), AutoSize = true, Location = new Point(138, 43) };
        keyPanel.Controls.AddRange([keycap, keyTitle, _lastEvent]); root.Controls.Add(keyPanel, 0, 2);

        var card = new SurfacePanel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(14), CornerRadius = 15 };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = "Olay geçmişi", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(34, 48, 65) }, 0, 0);
        _list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), BackColor = Color.White, ForeColor = Color.FromArgb(43, 57, 73), HideSelection = false };
        _list.Columns.Add("ZAMAN", 190); _list.Columns.Add("HAREKET", 210); _list.Columns.Add("TUŞ", 260);
        var listRegion = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        _list.Dock = DockStyle.Fill; listRegion.Controls.Add(_list);
        _emptyState = new Label { Text = "Henüz kayıt yok\nKaydı başlatıp bir tuşa basın.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(111, 128, 140), BackColor = Color.White };
        listRegion.Controls.Add(_emptyState);
        listRegion.Resize += (_, _) => { if (_list.Columns.Count == 3) _list.Columns[2].Width = Math.Max(240, listRegion.ClientSize.Width - 430); };
        table.Controls.Add(listRegion, 0, 1); card.Controls.Add(table); root.Controls.Add(card, 0, 3);
        _count = new Label { Text = "0 olay  ·  Yalnızca bu uygulama penceresi dinlenir", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(76, 97, 111), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        root.Controls.Add(_count, 0, 4);
    }

    private static DepthButton ActionButton(string text, Color back, Color fore, int width) => new() { Text = text, Width = width, Height = 44, Margin = new Padding(0, 0, 10, 0), BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

    private void StartRecording() { _downInCurrentRun.Clear(); _recorder.Start(); _status.Text = "●  KAYIT AÇIK · BU PENCERE"; _status.ForeColor = Color.FromArgb(124, 233, 194); _lastKey.Text = "—"; _lastEvent.Text = "Dinleme başladı. Bu uygulamanın dışı okunmaz."; _start.Enabled = false; }
    private void StopRecording() { _recorder.Stop(); _downInCurrentRun.Clear(); _status.Text = "●  KAYIT KAPALI"; _status.ForeColor = Color.FromArgb(175, 192, 205); _lastEvent.Text = "Kayıt durduruldu."; _start.Enabled = true; }
    private void ClearRecords() { _records.Clear(); _list.Items.Clear(); _emptyState.Visible = true; RefreshCount(); }
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
        _emptyState.Visible = false;
        if (record.EventName == "BASMA") _lastKey.Text = record.KeyName;
        _lastEvent.Text = $"{record.EventName}  ·  {record.Timestamp:HH:mm:ss.fff}";
        var item = new ListViewItem(record.Timestamp.ToString("HH:mm:ss.fff")); item.SubItems.Add(record.EventName); item.SubItems.Add(record.KeyName); _list.Items.Add(item); item.EnsureVisible(); RefreshCount();
    }
    private void RefreshCount() => _count.Text = $"{_records.Count} olay  ·  Tuşlar bu pencere dışındayken okunmaz";
    private void ExportRecords()
    {
        if (_records.Count == 0) { MessageBox.Show(this, "Dışa aktarılacak kayıt bulunmuyor. Önce kaydı başlatıp tuşlara basın.", "Kayıt yok", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        using var dialog = new SaveFileDialog { Filter = "Metin dosyası (*.txt)|*.txt", FileName = $"tus-kayitlari-{DateTime.Now:yyyyMMdd-HHmm}.txt", Title = "Tuş kayıtlarını kaydet" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllLines(dialog.FileName, ["Zaman\tOlay\tTuş", .. _records.Select(r => r.ToFileString())]); MessageBox.Show(this, "Kayıt dosyası başarıyla kaydedildi.", "Dışa aktarma tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { MessageBox.Show(this, $"Dosya kaydedilemedi: {ex.Message}", "Kayıt hatası", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
