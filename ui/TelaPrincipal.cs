using conta_azul;
using CriadorDePastas;
using CriadorDePastas.trello;
using file.io;
using FileIO;
using Microsoft.VisualBasic;
using Microsoft.Web.WebView2.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using NovoServico;
using NovoServico.outros;
using outros;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using trello;
using static System.Collections.Specialized.BitVector32;
using static System.Formats.Asn1.AsnWriter;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;

namespace ui
{
    /// <summary>
    /// Lógica interna para TelaPrincipal.xaml
    /// </summary>
    public partial class TelaPrincipal : Window
    {

        public ListaDeServicos? listaDeServicos;
        private ArquivosDoServico? arquivosDoServico;
        public TelaPrincipal()
        {
            new WindowStateSaveHelper(this);
            InitializeComponent();
            testarCaminhoDoServidor();
            testarCaminhoDosTemplates();
            inicializarWebViewTrello();
            exibirCaminhos();
            verificarApis();
            verificarValidadeDoApp();

        }


        private void testarCaminhoDoServidor()
        {
            try
            {
                Directory.GetDirectories(new Pastas().lerCaminhoDoServidor()!);
            }
            catch (Exception e)
            {
                //https://wpf-tutorial.com/dialogs/the-messagebox/
                SystemSounds.Hand.Play();
                MessageBoxResult result = MessageBox.Show("Caminho para o servidor não foi definido ou é inválido.\nO app não vai funcionar sem essa informação.\n\nDeseja definir o caminho agora?", "Caminho p/ servidor indefinido", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        mudarCaminhoDoServidor(null, null);
                        testarCaminhoDoServidor();
                        break;
                                  
                }

                
            }
        }

        private void testarCaminhoDosTemplates()
        {
            try
            {
                Directory.GetDirectories(new Pastas().lerCaminhoPraPastaDeTemplates()!);
            }
            catch (Exception e)
            {
                //https://wpf-tutorial.com/dialogs/the-messagebox/
                SystemSounds.Hand.Play();
                MessageBoxResult result = MessageBox.Show("Caminho para a pasta de templates não foi definido ou é inválido. Não crie serviços sem definir o caminho antes.\n\nDeseja definir o caminho agora?", "Caminho p/ templates indefinido", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        mudarCaminhoPraPastaDeTemplate(null, null);
                        testarCaminhoDosTemplates();
                        break;
                                  
                }

                
            }
        }

        private void verificarApis()
        {
            lblContaAzulStatus.Content = "Status Conta Azul: Verificando...";
            lblTrelloStatus.Content = "Status Trello: Verificando...";

            Async.runAsync(500, () =>
            {
                var status = new TrelloApi().verificarConexao();
                Async.runOnUI(() => { lblTrelloStatus.Content = status; });

                new ContaAzulManager().verificarConexao((String? status) =>
                {
                    Async.runOnUI(() => { lblContaAzulStatus.Content = "Status Conta Azul: " + status; });
                });
            });
        }

        private void verificarValidadeDoApp()
        {
            var lancamento = new DateTime(2023, 1, 1, 9, 00, 00, DateTimeKind.Local);
            var vencimento = lancamento.AddYears(1);
            var hoje = DateTime.Now;

            if (hoje.Ticks > vencimento.Ticks)
            {
                UiUtils.erroMsg(this.GetType().Name,"Manifesto expirou");
                Close();

            }
        }

        //------------------------------------------------------------- menu

        private void mudarIdDaLista(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Insira a cartaoId da nova lista onde os cartões de serviço serão criados", "Atualizar lista", Preferencias.inst().getString(Preferencias.trelloListaIdLayout)!);
            if (input != null && input.Length > 0) Preferencias.inst().save(Preferencias.trelloListaIdLayout, input);
        
        }

        private void mudarEtiquetas(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Insira as ids das etiquetas separadas por vírgula.\nTodos os cartões serão criados com as etiquetas definidas aqui.", "Atualizar etiquetas padrão", Preferencias.inst().getString(Preferencias.trelloDefLabels)!);
            if (input != null && input.Length > 0) Preferencias.inst().save(Preferencias.trelloDefLabels, input);
        }

        private void mudarDescricaoItem(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Digite o novo template de descrição de itens ", "Template de descrição de itens do Trello", Preferencias.inst().getString(Preferencias.trelloDescTemplate)!);
            if (input != null && input.Length > 0) Preferencias.inst().save(Preferencias.trelloDescTemplate, input);
        }

        private void mudarLinhaDeIdDoServicoNoContaAzul(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Digite a nova linha de identificação do Conta Azul para a descrição no Trello ", "Identificação do Conta Azul p/ descrição de itens do Trello", Preferencias.inst().getString(Preferencias.trelloTemplateIdContaAzul)!);
            if (input != null && input.Length > 0) Preferencias.inst().save(Preferencias.trelloTemplateIdContaAzul, input);

        }

        private void mudarCaminhoDoServidor(object? sender, RoutedEventArgs? e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Preferencias.inst().save(Preferencias.caminhoServidor, dialog.FileName);
                exibirCaminhos();
            }
        }

        private void mudarCaminhoPraPastaDeTemplate(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Preferencias.inst().save(Preferencias.caminhoTemplates, dialog.FileName);
                exibirCaminhos();
            }
        }

        private void autenticarTrello(object sender, RoutedEventArgs e)
        {

            var trelloAuth = new TrelloAuth();
            trelloAuth.Owner = this;
            trelloAuth.Show();
            trelloAuth.autenticar(() => { trelloAuth.Close(); verificarApis(); });
        }

        private void autenticarContaAzul(object sender, RoutedEventArgs e)
        {


            var mContaAzulAuth = new ContaAzulAuth();
            mContaAzulAuth.Show();
            mContaAzulAuth.Owner = this;
            mContaAzulAuth.autenticar((String? erro) =>
            {
                mContaAzulAuth.Close();
                if (erro != null) UiUtils.erroMsg(this.GetType().Name,erro);
                else verificarApis();
            });


        }

        private void verificarStatusApis(object sender, RoutedEventArgs e) => verificarApis();

        private void novoServicoComOs(object sender, RoutedEventArgs e)
        {
            var novoServico = new NovoServicoComOS(this);
            novoServico.Owner = this;
            novoServico.Show();
        }

        private void novoServicoSemOs(object sender, RoutedEventArgs e)
        {
            var novoServico = new NovoServicoSemOs(this);
            novoServico.Owner = this;
            novoServico.Show();

        }

        private void pesquisarServicos(object sender, RoutedEventArgs e)
        {

            if (listaDeServicos != null)
            {
                listaDeServicos.Close();
                listaDeServicos = null;
            }

            listaDeServicos = new ListaDeServicos(this);
            // listaDeServicos.Owner = this;
            listaDeServicos.Show();

            //chamar a partir do construtor
            TaskbarManager.Instance.SetApplicationIdForSpecificWindow(new WindowInteropHelper(listaDeServicos).Handle, "NovoServico_ListaDeServicos");

        }


        //------------------------------------------------------------- menu


        /// <summary>
        /// Mostra pro usuario o caminho da pasta de templates e do servidor 
        /// atualizando os campos de texto na tela
        /// </summary>
        private void exibirCaminhos()
        {
            Async.runAsync(1000, () =>
            {
                string? path = new Pastas().lerCaminhoDoServidor();
                string? path2 = new Pastas().lerCaminhoPraPastaDeTemplates();
                Async.runOnUI(() =>
                {
                    btnServerPath.Content = path != null ? "Servidor: " + path : "Defina o caminho p/ servidor";
                    btntemplatePath.Content = path2 != null ? "Templates: " + path2 : "Defina o caminho p/ templates";

                });
            });


        }

        public void inicializarWebViewTrello()
        {

            // detecta quando o webview esta pronto pra ser usado
            webViewTrello.CoreWebView2InitializationCompleted += (object? sender, CoreWebView2InitializationCompletedEventArgs e) =>
            {
                webViewTrello.CoreWebView2.HistoryChanged += (object? sender, object e) =>
                {

                    var url = Uri.UnescapeDataString(webViewTrello.Source.AbsoluteUri);
                    Debug.WriteLine(":: " + url);
                    if (url.StartsWith("https://trello.com/c")) // cartaoPrincipal selecionado
                    {
                        var numServicoTrello = new Regex(@"[serviço]{7}[-]([0-9]+)[-]").Match(url).Groups[1].ToString();
                        arquivosDoServico?.Close();
                        arquivosDoServico = new ArquivosDoServico(this, numServicoTrello);
                        arquivosDoServico.Owner = this;
                        arquivosDoServico.Show();
                        TaskbarManager.Instance.SetApplicationIdForSpecificWindow(new WindowInteropHelper(arquivosDoServico).Handle, "NovoServico_ArquivosDoServico");

                    }
                    else if (url.StartsWith("https://trello.com/b")) // nenhum cartaoPrincipal selecionado
                    {
                        arquivosDoServico?.Close();
                        arquivosDoServico = null;
                    }

                };

                webViewTrello.CoreWebView2.NavigationCompleted += (object? sender, CoreWebView2NavigationCompletedEventArgs evt) =>
               {
                   Async.runOnUI(500, () => { webViewTrello.Visibility = Visibility.Visible; });
               };


            };
        }

        private void forcarFechamentoDoApp(object sender, EventArgs e)
        {
            webViewTrello.Dispose();
            webViewContaAzul.Dispose();
            App.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        public void alternarSite_Click(object? sender, RoutedEventArgs? e)
        {
            if (webViewTrello.Visibility == Visibility.Visible)
            {
                alternarSite.Header = "Ir ao Trello";
                webViewTrello.Visibility = Visibility.Hidden;
                webViewContaAzul.Visibility = Visibility.Visible;
                if (arquivosDoServico != null) arquivosDoServico.WindowState = WindowState.Minimized;

            }
            else
            {
                alternarSite.Header = "Ir ao Conta Azul";
                webViewTrello.Visibility = Visibility.Visible;
                webViewContaAzul.Visibility = Visibility.Hidden;
                if (arquivosDoServico != null) arquivosDoServico.WindowState = WindowState.Normal;
            }


        }

        internal void carregarOs(string url)
        {
            // resolver conflitos com visibilidade
            webViewContaAzul.Source = new Uri(url);
            webViewContaAzul.Visibility = Visibility.Visible;
            webViewTrello.Visibility = Visibility.Hidden;
        }

    }
}
