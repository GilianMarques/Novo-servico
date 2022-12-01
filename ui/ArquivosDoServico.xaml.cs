using CriadorDePastas.trello;
using FileIO;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ui
{
    /// <summary>
    /// Lógica interna para ArquivosDoServico.xaml
    /// </summary>
    public partial class ArquivosDoServico : Window
    {
        private readonly TelaPrincipal telaPrincipal;
        private readonly string numServicoTrello;
        private Uri? caminhoDoServicoAtual;
        private ControleServico mControleServico;

        public ArquivosDoServico(TelaPrincipal telaPrincipal, string numServicoTrello)
        {

            if (numServicoTrello.Length == 0)
            {
                this.Close();
                return;
            }

            new WindowStateSaveHelper(this);

            InitializeComponent();


            this.telaPrincipal = telaPrincipal;
            this.numServicoTrello = numServicoTrello;
            carregarArquivosDoServico(numServicoTrello);

        }

        private void carregarArquivosDoServico(string? numServicoTrello)
        {

            Async.runAsync(() =>
            {
                var servicos = new Pastas().lerServicos(numServicoTrello, false);
                if (servicos.Count == 0) servicos = new Pastas().lerServicos(numServicoTrello, true);
                if (servicos.Count == 0) Async.runOnUI(() =>
                    {
                        UiUtils.erroNot($"Nenhum serviço encontrado com o número ({numServicoTrello})");
                        this.Close();
                    });

                else Async.runOnUI(() =>
                {
                    if (servicos.Count > 1) UiUtils.erroNot($"Mais de um serviço com o mesmo número encontrado ({numServicoTrello})");
                    else if (servicos.Count == 1)
                    {
                        /*
                         por algum motivo, quando a pasta do serviço esta dentro de serviços feitos, o webbrowser nao consegue abrir 
                         parece que é um problema com a quantidade de '//' (barras) no caminho, a solução foi ajustar o path manualmente
                         */

                        var array = String.Join("\\", servicos[0].Split("\\", StringSplitOptions.RemoveEmptyEntries).ToList());
                        caminhoDoServicoAtual = new Uri(array);
                        Debug.WriteLine($":: {caminhoDoServicoAtual.OriginalString}");
                        wbArquivos.Source = caminhoDoServicoAtual;
                        mControleServico = new ControleServico(telaPrincipal, caminhoDoServicoAtual!.LocalPath);

                    }
                });
            });


        }

        private void navegarArquivos(object sender, RoutedEventArgs e)
        {

            var name = ((Button)(sender)).Name.ToString();

            if (btnFileAvancar.Name == name && wbArquivos.CanGoForward) wbArquivos.GoForward();
            if (btnFileHome.Name == name && caminhoDoServicoAtual != null) wbArquivos.Source = caminhoDoServicoAtual;
            if (btnFileVoltar.Name == name && wbArquivos.CanGoBack) wbArquivos.GoBack();

        }

        private void abrirPasta(object sender, RoutedEventArgs e)
        {

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = caminhoDoServicoAtual?.LocalPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception) { UiUtils.erroNot(String.Format("Não foi possível abrir {0}", caminhoDoServicoAtual?.LocalPath)); }
        }

        //----------------------------------------> menu

        private void anexarOs(object sender, RoutedEventArgs e) => mControleServico.anexarOs(null, null);

        private void renomearServico(object sender, RoutedEventArgs e) => mControleServico.renomearServico(null, null);

        private void abrirOs(object sender, RoutedEventArgs e) => mControleServico.abrirOs(null, null);

        private void clonarServico(object sender, RoutedEventArgs e) => mControleServico.clonarServico(null, null);

        private void CriarCartaoParcial(object sender, RoutedEventArgs e) => mControleServico.CriarCartaoParcial(null, null);

        private void copiarCaminho(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(caminhoDoServicoAtual?.LocalPath.ToString());
            UiUtils.notificarSemSom("Caminho copiado para a área de transferência");
        }
    }
}

