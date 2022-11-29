using CriadorDePastas.trello;
using FileIO;
using MaterialDesignThemes.Wpf;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel.Activation;
using Path = System.IO.Path;
using TrelloCard = trello.modelos.TrelloCard;

namespace ui
{
    /// <summary>
    /// Interação lógica para ControleServico.xam
    /// </summary>
    public partial class ControleServico : UserControl
    {
        private readonly TelaPrincipal telaPrincipal;
        private string caminhoServico;
        private string nomeServico;
        private int numeroServico;
        private Func<ControleServico, int> cliqueCallback;

        public ControleServico(TelaPrincipal listaDeServicos, string caminhoServico)
        {
            InitializeComponent();
            this.telaPrincipal = listaDeServicos;
            this.caminhoServico = caminhoServico;
            nomeServico = System.IO.Path.GetFileName(caminhoServico);
            numeroServico = new Pastas().obterNumero(nomeServico);
        }

        private void abrirLayout(object sender, MouseButtonEventArgs e)
        {
            
            e.Handled = true;

            var arquivos = Directory.GetFiles(caminhoServico);
            List<String> correspondencias = new List<String>();

            foreach (var arquivo in arquivos)
            {
                String nomeArquivo = Path.GetFileNameWithoutExtension(arquivo);

                if (Path.GetExtension(arquivo).ToLower() == ".cdr"
                && nomeArquivo.ToUpper().StartsWith("LAYOUT")
                && nomeArquivo.Contains(numeroServico.ToString()))
                    correspondencias.Add(arquivo);
            }

            if (correspondencias.Count == 0)
            {
                UiUtils.erroNot("Não existe um arquivo de layout padrão para esse serviço. Localize manualmente");
                abrirPastaOuArquivo(caminhoServico);
            }
            else if (correspondencias.Count > 1)
            {
                UiUtils.erroNot("Existe mais que um arquivo de layout para este serviço. Escolha manualmente.");
                abrirPastaOuArquivo(caminhoServico);
            }
            else abrirPastaOuArquivo(correspondencias[0]);

            cliqueCallback(this);
        }

        private void abrirCorte(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var arquivos = Directory.GetFiles(caminhoServico);
            List<String> correspondencias = new List<String>();

            foreach (var arquivo in arquivos)
            {
                String nomeArquivo = Path.GetFileNameWithoutExtension(arquivo);

                if (Path.GetExtension(arquivo).ToLower() == ".cdr"
                && nomeArquivo.ToUpper().StartsWith("CORTE")
                && nomeArquivo.Contains(numeroServico.ToString()))
                    correspondencias.Add(arquivo);
            }

            if (correspondencias.Count == 0)
            {
                UiUtils.erroNot("Não existe um arquivo de corte padrão para esse serviço. Localize manualmente");
                abrirPastaOuArquivo(caminhoServico);
            }
            else if (correspondencias.Count > 1)
            {
                UiUtils.erroNot("Existe mais que um arquivo de corte para este serviço. Escolha manualmente.");
                abrirPastaOuArquivo(caminhoServico);
            }
            else abrirPastaOuArquivo(correspondencias[0]);
            cliqueCallback(this);
        }

        private void abrirImpressao(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var arquivos = Directory.GetFiles(caminhoServico);
            List<String> correspondencias = new List<String>();

            foreach (var arquivo in arquivos)
            {
                String nomeArquivo = Path.GetFileNameWithoutExtension(arquivo);

                if (Path.GetExtension(arquivo).ToLower() == ".cdr"
                && nomeArquivo.ToUpper().StartsWith("IMPRESSÃO") || nomeArquivo.ToUpper().StartsWith("IMPRESSAO")
                && nomeArquivo.Contains(numeroServico.ToString()))
                    correspondencias.Add(arquivo);
            }

            if (correspondencias.Count == 0)
            {
                UiUtils.erroNot("Não existe um arquivo de impressão padrão para esse serviço. Localize manualmente");
                abrirPastaOuArquivo(caminhoServico);
            }
            else if (correspondencias.Count > 1)
            {
                UiUtils.erroNot("Existe mais que um arquivo de impressão para este serviço. Escolha manualmente.");
                abrirPastaOuArquivo(caminhoServico);
            }
            else abrirPastaOuArquivo(correspondencias[0]);
            cliqueCallback(this);
        }

        private void abrirPasta(object sender, MouseButtonEventArgs e)
        {
            abrirPastaOuArquivo(caminhoServico);
        }

        private void abrirPastaOuArquivo(String caminho)
        {

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = caminho,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception) { UiUtils.erroNot(String.Format("Não foi possível abrir {0}", caminho)); }
            cliqueCallback(this);
        }

        public void anexarOs(object sender, RoutedEventArgs e)
        {
            var nexarOS = new AnexarOS(caminhoServico);
            nexarOS.Owner = telaPrincipal;
            nexarOS.Show();
        }

        public void renomearServico(object sender, RoutedEventArgs e)
        {
            void atualizarLista() => telaPrincipal.listaDeServicos?.carregarServicos();

            var renomearServico = new RenomearServico(caminhoServico, nomeServico, atualizarLista);
            renomearServico.Owner = telaPrincipal;
            renomearServico.Show();

        }

        public void abrirCartao(object sender, RoutedEventArgs e)
        {
            Async.runAsync(() =>
            {
                new TrelloApi().obterCartaoPorNome(nomeServico, (String? erro, TrelloCard? cartao) =>
                {

                    Async.runOnUI(() =>
                    {
                        if (erro != null) UiUtils.erroNot(erro);
                        else if (cartao != null)
                        {
                            telaPrincipal.webViewTrello.Source = cartao.Url;
                            if (telaPrincipal.webViewTrello.Visibility != Visibility.Visible) telaPrincipal.alternarSite_Click(null, null);

                        }
                        else UiUtils.erroNot("Cartão não existe");
                    });
                });
            });
        }

        public void abrirOs(object sender, RoutedEventArgs e)
        {
            Async.runAsync(() =>
            {
                new TrelloApi().obterCartaoPorNome(nomeServico, (String? erro, TrelloCard? cartao) =>
                {
                    if (erro != null) UiUtils.erroNot("Erro buscando pelo cartão para abrir OS:" + erro);
                    else if (cartao != null)
                    {
                        baixarOs(cartao);
                    }
                    else UiUtils.erroNot("Cartão não existe. Sem um cartão não é possível encontrar a OS.");

                });
            });
        }

        public void baixarOs(TrelloCard cartao)
        {
            foreach (var item in cartao.Desc.Split("\n"))
            {
                if (item.Contains("Identificação no Conta Azul N°"))
                    if (item.Contains("https://app.contaazul.com/#/vendas-e-orcamentos/visualizar-orcamento/"))
                    {
                        String url = item.Split("(")[1].Split(")")[0];
                        Async.runOnUI(() => { telaPrincipal.carregarOs(url); });
                    }
                    else
                    {
                        Debug.WriteLine(item);
                    }

            }
        }

        public void clonarServico(object sender, RoutedEventArgs e)
        {

            void atualizarLista() => telaPrincipal.listaDeServicos?.carregarServicos();

            var clonarServico = new ClonarServico(telaPrincipal, caminhoServico, nomeServico, atualizarLista);
            clonarServico.Owner = telaPrincipal;
            clonarServico.Show();
        }

        public void CriarCartaoParcial(object sender, RoutedEventArgs e)
        {
            var criarParcial = new CriarParcial(nomeServico);
            criarParcial.Owner = telaPrincipal;
            criarParcial.Show();

        }

        internal void definirCallbackDeClique(Func<ControleServico, int> callback)
        {
           this.cliqueCallback = callback;
        }
    }
}

