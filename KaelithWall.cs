// KaelithWall -- trocador de papel de parede leve.
//
// Principio de projeto: o app NUNCA decodifica, escala ou grava imagem.
// Ele so entrega o caminho do arquivo original ao Windows
// (SystemParametersInfo/SPI_SETDESKWALLPAPER). Quem decodifica e escala e o
// proprio Windows, no processo dele, com cache. E por isso que nao travar e
// de graca: o custo de CPU do KaelithWall e praticamente zero entre trocas.
//
// Compila com o csc.exe do .NET Framework 4 que ja vem no Windows -- sem SDK,
// sem pacote, sem dependencia. Ver compilar.sh.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KaelithWall
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

    sealed class Config
    {
        public string Pasta = "";
        public int Intervalo = 10;        // segundos
        public bool Aleatorio = true;
        public bool Pausado = false;
        public bool Subpastas = false;
        public int Ajuste = KaelithWall.Ajuste.Preencher;
        public string Atual = "";

        static string Caminho()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "KaelithWall");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "config.txt");
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
                    }
                }
            }
            catch { }
            if (c.Intervalo < 5) c.Intervalo = 5;
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
                File.WriteAllText(Caminho(), sb.ToString(), Encoding.UTF8);
            }
            catch { }
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
        readonly Random sorte = new Random();
        readonly bool teste;   // em autoteste nao mexe no papel de parede de verdade

        List<string> ordem = new List<string>();
        int pos = -1;
        int trocando;              // trava simples: 0 livre, 1 ocupado
        string erroUltimo = "";

        ToolStripMenuItem itemAtual, itemPausar, itemAleatorio, itemSequencia, itemSubpastas;
        ToolStripMenuItem menuIntervalo, menuAjuste;

        public Bandeja() : this(false) { }

        public Bandeja(bool modoTeste)
        {
            teste = modoTeste;
            linhaPrincipal = SynchronizationContext.Current;
            cfg = Config.Carregar();

            icone = new NotifyIcon();
            icone.Icon = CriarIcone();
            icone.Text = "KaelithWall";
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
            AddAjuste(Ajuste.Preencher, "Preencher (corta a sobra)");
            AddAjuste(Ajuste.Ajustar, "Ajustar (imagem inteira)");
            AddAjuste(Ajuste.Esticar, "Esticar");
            AddAjuste(Ajuste.Centralizar, "Centralizar");
            m.Items.Add(menuAjuste);

            m.Items.Add(Item("Escolher &pasta...", EscolherPasta));
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

            string dica = "KaelithWall" + (cfg.Pausado ? " (pausado)" : "");
            if (!string.IsNullOrEmpty(nome)) dica += "\n" + Path.GetFileName(nome);
            if (dica.Length > 127) dica = dica.Substring(0, 127);
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
                int j = sorte.Next(i + 1);
                string t = lista[i]; lista[i] = lista[j]; lista[j] = t;
            }
            // Nao repetir de cara a imagem que acabou de sair.
            if (lista.Count > 1 && evitarNoInicio != null && lista[0] == evitarNoInicio)
            {
                string t = lista[0]; lista[0] = lista[1]; lista[1] = t;
            }
        }

        void Varrer()
        {
            string atual = ArquivoAtual();
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
            if (cfg.Aleatorio) Embaralhar(achados, null);

            ordem = achados;
            pos = (atual != null) ? ordem.IndexOf(atual) : -1;
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
                    // Fim (ou inicio) do ciclo: revarre para pegar arquivos novos
                    // e, no modo aleatorio, embaralha de novo.
                    Varrer();
                    if (ordem.Count == 0) { Atualizar(); return; }
                    if (cfg.Aleatorio) Embaralhar(ordem, saindo);
                    pos = (passo > 0) ? 0 : ordem.Count - 1;
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

            cfg.Atual = caminho;
            if (teste) return;
            cfg.Salvar();

            // A troca sai da linha principal: num JPEG grande o Windows pode
            // levar algumas centenas de milissegundos recodificando, e o menu
            // da bandeja nao pode congelar junto.
            if (Interlocked.Exchange(ref trocando, 1) == 1) return;
            int ajuste = cfg.Ajuste;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { Definir(caminho, ajuste); }
                catch (Exception e) { Avisar("(erro ao aplicar: " + e.Message + ")"); }
                finally { Interlocked.Exchange(ref trocando, 0); }
            });
        }

        static void Definir(string caminho, int ajuste)
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                if (k != null)
                {
                    k.SetValue("WallpaperStyle", ajuste.ToString(), RegistryValueKind.String);
                    k.SetValue("TileWallpaper", "0", RegistryValueKind.String);
                }
            }
            Nativo.SystemParametersInfoW(
                Nativo.SPI_SETDESKWALLPAPER, 0, caminho,
                Nativo.SPIF_UPDATEINIFILE | Nativo.SPIF_SENDCHANGE);
        }

        void Avisar(string mensagem)
        {
            if (linhaPrincipal == null) return;
            linhaPrincipal.Post(delegate { erroUltimo = mensagem; Atualizar(); }, null);
        }

        void AlternarPausa()
        {
            cfg.Pausado = !cfg.Pausado;
            cfg.Salvar();
            Atualizar();
        }

        void DefinirOrdem(bool aleatorio)
        {
            if (cfg.Aleatorio == aleatorio) return;
            cfg.Aleatorio = aleatorio;
            cfg.Salvar();
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
                cfg.Pasta = d.SelectedPath;
                cfg.Atual = "";
                cfg.Salvar();
                pos = -1;
                Varrer();
                Aplicar();
            }
        }

        void Sair()
        {
            relogio.Stop();
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
                           .GetManifestResourceStream("KaelithWall.icone"))
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
            using (Mutex trava = new Mutex(true, "KaelithWall_instancia_unica", out primeira))
            {
                if (!primeira) return 0;
                Application.Run(new Bandeja());
            }
            return 0;
        }
    }
}
