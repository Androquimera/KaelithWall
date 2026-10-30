// Kwall -- trocador de papel de parede leve.
//
// Prepara um BMP completo antes de pedir ao Windows a troca do wallpaper.
// A imagem anterior permanece aplicada durante a preparacao.
//
// Compila com o csc.exe do .NET Framework 4 que ja vem no Windows -- sem SDK,
// sem pacote, sem dependencia. Ver compilar.sh.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Kwall")]
[assembly: AssemblyProduct("Kwall")]
[assembly: AssemblyDescription("Trocador leve de papel de parede para Windows")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]
[assembly: AssemblyInformationalVersion("1.2.0")]

namespace Kwall
{
    static class Nativo
    {
        public const uint SPI_SETDESKWALLPAPER = 0x0014;
        public const uint SPIF_UPDATEINIFILE = 0x01;
        public const uint SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SystemParametersInfoW(uint acao, uint parametro, string valor, uint flags);

        // A mesma comparacao que o Explorer usa: "9.jpg" vem antes de "10.jpg".
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string a, string b);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr icone);

        [DllImport("kernel32.dll")]
        public static extern bool AttachConsole(int processo);
    }

    sealed class OrdemNatural : IComparer<string>
    {
        public int Compare(string a, string b) { return Nativo.StrCmpLogicalW(a, b); }
    }

    // Ajuste da imagem na tela, nos valores que o proprio Windows entende.
    static class Ajuste
    {
        public const int Centralizar = 0;
        public const int Esticar = 2;
        public const int Ajustar = 6;    // cabe inteira, sem cortar
        public const int Preencher = 10; // enche a tela, corta a sobra
    }

    static class Enquadramento
    {
        public static Rectangle Preencher(Size imagem, Size tela, PointF? posicaoManual)
        {
            PointF posicao = posicaoManual ?? new PointF(0.5f, 0.5f);
            float escala = Math.Max(
                tela.Width / (float)imagem.Width,
                tela.Height / (float)imagem.Height);
            int largura = Math.Max(tela.Width, (int)Math.Ceiling(imagem.Width * escala));
            int altura = Math.Max(tela.Height, (int)Math.Ceiling(imagem.Height * escala));
            float x = Math.Max(0f, Math.Min(1f, posicao.X));
            float y = Math.Max(0f, Math.Min(1f, posicao.Y));
            return new Rectangle(
                (int)Math.Round((tela.Width - largura) * x),
                (int)Math.Round((tela.Height - altura) * y),
                largura, altura);
        }
    }

    sealed class Config
    {
        public string Pasta = "";
        public int Intervalo = 10;        // segundos
        public bool Aleatorio = true;
        public bool Pausado = false;
        public bool Subpastas = false;
        public int Ajuste = Kwall.Ajuste.Preencher;
        public string Atual = "";
        public bool MemoriaExibicao = false;
        public bool PosicoesIdentificadas = false;
        public readonly List<string> PastasRecentes = new List<string>();
        public readonly List<string> OrdemAleatoria = new List<string>();
        public readonly List<string> ExibidasNaRodada = new List<string>();
        readonly Dictionary<string, PointF> posicoes =
            new Dictionary<string, PointF>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, PointF> posicoesPorId =
            new Dictionary<string, PointF>(StringComparer.Ordinal);

        static string Caminho()
        {
            string raiz = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(raiz, "Kwall");
            Directory.CreateDirectory(dir);
            string atual = Path.Combine(dir, "config.txt");
            string dirAntigo = Path.Combine(raiz, "KaelithWall");
            string antigo = Path.Combine(dirAntigo, "config.txt");
            if (!File.Exists(atual) && File.Exists(antigo))
            {
                try { File.Move(antigo, atual); }
                catch
                {
                    File.Copy(antigo, atual, false);
                    File.Delete(antigo);
                }
            }
            if (File.Exists(atual) && File.Exists(antigo))
                File.Delete(antigo);
            try
            {
                if (Directory.Exists(dirAntigo) &&
                    Directory.GetFileSystemEntries(dirAntigo).Length == 0)
                    Directory.Delete(dirAntigo);
            }
            catch { }
            return atual;
        }

        public static Config Carregar()
        {
            Config c = new Config();
            try
            {
                string caminho = Caminho();
                if (!File.Exists(caminho)) return c;
                foreach (string linha in File.ReadAllLines(caminho, Encoding.UTF8))
                {
                    int igual = linha.IndexOf('=');
                    if (igual <= 0) continue;
                    string chave = linha.Substring(0, igual).Trim();
                    string valor = linha.Substring(igual + 1).Trim();
                    switch (chave)
                    {
                        case "pasta": c.Pasta = valor; break;
                        case "intervalo": int.TryParse(valor, out c.Intervalo); break;
                        case "aleatorio": c.Aleatorio = (valor == "1"); break;
                        case "pausado": c.Pausado = (valor == "1"); break;
                        case "subpastas": c.Subpastas = (valor == "1"); break;
                        case "ajuste": int.TryParse(valor, out c.Ajuste); break;
                        case "atual": c.Atual = valor; break;
                        case "memoria_exibicao": c.MemoriaExibicao = (valor == "1"); break;
                        case "posicoes_identificadas":
                            c.PosicoesIdentificadas = (valor == "1"); break;
                        case "ordem_aleatoria":
                            try
                            {
                                c.OrdemAleatoria.Add(Encoding.UTF8.GetString(
                                    Convert.FromBase64String(valor)));
                            }
                            catch { }
                            break;
                        case "exibida":
                            try
                            {
                                c.ExibidasNaRodada.Add(Encoding.UTF8.GetString(
                                    Convert.FromBase64String(valor)));
                            }
                            catch { }
                            break;
                        case "pasta_recente":
                            try
                            {
                                c.PastasRecentes.Add(Encoding.UTF8.GetString(
                                    Convert.FromBase64String(valor)));
                            }
                            catch { }
                            break;
                        case "posicao":
                            string[] partes = valor.Split('|');
                            float x, y;
                            if (partes.Length == 3 &&
                                float.TryParse(partes[1], NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out x) &&
                                float.TryParse(partes[2], NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out y))
                            {
                                string arquivo = Encoding.UTF8.GetString(
                                    Convert.FromBase64String(partes[0]));
                                c.posicoes[arquivo] = new PointF(x, y);
                            }
                            break;
                        case "posicao_id":
                            string[] idPartes = valor.Split('|');
                            float idX, idY;
                            if (idPartes.Length == 3 &&
                                float.TryParse(idPartes[1], NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out idX) &&
                                float.TryParse(idPartes[2], NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out idY))
                                c.posicoesPorId[idPartes[0]] = new PointF(idX, idY);
                            break;
                    }
                }
            }
            catch { }
            if (c.Intervalo < 5) c.Intervalo = 5;
            if (!string.IsNullOrEmpty(c.Pasta)) c.LembrarPasta(c.Pasta);
            if (!c.PosicoesIdentificadas)
            {
                foreach (KeyValuePair<string, PointF> p in c.posicoes)
                {
                    string identidade = IdentidadeImagem(p.Key);
                    if (identidade != null) c.posicoesPorId[identidade] = p.Value;
                }
                c.PosicoesIdentificadas = true;
            }
            return c;
        }

        public void Salvar()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("pasta=" + Pasta);
                sb.AppendLine("intervalo=" + Intervalo.ToString());
                sb.AppendLine("aleatorio=" + (Aleatorio ? "1" : "0"));
                sb.AppendLine("pausado=" + (Pausado ? "1" : "0"));
                sb.AppendLine("subpastas=" + (Subpastas ? "1" : "0"));
                sb.AppendLine("ajuste=" + Ajuste.ToString());
                sb.AppendLine("atual=" + Atual);
                sb.AppendLine("memoria_exibicao=" + (MemoriaExibicao ? "1" : "0"));
                sb.AppendLine("posicoes_identificadas=" +
                    (PosicoesIdentificadas ? "1" : "0"));
                foreach (string arquivo in OrdemAleatoria)
                    sb.AppendLine("ordem_aleatoria=" + Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(arquivo)));
                foreach (string arquivo in ExibidasNaRodada)
                    sb.AppendLine("exibida=" + Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(arquivo)));
                foreach (string pasta in PastasRecentes)
                    sb.AppendLine("pasta_recente=" + Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(pasta)));
                foreach (KeyValuePair<string, PointF> p in posicoes)
                {
                    string arquivo = Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Key));
                    sb.AppendLine("posicao=" + arquivo + "|" +
                        p.Value.X.ToString("R", CultureInfo.InvariantCulture) + "|" +
                        p.Value.Y.ToString("R", CultureInfo.InvariantCulture));
                }
                foreach (KeyValuePair<string, PointF> p in posicoesPorId)
                    sb.AppendLine("posicao_id=" + p.Key + "|" +
                        p.Value.X.ToString("R", CultureInfo.InvariantCulture) + "|" +
                        p.Value.Y.ToString("R", CultureInfo.InvariantCulture));
                File.WriteAllText(Caminho(), sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public PointF? PosicaoDe(string caminho)
        {
            PointF valor;
            string identidade;
            if (posicoes.TryGetValue(caminho, out valor))
            {
                identidade = IdentidadeImagem(caminho);
                if (identidade != null) posicoesPorId[identidade] = valor;
                return valor;
            }
            identidade = IdentidadeImagem(caminho);
            return identidade != null && posicoesPorId.TryGetValue(identidade, out valor)
                ? (PointF?)valor : null;
        }

        public void GuardarPosicao(string caminho, PointF valor)
        {
            posicoes[caminho] = valor;
            string identidade = IdentidadeImagem(caminho);
            if (identidade != null) posicoesPorId[identidade] = valor;
        }

        static string IdentidadeImagem(string caminho)
        {
            try
            {
                using (SHA256 sha = SHA256.Create())
                using (FileStream arquivo = File.OpenRead(caminho))
                    return Convert.ToBase64String(sha.ComputeHash(arquivo));
            }
            catch { return null; }
        }

        public void MarcarExibida(string caminho)
        {
            if (!ExibidasNaRodada.Exists(delegate(string p) {
                return string.Equals(p, caminho, StringComparison.OrdinalIgnoreCase);
            })) ExibidasNaRodada.Add(caminho);
            for (int i = OrdemAleatoria.Count - 1; i >= 0; i--)
                if (string.Equals(OrdemAleatoria[i], caminho,
                    StringComparison.OrdinalIgnoreCase))
                    OrdemAleatoria.RemoveAt(i);
        }

        public void LembrarPasta(string pasta)
        {
            for (int i = PastasRecentes.Count - 1; i >= 0; i--)
                if (string.Equals(PastasRecentes[i], pasta,
                    StringComparison.OrdinalIgnoreCase))
                    PastasRecentes.RemoveAt(i);
            PastasRecentes.Insert(0, pasta);
        }
    }

    sealed class EditorPosicao : Form
    {
        readonly Image imagem;
        PointF posicao;
        Point ultimoMouse;
        bool arrastando;
        readonly Button aplicar;
        readonly Button cancelar;

        public PointF Posicao { get { return posicao; } }

        public EditorPosicao(Image imagemAtual, PointF posicaoAtual)
        {
            imagem = imagemAtual;
            posicao = posicaoAtual;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen.Bounds;
            TopMost = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Color.Black;
            Cursor = Cursors.SizeAll;

            aplicar = new Button();
            aplicar.Text = "Aplicar";
            aplicar.Size = new Size(96, 34);
            aplicar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            aplicar.Cursor = Cursors.Default;
            aplicar.UseVisualStyleBackColor = false;
            aplicar.FlatStyle = FlatStyle.Flat;
            aplicar.FlatAppearance.BorderColor = Color.FromArgb(117, 168, 255);
            aplicar.BackColor = Color.FromArgb(47, 111, 214);
            aplicar.ForeColor = Color.White;
            aplicar.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
            aplicar.AccessibleName = "Aplicar enquadramento";
            aplicar.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(aplicar);

            cancelar = new Button();
            cancelar.Text = "Cancelar";
            cancelar.Size = new Size(96, 34);
            cancelar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            cancelar.Cursor = Cursors.Default;
            cancelar.UseVisualStyleBackColor = false;
            cancelar.FlatStyle = FlatStyle.Flat;
            cancelar.FlatAppearance.BorderColor = Color.FromArgb(130, 130, 136);
            cancelar.BackColor = Color.FromArgb(48, 48, 52);
            cancelar.ForeColor = Color.White;
            cancelar.Font = SystemFonts.MessageBoxFont;
            cancelar.AccessibleName = "Cancelar ajuste manual";
            cancelar.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancelar);

            AcceptButton = aplicar;
            CancelButton = cancelar;
            PosicionarBotoes();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PosicionarBotoes();
            Invalidate();
        }

        void PosicionarBotoes()
        {
            if (aplicar == null || cancelar == null) return;
            cancelar.Location = new Point(ClientSize.Width - cancelar.Width - 24,
                ClientSize.Height - cancelar.Height - 22);
            aplicar.Location = new Point(cancelar.Left - aplicar.Width - 10, cancelar.Top);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle destino = Enquadramento.Preencher(imagem.Size, ClientSize, posicao);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(imagem, destino);

            Rectangle faixa = new Rectangle(0, ClientSize.Height - 78, ClientSize.Width, 78);
            using (Brush fundo = new SolidBrush(Color.FromArgb(185, 18, 18, 20)))
                e.Graphics.FillRectangle(fundo, faixa);
            using (Brush texto = new SolidBrush(Color.White))
                e.Graphics.DrawString("Arraste a imagem para escolher o enquadramento  •  Enter aplica  •  Esc cancela",
                    SystemFonts.MessageBoxFont, texto, new PointF(24, ClientSize.Height - 50));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            arrastando = true;
            ultimoMouse = e.Location;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!arrastando) return;
            Rectangle r = Enquadramento.Preencher(imagem.Size, ClientSize, posicao);
            int x = Math.Max(ClientSize.Width - r.Width,
                Math.Min(0, r.X + e.X - ultimoMouse.X));
            int y = Math.Max(ClientSize.Height - r.Height,
                Math.Min(0, r.Y + e.Y - ultimoMouse.Y));
            posicao = new PointF(
                r.Width == ClientSize.Width ? 0.5f : x / (float)(ClientSize.Width - r.Width),
                r.Height == ClientSize.Height ? 0.5f : y / (float)(ClientSize.Height - r.Height));
            ultimoMouse = e.Location;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            arrastando = false;
            Capture = false;
        }
    }

    sealed class Bandeja : ApplicationContext
    {
        static readonly string[] Extensoes =
            { ".jpg", ".jpeg", ".jfif", ".png", ".bmp", ".gif", ".tif", ".tiff", ".dib" };

        readonly Config cfg;
        readonly NotifyIcon icone;
        readonly System.Windows.Forms.Timer relogio;
        readonly SynchronizationContext linhaPrincipal;
        readonly RandomNumberGenerator sorte = RandomNumberGenerator.Create();
        readonly bool teste;   // em autoteste nao mexe no papel de parede de verdade

        List<string> ordem = new List<string>();
        int pos = -1;
        int trocando;              // trava simples: 0 livre, 1 ocupado
        int reaplicar;             // guarda o ultimo pedido feito durante uma troca
        string erroUltimo = "";

        ToolStripMenuItem itemAtual, itemPausar, itemAleatorio, itemSequencia, itemSubpastas;
        ToolStripMenuItem menuIntervalo, menuAjuste, menuPastasRecentes;

        public Bandeja() : this(false) { }

        public Bandeja(bool modoTeste)
        {
            teste = modoTeste;
            linhaPrincipal = SynchronizationContext.Current;
            cfg = Config.Carregar();

            icone = new NotifyIcon();
            icone.Icon = CriarIcone();
            icone.Text = "Kwall";
            icone.Visible = !teste;
            icone.DoubleClick += delegate { Trocar(+1); };
            icone.ContextMenuStrip = MontarMenu();

            relogio = new System.Windows.Forms.Timer();
            relogio.Tick += delegate { Trocar(+1); };

            Varrer();
            // Comeca de onde parou, sem trocar o wallpaper ja na tela.
            if (!string.IsNullOrEmpty(cfg.Atual))
            {
                int i = ordem.IndexOf(cfg.Atual);
                if (i >= 0) pos = i;
            }

            if (!teste && (string.IsNullOrEmpty(cfg.Pasta) || !Directory.Exists(cfg.Pasta)))
                EscolherPasta(null, null);

            Aplicar();
        }

        // ---------- menu ----------

        ContextMenuStrip MontarMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.ShowImageMargin = false;

            itemAtual = new ToolStripMenuItem("(nenhuma imagem)");
            itemAtual.Enabled = false;
            m.Items.Add(itemAtual);
            m.Items.Add(new ToolStripSeparator());

            m.Items.Add(Item("Pro&ximo", delegate { Trocar(+1); }));
            m.Items.Add(Item("&Anterior", delegate { Trocar(-1); }));
            itemPausar = Item("&Pausar", delegate { AlternarPausa(); });
            m.Items.Add(itemPausar);
            m.Items.Add(new ToolStripSeparator());

            m.Items.Add(Item("Abrir &local da imagem", delegate { AbrirLocal(); }));
            m.Items.Add(new ToolStripSeparator());

            itemAleatorio = Item("Ordem a&leatoria", delegate { DefinirOrdem(true); });
            itemSequencia = Item("Ordem em &sequencia", delegate { DefinirOrdem(false); });
            m.Items.Add(itemAleatorio);
            m.Items.Add(itemSequencia);
            m.Items.Add(new ToolStripSeparator());

            menuIntervalo = new ToolStripMenuItem("&Intervalo");
            AddIntervalo(10, "10 segundos");
            AddIntervalo(30, "30 segundos");
            AddIntervalo(60, "1 minuto");
            AddIntervalo(300, "5 minutos");
            AddIntervalo(600, "10 minutos");
            AddIntervalo(1800, "30 minutos");
            AddIntervalo(3600, "1 hora");
            m.Items.Add(menuIntervalo);

            menuAjuste = new ToolStripMenuItem("A&juste na tela");
            AddAjuste(Ajuste.Preencher, "Preencher (corte central, sem barras)");
            AddAjuste(Ajuste.Ajustar, "Ajustar (imagem inteira, centralizada)");
            AddAjuste(Ajuste.Esticar, "Esticar (sem corte, pode deformar)");
            AddAjuste(Ajuste.Centralizar, "Centralizar (tamanho original, pode vazar)");
            m.Items.Add(menuAjuste);
            m.Items.Add(Item("Ajustar &manualmente...", delegate { AjustarManualmente(); }));

            m.Items.Add(Item("Escolher &pasta...", EscolherPasta));
            menuPastasRecentes = new ToolStripMenuItem("Pastas &recentes");
            menuPastasRecentes.DropDownOpening += delegate { MontarPastasRecentes(); };
            m.Items.Add(menuPastasRecentes);
            itemSubpastas = Item("Incluir subpastas", delegate
            {
                cfg.Subpastas = !cfg.Subpastas;
                cfg.Salvar();
                Varrer();
                Atualizar();
            });
            m.Items.Add(itemSubpastas);
            m.Items.Add(Item("&Recarregar pasta", delegate { Varrer(); Atualizar(); }));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add(Item("Sai&r", delegate { Sair(); }));
            return m;
        }

        static ToolStripMenuItem Item(string texto, EventHandler acao)
        {
            ToolStripMenuItem i = new ToolStripMenuItem(texto);
            i.Click += acao;
            return i;
        }

        void AddIntervalo(int segundos, string texto)
        {
            int s = segundos;
            ToolStripMenuItem i = Item(texto, delegate
            {
                cfg.Intervalo = s;
                cfg.Salvar();
                Atualizar();
            });
            i.Tag = s;
            menuIntervalo.DropDownItems.Add(i);
        }

        void AddAjuste(int modo, string texto)
        {
            int a = modo;
            ToolStripMenuItem i = Item(texto, delegate
            {
                cfg.Ajuste = a;
                cfg.Salvar();
                Aplicar();
                Atualizar();
            });
            i.Tag = a;
            menuAjuste.DropDownItems.Add(i);
        }

        void Atualizar()
        {
            string nome = ArquivoAtual();
            itemAtual.Text = string.IsNullOrEmpty(nome)
                ? (string.IsNullOrEmpty(erroUltimo) ? "(nenhuma imagem na pasta)" : erroUltimo)
                : Path.GetFileName(nome) + "   (" + (pos + 1) + "/" + ordem.Count + ")";

            itemPausar.Text = cfg.Pausado ? "Re&tomar" : "&Pausar";
            itemAleatorio.Checked = cfg.Aleatorio;
            itemSequencia.Checked = !cfg.Aleatorio;
            itemSubpastas.Checked = cfg.Subpastas;

            foreach (ToolStripItem i in menuIntervalo.DropDownItems)
            {
                ToolStripMenuItem mi = i as ToolStripMenuItem;
                if (mi != null) mi.Checked = ((int)mi.Tag == cfg.Intervalo);
            }
            foreach (ToolStripItem i in menuAjuste.DropDownItems)
            {
                ToolStripMenuItem mi = i as ToolStripMenuItem;
                if (mi != null) mi.Checked = ((int)mi.Tag == cfg.Ajuste);
            }

            string dica = "Kwall" + (cfg.Pausado ? " (pausado)" : "");
            if (!string.IsNullOrEmpty(nome)) dica += "\n" + Path.GetFileName(nome);
            if (dica.Length > 63) dica = dica.Substring(0, 63);
            icone.Text = dica;

            // O relogio so corre quando ha mais de uma imagem e nao esta pausado.
            bool deveCorrer = !cfg.Pausado && ordem.Count > 1;
            relogio.Interval = Math.Max(5, cfg.Intervalo) * 1000;
            relogio.Enabled = deveCorrer;
        }

        // ---------- lista de imagens ----------

        static bool Suportado(string caminho)
        {
            string ext = Path.GetExtension(caminho).ToLowerInvariant();
            for (int i = 0; i < Extensoes.Length; i++)
                if (Extensoes[i] == ext) return true;
            return false;
        }

        void Embaralhar(List<string> lista, string evitarNoInicio)
        {
            for (int i = lista.Count - 1; i > 0; i--)
            {
                int j = Sortear(i + 1);
                string t = lista[i]; lista[i] = lista[j]; lista[j] = t;
            }
            // Nao repetir de cara a imagem que acabou de sair.
            if (lista.Count > 1 && evitarNoInicio != null && lista[0] == evitarNoInicio)
            {
                string t = lista[0]; lista[0] = lista[1]; lista[1] = t;
            }
        }

        int Sortear(int limite)
        {
            byte[] bytes = new byte[4];
            uint teto = uint.MaxValue - (uint.MaxValue % (uint)limite);
            uint valor;
            do { sorte.GetBytes(bytes); valor = BitConverter.ToUInt32(bytes, 0); }
            while (valor >= teto);
            return (int)(valor % (uint)limite);
        }

        static int IndiceDe(List<string> lista, string caminho)
        {
            if (caminho == null) return -1;
            for (int i = 0; i < lista.Count; i++)
                if (string.Equals(lista[i], caminho, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        void Varrer()
        {
            string atual = ArquivoAtual();
            if (atual == null) atual = cfg.Atual;
            List<string> achados = new List<string>();
            erroUltimo = "";
            try
            {
                if (Directory.Exists(cfg.Pasta))
                {
                    SearchOption opcao = cfg.Subpastas
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly;
                    foreach (string p in Directory.EnumerateFiles(cfg.Pasta, "*", opcao))
                        if (Suportado(p)) achados.Add(p);
                }
                else if (!string.IsNullOrEmpty(cfg.Pasta))
                {
                    erroUltimo = "(pasta nao encontrada)";
                }
            }
            catch (Exception e) { erroUltimo = "(erro ao ler a pasta: " + e.Message + ")"; }

            achados.Sort(new OrdemNatural());
            if (cfg.Aleatorio)
            {
                HashSet<string> encontrados = new HashSet<string>(
                    achados, StringComparer.OrdinalIgnoreCase);
                HashSet<string> exibidas = new HashSet<string>(
                    cfg.ExibidasNaRodada, StringComparer.OrdinalIgnoreCase);
                exibidas.IntersectWith(encontrados);

                // Versoes antigas nao sabiam o que realmente chegou a tela. Comeca
                // uma rodada verificavel usando o wallpaper atual como ponto zero.
                if (!cfg.MemoriaExibicao ||
                    (achados.Count > 0 && exibidas.Count >= achados.Count))
                {
                    exibidas.Clear();
                    cfg.OrdemAleatoria.Clear();
                    cfg.MemoriaExibicao = true;
                    if (atual != null && encontrados.Contains(atual)) exibidas.Add(atual);
                }

                cfg.ExibidasNaRodada.Clear();
                cfg.ExibidasNaRodada.AddRange(exibidas);
                HashSet<string> incluidos = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);
                List<string> preservada = new List<string>();
                foreach (string p in cfg.OrdemAleatoria)
                    if (encontrados.Contains(p) && !exibidas.Contains(p) &&
                        incluidos.Add(p)) preservada.Add(p);

                List<string> novos = new List<string>();
                foreach (string p in achados)
                    if (!exibidas.Contains(p) && incluidos.Add(p)) novos.Add(p);
                Embaralhar(novos, null);
                preservada.AddRange(novos);

                cfg.OrdemAleatoria.Clear();
                cfg.OrdemAleatoria.AddRange(preservada);
                ordem = new List<string>();
                if (atual != null && encontrados.Contains(atual) && exibidas.Contains(atual))
                    ordem.Add(atual);
                ordem.AddRange(preservada);
                if (!teste) cfg.Salvar();
            }
            else
            {
                ordem = achados;
            }
            pos = IndiceDe(ordem, atual);
            if (pos < 0) pos = (ordem.Count > 0) ? 0 : -1;
        }

        string ArquivoAtual()
        {
            if (pos < 0 || pos >= ordem.Count) return null;
            return ordem[pos];
        }

        // ---------- acoes ----------

        void Trocar(int passo)
        {
            if (ordem.Count == 0)
            {
                Varrer();
                if (ordem.Count == 0) { Atualizar(); return; }
            }
            else if (ordem.Count == 1)
            {
                pos = 0;
            }
            else
            {
                string saindo = ArquivoAtual();
                int novo = pos + passo;
                if (novo >= ordem.Count || novo < 0)
                {
                    // Revarre antes da virada para incluir arquivos novos. No modo
                    // aleatorio, uma nova rodada so nasce depois de esgotar a atual.
                    Varrer();
                    if (ordem.Count == 0) { Atualizar(); return; }
                    pos = IndiceDe(ordem, saindo);
                    novo = pos + passo;
                    if (novo >= 0 && novo < ordem.Count)
                    {
                        pos = novo;
                    }
                    else
                    {
                        if (cfg.Aleatorio && passo > 0)
                        {
                            Varrer();
                        }
                        pos = (passo > 0) ? 0 : ordem.Count - 1;
                    }
                }
                else
                {
                    pos = novo;
                }
            }
            Aplicar();
            // Cada troca manual reinicia a contagem, para a proxima automatica
            // nao chegar logo depois do clique.
            if (relogio.Enabled) { relogio.Stop(); relogio.Start(); }
        }

        void Aplicar()
        {
            Atualizar();
            string caminho = ArquivoAtual();
            if (string.IsNullOrEmpty(caminho)) return;

            if (teste)
            {
                cfg.Atual = caminho;
                cfg.MarcarExibida(caminho);
                return;
            }

            // A troca sai da linha principal: num JPEG grande o Windows pode
            // levar algumas centenas de milissegundos recodificando, e o menu
            // da bandeja nao pode congelar junto.
            if (!ReservarTroca(ref trocando, ref reaplicar)) return;
            int ajuste = cfg.Ajuste;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    PointF? posicaoManual;
                    lock (cfg) { posicaoManual = cfg.PosicaoDe(caminho); }
                    Definir(caminho, ajuste, posicaoManual);
                    lock (cfg)
                    {
                        cfg.Atual = caminho;
                        cfg.MarcarExibida(caminho);
                        cfg.Salvar();
                    }
                }
                catch (Exception e) { Avisar("(erro ao aplicar: " + e.Message + ")"); }
                finally
                {
                    bool repetir = LiberarTroca(ref trocando, ref reaplicar);
                    if (repetir && linhaPrincipal != null)
                        linhaPrincipal.Post(delegate { Aplicar(); }, null);
                }
            });
        }

        static bool ReservarTroca(ref int trocando, ref int reaplicar)
        {
            if (Interlocked.Exchange(ref trocando, 1) == 0) return true;
            Interlocked.Exchange(ref reaplicar, 1);
            return false;
        }

        static bool LiberarTroca(ref int trocando, ref int reaplicar)
        {
            Interlocked.Exchange(ref trocando, 0);
            return Interlocked.Exchange(ref reaplicar, 0) == 1;
        }

        static void Definir(string caminho, int ajuste, PointF? posicaoManual)
        {
            // Dois slots: nunca sobrescrever o arquivo que o Windows esta exibindo.
            string dir = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "Kwall");
            Directory.CreateDirectory(dir);
            string anterior;
            using (RegistryKey desktop = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                anterior = desktop == null ? "" : (string)desktop.GetValue("Wallpaper", "");
            string primeiro = Path.Combine(dir, "wallpaper-0.bmp");
            string pronto = string.Equals(anterior, primeiro, StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(dir, "wallpaper-1.bmp") : primeiro;
            int ajusteEfetivo = ajuste;
            using (Image imagem = Image.FromFile(caminho))
            {
                Orientar(imagem);
                Size tamanho = imagem.Size;
                Rectangle destino = new Rectangle(Point.Empty, tamanho);
                if (ajuste == Ajuste.Preencher)
                {
                    tamanho = Screen.PrimaryScreen.Bounds.Size;
                    destino = Enquadramento.Preencher(imagem.Size, tamanho, posicaoManual);
                    ajusteEfetivo = Ajuste.Centralizar;
                }
                using (Bitmap bmp = new Bitmap(tamanho.Width, tamanho.Height,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Black);
                        if (ajuste == Ajuste.Preencher)
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.DrawImage(imagem, destino);
                        }
                        else CopiarPixels(g, imagem);
                    }
                    bmp.Save(pronto, System.Drawing.Imaging.ImageFormat.Bmp);
                }
            }
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                if (k != null)
                {
                    k.SetValue("WallpaperStyle", ajusteEfetivo.ToString(), RegistryValueKind.String);
                    k.SetValue("TileWallpaper", "0", RegistryValueKind.String);
                    k.SetValue("WallpaperOriginX", "0", RegistryValueKind.String);
                    k.SetValue("WallpaperOriginY", "0", RegistryValueKind.String);
                }
            }
            if (!Nativo.SystemParametersInfoW(
                Nativo.SPI_SETDESKWALLPAPER, 0, pronto,
                Nativo.SPIF_UPDATEINIFILE | Nativo.SPIF_SENDCHANGE))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            LimparCacheLegado();
        }

        static void LimparCacheLegado()
        {
            string antigo = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData), "KaelithWall");
            try
            {
                foreach (string nome in new string[] { "wallpaper-0.bmp", "wallpaper-1.bmp" })
                {
                    string arquivo = Path.Combine(antigo, nome);
                    if (File.Exists(arquivo)) File.Delete(arquivo);
                }
                if (Directory.Exists(antigo) &&
                    Directory.GetFileSystemEntries(antigo).Length == 0)
                    Directory.Delete(antigo);
            }
            catch { }
        }

        static RotateFlipType RotacaoExif(int orientacao)
        {
            switch (orientacao)
            {
                case 2: return RotateFlipType.RotateNoneFlipX;
                case 3: return RotateFlipType.Rotate180FlipNone;
                case 4: return RotateFlipType.Rotate180FlipX;
                case 5: return RotateFlipType.Rotate90FlipX;
                case 6: return RotateFlipType.Rotate90FlipNone;
                case 7: return RotateFlipType.Rotate270FlipX;
                case 8: return RotateFlipType.Rotate270FlipNone;
                default: return RotateFlipType.RotateNoneFlipNone;
            }
        }

        static void CopiarPixels(Graphics destino, Image imagem)
        {
            destino.DrawImage(imagem,
                new Rectangle(0, 0, imagem.Width, imagem.Height),
                0, 0, imagem.Width, imagem.Height, GraphicsUnit.Pixel);
        }

        static void Orientar(Image imagem)
        {
            const int tagOrientacao = 0x0112;
            if (Array.IndexOf(imagem.PropertyIdList, tagOrientacao) < 0) return;
            byte[] valor = imagem.GetPropertyItem(tagOrientacao).Value;
            if (valor.Length >= 2)
                imagem.RotateFlip(RotacaoExif(BitConverter.ToUInt16(valor, 0)));
        }

        void AjustarManualmente()
        {
            string caminho = ArquivoAtual();
            if (string.IsNullOrEmpty(caminho) || !File.Exists(caminho)) return;
            try
            {
                using (Image arquivo = Image.FromFile(caminho))
                {
                    Orientar(arquivo);
                    using (Bitmap imagem = new Bitmap(arquivo))
                    using (EditorPosicao editor = new EditorPosicao(imagem,
                        cfg.PosicaoDe(caminho) ?? new PointF(0.5f, 0.5f)))
                    {
                        if (editor.ShowDialog() != DialogResult.OK) return;
                        cfg.GuardarPosicao(caminho, editor.Posicao);
                    }
                }
                cfg.Ajuste = Ajuste.Preencher;
                cfg.Salvar();
                Aplicar();
                Atualizar();
            }
            catch (Exception e) { Avisar("(erro no ajuste manual: " + e.Message + ")"); }
        }

        void Avisar(string mensagem)
        {
            if (linhaPrincipal == null) return;
            linhaPrincipal.Post(delegate { erroUltimo = mensagem; Atualizar(); }, null);
        }

        void AlternarPausa()
        {
            cfg.Pausado = !cfg.Pausado;
            if (!teste) cfg.Salvar();
            Atualizar();
        }

        void DefinirOrdem(bool aleatorio)
        {
            if (cfg.Aleatorio == aleatorio) return;
            cfg.Aleatorio = aleatorio;
            cfg.OrdemAleatoria.Clear();
            cfg.ExibidasNaRodada.Clear();
            cfg.MemoriaExibicao = false;
            if (!teste) cfg.Salvar();
            Varrer();   // reordena mantendo a imagem atual como ponto de partida
            Atualizar();
        }

        void AbrirLocal()
        {
            string caminho = ArquivoAtual();
            if (string.IsNullOrEmpty(caminho) || !File.Exists(caminho)) return;
            // /select abre a pasta com o arquivo atual ja selecionado.
            Process.Start("explorer.exe", "/select,\"" + caminho + "\"");
        }

        void EscolherPasta(object remetente, EventArgs e)
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "Pasta com as imagens do papel de parede";
                d.ShowNewFolderButton = false;
                if (Directory.Exists(cfg.Pasta)) d.SelectedPath = cfg.Pasta;
                if (d.ShowDialog() != DialogResult.OK) return;
                UsarPasta(d.SelectedPath);
            }
        }

        void MontarPastasRecentes()
        {
            menuPastasRecentes.DropDownItems.Clear();
            if (cfg.PastasRecentes.Count == 0)
            {
                ToolStripMenuItem vazio = new ToolStripMenuItem("(nenhuma pasta lembrada)");
                vazio.Enabled = false;
                menuPastasRecentes.DropDownItems.Add(vazio);
                return;
            }
            foreach (string pastaSalva in cfg.PastasRecentes)
            {
                string pasta = pastaSalva;
                bool existe = Directory.Exists(pasta);
                ToolStripMenuItem item = Item(existe ? pasta : pasta + " (não encontrada)",
                    delegate { UsarPasta(pasta); });
                item.ToolTipText = pasta;
                item.Enabled = existe;
                item.Checked = string.Equals(cfg.Pasta, pasta,
                    StringComparison.OrdinalIgnoreCase);
                menuPastasRecentes.DropDownItems.Add(item);
            }
        }

        void UsarPasta(string pasta)
        {
            if (!Directory.Exists(pasta)) return;
            cfg.Pasta = pasta;
            cfg.LembrarPasta(pasta);
            cfg.Atual = "";
            cfg.OrdemAleatoria.Clear();
            cfg.ExibidasNaRodada.Clear();
            cfg.MemoriaExibicao = false;
            cfg.Salvar();
            pos = -1;
            Varrer();
            Aplicar();
        }

        void Sair()
        {
            relogio.Stop();
            sorte.Dispose();
            icone.Visible = false;
            icone.Dispose();
            ExitThread();
        }

        // ---------- autoteste ----------

        // Exercita a logica de verdade (varredura, ordem, proximo, anterior,
        // volta do ciclo, pausa, abrir local) sem tocar no papel de parede.
        public string Autoteste()
        {
            StringBuilder r = new StringBuilder();
            int falhas = 0;
            Action<bool, string> checar = delegate(bool ok, string oque)
            {
                if (!ok) falhas++;
                r.AppendLine((ok ? "  ok   " : "  FALHA") + "  " + oque);
            };

            r.AppendLine("Pasta: " + cfg.Pasta);
            cfg.Aleatorio = false;
            Varrer();
            int total = ordem.Count;
            r.AppendLine("Imagens encontradas: " + total);
            checar(total > 1, "a pasta tem mais de uma imagem");
            if (total < 2) { r.AppendLine("Sem imagens suficientes para testar."); return r.ToString(); }

            // Sequencia: ordem natural, igual a do Explorer.
            bool ordenado = true;
            for (int i = 1; i < ordem.Count; i++)
                if (Nativo.StrCmpLogicalW(ordem[i - 1], ordem[i]) > 0) ordenado = false;
            checar(ordenado, "modo sequencia esta em ordem natural (9 antes de 10)");

            // Proximo e anterior.
            pos = 0;
            string primeiro = ArquivoAtual();
            Trocar(+1);
            string segundo = ArquivoAtual();
            checar(pos == 1 && segundo != primeiro, "Proximo avanca uma imagem");
            Trocar(-1);
            checar(pos == 0 && ArquivoAtual() == primeiro, "Anterior volta para a imagem de antes");

            // Volta do ciclo nas duas pontas.
            pos = ordem.Count - 1;
            Trocar(+1);
            checar(pos == 0, "depois da ultima, Proximo volta para a primeira");
            pos = 0;
            Trocar(-1);
            checar(pos == ordem.Count - 1, "antes da primeira, Anterior vai para a ultima");

            // Aleatorio: o embaralhado nao perde nem repete imagem.
            DefinirOrdem(true);
            checar(ordem.Count == total, "modo aleatorio mantem a mesma quantidade de imagens");
            Dictionary<string, bool> vistos = new Dictionary<string, bool>();
            bool repetiu = false;
            foreach (string p in ordem)
            {
                if (vistos.ContainsKey(p)) repetiu = true;
                vistos[p] = true;
            }
            checar(!repetiu, "modo aleatorio nao repete imagem dentro do ciclo");
            bool mexeu = false;
            List<string> sequencial = new List<string>(ordem);
            sequencial.Sort(new OrdemNatural());
            for (int i = 0; i < ordem.Count; i++)
                if (ordem[i] != sequencial[i]) mexeu = true;
            checar(mexeu, "modo aleatorio realmente embaralha");

            // Uma rodada usa todas as imagens uma vez e sobrevive a uma nova
            // varredura (o equivalente a reiniciar o aplicativo).
            pos = 0;
            Dictionary<string, bool> rodada = new Dictionary<string, bool>(
                StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < total; i++)
            {
                string p = ArquivoAtual();
                if (p != null) rodada[p] = true;
                if (i + 1 < total) Trocar(+1);
            }
            checar(rodada.Count == total,
                   "modo aleatorio usa todas as imagens antes de repetir");
            checar(cfg.ExibidasNaRodada.Count == total,
                   "a rodada conta apenas imagens aplicadas com sucesso");
            string ultimaDaRodada = ArquivoAtual();
            Trocar(+1);
            checar(ArquivoAtual() != ultimaDaRodada,
                   "nova rodada nao repete imediatamente a ultima imagem");
            string atualSalvo = ArquivoAtual();
            cfg.Atual = atualSalvo;
            int exibidasSalvas = cfg.ExibidasNaRodada.Count;
            Varrer();
            HashSet<string> jaExibidas = new HashSet<string>(
                cfg.ExibidasNaRodada, StringComparer.OrdinalIgnoreCase);
            bool retomou = ArquivoAtual() == atualSalvo &&
                ordem.Count == 1 + total - exibidasSalvas;
            for (int i = 1; retomou && i < ordem.Count; i++)
                retomou = !jaExibidas.Contains(ordem[i]);
            checar(retomou,
                   "reabrir retoma apenas as imagens ainda nao exibidas na rodada");

            string identidadeA = Path.GetTempFileName();
            string identidadeB = Path.GetTempFileName();
            try
            {
                File.WriteAllText(identidadeA, "mesma imagem", Encoding.UTF8);
                File.Copy(identidadeA, identidadeB, true);
                Config memoriaImagem = new Config();
                PointF posicaoImagem = new PointF(0.25f, 0.75f);
                memoriaImagem.GuardarPosicao(identidadeA, posicaoImagem);
                PointF? recuperada = memoriaImagem.PosicaoDe(identidadeB);
                checar(recuperada.HasValue && recuperada.Value == posicaoImagem,
                       "posicao manual acompanha a identidade da imagem mesmo em outro caminho");
            }
            finally
            {
                try { File.Delete(identidadeA); } catch { }
                try { File.Delete(identidadeB); } catch { }
            }

            // Pausa para e retoma o relogio.
            cfg.Pausado = false; Atualizar();
            bool corria = relogio.Enabled;
            AlternarPausa();
            checar(corria && !relogio.Enabled, "Pausar para a troca automatica");
            AlternarPausa();
            checar(relogio.Enabled, "Retomar volta a trocar sozinho");
            checar(relogio.Interval == Math.Max(5, cfg.Intervalo) * 1000,
                   "o intervalo do relogio bate com o escolhido");

            // Abrir local da imagem: o arquivo atual existe mesmo.
            string atual = ArquivoAtual();
            checar(!string.IsNullOrEmpty(atual) && File.Exists(atual),
                   "o arquivo atual existe em disco (Abrir local funciona)");

            // Quantas imagens o menu de extensoes aceita.
            checar(Suportado("x.JPG") && Suportado("x.png") && !Suportado("x.txt"),
                   "filtro de extensao aceita imagem e recusa o resto");
            Config memoriaPastas = new Config();
            memoriaPastas.LembrarPasta(@"C:\Imagens\A");
            memoriaPastas.LembrarPasta(@"C:\Imagens\B");
            memoriaPastas.LembrarPasta(@"c:\imagens\a");
            checar(memoriaPastas.PastasRecentes.Count == 2 &&
                   memoriaPastas.PastasRecentes[0] == @"c:\imagens\a" &&
                   memoriaPastas.PastasRecentes[1] == @"C:\Imagens\B",
                   "memoria de pastas evita duplicatas e deixa a mais recente primeiro");
            checar(RotacaoExif(1) == RotateFlipType.RotateNoneFlipNone &&
                   RotacaoExif(6) == RotateFlipType.Rotate90FlipNone &&
                   RotacaoExif(8) == RotateFlipType.Rotate270FlipNone,
                   "orientacao EXIF vira a rotacao correta");
            checar(Ajuste.Centralizar == 0 && Ajuste.Esticar == 2 &&
                   Ajuste.Ajustar == 6 && Ajuste.Preencher == 10,
                   "os quatro ajustes usam exatamente os modos nativos do Windows");
            Rectangle centro = Enquadramento.Preencher(
                new Size(3732, 2524), new Size(1824, 982), new PointF(0.5f, 0.5f));
            Rectangle padrao = Enquadramento.Preencher(
                new Size(3732, 2524), new Size(1824, 982), null);
            Rectangle topo = Enquadramento.Preencher(
                new Size(3732, 2524), new Size(1824, 982), new PointF(0f, 0f));
            Rectangle fundo = Enquadramento.Preencher(
                new Size(3732, 2524), new Size(1824, 982), new PointF(1f, 1f));
            checar(padrao == centro && centro.Width == 1824 && centro.X == 0 && centro.Y < 0 &&
                   topo.Y == 0 && fundo.Bottom == 982,
                   "Preencher usa o mesmo centro inicial do editor e alcanca as bordas");
            using (Bitmap fonteDpi = new Bitmap(12, 12))
            using (Bitmap copiaDpi = new Bitmap(12, 12))
            {
                fonteDpi.SetResolution(72f, 72f);
                copiaDpi.SetResolution(96f, 96f);
                using (Graphics g = Graphics.FromImage(fonteDpi)) g.Clear(Color.Blue);
                fonteDpi.SetPixel(11, 11, Color.Red);
                using (Graphics g = Graphics.FromImage(copiaDpi)) CopiarPixels(g, fonteDpi);
                checar(copiaDpi.GetPixel(11, 11).R > 200,
                       "a conversao copia pixels sem ampliar imagens de 72 DPI");
            }
            int trocaTeste = 0, reaplicarTeste = 0;
            bool primeiraTroca = ReservarTroca(ref trocaTeste, ref reaplicarTeste);
            bool segundaTroca = ReservarTroca(ref trocaTeste, ref reaplicarTeste);
            bool repetiuTroca = LiberarTroca(ref trocaTeste, ref reaplicarTeste);
            checar(primeiraTroca && !segundaTroca && repetiuTroca,
                   "o ultimo ajuste pedido durante uma troca nao e descartado");

            r.AppendLine(falhas == 0
                ? "RESULTADO: todos os testes passaram."
                : "RESULTADO: " + falhas + " falha(s).");
            return r.ToString();
        }

        // ---------- icone ----------

        static Icon CriarIcone()
        {
            // O .ico embutido tem dez tamanhos; pedindo o tamanho de icone
            // pequeno do sistema, o Windows escolhe o certo para a bandeja
            // nesta escala de tela (16 px a 100%, 20 px a 125%) em vez de
            // espremer o de 256.
            try
            {
                using (Stream fluxo = System.Reflection.Assembly
                           .GetExecutingAssembly()
                           .GetManifestResourceStream("Kwall.icone"))
                {
                    if (fluxo != null)
                        return new Icon(fluxo, SystemInformation.SmallIconSize);
                }
            }
            catch { }
            return IconeDesenhado();
        }

        // Reserva, caso o recurso nao esteja no exe.
        static Icon IconeDesenhado()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (SolidBrush moldura = new SolidBrush(Color.FromArgb(240, 240, 248)))
                        g.FillRectangle(moldura, 2, 5, 28, 22);
                    using (SolidBrush ceu = new SolidBrush(Color.FromArgb(58, 104, 183)))
                        g.FillRectangle(ceu, 4, 7, 24, 18);
                    using (SolidBrush sol = new SolidBrush(Color.FromArgb(250, 220, 130)))
                        g.FillEllipse(sol, 20, 9, 6, 6);
                    using (SolidBrush morro = new SolidBrush(Color.FromArgb(28, 52, 92)))
                        g.FillPolygon(morro, new Point[] {
                            new Point(4, 25), new Point(13, 12), new Point(22, 25) });
                }
                IntPtr h = bmp.GetHicon();
                try
                {
                    using (Icon temporario = Icon.FromHandle(h))
                        return (Icon)temporario.Clone();
                }
                finally { Nativo.DestroyIcon(h); }
            }
        }
    }

    static class Programa
    {
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0] == "--autoteste")
            {
                string relatorio = new Bandeja(true).Autoteste();
                Nativo.AttachConsole(-1);
                Console.Write(relatorio);
                string saida = Path.Combine(
                    Path.GetDirectoryName(Application.ExecutablePath), "autoteste.txt");
                try { File.WriteAllText(saida, relatorio, Encoding.UTF8); }
                catch { }
                return relatorio.Contains("FALHA") ? 2 : 0;
            }

            bool primeira;
            bool primeiraLegada;
            using (Mutex travaLegada = new Mutex(true, "KaelithWall_instancia_unica", out primeiraLegada))
            using (Mutex trava = new Mutex(true, "Kwall_instancia_unica", out primeira))
            {
                if (!primeira || !primeiraLegada) return 0;
                Application.Run(new Bandeja());
            }
            return 0;
        }
    }
}
